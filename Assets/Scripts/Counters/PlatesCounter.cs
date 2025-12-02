using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Counter that spawns plates when the user interacts with it
/// </summary>
public class PlatesCounter : BaseCounter {

    /// <summary>
    /// Fires when a plate is spawned.
    /// Specifically used to trigger visual
    /// </summary>
    public event EventHandler OnPlateSpawned;

    /// <summary>
    /// Fires when the user takes a plate from counter
    /// Specifically used to trigger visual
    /// </summary>
    public event EventHandler OnPlateRemoved;

    /// <summary>
    /// Reference of playe kitchen object to spawn it.
    /// </summary>
    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;

    //current time for the plate timer
    private float spawnPlateTimer;

    // spawns new plate every 4 seconds
    private float spawnPlateTimerMax = 4f;

    //current number of plates on the counter
    private int platesSpawnedAmount;

    // the max number of plates that can be spawned at a time
    private int platesSpawnedAmountMax = 4;


    private void Update() {
        if (!IsServer) {
            return;
        }
        // updates timer for plate spawning
        spawnPlateTimer += Time.deltaTime;
        if (spawnPlateTimer > spawnPlateTimerMax) {
            spawnPlateTimer = 0f;

            if (KitchenGameManager.Instance.IsGamePlaying() && platesSpawnedAmount < platesSpawnedAmountMax) {
                SpawnPlateServerRpc();
            }
        }
    }

    /// <summary>
    /// Server that initiates spawning plates.
    /// Calls client to stay synchronized
    /// </summary>
    [Rpc(SendTo.Server)]
    private void SpawnPlateServerRpc() {
        SpawnPlateClientRpc();
    }

    /// <summary>
    /// Runs all clients when new plate is spawned
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnPlateClientRpc() {
        platesSpawnedAmount++;

        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            // Player is empty handed
            if (platesSpawnedAmount > 0) {
                // There's at least one plate here
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                InteractLogicServerRpc();
            }
        }
    }

    /// <summary>
    /// Server that initiates removing a plate.
    /// Calls client to stay synchronized
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    /// <summary>
    /// Runs all clients when a plate is taken away
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void InteractLogicClientRpc() {
        platesSpawnedAmount--;
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }

}