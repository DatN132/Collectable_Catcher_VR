using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Linq;


/// <summary>
/// This class handles the behaviour of the game when game is over.
/// </summary>
public class GameOver : NetworkBehaviour {
	/// <summary>
	/// Manages the audio for the game over behaviors
	/// </summary>
	private MainMenuAudioManager audioManager;
	
	/// <summary>
	/// The canvas that contains the game over panel
	/// </summary>
	private NetworkObject uiCanvas;
	/// <summary>
	/// The label indicating if the player lost of won their game
	/// </summary>
	private GameObject gameLabel;
	/// <summary>
	/// The label containing the user's final score
	/// </summary>
	private GameObject finalScoreLabel;
	/// <summary>
	/// The TextMeshPro object containing the text of the user's final score
	/// </summary>
	private TextMeshProUGUI scoreText;
	/// <summary>
	/// The button that will take the user back to the main menu
	/// </summary>
	private GameObject mainMenuButton;
	
	/// <summary>
	/// Boolean indicator as to when to move the gameover UI panel to in front of the player
	/// </summary>
	public static bool moveCanvasToStart = false;
	/// <summary>
	/// Boolean indicator as to when to animate the buttons on the game over panel
	/// </summary>
	private bool animateButtonsToStart = false;
	/// <summary>
	/// Boolean indicator as to when to fade in the button on the game over panel
	/// </summary>
	private bool fadeButtonIn = false;
	/// <summary>
	/// The index of the local player
	/// </summary>
	private int localPlayerIndex = 0;
	/// <summary>
	/// The index of the other player in multiplayer
	/// </summary>
	private int otherPlayerIndex = 1;
	/// <summary>
	/// The position of the current user
	/// </summary>
	private Vector3 userPosition;
	/// <summary>
	/// Boolean indicator to begin animating in the canvas
	/// </summary>
	private bool updateCanvasPosition = true;
	/// <summary>
	/// Boolean indicator indicating whether the canvas has been moved
	/// </summary>
	private bool canvasMovedBefore = false;
	
	void Start() {
		moveCanvasToStart = false;
		animateButtonsToStart = false;
		fadeButtonIn = false;
		audioManager = GameObject.Find("UISoundManager").GetComponent<MainMenuAudioManager>();
		uiCanvas = Runner.Spawn((GameObject)Resources.Load("GameOverPanel", typeof(GameObject)), new Vector3(userPosition.x, userPosition.y, 0.07f), Quaternion.identity, Runner.LocalPlayer);
		uiCanvas.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
		gameLabel = uiCanvas.transform.Find("Game Title").gameObject;
		finalScoreLabel = uiCanvas.transform.Find("Game Final Score").gameObject;
		scoreText = finalScoreLabel.GetComponent<TextMeshProUGUI>();
		mainMenuButton = uiCanvas.transform.Find("Main Menu Button").gameObject;
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
		updateCanvasPosition = true;
		canvasMovedBefore = false;
	}
	
	void Update() {
		if (updateCanvasPosition)
		{
			userPosition = Camera.main.transform.position;
			uiCanvas.transform.position = new Vector3(userPosition.x, userPosition.y, 0.07f);
		}
		if (!canvasMovedBefore && GameOver.moveCanvasToStart) {
			updateCanvasPosition = false;
			uiCanvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
			float finalZPosition = userPosition.z + 0.7f;
			Vector3 newCanvasPosition = new Vector3(uiCanvas.transform.position.x, uiCanvas.transform.position.y, finalZPosition);
			uiCanvas.transform.position = Vector3.MoveTowards(uiCanvas.transform.position, newCanvasPosition, 10.0f * Time.deltaTime);
			if (uiCanvas.transform.position.z <= finalZPosition) {
				moveCanvasToStart = false;
				animateButtonsToStart = true;
				canvasMovedBefore = true;
			}
		} else if (animateButtonsToStart) {
			float finalYPosition = 20f;//1.550f + (18.3f - 4.1f);
			Vector3 newGameLabelPosition = new Vector3(gameLabel.transform.localPosition.x, finalYPosition, gameLabel.transform.localPosition.z);
			gameLabel.transform.localPosition = Vector3.MoveTowards(gameLabel.transform.localPosition, newGameLabelPosition, 10.0f * Time.deltaTime);
			if (gameLabel.transform.localPosition.y >= finalYPosition) {
				mainMenuButton.SetActive(true);
				if (NetworkManager.isMultiplayer)
				{
					if (Runner.ActivePlayers.Count() < 2)
					{
						scoreText.text = "Other Player Left\nYou Win!!!";
					}
					else if (GameplayManager.health[localPlayerIndex] > GameplayManager.health[otherPlayerIndex])
					{
						scoreText.text = "You Win!!!";
					}
					else if (GameplayManager.health[localPlayerIndex] < GameplayManager.health[otherPlayerIndex])
					{
						scoreText.text = "You Lose!!!";
					}
					else
					{
						if (GameplayManager.scores[localPlayerIndex] > GameplayManager.scores[otherPlayerIndex])
						{
							scoreText.text = "You Win!!!";
						}
						else if (GameplayManager.scores[localPlayerIndex] < GameplayManager.scores[otherPlayerIndex])
						{
							scoreText.text = "You Lose!!!";
						}
						else
						{
							scoreText.text = "Game Tied";
						}
					}
				}
				else
				{
					scoreText.text = $"Final Score: {GameplayManager.scores[localPlayerIndex]}";
					LeaderboardHandler.UpdateLeaderboardScores(GameplayManager.scores[localPlayerIndex]);
				}
				finalScoreLabel.SetActive(true);
				
				Color startColor = mainMenuButton.GetComponent<Image>().material.color;
				startColor.a = 0.0f;
				mainMenuButton.GetComponent<Image>().material.color = startColor;
				animateButtonsToStart = false;
				fadeButtonIn = true;
			}
		} else if (fadeButtonIn) {
			Color finalColor = mainMenuButton.GetComponent<Image>().material.color;
			finalColor.a += 5.0f * Time.deltaTime;
			mainMenuButton.GetComponent<Image>().material.color = finalColor;
			if (finalColor.a >= 1.0f) {
				fadeButtonIn = false;
			}
		}
	}
	
	/// <summary>
	/// Leaves the multiplayer room and opens the Main Menu scene
	/// </summary>
	public void OpenMainMenu()
	{
		audioManager.PlayButtonClickSound();
		Runner.Shutdown();
		SceneManager.LoadScene("MainMenu");
	}
}