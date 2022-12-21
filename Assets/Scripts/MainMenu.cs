using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;
using Fusion;

/// <summary>
/// Enum that holds the difficulty of the game
/// </summary>
public enum Difficulty {
	Easy = 0,
	Medium = 1,
	Hard = 2
}


/// <summary>
/// Main menu class where user can select to create room, join room, set room difficulty, etc.
/// </summary>
public class MainMenu : NetworkBehaviour
{
	/// <summary>
	/// This variable holds the currently set game difficulty
	/// </summary>
	public static Difficulty difficulty = Difficulty.Easy;
	private MainMenuAudioManager audioManager;

	/// <summary>
	/// Reference to the UICanvas gameobject
	/// </summary>
	public GameObject uiCanvas;

	/// <summary>
	/// Reference to the Menu title
	/// </summary>
	public GameObject gameLabel;

	/// <summary>
	/// Reference to the Single player button
	/// </summary>
	public GameObject singlePlayerButton;

	/// <summary>
	/// Reference to the Start game button
	/// </summary>
	public GameObject startButton;

	/// <summary>
	/// Reference to the Exit game button
	/// </summary>
	public GameObject exitButton;

	/// <summary>
	/// Reference to the HELP button
	/// </summary>
	public GameObject helpButton;

	/// <summary>
	/// Reference to the HELP panel
	/// </summary>
	public GameObject helpPanel;

	/// <summary>
	/// Reference to the Multiplayer button
	/// </summary>
	public GameObject multiPlayerButton;

	/// <summary>
	/// Reference to the Notification text in the UI Menu
	/// </summary>
	public GameObject notificationText;

	/// <summary>
	/// Reference to the difficulty dropdown menu
	/// </summary>
	public GameObject difficultyDropdown;

	/// <summary>
	/// Reference to the leaderboard toggle button
	/// </summary>
	public GameObject leaderboardButton;

	/// <summary>
	/// Reference to the leaderboard canvas
	/// </summary>
	public GameObject leaderboardCanvas;

	private GameObject overrideButton;
	private NetworkManager networkManager;
	
	private bool moveCanvasToStart = false;
	private bool animateButtonsToStart = false;
	private bool fadeButtonIn = false;
	
	void Start()
	{
		audioManager = GameObject.Find("SoundManager").GetComponent<MainMenuAudioManager>();
		uiCanvas.transform.position = new Vector3(uiCanvas.transform.position.x, uiCanvas.transform.position.y, 0.07f);
		moveCanvasToStart = true;
		networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
		overrideButton = transform.Find("OverrideDropdownButton").gameObject;
		difficulty = Difficulty.Easy;
		UnityEngine.Random.InitState((int)(DateTime.UtcNow - new DateTime(1970,1,1)).TotalMilliseconds);
	}
	
	void Update()
	{
		if (moveCanvasToStart) {
			float finalZPosition = -9.0f;
			Vector3 newCanvasPosition = new Vector3(uiCanvas.transform.position.x, uiCanvas.transform.position.y, finalZPosition);
			uiCanvas.transform.position = Vector3.MoveTowards(uiCanvas.transform.position, newCanvasPosition, 10.0f * Time.deltaTime);
			if (uiCanvas.transform.position.z <= finalZPosition) {
				moveCanvasToStart = false;
				animateButtonsToStart = true;
			}
		} else if (animateButtonsToStart) {
			float finalYPosition = 22f;//1.550f + (18.3f - 4.1f);
			Vector3 newGameLabelPosition = new Vector3(gameLabel.transform.localPosition.x, finalYPosition, gameLabel.transform.localPosition.z);
			gameLabel.transform.localPosition = Vector3.MoveTowards(gameLabel.transform.localPosition, newGameLabelPosition, 10.0f * Time.deltaTime);
			if (gameLabel.transform.localPosition.y >= finalYPosition) {				
				Color createColor = singlePlayerButton.GetComponent<Image>().material.color;
				createColor.a = 0.0f;
				singlePlayerButton.GetComponent<Image>().material.color = createColor;
				Color joinColor = multiPlayerButton.GetComponent<Image>().material.color;
				joinColor.a = 0.0f;
				multiPlayerButton.GetComponent<Image>().material.color = joinColor;
				animateButtonsToStart = false;
				fadeButtonIn = true;
			}
		} else if (fadeButtonIn) {
			Color finalColor = singlePlayerButton.GetComponent<Image>().material.color;
			finalColor.a += 5.0f * Time.deltaTime;
			singlePlayerButton.GetComponent<Image>().material.color = finalColor;
			finalColor = multiPlayerButton.GetComponent<Image>().material.color;
			finalColor.a += 5.0f * Time.deltaTime;
			multiPlayerButton.GetComponent<Image>().material.color = finalColor;
			if (finalColor.a >= 1.0f) 
			{
				fadeButtonIn = false;
				notificationText.SetActive(false);
				singlePlayerButton.SetActive(true);
				multiPlayerButton.SetActive(true);
				exitButton.SetActive(true);
				helpButton.SetActive(true);
				leaderboardButton.GetComponentInChildren<TextMeshProUGUI>().text = "Hide Leaderboard";
				leaderboardButton.SetActive(true);
				leaderboardCanvas.SetActive(true);
			}
		}
	}

