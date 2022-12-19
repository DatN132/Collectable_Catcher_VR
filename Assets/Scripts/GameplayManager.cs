using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

/// <summary>
/// This class handles pointing for the player.
/// </summary>
public class GameplayManager : MonoBehaviourPunCallbacks
{
	private MainMenuAudioManager audioManager;
	private AudioManager nonMainMenuAudioManager;

	private TextMeshProUGUI[] scoreText;
	private TextMeshProUGUI[] deterrentCount;
	public static int[] health;
	/// <summary>
	/// Public reference to indicate whether the game is still going on.
	/// </summary>
	public static bool gameIsOver = false;
	
	private GameObject[] heart1;
	private GameObject[] heart2;
	private GameObject[] heart3;
	private GameObject[] heart4;
	private GameObject[] heart5;
	/// <summary>
	/// Reference to the minus sign that is to be spawned when user loses points.
	/// </summary>
	public GameObject minusSign;
	
	private GameObject[] scoreCanvas;
	private GameObject[] allHearts;
	private GameObject tombstone;
	private GameObject[] graveUpright;
	private GameObject[] graveDown;
	private GameObject deterrentCanvas;
	
	private TwoHandGrabInteractable basket;
	private NetworkVariablesAndReferences networkVar;
	/// <summary>
	/// Variables that keep track of the scores of both users.
	/// </summary>
	/// <value>Index 0 is the score of MasterClient. Index 1 is the score of remote client</value>
	public static int[] scores = {0, 0};
	private int localPlayerIndex = -1;
	private int otherPlayerIndex = -1;

	private int gameStreak;
	/// <summary>
	/// Variables that keep track of how many deterrents each player has to send.
	/// </summary>
	/// <value>Index 0 is the number of deterrents of MasterClient. Index 1 is the number of deterrents of remote client</value>
	public int[] deterrentsAvailable = {0, 0};
	/// <summary>
	/// User-defined value as to how long of a streak is needed before the user can send deterrents to the opponent.
	/// </summary>
	public int streakToDeterrent;
	private bool firstTimeStreak = true;

	void Awake()
	{
		gameIsOver = false;
		firstTimeStreak = true;
	}

