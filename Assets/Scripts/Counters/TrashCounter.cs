using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The counter that destroys whatever is in the players 
/// </summary>
public class TrashCounter : BaseCounter {

    /// <summary>
    /// Fired whenever any trash is interacted with
    /// </summary>   
    public static event EventHandler OnAnyObjectTrashed;

    // makes sure that if a player is delayed, that it won't crash game
    new public static void ResetStaticData() {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());

            InteractLogicServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void InteractLogicClientRpc() {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}