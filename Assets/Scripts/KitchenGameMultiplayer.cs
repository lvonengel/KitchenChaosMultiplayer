using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages multiplayer for the game. This includes hosting/joining,
/// player data, spawning/destroying kitchen objects
/// </summary>
public class KitchenGameMultiplayer : NetworkBehaviour {

    //The max player amount
    public const int MAX_PLAYER_AMOUNT = 4;

    //PlayerPrefs key to store local player's multiplayer name
    public const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    public static KitchenGameMultiplayer Instance { get; private set;}

    /// <summary>
    /// Reference for all available kitchen objects
    /// </summary>
    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

    /// <summary>
    /// List of all colors the player can select to be
    /// </summary>
    [SerializeField] private List<Color> playerColorList;

    /// <summary>
    /// Fired when a client attempts to join a game
    /// </summary>
    public event EventHandler OnTryingToJoinGame;

    /// <summary>
    /// Fired when a client fails to join a game
    /// </summary>
    public event EventHandler OnFailedToJoinGame;

    /// <summary>
    /// Fired when a player joins/leaves
    /// </summary>
    public event EventHandler OnPlayerDataNetworkListChanged;

    /// <summary>
    /// Network list of player data
    /// </summary>
    private NetworkList<PlayerData> playerDataNetworkList;

    /// <summary>
    /// The local player name
    /// </summary>
    private string playerName;

    private void Awake() {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //loads the players name
        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += playerDataNetworkList_OnListChanged;
    }

    /// <summary>
    /// Gets the local player's display name
    /// </summary>
    /// <returns>Local player's display name</returns>
    public string GetPlayerName() {
        return playerName;
    }

    /// <summary>
    /// Sets the local player's display name
    /// </summary>
    /// <param name="playerName">The new name</param>
    public void SetPlayerName(string playerName) {
        this.playerName = playerName;
        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    /// <summary>
    /// Fired when the network player data list changes
    /// </summary>
    /// <param name="changeEvent">The changes to the list</param>
    private void playerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent) {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    #region connection

    public void StartHost() {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Connection approval used by game to accept or reject new player.
    /// </summary>
    /// <param name="connectionApprovalRequest">Incoming connection request data</param>
    /// <param name="connectionApprovalResponse">Response that will be sent back</param>
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse) {
        if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString()) {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT) {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        connectionApprovalResponse.Approved = true;

    }

    /// <summary>
    /// Server when a client connects. Adds them to playerdata and 
    /// initializes name and id
    /// </summary>
    /// <param name="clientId"></param>
    private void NetworkManager_OnClientConnectedCallback(ulong clientId) {
        playerDataNetworkList.Add(new PlayerData {
            clientId = clientId,
            colorId = GetFirstUnusedColorId(),
        });
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    /// <summary>
    /// Server when a client disconncets. Removes the playerdata from the network list
    /// </summary>
    /// <param name="clientId">The client id that disconnected</param>
    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId) {
        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId) {
                //Disconnected
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Starts the game as a client and initiates client connection
    /// </summary>
    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        NetworkManager.Singleton.OnClientConnectedCallback += Network_Client_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += Network_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>
    /// Fires when client successfully connects. Sends player name and id to server
    /// </summary>
    /// <param name="clientId">Client id that connected</param>
    private void Network_Client_OnClientConnectedCallback(ulong clientId) {
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    /// <summary>
    /// Server RPC that sets name of player associated with calling client
    /// </summary>
    /// <param name="playerName">The name to assign to the player</param>
    /// <param name="rpcParams">The client that is changing names</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerNameServerRpc(string playerName, RpcParams rpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);
        // must grab it, modify, then update it
        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    /// <summary>
    /// Server RPC that sets id of player associated with calling client
    /// </summary>
    /// <param name="playerId">The id to assign to the player</param>
    /// <param name="rpcParams">The client that is changing id</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerIdServerRpc(string playerId, RpcParams rpcParams = default) {
        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);
        // must grab it, modify, then update it
        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    /// <summary>
    /// Fires when client fails to connect or disconnects
    /// </summary>
    /// <param name="clientId">Client id that disconnected</param>
    private void Network_Client_OnClientDisconnectCallback(ulong clientId) {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region kitchen object

    /// <summary>
    /// Spawns a network kitchen object and assigns to parent
    /// </summary>
    /// <param name="kitchenObjectSO">The type of kitchen object to spawn</param>
    /// <param name="kitchenObjectParent">The parent to assign it to</param>
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    /// <summary>
    /// Server RPC that spawns the kitchen object and assigns to parent
    /// </summary>
    /// <param name="kitchenObjectSOIndex">Index of the kitchen object</param>
    /// <param name="kitchenObjectParentNetworkObjectReference">Reference to parent object</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        //placed to handle if client has delay and this function is continously called
        if (kitchenObjectParent.HasKitchenObject()) {
            //Parent already spawned an object
            return;
        }

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);
        
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();


        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    /// <summary>
    /// Gets index of the kitchen object SO in the list
    /// </summary>
    /// <param name="kitchenObjectSO">Kitchen object SO to be checked</param>
    /// <returns>Index of the object in the list, if not found, returns -1</returns>
    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    /// <summary>
    /// Gets kitchen object so from the index
    /// </summary>
    /// <param name="kitchenObjectSOIndex">Index in the kitchen object list</param>
    /// <returns>Corresponding kitchen object SO</returns>
    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    /// <summary>
    /// Destroys the kitchen object
    /// </summary>
    /// <param name="kitchenObject"></param>
    public void DestroyKitchenObject(KitchenObject kitchenObject) {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    /// <summary>
    /// Server RPC that destroys a kitchen object and clears parent
    /// </summary>
    /// <param name="kitchenObjectNetworkObjectReference"></param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        //must have check if user spams it with delay
        if (kitchenObjectNetworkObject == null) {
            //object is already destroyed
            return;
        }
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        ClearKitchenObjectOnParentClientRpc(kitchenObjectNetworkObjectReference);
        kitchenObject.DestroySelf();
    }

    /// <summary>
    /// Client RPC that clears the kitchen object reference on the parent object
    /// </summary>
    /// <param name="kitchenObjectNetworkObjectReference"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference) {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        kitchenObject.ClearKitchenObjectOnParent();
    }

    #endregion

    /// <summary>
    /// Checks if the player index is a connected player
    /// </summary>
    /// <param name="playerIndex">The player index to check</param>
    /// <returns>True if player is connected, otherwise false</returns>
    public bool IsPlayerIndexConnected(int playerIndex) {
        return playerIndex < playerDataNetworkList.Count;
    }

    /// <summary>
    /// Gets index in player data network list for a given client
    /// </summary>
    /// <param name="clientId">Client id to check</param>
    /// <returns>index of the player's data, otherwise -1</returns>
    public int GetPlayerDataIndexFromClientId(ulong clientId) {
        for (int i = 0; i < playerDataNetworkList.Count; i++) {
            if (playerDataNetworkList[i].clientId == clientId) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Gets the player data associated with given client id
    /// </summary>
    /// <param name="clientId">The client id to check</param>
    /// <returns>Corresponding player data</returns>
    public PlayerData GetPlayerDataFromClientId(ulong clientId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.clientId == clientId) {
                return playerData;
            }
        }
        return default;
    }

    /// <summary>
    /// Gets player data for local client
    /// </summary>
    /// <returns>player data</returns>
    public PlayerData GetPlayerData() {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }
    
    /// <summary>
    /// Gets player data at a given player index
    /// </summary>
    /// <param name="playerIndex">player index to check</param>
    /// <returns>Player data at the given index</returns>
    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) {
        return playerDataNetworkList[playerIndex];
    }

