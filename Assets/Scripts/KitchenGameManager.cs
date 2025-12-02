using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the game state, countdown, gameplay timer,
/// player ready state, and pause logic.
/// </summary>
public class KitchenGameManager : NetworkBehaviour {

    public static KitchenGameManager Instance {get; private set;}

    /// <summary>
    /// Reference prefab that will be spawned for each connected player
    /// </summary>
    [SerializeField] private Transform playerPrefab;

    /// <summary>
    /// Fired when the game state changes
    /// </summary>
    public event EventHandler OnStateChanged;

    /// <summary>
    /// Fired when the player pauses the game
    /// </summary>
    public event EventHandler OnLocalGamePaused;

    /// <summary>
    /// Fired when the player unpauses the game
    /// </summary>
    public event EventHandler OnLocalGameUnpaused;

    /// <summary>
    /// Fired when the player's ready state changes
    /// </summary>
    public event EventHandler OnLocalPlayerReadyChanged;

    /// <summary>
    /// Fired when the multiplayer game is paused
    /// Happens when at least one player is paused
    /// </summary>
    public event EventHandler OnMultiplayerGamePaused;

    /// <summary>
    /// Fired when the multiplayer game is unpaused
    /// Happens when at all players are unpaused
    /// </summary>
    public event EventHandler OnMultiplayerGameUnpaused;

    /// <summary>
    /// The states that the game can be in
    /// </summary>
    private enum State {
        WaitingToStart, //When players are readying up
        CoundownToStart, //Countdown before game starts
        GamePlaying, // When game is running
        GameOver, // When game is over
    }

    /// <summary>
    /// Network state that is synchronized across all clients
    /// </summary>
    private NetworkVariable<State> state = new NetworkVariable<State>(State.WaitingToStart);

    /// <summary>
    /// Timer for when the game is about to begin
    /// </summary>
    private NetworkVariable<float> countdownToStartTimer = new NetworkVariable<float>(3f);
    
    /// <summary>
    /// How long the game has currently been on for
    /// </summary>
    private NetworkVariable<float> gamePlayingTimer = new NetworkVariable<float>(0f);
    
    /// <summary>
    /// How long the game will play for
    /// </summary>
    private float gamePlayingTimerMax = 120f;

    /// <summary>
    /// Checks whether the local player is ready
    /// </summary>
    private bool isLocalPlayerReady;

    /// <summary>
    /// Checks whether the local player is paused
    /// </summary>
    private bool isLocalGamePaused = false;

    /// <summary>
    /// Checks whether the game is paused
    /// </summary>
    private bool autoTestGamePausedState;

    /// <summary>
    /// Checks if the game is paused
    /// </summary>
    private NetworkVariable<bool> isGamePaused = new NetworkVariable<bool>(false);

    /// <summary>
    /// Maps the player to if they are ready
    /// </summary>
    private Dictionary<ulong, bool> playerReadyDictionary;

    /// <summary>
    /// Maps the player to if they are paused
    /// </summary>
    private Dictionary<ulong, bool> playerPausedDictionary;

    private void Awake() {
        Instance = this;

        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerPausedDictionary = new Dictionary<ulong, bool>();
    }

    public override void OnNetworkSpawn() {
        state.OnValueChanged += State_OnValueChanged;
        isGamePaused.OnValueChanged += IsGamePaused_OnValueChanged;

        if (IsServer) {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += NetworkManager_OnLoadEventCompleted;
        }
    }