	/// <summary>
	/// Override the on enable method of MonoBehaviourPunCallbacks.
	/// Instantiates necessary variables.
	/// </summary>
	public override void OnEnable()
	{
		base.OnEnable();
		audioManager = GameObject.Find("UISoundManager").GetComponent<MainMenuAudioManager>();
		nonMainMenuAudioManager = GameObject.Find("SoundManager").GetComponent<AudioManager>();
		networkVar = GameObject.Find("Network Interaction Statuses").GetComponent<NetworkVariablesAndReferences>();
		scoreText = new TextMeshProUGUI[2];
		deterrentCount = new TextMeshProUGUI[2];
		deterrentsAvailable = new int[2];
		allHearts = new GameObject[2];
		graveDown = new GameObject[2];
		graveUpright = new GameObject[2];
		scoreCanvas = new GameObject[2];
		health = new int[2];
		
		if (PhotonNetwork.IsMasterClient)
		{
			localPlayerIndex = 0;
			otherPlayerIndex = 1;
		}
		else
		{
			localPlayerIndex = 1;
			otherPlayerIndex = 0;
		}
		basket = PhotonView.Find(networkVar.basketIDs[localPlayerIndex]).GetComponent<TwoHandGrabInteractable>();
		tombstone = PhotonView.Find(networkVar.tombstoneIDs[localPlayerIndex]).gameObject;
		graveUpright[localPlayerIndex] = tombstone.transform.Find("Game Gravestone Upright").gameObject;
		graveDown[localPlayerIndex] = tombstone.transform.Find("Game Gravestone Down").gameObject;
		Transform hearts = tombstone.transform.Find("Hearts");
		heart1 = new GameObject[2];
		heart2 = new GameObject[2];
		heart3 = new GameObject[2];
		heart4 = new GameObject[2];
		heart5 = new GameObject[2];
		heart1[localPlayerIndex] = hearts.Find("Heart 1").gameObject;
		heart2[localPlayerIndex] = hearts.Find("Heart 2").gameObject;
		heart3[localPlayerIndex] = hearts.Find("Heart 3").gameObject;
		heart4[localPlayerIndex] = hearts.Find("Heart 4").gameObject;
		heart5[localPlayerIndex] = hearts.Find("Heart 5").gameObject;
		allHearts[localPlayerIndex] = hearts.gameObject;
		scoreCanvas[localPlayerIndex] = tombstone.transform.Find("Canvas").gameObject;
		scoreText[localPlayerIndex]  = scoreCanvas[localPlayerIndex].transform.Find("Score Value Label").GetComponent<TextMeshProUGUI>();
		deterrentCanvas = tombstone.transform.Find("Deterrent_Bomb").GetChild(0).gameObject;
		deterrentCount[localPlayerIndex] = deterrentCanvas.transform.Find("Deterrent Count").GetComponent<TextMeshProUGUI>();
		scores[localPlayerIndex] = 0;
		health[localPlayerIndex] = 0;
		scoreText[localPlayerIndex].text = $"{scores[localPlayerIndex]}";
		gameStreak = 0;
		deterrentsAvailable[localPlayerIndex] = 0;
		deterrentCount[localPlayerIndex].text = $"{deterrentsAvailable[localPlayerIndex]}";

		if (NetworkManager.isMultiplayer)
		{
			GameObject otherTombstone = PhotonView.Find(networkVar.tombstoneIDs[otherPlayerIndex]).gameObject;
			graveUpright[otherPlayerIndex] = otherTombstone.transform.Find("Game Gravestone Upright").gameObject;
			graveDown[otherPlayerIndex] = otherTombstone.transform.Find("Game Gravestone Down").gameObject;
			deterrentCount[otherPlayerIndex] = otherTombstone.transform.Find("Deterrent_Bomb").GetChild(0).Find("Deterrent Count").GetComponent<TextMeshProUGUI>();
			scoreCanvas[otherPlayerIndex] =otherTombstone.transform.Find("Canvas").gameObject;
			scoreText[otherPlayerIndex] = scoreCanvas[otherPlayerIndex].transform.Find("Score Value Label").GetComponent<TextMeshProUGUI>();
			scoreText[otherPlayerIndex].text = $"{scores[otherPlayerIndex]}";
			deterrentCount[otherPlayerIndex].text = $"{deterrentsAvailable[otherPlayerIndex]}";
			Transform otherHearts = otherTombstone.transform.Find("Hearts");
			heart1[otherPlayerIndex] = otherHearts.Find("Heart 1").gameObject;
			heart2[otherPlayerIndex] = otherHearts.Find("Heart 2").gameObject;
			heart3[otherPlayerIndex] = otherHearts.Find("Heart 3").gameObject;
			heart4[otherPlayerIndex] = otherHearts.Find("Heart 4").gameObject;
			heart5[otherPlayerIndex] = otherHearts.Find("Heart 5").gameObject;
			allHearts[otherPlayerIndex] = otherHearts.gameObject;
			health[otherPlayerIndex] = 0;
			photonView.RPC("SyncScore", RpcTarget.All, scores[localPlayerIndex], localPlayerIndex);
		}

		photonView.RPC("SyncHearts", RpcTarget.All, 3, localPlayerIndex);

		if (streakToDeterrent == 0)
		{
			streakToDeterrent = 10;
		}
	}

	/// <summary>
	/// Method that adds score to the user.
	/// </summary>
	public void IncreaseScore()
	{
		 if (GameplayManager.gameIsOver) {
			 return;
		 }
		float percentage = basket.transform.localScale.x / basket.maxScale;
		int scoreIncrease = (int)(1f + (5f * (1f - percentage)));
		scores[localPlayerIndex] += scoreIncrease;
		photonView.RPC("SyncScore", RpcTarget.All, scores[localPlayerIndex], localPlayerIndex);
		gameStreak++;
		if (NetworkManager.isMultiplayer && (gameStreak % streakToDeterrent == 0))
		{
			IncreaseDeterrentsAvailable();
		}
	}

	private void IncreaseDeterrentsAvailable()
	{
		deterrentsAvailable[localPlayerIndex]++;
		if (firstTimeStreak)
		{
			deterrentCanvas.transform.Find("Deterrent Notification").gameObject.SetActive(true);
			firstTimeStreak = false;
		}
		UpdateDeterrentCountText();
	}

	/// <summary>
	/// Method that is used to update the deterrent count text for both users.
	/// </summary>
	public void UpdateDeterrentCountText()
	{
		photonView.RPC("SyncDeterrentCount", RpcTarget.All, deterrentsAvailable[localPlayerIndex], localPlayerIndex);
	}

