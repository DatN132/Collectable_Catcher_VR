using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using TMPro;
using System.Linq;


/// <summary>
/// Class that holds everything and anything networked. This class keeps variables that others can reference.
/// This class also sync variable changes across the network.
/// </summary>
public class NetworkVariablesAndReferences : NetworkBehaviour
{
    /// <summary>
    /// Keep reference of how many player have grabbed their basket. Start the game if the number
    /// of players garbbing is equals to the room capacity
    /// </summary>
    public int playerGrabbed = 0;

    /// <summary>
    /// Store the view ID of each network player prefab
    /// </summary>
    public NetworkId[] playerIDs = new NetworkId[2];

    /// <summary>
    /// Store the view ID of each network basket prefab
    /// </summary>
    public NetworkId[] basketIDs = new NetworkId[2];

    /// <summary>
    /// Store the view ID of each network shadow basket prefab
    /// </summary>
    public NetworkId[] shadowBasketIDs = new NetworkId[2];

    /// <summary>
    /// Store the view ID of each network gamescore/tombstone prefab
    /// </summary>
    public NetworkId[] tombstoneIDs = new NetworkId[2];

    /// <summary>
    /// Hold the reference to game over state. Set by gameplay manager
    /// </summary>
    public bool isGameOver = false;

    private int roomCapacity;
    private bool gameStarted = false;
    private Gameplay gameplay;
    private GameplayManager gameplayManager;
    private TextMeshProUGUI[] countDown;
    private int localPlayerIndex = 0;
    private int otherPlayerIndex = 1;

    private AudioManager _audioManager;

    void Awake()
    {
        gameStarted = false;
        isGameOver = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        roomCapacity = Runner.ActivePlayers.Count();
        gameplay = FindObjectOfType<Gameplay>();
        gameplayManager = FindObjectOfType<GameplayManager>();
        if (gameplayManager == null)
        {
            gameplayManager = GameObject.Find("GameplayManager").GetComponent<GameplayManager>();
        }
        isGameOver = false;
        
        if(Runner.IsSharedModeMasterClient && NetworkManager.isMultiplayer)
        {
            RPC_SyncIsMultiplayer(NetworkManager.isMultiplayer);
        }
        if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
		{
			localPlayerIndex = 0;
			otherPlayerIndex = 1;
		}
		else
		{
			localPlayerIndex = 1;
			otherPlayerIndex = 0;
		}
        countDown = new TextMeshProUGUI[2];
        NetworkObject tombstone;
        Runner.TryFindObject(tombstoneIDs[localPlayerIndex], out tombstone);
        countDown[localPlayerIndex]  = tombstone.transform.Find("Canvas").Find("Count Down Value Label").GetComponent<TextMeshProUGUI>();
        _audioManager = GameObject.Find("SoundManager").GetComponent<AudioManager>();
    }

    void Reset()
    {
        gameStarted = false;
        isGameOver = false;    
    }

