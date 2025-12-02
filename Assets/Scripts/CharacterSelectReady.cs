using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Tracks which players are ready in the character select scene.
/// When all players are ready, starts the game.
/// </summary>
public class CharacterSelectReady : NetworkBehaviour {
    public static CharacterSelectReady Instance {get; private set;}

    /// <summary>
    /// Maps each client Id to their ready state
    /// True if ready, false otherwise
    /// </summary>
    private Dictionary<ulong, bool> playerReadyDictionary;

    /// <summary>
    /// Fired whenever a player's ready state changes
    /// Used mostly for visuals
    /// </summary>
    public event EventHandler OnReadyChanged;

    private void Awake() {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
    } 

    #region setters/getters

    public void SetPlayerReady() {
        SetPlayerReadyServerRpc();
    }

    /// <summary>
    /// Server that marks which client is ready.
    /// Checks if everyone is ready, and starts game
    /// </summary>
    /// <param name="rpcParams">Contains the info from sender</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetPlayerReadyServerRpc(RpcParams rpcParams = default) {
        SetPlayReadyClientRpc(rpcParams.Receive.SenderClientId);
        playerReadyDictionary[rpcParams.Receive.SenderClientId] = true;
        
        bool allClientsReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds) {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId]) {
                // This player is not ready
                allClientsReady = false;
                break;
            }
        }
        // if everyone's ready, starts game
        if (allClientsReady) {
            KitchenGameLobby.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    /// <summary>
    /// Client that sets specific client as ready
    /// </summary>
    /// <param name="clientId">The client who just readied up</param>
    [Rpc(SendTo.ClientsAndHost)]
    private void SetPlayReadyClientRpc(ulong clientId) {
        playerReadyDictionary[clientId] = true;
        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if a player is ready
    /// </summary>
    /// <param name="clientId">The client who will be checked</param>
    /// <returns>True if client is ready, otherwise false</returns>
    public bool IsPlayerReady(ulong clientId) {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
    #endregion

}