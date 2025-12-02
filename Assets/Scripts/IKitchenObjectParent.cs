using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Interface for any object that is a parent to a kitchen object
/// This is for counters, players, and plates.
/// 
/// Lets player attatch, remove, or clear the kitchen object
/// </summary>
public interface IKitchenObjectParent {

    /// <summary>
    /// Gets the transform that a kitchen object is following
    /// </summary>
    /// <returns>The follow transform used by kitchen object</returns>
    public Transform GetKitchenObjectFollowTransform();

    /// <summary>
    /// Sets the kitchen object to this parent
    /// </summary>
    /// <param name="kitchenObject">The kitchen object that will be assigned</param>
    public void SetKitchenObject(KitchenObject kitchenObject);

    /// <summary>
    /// Gets the kitchen object on this parent
    /// </summary>
    /// <returns>The kitchen object</returns>
    public KitchenObject GetKitchenObject();

    /// <summary>
    /// Gets rid of the kitchen object attached to this
    /// </summary>
    public void ClearKitchenObject();

    /// <summary>
    /// Checks whether this parent has a kitchen object
    /// </summary>
    /// <returns>Returns true if it does, otherwise false</returns>
    public bool HasKitchenObject();

    /// <summary>
    /// Gets the network object associated with the parent
    /// </summary>
    /// <returns>The network object for this parent</returns>
    public NetworkObject GetNetworkObject();
}