    //Fired when a scene load event is done, spawns a player for each connected client
    private void NetworkManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut) {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            Transform playerTransform = Instantiate(playerPrefab);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }

    /// <summary>
    /// Called when a client disconnects from the server
    /// </summary>
    /// <param name="clientId">The client ID that disconnected</param>
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        autoTestGamePausedState = true;
    }

    // Handles changing the network paused state
    private void IsGamePaused_OnValueChanged(bool previousValue, bool newValue) {
        if (isGamePaused.Value) {
            Time.timeScale = 0f;
            OnMultiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            Time.timeScale = 1f;
            OnMultiplayerGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    //Handles changing the network game state
    private void State_OnValueChanged(State previousValue, State newValue) {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInput_OnPauseAction;
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
    }

    // Handles when pause is pressed
    private void GameInput_OnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    // Fires when the player interacts/readies during the tutorial UI page
    private void GameInput_OnInteractAction(object sender, EventArgs e) {
        if (!IsSpawned) {
            // Debug.LogWarning("Tried to ready up before KitchenGameManager was spawned.");
            return;
        }
        if (state.Value == State.WaitingToStart) {
            isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            SetPlayerReadyServerRpc();
        }
    }

    /// <summary>
    /// Server that marks the given client ready.
    /// If all clients are ready, starts the game
    /// </summary>
    /// <param name="rpcParams">The client that is ready</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerReadyServerRpc(RpcParams rpcParams = default) {
        playerReadyDictionary[rpcParams.Receive.SenderClientId] = true;
        
        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId]) {
                // This player is not ready
                allClientsReady = false;
                break;
            }
        }
        //starts the game
        if (allClientsReady) {
            state.Value = State.CoundownToStart;
        }
    }

    //handles the game states for during a game
    private void Update() {
        if (!IsServer) {
            return;
        }
        switch (state.Value) {
            case State.WaitingToStart:

                break;
            case State.CoundownToStart:
                countdownToStartTimer.Value -= Time.deltaTime;
                if (countdownToStartTimer.Value < 0f) {
                    state.Value = State.GamePlaying;
                    gamePlayingTimer.Value = gamePlayingTimerMax;
                }
                break;
            case State.GamePlaying:
                gamePlayingTimer.Value -= Time.deltaTime;
                if (gamePlayingTimer.Value < 0f) {
                    state.Value = State.GameOver;
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void LateUpdate() {
        if (autoTestGamePausedState) {
            autoTestGamePausedState = false;
            TestGamePausedState();
        }
    }

    /// <summary>
    /// Checks if the state of the game is playing
    /// </summary>
    /// <returns>True if state is playing, otherwise false</returns>
    public bool IsGamePlaying() {
        return state.Value == State.GamePlaying;
    }

    /// <summary>
    /// Checks if the state of the game is counting down
    /// </summary>
    /// <returns>True if state is counting down, otherwise false</returns>
    public bool IsCountdownToStartActive() {
        return state.Value == State.CoundownToStart;
    }

    /// <summary>
    /// Checks if the state of the game is over
    /// </summary>
    /// <returns>True if state is over, otherwise false</returns>
    public bool IsGameOver() {
        return state.Value == State.GameOver;
    }

    /// <summary>
    /// Checks if the state of the game is waiting to start
    /// </summary>
    /// <returns>True if state is waiting to start, otherwise false</returns>
    public bool IsWaitingToStart() {
        return state.Value == State.WaitingToStart;
    }

    /// <summary>
    /// Gets the countdown to start timer value
    /// Used mostly for UI
    /// </summary>
    /// <returns>The countdown start value</returns>
    public float GetCountdownToStartTimer() {
        return countdownToStartTimer.Value;
    }

    /// <summary>
    /// Checks whether the local player is ready
    /// </summary>
    /// <returns>True if the local player is ready, otherwise false</returns>
    public bool IsLocalPlayerReady() {
        return isLocalPlayerReady;
    }

    /// <summary>
    /// Gets the normalized value of the game playing timer
    /// </summary>
    /// <returns>Normalized value of the game playing timer</returns>
    public float GetGamePlayingTimerNormalized() {
        return 1 - (gamePlayingTimer.Value / gamePlayingTimerMax);
    }

    #region pause

    /// <summary>
    /// Toggles the local pause state and updates global pause state
    /// </summary>
    public void TogglePauseGame() {
        if (!IsSpawned) {
            // Debug.LogWarning("Tried before KitchenGameManager was spawned.");
            return;
        }
        isLocalGamePaused = !isLocalGamePaused;
        if (isLocalGamePaused) {
            PauseGameServerRpc();
            OnLocalGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            UnpauseGameServerRpc();
            OnLocalGameUnpaused?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Server that marks the client as paused
    /// </summary>
    /// <param name="rpcParams">The client that paused</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void PauseGameServerRpc(RpcParams rpcParams = default) {
        playerPausedDictionary[rpcParams.Receive.SenderClientId] = true;
        TestGamePausedState();
    }

    /// <summary>
    /// Server that marks the client as unpaused
    /// </summary>
    /// <param name="rpcParams">The client that unpaused</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UnpauseGameServerRpc(RpcParams rpcParams = default) {
        playerPausedDictionary[rpcParams.Receive.SenderClientId] = false;
        TestGamePausedState();
    }

    /// <summary>
    /// Checks if all clients are paused and updates global pause
    /// </summary>
    private void TestGamePausedState() {
        if (NetworkManager.Singleton == null) {
            return;
        }

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (playerPausedDictionary.ContainsKey(clientId) && playerPausedDictionary[clientId]) {
                // This player is paused
                isGamePaused.Value = true;
                return;
            }
        }

        // All players unpaused
        isGamePaused.Value = false;
    }

    #endregion

}
