using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using Fusion;

/// <summary>
/// This class handles the gameplay behaviors, i.e. when to spawn objects.
/// </summary>
public class Gameplay : NetworkBehaviour
{
    private float spawnTime = 1.0f;
    private float startingDifficulty;
    /// <summary>
    /// Reference to the middle object spawn path
    /// </summary>
    public PathCreator path;
    /// <summary>
    /// Reference to the left object spawn path
    /// </summary>
    public PathCreator leftPath;
    /// <summary>
    /// Reference to the right object spawn path
    /// </summary>
    public PathCreator rightPath;
    /// <summary>
    /// Reference to the middle object spawn path for remote player
    /// </summary>
    public PathCreator path2;
    /// <summary>
    /// Reference to the left object spawn path for the remote player
    /// </summary>
    public PathCreator leftPath2;
    /// <summary>
    /// Reference to the right object spawn path for the remote player
    /// </summary>
    public PathCreator rightPath2;
    /// <summary>
    /// Default behavior of the spawned object when it reaches the end of path
    /// </summary>
    public EndOfPathInstruction end;
    private float difficulty;
    private NetworkObject a;
    private float currentTime;
    private float previousTime;
    private NetworkVariablesAndReferences networkVar;
    private float deterrentChance = 15.0f;
    /// <summary>
    /// User-defined difficulty of the game that is set by MasterCLient.
    /// </summary>
    public static Difficulty menuDifficulty = Difficulty.Easy;
    private GameplayManager gameplayManager;
    private Material sendingDeterrentMaterial;
    private int localPlayerIndex = 0;
    private bool heartSpawnerCRRunning = false;

    /// <summary>
    /// User-defined probability of spawning a heart
    /// </summary>
    public float heartSpawnChance = 0.8f;