	/// <summary>
	/// This method is used to display the help menu
	/// </summary>
	public void Help()
	{
		helpPanel.SetActive(!helpPanel.activeSelf);
		audioManager.PlayButtonClickSound();
	}
	
	/// <summary>
	/// This method puts the user into a room and start the game
	/// </summary>
	public void StartGame() {
		audioManager.PlayButtonClickSound();
		if (startButton.GetComponentInChildren<TextMeshProUGUI>().text == "Start")
		{
			if (Runner && Runner.SessionInfo.IsValid)
			{
				StartCoroutine(SetNotification("You are already in a room or did not exit the room correctly.\nPlease hit \"Back\" and try again!", 0));
			}
			else
			{
				networkManager.InitializeSinglePlayerRoom();
			}			
		}
		else if (startButton.GetComponentInChildren<TextMeshProUGUI>().text == "Join Random")
		{
			if (Runner && Runner.SessionInfo.IsValid)
			{
				StartCoroutine(SetNotification("You are already in a room or did not exit the room correctly.\nPlease hit \"Back\" and try again!", 0));
			}
			else 
			{
				networkManager.JoinRandomRoom();
			}				
		}
	}

	// /// <summary>
	// /// Override parent method. Notify player if the room creation failed
	// /// </summary>
	// /// <param name="returnCode">Photon room create failure return code</param>
	// /// <param name="message">Photon error message</param>
	// public override void OnCreateRoomFailed (short returnCode, string message)
	// {
	// 	base.OnCreateRoomFailed(returnCode, message);
	// 	StartCoroutine(SetNotification("Failed to create room. Network error.\nPlease try again later", 1f));
	// }

	// /// <summary>
	// /// Override parent method. Notify player if the room joining failed
	// /// </summary>
	// /// <param name="returnCode">Photn join room failure return code</param>
	// /// <param name="message">Photon error message</param>
	// public override void OnJoinRoomFailed (short returnCode, string message)
	// {
	// 	base.OnJoinRoomFailed(returnCode, message);
	// 	StartCoroutine(SetNotification("Failed to join room. Network error.\nPlease try again later", 0.5f));
	// }

	// /// <summary>
	// /// Override parent method. This method stop the SetNotification coroutine in the current scene.
	// /// This method will also start the game if the game mode is set to single player
	// /// </summary>
	// public override void OnJoinedRoom()
	// {
	// 	base.OnJoinedRoom();
	// 	StopCoroutine(SetNotification());
	// 	if (!NetworkManager.isMultiplayer)
	// 	{
	// 		PhotonNetwork.LoadLevel("GameScene");
	// 	}
	// 	else
	// 	{
	// 		if (PhotonNetwork.IsMasterClient)
	// 		{
	// 			StartCoroutine(SetNotification("Failed to join a room.\nCreating a room instead", 0));
	// 			StartCoroutine(SetNotification("You are the first player, your difficulty selection will be used!\nWaiting for other player to join...", 1f));
	// 		}
	// 	}
	// }

	// /// <summary>
	// /// Override parent method. This method notifies the user that no room exists that user can join.
	// /// </summary>
	// /// <param name="returnCode">Photon join random failure return code</param>
	// /// <param name="message">Photon error message</param>
	// public override void OnJoinRandomFailed(short returnCode, string message)
	// {
	// 	base.OnJoinRandomFailed(returnCode, message);
	// 	StartCoroutine(SetNotification("Failed to join a room due to potentially network error.\nPlease try again in a few seconds!", 0));
	// }

	// /// <summary>
	// /// Override parent method. This method notifies the Multiplayer user that a room was created successfully.
	// /// </summary>
	// public override void OnCreatedRoom()
	// {
	// 	base.OnCreatedRoom();
	// 	Debug.Log("Created Room successfully");
	// 	if (NetworkManager.isMultiplayer)
	// 	{
	// 		StartCoroutine(SetNotification("You are the first player, your difficulty selection will be used!\nWaiting for other player to join...", 1f));
	// 	}
	// }

