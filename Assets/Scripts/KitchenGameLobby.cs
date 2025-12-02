using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles creating, joining, and managing a multiplayer lobby.
/// Uses a depracated Lobby and Relay...
/// </summary>
public class KitchenGameLobby : MonoBehaviour {

    /// <summary>
    /// Key to store and retrieve the relay join code in lobby's data
    /// </summary>
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";

    public static KitchenGameLobby Instance {get; private set;}

    /// <summary>
    /// Fired when a lobby is created
    /// </summary>
    public event EventHandler OnCreateLobbyStarted;

    /// <summary>
    /// Fired when creating a lobby failed
    /// </summary>
    public event EventHandler OnCreateLobbyFailed;

    /// <summary>
    /// Fired when a player attempts to join a lobby
    /// </summary>
    public event EventHandler OnJoinStarted;

    /// <summary>
    /// Fired when a player fails to join a lobby (ID or code)
    /// </summary>
    public event EventHandler OnJoinFailed;

    /// <summary>
    /// Fired when a player fails to join a lobby using quick join
    /// </summary>
    public event EventHandler OnQuickJoinFailed;

    /// <summary>
    /// Fires when the list of lobbies change.
    /// Event arguments contain updated list of lobbies
    /// </summary>
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }

    /// <summary>
    /// The lobby that the player is in
    /// </summary>
    private Lobby joinedLobby;

    /// <summary>
    /// Timer for sending a hearbeat to the lobby so it doesn't close
    /// </summary>
    private float heartbeatTimer;

    /// <summary>
    /// Timer for refreshing the lobby list
    /// </summary>
    private float listLobbiesTimer;
    
    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeUnityAuthentication();
    }

    /// <summary>
    /// Initializes Unity Services and initializes anonymous authentication
    /// </summary>
    private async void InitializeUnityAuthentication() {
        // if its not initialized, initialize
        //async can only be initialized once
        if (UnityServices.State != ServicesInitializationState.Initialized) {
            InitializationOptions initializationOptions = new InitializationOptions();
            // initializationOptions.SetProfile(UnityEngine.Random.Range(0, 10000).ToString());

            await UnityServices.InitializeAsync(initializationOptions);

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private void Update() {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    /// <summary>
    /// Sends periodic heartbeat to the lobby so that the lobby
    /// does not auto close after 30 seconds
    /// </summary>
    private void HandleHeartbeat() {
        if (IsLobbyHost()) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f) {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    /// <summary>
    /// Refreshes the list of available lobbies the player can join
    /// </summary>
    private void HandlePeriodicListLobbies() {
        if (joinedLobby == null && 
            AuthenticationService.Instance.IsSignedIn && 
            SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString()) {

            listLobbiesTimer -= Time.deltaTime;
            if (listLobbiesTimer <= 0f) {
                float listLobbiesTimerMax = 3f;
                listLobbiesTimer = listLobbiesTimerMax;

                ListLobbies();
            }
        }
    }

    #region relay

    /// <summary>
    /// Gets the UnityTransport component used by the network manager
    /// </summary>
    /// <returns>Active UnityTransport instance</returns>
    private UnityTransport GetTransport() {
        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    /// <summary>
    /// Checks whether the game is running as a WebGL build
    /// Web can run wss, not dtls
    /// </summary>
    /// <returns>True if its a WebGL, otherwise false</returns>
    private bool IsWebGL() {
        return Application.platform == RuntimePlatform.WebGLPlayer;
    }

    /// <summary>
    /// Gets the relay connection type based on the platform
    /// </summary>
    /// <returns>wss if its for the web, otherwise dtls</returns>
    private string GetRelayConnectionType() {
        // WebGL builds must use secure WebSockets
        if (IsWebGL()) {
            return "wss";
        }
        // Desktop / standalone can use dtls (UDP)
        return "dtls";
    }

    /// <summary>
    /// Configures Unity transport to host by Relay using given allocation
    /// </summary>
    /// <param name="allocation">Relay allocation created for the host</param>
    private void ConfigureRelayForHost(Allocation allocation) {
        var transport = GetTransport();

        // WebGL for WebSockets; others are normal UDP
        transport.UseWebSockets = IsWebGL();

        string connectionType = GetRelayConnectionType();
        transport.SetRelayServerData(new RelayServerData(allocation, connectionType));
    }

    /// <summary>
    /// Configures UnityTransport to join a relay host using given join allocation
    /// </summary>
    /// <param name="joinAllocation">Relay join allocation for the client</param>
    private void ConfigureRelayForClient(JoinAllocation joinAllocation) {
        var transport = GetTransport();

        transport.UseWebSockets = IsWebGL();

        string connectionType = GetRelayConnectionType();
        transport.SetRelayServerData(new RelayServerData(joinAllocation, connectionType));
    }

    /// <summary>
    /// Checks whether the player is the host of the current lobby
    /// </summary>
    /// <returns>True if the player is, otherwise false</returns>
    private bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    /// <summary>
    /// Gets the list of current lobbies and sends data to UI
    /// </summary>
    private async void ListLobbies() {
        try {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            
            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs {
                lobbyList = queryResponse.Results
            });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Creates a relay allocation for hosting a game
    /// </summary>
    /// <returns>Task that returns the allocation</returns>
    private async Task<Allocation> AllocateRelay() {
        try {
            // host does not count
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(KitchenGameMultiplayer.MAX_PLAYER_AMOUNT - 1);
            return allocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    /// <summary>
    /// Gets the relay join code associated with the given allocation
    /// </summary>
    /// <param name="allocation">Relay allocation to get a join code for</param>
    /// <returns>Task that returns the relay join code</returns>
    private async Task<string> GetRelayJoinCode(Allocation allocation) {
        try {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    /// <summary>
    /// Joins a relay allocation from the given code
    /// </summary>
    /// <param name="joinCode">The relay join code to join</param>
    /// <returns>Task that returns the join allocation</returns>
    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
        
    }

    #endregion

    #region create/join lobby

    /// <summary>
    /// Creates a new lobby with the given name and privacy, allocates
    /// a relay, and starts hosting the game
    /// </summary>
    /// <param name="lobbyName">The name of the lobby</param>
    /// <param name="isPrivate">The privacy level</param>
    /// <returns>Task that completes when itcreates a lobby</returns>
    public async Task CreateLobby(string lobbyName, bool isPrivate) {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName, KitchenGameMultiplayer.MAX_PLAYER_AMOUNT, new CreateLobbyOptions {
                    IsPrivate = isPrivate,
                });

            Allocation allocation = await AllocateRelay();

            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    {KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });
            ConfigureRelayForHost(allocation);

            KitchenGameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Joins an available lobby (must be public and not full),
    /// configures relay, and starts the client
    /// </summary>
    /// <returns>Task that completes when join is done</returns>
    public async Task QuickJoin() {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);//
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;

            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

            ConfigureRelayForClient(joinAllocation);

            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Joins a lobby with the given lobby id,
    /// configures relay, and starts the client
    /// </summary>
    /// <param name="lobbyId">The lobby Id the player wants to join</param>
    /// <returns>Task that completes when join is done</returns>
    public async Task JoinWithId(string lobbyId) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            ConfigureRelayForClient(joinAllocation);

            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Joins a lobby with the given lobby code,
    /// configures relay, and starts the client
    /// </summary>
    /// <param name="lobbyCode">The lobby code the player wants to join</param>
    /// <returns>Task that completes when join is done</returns>
    public async Task JoinWithCode(string lobbyCode) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);
            ConfigureRelayForClient(joinAllocation);

            KitchenGameMultiplayer.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion

    /// <summary>
    /// Gets the lobby the player is currently in
    /// </summary>
    /// <returns>The current lobby</returns>
    public Lobby GetLobby() {
        return joinedLobby;
    }

    /// <summary>
    /// Deletes the current lobby if the player is in one
    /// Used for when the host wants to leave lobby
    /// </summary>
    public async void DeleteLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    /// <summary>
    /// Leaves the current lobby the player is in
    /// </summary>
    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    /// <summary>
    /// Kicks the player from the current lobby if the player is the host
    /// </summary>
    /// <param name="playerId">The player id that will be removed</param>
    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }


}