	[PunRPC]
	private void SyncHearts(int heartCount, int playerIndex)
	{
		health[playerIndex] = heartCount;
		switch(heartCount)
		{
			case 0: 
				heart1[playerIndex].SetActive(false);
				heart2[playerIndex].SetActive(false);
				heart3[playerIndex].SetActive(false);
				heart4[playerIndex].SetActive(false);
				heart5[playerIndex].SetActive(false);
				if (PhotonNetwork.IsMasterClient)
        {
					networkVar.UpdateIsGameOver(true);
				}
				break;
			case 1:
				heart1[playerIndex].SetActive(false);
				heart2[playerIndex].SetActive(false);
				heart3[playerIndex].SetActive(true);
				heart4[playerIndex].SetActive(false);
				heart5[playerIndex].SetActive(false);
				break;
			case 2:
				heart1[playerIndex].SetActive(false);
				heart2[playerIndex].SetActive(true);
				heart3[playerIndex].SetActive(true);
				heart4[playerIndex].SetActive(false);
				heart5[playerIndex].SetActive(false);
				break;
			case 3:
				heart1[playerIndex].SetActive(false);
				heart2[playerIndex].SetActive(true);
				heart3[playerIndex].SetActive(true);
				heart4[playerIndex].SetActive(true);
				heart5[playerIndex].SetActive(false);
				break;
			case 4:
				heart1[playerIndex].SetActive(true);
				heart2[playerIndex].SetActive(true);
				heart3[playerIndex].SetActive(true);
				heart4[playerIndex].SetActive(true);
				heart5[playerIndex].SetActive(false);
				break;
			case 5:
				heart1[playerIndex].SetActive(true);
				heart2[playerIndex].SetActive(true);
				heart3[playerIndex].SetActive(true);
				heart4[playerIndex].SetActive(true);
				heart5[playerIndex].SetActive(true);
				break;
		}
	}

	[PunRPC]
	private void SyncDeterrentCount(int newData, int playerIndex)
	{
		deterrentsAvailable[playerIndex] = newData;
		deterrentCount[playerIndex].text = $"{deterrentsAvailable[playerIndex]}";
	}

	[PunRPC]
	private void SyncScore(int newScore, int playerIndex)
	{
		scores[playerIndex] = newScore;
		scoreText[playerIndex].text = $"{scores[playerIndex]}";
	}

	/// <summary>
	/// Method that is used to take away life from the user.
	/// </summary>
	public void DecreaseScore()
	{
		if (GameplayManager.gameIsOver) {
			return;
		}
		if (localPlayerIndex == 0)
		{
			Instantiate(minusSign, new Vector3(0.03999999f, 0.45f, -8.3f), Quaternion.identity);
		}
		else if (localPlayerIndex == 1)
		{
			Instantiate(minusSign, new Vector3(5f, 0.45f, -8.3f), Quaternion.identity);
		}
		
		if (health[localPlayerIndex] == 0)
		{
			return;
		}
		photonView.RPC("SyncHearts", RpcTarget.All, health[localPlayerIndex] - 1, localPlayerIndex);
		gameStreak = 0;
	}

	/// <summary>
	/// Method that is used to increase the number of hearts when the player catch a heart
	/// </summary>
	public void IncreaseHeart()
	{
		if (GameplayManager.gameIsOver) {
			return;
		}
		if (health[localPlayerIndex] == 5)
		{
			return;
		}
		photonView.RPC("SyncHearts", RpcTarget.All, health[localPlayerIndex] + 1, localPlayerIndex);
	}
	
	/// <summary>
	/// Method that is called to end the game.
	/// </summary>
	public void gameOver()
	{
		nonMainMenuAudioManager.StopBackgroundMusic();
		audioManager.PlayGameOverSound();
		scoreCanvas[localPlayerIndex].SetActive(false);
		allHearts[localPlayerIndex].SetActive(false);
		GameplayManager.gameIsOver = true;
		graveUpright[localPlayerIndex].SetActive(false);
		graveDown[localPlayerIndex].SetActive(true);
		deterrentCount[localPlayerIndex].transform.parent.parent.gameObject.SetActive(false);
		if (NetworkManager.isMultiplayer)
		{
			allHearts[otherPlayerIndex].SetActive(false);
			scoreCanvas[otherPlayerIndex].SetActive(false);
			graveUpright[otherPlayerIndex].SetActive(false);
			graveDown[otherPlayerIndex].SetActive(true);
			deterrentCount[otherPlayerIndex].transform.parent.parent.gameObject.SetActive(false);
		}

		// give server a bit of time to make sure the final scores are synced before displaying gameover panel
		StartCoroutine(MoveGameOverCanvasToStart());
	}

	IEnumerator MoveGameOverCanvasToStart()
	{
		yield return new WaitForSeconds(0.5f);
		GameOver.moveCanvasToStart = true;
	}
}
