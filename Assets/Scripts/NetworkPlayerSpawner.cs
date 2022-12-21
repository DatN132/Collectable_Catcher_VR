using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Class that spawns networked players after the transition to the main scene
/// </summary>
public class NetworkPlayerSpawner : NetworkBehaviour
{
  private NetworkObject spawnedPlayerPrefab;
  private NetworkObject spawnedBasketPrefab;
  private NetworkObject spawnedShadowBasketPrefab;
  private NetworkObject spawnedTombstonePrefab;

  /// <summary>
  /// Array of locations where players can spawn.
  /// </summary>
  public Transform[] playerSpawnLocations;

  /// <summary>
  /// Array of locations where baskets can spawn.
  /// </summary>
  public Transform[] basketSpawnLocations; 

  /// <summary>
  /// Array of locations where gamescore/tombstone can spawn.
  /// </summary>
  public Transform[] tombstoneSpawnLocations;
  private NetworkVariablesAndReferences networkVar;

  void Start()
  {
    // If player is not connected to the server, send them back to main menu scene
    if (!Runner.IsRunning)
    {
      AutoScroll.textToScrollThrough = "\n\n\n\n\n\n\n\n\n\n\nYou are not connected to the server.\n\nPlease try again!\n\n\n\n\n\n\n\n\n\n\n\n";
      SceneManager.LoadScene("OwnerLeftRoom");
    }
    networkVar = GameObject.Find("Network Interaction Statuses").GetComponent<NetworkVariablesAndReferences>();
    if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
    {
      spawnedPlayerPrefab = Runner.Spawn((GameObject)Resources.Load("Network Player", typeof(GameObject)), playerSpawnLocations[0].position, playerSpawnLocations[0].rotation, Runner.LocalPlayer);
      spawnedBasketPrefab = Runner.Spawn((GameObject)Resources.Load("Network Basket", typeof(GameObject)), basketSpawnLocations[0].position, basketSpawnLocations[0].rotation, Runner.LocalPlayer);
      spawnedShadowBasketPrefab = Runner.Spawn((GameObject)Resources.Load("Network Shadow Basket", typeof(GameObject)), basketSpawnLocations[0].position, basketSpawnLocations[0].rotation, Runner.LocalPlayer);
      spawnedBasketPrefab.transform.localScale = new Vector3(25,25,25);
      spawnedTombstonePrefab = Runner.Spawn((GameObject)Resources.Load("Game Score", typeof(GameObject)), tombstoneSpawnLocations[0].position, tombstoneSpawnLocations[0].rotation, Runner.LocalPlayer);
      networkVar.UpdateBasketIDs(spawnedBasketPrefab.Id, 0);
      networkVar.UpdateShadowBasketIDs(spawnedShadowBasketPrefab.Id, 0);
      networkVar.UpdatePlayerIDs(spawnedPlayerPrefab.Id, 0);
      networkVar.UpdateTombstoneIDs(spawnedTombstonePrefab.Id, 0);
      if (!NetworkManager.isMultiplayer)
      {
        spawnedTombstonePrefab.transform.Find("Deterrent_Bomb").gameObject.SetActive(false);
      }
    }
    else
    {
      spawnedPlayerPrefab = Runner.Spawn((GameObject)Resources.Load("Network Player", typeof(GameObject)), playerSpawnLocations[1].position, playerSpawnLocations[1].rotation, Runner.LocalPlayer);
      XROrigin origin = FindObjectOfType<XROrigin>();
      origin.transform.position = playerSpawnLocations[1].position;
      origin.transform.rotation = playerSpawnLocations[1].rotation;
      spawnedBasketPrefab = Runner.Spawn((GameObject)Resources.Load("Network Basket", typeof(GameObject)), basketSpawnLocations[1].position, basketSpawnLocations[1].rotation, Runner.LocalPlayer);
      spawnedShadowBasketPrefab = Runner.Spawn((GameObject)Resources.Load("Network Shadow Basket", typeof(GameObject)), basketSpawnLocations[1].position, basketSpawnLocations[1].rotation, Runner.LocalPlayer);
      spawnedBasketPrefab.transform.localScale = new Vector3(25,25,25);
      spawnedTombstonePrefab = Runner.Spawn((GameObject)Resources.Load("Game Score", typeof(GameObject)), tombstoneSpawnLocations[1].position, tombstoneSpawnLocations[1].rotation, Runner.LocalPlayer);
      networkVar.UpdateBasketIDs(spawnedBasketPrefab.Id, 1);
      networkVar.UpdateShadowBasketIDs(spawnedShadowBasketPrefab.Id, 1);
      networkVar.UpdatePlayerIDs(spawnedPlayerPrefab.Id, 1);
      networkVar.UpdateTombstoneIDs(spawnedTombstonePrefab.Id, 1);
    }
    Debug.Log("Joined Room");
  }

  void Update()
  {
    if (networkVar.isGameOver && spawnedPlayerPrefab)
    {
      spawnedPlayerPrefab.gameObject.SetActive(false);
    }
  }
}
