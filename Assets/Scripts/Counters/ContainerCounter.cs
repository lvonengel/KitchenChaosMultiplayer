using System;
using UnityEngine;
using Unity.Netcode;


/// <summary>
/// Counter that spawns a single Kitchen object
/// If player is not holding something, then can interact 
/// with container and spawn kitchen object.
/// </summary>
public class ContainerCounter : BaseCounter {

    /// <summary>
    /// Triggers the animation for when the player interacts with container
    /// </summary>
    public event EventHandler OnPlayerGrabbedObject;

    /// <summary>
    /// The kitchen object that the container will spawn
    /// </summary>
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    /// <summary>
    /// Spawns the kitchen object onto the player
    /// </summary>
    /// <param name="player">The player that interacts with the container</param>
    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is not holding anything so gives player kitchen object

            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);

            InteractLogicServerRpc();
        
        }
    }

    /// <summary>
    /// Sends interaction logic to the server.
    /// RPC is invoked after the player grabs an object from the container.
    /// server then relays the interaction to all connected clients.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    /// <summary>
    /// Client-side logic for the interaction.
    /// Invokes event on all clients so animations and visuals stay in sync across multiplayer.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void InteractLogicClientRpc() {
        OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
    }
    

}