    #region color 
    /// <summary>
    /// Gets the player color
    /// </summary>
    /// <param name="colorId">The color id</param>
    /// <returns>The corresponding color</returns>
    public Color GetPlayerColor(int colorId) {
        return playerColorList[colorId];
    }

    /// <summary>
    /// Changes the local player's color
    /// </summary>
    /// <param name="colorId">The color Id to change to</param>
    public void ChangePlayerColor(int colorId) {
        ChangePlayerColorServerRpc(colorId);
    }

    /// <summary>
    /// Server RPC that changes the color id for the player if its available
    /// </summary>
    /// <param name="colorId">The color id to cahnge to</param>
    /// <param name="rpcParams">The client requesting this</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ChangePlayerColorServerRpc(int colorId, RpcParams rpcParams = default) {
        if (!IsColorAvailable(colorId)) {
            // Colors not available
            return;
        }
        int playerDataIndex = GetPlayerDataIndexFromClientId(rpcParams.Receive.SenderClientId);
        // must grab it, modify, then update it
        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    /// <summary>
    /// Checks if a color id is available (not used by other players)
    /// </summary>
    /// <param name="colorId">The color id to check</param>
    /// <returns>True if its available, otherwise false</returns>
    private bool IsColorAvailable(int colorId) {
        foreach (PlayerData playerData in playerDataNetworkList) {
            if (playerData.colorId == colorId) {
                //Colors already in use
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets the first unused color id in the player color list
    /// </summary>
    /// <returns>The index of an available color, otherwise -1</returns>
    private int GetFirstUnusedColorId() {
        for (int i = 0; i < playerColorList.Count; i++) {
            if (IsColorAvailable(i)) {
                return i;
            }
        }
        return -1;
    }

    #endregion

    /// <summary>
    /// Kicks a player from the game by disconnecting their client
    /// and deleting their player data entry
    /// </summary>
    /// <param name="clientId"></param>
    public void KickPlayer(ulong clientId) {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }

}