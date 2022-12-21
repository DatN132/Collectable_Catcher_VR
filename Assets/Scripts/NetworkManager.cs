using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;
using Fusion.Sockets;

/// <summary>
/// This class handles communication with the server, creating rooms, and putting users into rooms
/// </summary>
public class NetworkManager : NetworkBehaviour, INetworkRunnerCallbacks
{
  /// <summary>
  /// Variable that determines whether the room's setting is set to Multiplayer
  /// </summary>
  public static bool isMultiplayer = false;

  private NetworkRunner networkRunner;
  private MainMenu mainMenu;


  void Awake()
  {
    mainMenu = GameObject.Find("UI Canvas").GetComponent<MainMenu>();
    networkRunner = gameObject.AddComponent<NetworkRunner>();
    isMultiplayer = false;
  }

  /// <summary>
  /// This method is used to create a room with the room name set to the passed in room number.
  /// </summary>
  /// <param name="roomNumber">Room number that is used to set the name of the room.</param>
  /// <returns>Returns true if the room creation request is sucessfully put into the network queue. Returns false otherwise.</returns>
  public async void InitializeSinglePlayerRoom()
  {
    StartCoroutine(mainMenu.SetNotification("Creating a room", 0f));
    StartGameResult res = await networkRunner.StartGame(new StartGameArgs()
    {
      GameMode = GameMode.Single,
      PlayerCount = 1,
    });

    if (!res.Ok)
    {
      StartCoroutine(mainMenu.SetNotification("Failed to create room due network error!", 0f));
      Debug.Log("Failed to create a single player room");
    }
  }

  /// <summary>
  /// This method allows user to join a random room in multiplayer mode
  /// </summary>
  /// <returns>Returns true if the room joining request is sucessfully put into the network queue. Returns false otherwise.</returns>
  public async void JoinRandomRoom()
  {
    StartCoroutine(mainMenu.SetNotification("Trying to join a random room", 0f));
    StartGameResult res = await networkRunner.StartGame(new StartGameArgs()
    {
      GameMode = GameMode.Shared,
      PlayerCount = 2,
    });

    if (!res.Ok)
    {
      StartCoroutine(mainMenu.SetNotification("Failed to create join a room due network error!", 0f));
      Debug.Log("Failed to join multiplayer room");
    }
  }

  // /// <summary>
  // /// Override parent method. Upon a new player entering the room, load game scene
  // /// </summary>
  // /// <param name="newPlayer">New Player that joined the room</param>
  // public override void OnPlayerEnteredRoom(Player newPlayer)
  // {
  //   base.OnPlayerEnteredRoom(newPlayer);
  //   PhotonNetwork.LoadLevel("GameScene");
  // }

  public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
  {
    Debug.Log("Player joined");
    if (runner.IsSinglePlayer)
    {
      runner.InvokeSceneLoadStart();
      // runner.SetActiveScene("GameScene");
      SceneManager.LoadScene("GameScene");
      runner.InvokeSceneLoadDone();
    }
    else if (runner.IsSharedModeMasterClient)
    {
      StartCoroutine(mainMenu.SetNotification("You are the first player, your difficulty selection will be used!\nWaiting for other player to join...", 0f));
    }
    else
    {
      runner.InvokeSceneLoadStart();
      // networkRunner.SetActiveScene("GameScene");
      SceneManager.LoadScene("GameScene");
      runner.InvokeSceneLoadDone();
    }
  }

  public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
  {
    Debug.Log("Player left");
  }

  public void OnInput(NetworkRunner runner, NetworkInput input)
  {
  }

  public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
  {
    Debug.Log("On Input Missing");
  }

  public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
  {
    Debug.Log("On Shutdown");
  }

  public void OnConnectedToServer(NetworkRunner runner)
  {
    Debug.Log("Connected To Server");
  }

  public void OnDisconnectedFromServer(NetworkRunner runner)
  {
    Debug.Log("Disconnect From Server");
  }

  public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
  {
    Debug.Log("Connect Request Received");
  }

  public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
  {
    Debug.Log("Connect Failed");
  }

  public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
  {
    Debug.Log("On User Simulation Message");
  }

  public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
  {
    Debug.Log("On Session List Updated");
  }

  public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
  {
    Debug.Log("On Custom Authentication Response");
  }

  public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
  {
    Debug.Log("On Host Migration");
  }

  public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
  {
    Debug.Log("On Reliable Data Received");
  }

  public void OnSceneLoadDone(NetworkRunner runner)
  {
    Debug.Log("On Scene Load Done");
  }

  public void OnSceneLoadStart(NetworkRunner runner)
  {
    Debug.Log("On Scene Load Start");
  }
}