    /// <summary>
    /// Override parent method. This method sets difficulties and set private variables to default values.
    /// </summary>
    void OnEnable()
    {
        networkVar = GameObject.Find("Network Interaction Statuses").GetComponent<NetworkVariablesAndReferences>();
        gameplayManager = FindObjectOfType<GameplayManager>();
        sendingDeterrentMaterial = Resources.Load<Material>("Sending Bomb Material");
        if (!sendingDeterrentMaterial)
        {
            Debug.Log("Failed to find mat");
        }
        Debug.Log("Starting Coroutine to spawn objects");
        currentTime = Time.time;
        previousTime = currentTime;
        switch(menuDifficulty) 
        {
            case Difficulty.Easy:
                startingDifficulty = 2.5f;
                spawnTime = 3.00f;
                break;
            case Difficulty.Medium:
                startingDifficulty = 3.7f;
                spawnTime = 2.3f;
                break;
            case Difficulty.Hard:
                startingDifficulty = 4.3f;
                spawnTime = 1.70f;
                break;
        }

        // determine send time and send settings on master only
        if(Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
        {
            localPlayerIndex = 0;
            StartCoroutine(collectableWave());
        }
        else
        {
            localPlayerIndex = 1;
        }
        heartSpawnerCRRunning = false;
    }

    private void Start()
    {
        if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
        {
            RPC_setDifficulty(MainMenu.difficulty);
        }
    }

    IEnumerator collectableWave() {
        while(!networkVar.isGameOver)
        {
            currentTime = Time.time;
            float deltaTime = currentTime - previousTime;
            previousTime = currentTime;
            difficulty = startingDifficulty + (deltaTime / 60);
            if (difficulty > 7.0f) {
                difficulty = 7.0f;
            }
            spawnTime = spawnTime - (deltaTime / 250) ;
            if (spawnTime < 0.35f) {
                spawnTime = 0.35f;
            }
            print("Difficulty: " + difficulty);
            print("Spawn Time: " + spawnTime);
            yield return new WaitForSeconds(spawnTime);
            int deterrentRoll = Random.Range(0, 100);
            int chosenPath = Random.Range(0, 3);
            RPC_spawnCollectable(deterrentRoll, chosenPath, difficulty, -1);
            if (!heartSpawnerCRRunning)
            {
                heartSpawnerCRRunning = true;
                StartCoroutine(randomHeartSpawner());
            }
        }
    }

    IEnumerator randomHeartSpawner() {
        while(!networkVar.isGameOver)
        {
            if (Random.Range(0f,100f) <= heartSpawnChance)
            {
                yield return new WaitForSeconds(Random.Range(0f,35f));
                int chosenPath = Random.Range(0, 3);
                RPC_spawnHeart(chosenPath);
            }
            else
            {
                yield return new WaitForSeconds(1f);
            }
        }
        heartSpawnerCRRunning = false;
    }

    /// <summary>
    /// This medthod checks as to whether a user is allowed to send deterrents to the opponent.
    /// </summary>
    public void CheckAndSendDeterrent()
    {
       if (NetworkManager.isMultiplayer && gameplayManager.deterrentsAvailable[localPlayerIndex] > 0)
       {
            SendDeterrent();
            gameplayManager.deterrentsAvailable[localPlayerIndex]--;
            Debug.Log("Sent Deterrent");
            gameplayManager.UpdateDeterrentCountText();
       }
    }

    /// <summary>
    /// Method for sending deterrent toward the other player
    /// </summary>
    private void SendDeterrent()
    {
        // maybe change deterrent mat a bit to differentiate
        // change mat in spawnCollectable
        int target_player;
        if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
        {
            target_player = 1;
        }
        else
        {
            target_player = 0;
        }
        int chosenPath = Random.Range(0, 3);
        RPC_spawnCollectable(0, chosenPath, difficulty, target_player);
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_setDifficulty(Difficulty newDifficulty)
    {
        menuDifficulty = newDifficulty;
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_spawnCollectable(int deterrentRoll, int chosenPath, float synced_difficulty, int target_player = -1)
    {
		if (GameplayManager.gameIsOver) {
			return;
		}

        // update difficulty for client
        if (!Runner.IsSinglePlayer && !Runner.IsSharedModeMasterClient)
        {
            difficulty = synced_difficulty;
        }
		if (target_player == -1 || (target_player == 0 && Runner.IsSharedModeMasterClient) || (target_player == 1 && !Runner.IsSharedModeMasterClient))
        {
            if (deterrentRoll < (int)deterrentChance) {
                a = Runner.Spawn((GameObject)Resources.Load("Deterrent_Bomb", typeof(GameObject)), transform.position, new Quaternion(-90,0,0,0), Runner.LocalPlayer);
                a.tag = "Deterrent";
            }
            else {
                a = Runner.Spawn((GameObject)Resources.Load("Collectable", typeof(GameObject)), transform.position, new Quaternion(-90,0,0,0), Runner.LocalPlayer);
                a.tag = "Collectable";
            }
            // Since object is spawned using PhotonNetwork.Instantiate, let photon handle viewID assignment

            //////////////////////////////////////////////////////////////////////////////////////////////////////
            // Change mat when player send deterrent intentially
            if (target_player != -1)
            {
                RPC_SetRedDeterrentSkin(a.Id);
            }
            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            
            var script = a.GetComponent<PathFollower>();
            script.speed = synced_difficulty;
            var script2 = a.GetComponent<CollectableBehavior>();
            
            script.endOfPathInstruction = end;
            
            // master uses path left, path, right.
            // client uses path left2, path2, right2
            if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
            {
                // set this to 0 or 1for multiplayer
                script2.playerIndex = 0;
                if (chosenPath == 0) {
                    script.pathCreator = leftPath;
                }
                else if (chosenPath == 1) {
                    script.pathCreator = path;
                }
                else if (chosenPath == 2) {
                    script.pathCreator = rightPath;
                }
            }
            else
            {
                // set this to 0 or 1for multiplayer
                script2.playerIndex = 1;
                if (chosenPath == 0) {
                    script.pathCreator = leftPath2;
                }
                else if (chosenPath == 1) {
                    script.pathCreator = path2;
                }
                else if (chosenPath == 2) {
                    script.pathCreator = rightPath2;
                }
            }
            a.gameObject.SetActive(true);
        }
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_spawnHeart(int chosenPath)
    {
        if (GameplayManager.gameIsOver || difficulty == 0) 
        {
			return;
		}
        a = Runner.Spawn((GameObject)Resources.Load("Heart", typeof(GameObject)), transform.position, new Quaternion(0,90,0,0), Runner.LocalPlayer);
        a.tag = "Heart";

        var script = a.GetComponent<PathFollower>();
        script.speed = difficulty;
        var script2 = a.GetComponent<CollectableBehavior>();
        
        script.endOfPathInstruction = end;
        
        // master uses path left, path, right.
        // client uses path left2, path2, right2
        if (Runner.IsSinglePlayer || Runner.IsSharedModeMasterClient)
        {
            // set this to 0 or 1for multiplayer
            script2.playerIndex = 0;
            if (chosenPath == 0) {
                script.pathCreator = leftPath;
            }
            else if (chosenPath == 1) {
                script.pathCreator = path;
            }
            else if (chosenPath == 2) {
                script.pathCreator = rightPath;
            }
        }
        else
        {
            // set this to 0 or 1for multiplayer
            script2.playerIndex = 1;
            if (chosenPath == 0) {
                script.pathCreator = leftPath2;
            }
            else if (chosenPath == 1) {
                script.pathCreator = path2;
            }
            else if (chosenPath == 2) {
                script.pathCreator = rightPath2;
            }
        }
        a.gameObject.SetActive(true);
        Debug.Log("Heart Spawned");
        Debug.Log("Difficulty: " + difficulty);
    }

    [Rpc(sources: RpcSources.All, targets: RpcTargets.All)]
    private void RPC_SetRedDeterrentSkin(NetworkId id)
    {
        NetworkObject obj;
        Runner.TryFindObject(id, out obj);
        obj.gameObject.GetComponent<MeshRenderer>().material = sendingDeterrentMaterial;
    }

}
