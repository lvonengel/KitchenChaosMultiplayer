using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Base class for all counter types in the kitchen.
/// Provides common logic for interaction, object placement,
/// and maintaining a reference to the sitting on it.
/// </summary>
public class BaseCounter : NetworkBehaviour, IKitchenObjectParent {

    /// <summary>
    /// Fired whenever any counter places a kitchen object on top.
    /// </summary>
    public static event EventHandler OnAnyObjectPlacedHere;

    // Point on counter that spawns kitchen objects
    [SerializeField] private Transform counterTopPoint;

    /// <summary>
    /// Clears all listeners
    /// </summary>
    public static void ResetStaticData() {
        OnAnyObjectPlacedHere = null;
    }

    //object currently on the counter
    private KitchenObject kitchenObject;

    /// <summary>
    ///  Base Interact when players pick up and drop off objects
    /// Happens when user presses 'E'
    /// </summary>
    /// <param name="player">The player interacting with the counter.</param>
    public virtual void Interact(Player player) {
        Debug.LogError("BaseCounter.Interact();");
    }

    /// <summary>
    /// The action applied to a kitchen object on a counter.
    /// Happens when user presses 'F'
    /// Ex: Chopping, frying
    /// </summary>
    /// <param name="player">The player interacting with the counter.</param>
    public virtual void InteractAlternate(Player player) {
        // Debug.LogError("BaseCounter.InteractAlternate();");
    }

    #region Getters/Setters

    /// <summary>
    /// Gets the Transform that kitchen objects should move toward.
    /// </summary>
    /// <returns>The counter top point.</returns>
    public Transform GetKitchenObjectFollowTransform() {
        return counterTopPoint;
    }

    /// <summary>
    /// Assigns a Kitchen object to this counter
    /// </summary>
    /// <param name="kitchenObject">The kitchen object to set onto the counter</param>
    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the kitchen object on the counter
    /// </summary>
    /// <returns>Kitchen object on the counter</returns>
    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    /// <summary>
    /// Clears the current kitchen object from this counter
    /// </summary>
    public void ClearKitchenObject() {
        kitchenObject = null;
    }
    
    /// <summary>
    /// Checks if counter has a kitchen object
    /// </summary>
    /// <returns>True if the kitchen object is not null</returns>
    public bool HasKitchenObject() {
        return kitchenObject != null;
    }

    /// <summary>
    /// Gets the network object
    /// </summary>
    /// <returns>The network object</returns>
    public NetworkObject GetNetworkObject() {
        return NetworkObject;
    }

    #endregion
}