	public IEnumerator SetNotification(string str = null, float delay = 1f)
	{
		if (str != null)
		{
			yield return new WaitForSeconds(delay);
			notificationText.GetComponent<TextMeshProUGUI>().text = str;
			notificationText.SetActive(true);
		}
  }

	/// <summary>
	/// This methods display options user can click to start a single player game
	/// </summary>
	public void OnCreateRoomButtonClicked()
	{
		NetworkManager.isMultiplayer = false;
		audioManager.PlayButtonClickSound();
		startButton.SetActive(true);
		singlePlayerButton.SetActive(false);
		multiPlayerButton.SetActive(false);
		difficultyDropdown.SetActive(true);
		exitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
		startButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
	}

	/// <summary>
	/// This method shows the difficulty dropdown so that the user can be randomly matched with another user
	/// </summary>
	public void OnJoinRoomButtonClicked()
	{
		NetworkManager.isMultiplayer = true;
		audioManager.PlayButtonClickSound();
		startButton.SetActive(true);
		singlePlayerButton.SetActive(false);
		multiPlayerButton.SetActive(false);
		difficultyDropdown.SetActive(true);
		StartCoroutine(SetNotification("NOTICE: Game only starts when both players have grabbed their basket in Multiplayer Mode\n       First player's difficulty setting will be used for the room", 0.1f));
		exitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
		startButton.GetComponentInChildren<TextMeshProUGUI>().text = "Join Random";
	}
	
	/// <summary>
	/// This method toggles the leaderboard ui canvas
	/// </summary>
	public void OnLeaderboardButtonClicked()
	{
		TextMeshProUGUI buttonText = leaderboardButton.GetComponentInChildren<TextMeshProUGUI>();
		if (buttonText.text == "Show Leaderboard")
		{
			leaderboardCanvas.SetActive(true);
			buttonText.text = "Hide Leaderboard";
		}
		else if (buttonText.text == "Hide Leaderboard")
		{
			leaderboardCanvas.SetActive(false);
			buttonText.text = "Show Leaderboard";
		}
		audioManager.PlayButtonClickSound();
	}
	
	/// <summary>
	/// This method is used to set the difficulty of the game.
	/// </summary>
	/// <param name="dropdown">This is the selected value from the difficulty dropdown menu.</param>
	public void SelectDifficulty(TMP_Dropdown dropdown) {
		audioManager.PlayButtonClickSound();
		MainMenu.difficulty = (Difficulty)dropdown.value;
	}
	
	/// <summary>
	/// This method is used to close the application wehen the Exit button is clicked.
	/// </summary>
	public void Exit() {
		if (exitButton.GetComponentInChildren<TextMeshProUGUI>().text == "Exit")
		{
			audioManager.PlayButtonClickSound();
			Application.Quit();
		}
		else if (exitButton.GetComponentInChildren<TextMeshProUGUI>().text == "Back")
		{
			if (Runner.SessionInfo.IsValid)
			{
				Runner.Shutdown();
			}
			else
			{
				singlePlayerButton.SetActive(true);
				multiPlayerButton.SetActive(true);
			}
			audioManager.PlayButtonClickSound();
			overrideButton.SetActive(false);
			difficultyDropdown.SetActive(false);
			startButton.SetActive(false);
			notificationText.SetActive(false);
			helpPanel.SetActive(false);
			exitButton.GetComponentInChildren<TextMeshProUGUI>().text = "Exit";
		}
		
	}

	// /// <summary>
	// /// Override parent method. If for whatever reason the application failed to connect to photon server, make it so that only single player mode is available.
	// /// </summary>
	// /// <param name="cause">Enum of the cause of disconnection</param>
	// public override void OnDisconnected(DisconnectCause cause)
	// {
	// 	base.OnDisconnected(cause);
	// 	StartCoroutine(SetNotification("Failed to lobby. Only Single Player mode is available!\nReason: " + cause.ToString(), 0.1f));
	// 	PhotonNetwork.OfflineMode = true;
	// 	singlePlayerButton.SetActive(true);
	// 	multiPlayerButton.SetActive(false);
	// 	overrideButton.SetActive(false);
	// 	difficultyDropdown.SetActive(false);
	// 	startButton.SetActive(false);
	// 	notificationText.SetActive(false);
	// }
}