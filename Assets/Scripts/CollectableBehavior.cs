using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.XR.Interaction.Toolkit;

// Change scorekeeping multiplier
// Record video
// Weekly report

/// <summary>
/// This class handles that collision behaviors of collectables and deterrents.
/// </summary>
public class CollectableBehavior : MonoBehaviourPunCallbacks
{
    //public GameObject GreenParticleGameObject;
    private MeshRenderer _basket = null;
    private MeshRenderer _rim = null;
    private NetworkVariablesAndReferences networkVar;

    private AudioManager _audioManager;
	private GameplayManager _gameplayManager;

    /// <summary>
    /// Reference to the default material of the basket.
    /// </summary>
    public Material defaultBasketMaterial;
    /// <summary>
    /// Reference to the default material of the rim of the basket.
    /// </summary>
    public Material defaultRimMaterial;
    /// <summary>
    /// Reference to the success material of the basket.
    /// </summary>
    public Material successBasketMaterial;
    /// <summary>
    /// Reference to the failure material of the basket.
    /// </summary>
    public Material failureBasketMaterial;
    /// <summary>
    /// Reference to the success material of the basket rim.
    /// </summary>
    public Material successRimMaterial;
    /// <summary>
    /// Reference to the failure material of the basket rim.
    /// </summary>
    public Material failureRimMaterial;
    /// <summary>
    /// Default value as to how long after the basket's material is changed to failure/success before it's changed back.
    /// </summary>
    public float timeBeforeResetMaterial = 0.3f;
    /// <summary>
    /// Variable that holds the index of the current player.
    /// 0 for MasterClient.
    /// 1 for remote client.
    /// </summary>
    public int playerIndex = 0;
    private static float timePassed = 0;

    // keep track of whether the object has collided with the basket
    // this is to prevent collision with outter colliders after colliding with inner colliders
    private bool collided = false;
    private static XRDirectInteractor[] handInteractors;

    // Start is called before the first frame update
    void Start()
    {
        networkVar = GameObject.Find("Network Interaction Statuses").GetComponent<NetworkVariablesAndReferences>();
        _audioManager = GameObject.Find("SoundManager").GetComponent<AudioManager>();
		_gameplayManager = GameObject.Find("GameplayManager").GetComponent<GameplayManager>();
        if (handInteractors == null || handInteractors.Length < 2)
        {
            handInteractors = FindObjectsOfType<XRDirectInteractor>();
        }

        // view id might now be available at this time
        // set by gameplay with some logic
        if (networkVar.basketIDs[playerIndex] > 0)
        {
            _basket = PhotonView.Find(networkVar.basketIDs[playerIndex]).transform.GetChild(0).GetComponent<MeshRenderer>();
            _rim = _basket.transform.GetChild(0).GetComponent<MeshRenderer>();
        }
        collided = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (_basket)
        {
            if (_basket.material != defaultBasketMaterial)
            {
                timePassed += Time.deltaTime;
                if (timePassed > timeBeforeResetMaterial)
                {
                    _basket.material = defaultBasketMaterial;
                    _rim.material = defaultRimMaterial;
                }
            }
        }
        else
        {
            // view id might now be available at this time
            if (networkVar.basketIDs[playerIndex] > 0)
            {
                _basket = PhotonView.Find(networkVar.basketIDs[playerIndex]).transform.GetChild(0).GetComponent<MeshRenderer>();
                _rim = _basket.transform.GetChild(0).GetComponent<MeshRenderer>();
            }
        }
        
	}

    private void OnTriggerEnter(Collider other)
    {
        if (!collided)
        {
            // do nothing if heart is collided with bomb or pumpkin
            if (other.gameObject.tag.Equals("Heart") || (gameObject.tag.Equals("Heart") && (other.gameObject.tag.Equals("Deterrent") || other.gameObject.tag.Equals("Collectable"))))
            {
                return;
            }
            collided = true;
            if (other.gameObject.tag.Equals("Deterrent") && gameObject.tag.Equals("Collectable"))
            {
                // Do nothing, just destroy right after
            }
            else if (other.gameObject.tag.Equals("Collectable") && gameObject.tag.Equals("Deterrent"))
            {
                GameObject explo = PhotonNetwork.Instantiate("ExplosionEffect", gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
                explo.SetActive(true);
                if (photonView.IsMine)
                {
                    _audioManager.PlayBombSound();
                }
            }
            else if (other.gameObject.tag.Equals("InnerBasket") && gameObject.tag.Equals("Collectable"))
            {
                _basket.material = successBasketMaterial;
                _rim.material = successRimMaterial;
                if (photonView.IsMine)
                {
                    timePassed = 0;
                    _audioManager.PlayCollectSound();
                    _gameplayManager.IncreaseScore();
                }
            }
            else if (other.gameObject.tag.Equals("InnerBasket") && gameObject.tag.Equals("Deterrent"))
            {
                GameObject explo = PhotonNetwork.Instantiate("ExplosionEffect", gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
                explo.SetActive(true);
                _basket.material = failureBasketMaterial;
                _rim.material = failureRimMaterial;
                if (photonView.IsMine)
                {
                    _audioManager.PlayBombSound();
                    foreach (XRDirectInteractor interactor in handInteractors)
                    {
                        interactor.xrController.SendHapticImpulse(0.7f, 1.3f);
                    }
                    _gameplayManager.DecreaseScore();
                }
            }
            else if (other.gameObject.tag.Equals("InnerBasket") && gameObject.tag.Equals("Heart"))
            {
                _basket.material = successBasketMaterial;
                _rim.material = successRimMaterial;
                if (photonView.IsMine)
                {
                    timePassed = 0;
                    // Change to play heart caught sound
                    _audioManager.PlayHeartSound();
                    _gameplayManager.IncreaseHeart();
                }
            }
            else // missed
            {
                if (gameObject.tag.Equals("Collectable"))
                {
                    _basket.material = failureBasketMaterial;
                    _rim.material = failureRimMaterial;
                    if (photonView.IsMine)
                    {
                        timePassed = 0;
                        _audioManager.PlayMissedSound();
                        _gameplayManager.DecreaseScore();
                    }
                }
            }
            // Master / Client destroy their own object
            if (photonView.AmOwner || photonView.IsMine)
            {
                PhotonNetwork.Destroy(this.gameObject);
            }
        }
    }

    /// <summary>
    /// Override parent method. This method destroys the collectable that corresponds to the leaving player.
    /// </summary>
    public override void OnPlayerLeftRoom(Player player)
    {
        base.OnPlayerLeftRoom(player);
        if(!player.IsMasterClient && playerIndex == 1)
        {
            PhotonNetwork.Destroy(this.gameObject);
        }
    }

}