    // Update is called once per frame
    void Update()
    {
        // update rpc only if needed. Don't polute the data stream
        if (!gameStarted)
        {
            roomCapacity = Runner.ActivePlayers.Count();
            if(roomCapacity > 0 && (roomCapacity == playerGrabbed))
            {
                gameStarted = true;
                if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
                {
                    StartCoroutine(StartCountDown());
                }
            }
        }
        if (Runner.ActivePlayers.Count() > 1 && tombstoneIDs[otherPlayerIndex].IsValid && !countDown[otherPlayerIndex])
		{
			NetworkObject otherTombstone;
            Runner.TryFindObject(tombstoneIDs[otherPlayerIndex], out otherTombstone);
			countDown[otherPlayerIndex]  = otherTombstone.transform.Find("Canvas").Find("Count Down Value Label").GetComponent<TextMeshProUGUI>();
		}
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
	private void RPC_SyncCountDown(bool toggleDisable, int number, int playerIndex)
	{
        countDown[playerIndex].gameObject.SetActive(!toggleDisable);
		countDown[playerIndex].text = $"{number}";
        if (!toggleDisable)
        {
            _audioManager.PlayCountdownSound();
        }
    }


    IEnumerator StartCountDown()
    {
        int count = 3;
        while (count >= 0)
        {
            RPC_SyncCountDown(false, count, 0);
            if (roomCapacity == 2)
            {
                RPC_SyncCountDown(false, count, 1);
            }
            count--;
            yield return new WaitForSeconds(1);
        }
        RPC_SyncCountDown(true, count, 0);
        if (roomCapacity == 2)
        {
            RPC_SyncCountDown(true, count, 1);
        }
        RPC_StartGameplay();
        Debug.Log("Starting game");
    }

    /// <summary>
    /// Update the number of players who have grabbed their basket.
    /// Use as a counter to know when to start game.
    /// Game is started when the number of player grabbed is the same as room capacity.
    /// </summary>
    public void UpdatePlayerGrabbed()
    {
        RPC_SyncPlayerGrabbed();
        Debug.Log("Syncing # Player Grabbed");
    }

    /// <summary>
    /// Update the player photonview ID
    /// </summary>
    /// <param name="newData">Photonview ID of the player</param>
    /// <param name="playerIndex">Player index. 0 is Master, 1 is client</param>
    public void UpdatePlayerIDs(NetworkId newData, int playerIndex)
    {
        RPC_SyncPlayerIDs(newData, playerIndex);
        Debug.Log("Syncing Player IDs");
    }

    /// <summary>
    /// Update the basket photonview ID
    /// </summary>
    /// <param name="newData">Photonview ID of the basket</param>
    /// <param name="playerIndex">Player index. 0 is Master, 1 is client</param>
    public void UpdateBasketIDs(NetworkId newData, int playerIndex)
    {
        RPC_SyncBasketIDs(newData, playerIndex);
        Debug.Log("Syncing Basket IDs");
    }

    /// <summary>
    /// Update the shadow basket photonview ID
    /// </summary>
    /// <param name="newData">Photonview ID of the basket</param>
    /// <param name="playerIndex">Player index. 0 is Master, 1 is client</param>
    public void UpdateShadowBasketIDs(NetworkId newData, int playerIndex)
    {
        RPC_SyncShadowBasketIDs(newData, playerIndex);
        Debug.Log("Syncing Basket IDs");
    }

    /// <summary>
    /// Update the tombstone photonview ID
    /// </summary>
    /// <param name="newData">Photonview ID of the tombstone</param>
    /// <param name="playerIndex">Player index. 0 is Master, 1 is client</param>
    public void UpdateTombstoneIDs(NetworkId newData, int playerIndex)
    {
        RPC_SyncTombstoneIDs(newData, playerIndex);
        Debug.Log("Syncing Tombstone IDs");
    }

    /// <summary>
    /// Update the game over boolean
    /// </summary>
    /// <param name="newData">Game over state</param>
    public void UpdateIsGameOver(bool newData)
    {
        RPC_SyncIsGameOver(newData);
        Debug.Log("Syncing isGameOver");
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_StartGameplay()
    {
        gameplayManager.enabled = true;
        gameplay.enabled = true;
        if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
        {
            Runner.SessionInfo.IsOpen = false;
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncPlayerIDs(NetworkId newData, int playerIndex)
    {
        playerIDs[playerIndex] = newData;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncBasketIDs(NetworkId newData, int playerIndex)
    {
        basketIDs[playerIndex] = newData;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncShadowBasketIDs(NetworkId newData, int playerIndex)
    {
        shadowBasketIDs[playerIndex] = newData;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncTombstoneIDs(NetworkId newData, int playerIndex)
    {
        tombstoneIDs[playerIndex] = newData;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncIsGameOver(bool newData)
    {
        isGameOver = newData;
        if (isGameOver)
        {
            gameplayManager.gameOver();
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncIsMultiplayer(bool newData)
    {
        NetworkManager.isMultiplayer = newData;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SyncPlayerGrabbed()
    {
        playerGrabbed++;
    }
}
