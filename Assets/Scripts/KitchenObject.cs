using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents a kitchen object (plate, food)
/// </summary>
public class KitchenObject : NetworkBehaviour {

    /// <summary>
    /// Defines type of kitchen object
    /// </summary>
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    /// <summary>
    /// Target transform the kitchen object follows
    /// </summary>
    private FollowTransform followTransform;

    /// <summary>
    /// Parent that holds the kitchen object
    /// </summary>
    private IKitchenObjectParent kitchenObjectParent;

    protected virtual void Awake() {
        followTransform = GetComponent<FollowTransform>();
    }

    /// <summary>
    /// Gets kitchen object SO
    /// </summary>
    /// <returns>Kitchen object SO</returns>
    public KitchenObjectSO GetKitchenObjectSO() {
        return kitchenObjectSO;
    }

    /// <summary>
    /// Gets kitchen object parent
    /// </summary>
    /// <returns>kitchen object parent</returns>
    public IKitchenObjectParent GetKitchenObjectParent() {
        return kitchenObjectParent;
    }

    /// <summary>
    /// Sets new parent for the kitchen object
    /// </summary>
    /// <param name="kitchenObjectParent">The parent to assign kitchen object to</param>
    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent) {
        SetKitchenObjectParentServerRpc(kitchenObjectParent.GetNetworkObject());
    }

    /// <summary>
    /// Server that calls client to change parent
    /// </summary>
    /// <param name="kitchenObjectParentNetworkObjectReference">Network object to the parent</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetKitchenObjectParentServerRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObjectReference);
    }

    /// <summary>
    /// Client RPC that updates parent of the kitchen object to all clients
    /// </summary>
    /// <param name="kitchenObjectParentNetworkObjectReference"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void SetKitchenObjectParentClientRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference) {
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        if (this.kitchenObjectParent != null) {
            this.kitchenObjectParent.ClearKitchenObject();
        }
        this.kitchenObjectParent = kitchenObjectParent;

        if (kitchenObjectParent.HasKitchenObject()) {
            Debug.LogError("IKitchenObjectParent already has a kitchen object");
        }
        kitchenObjectParent.SetKitchenObject(this);

        followTransform.SetTargetTransform(kitchenObjectParent.GetKitchenObjectFollowTransform());
    }

    /// <summary>
    /// Destroys the game object locally
    /// </summary>
    public void DestroySelf() {
        Destroy(gameObject);
    }

    /// <summary>
    /// Clears the kitchen object's for the parent
    /// </summary>
    public void ClearKitchenObjectOnParent() {
        kitchenObjectParent.ClearKitchenObject();
    }

    /// <summary>
    /// Destroys the kitchen object for multiplayer
    /// </summary>
    /// <param name="kitchenObject">The kitchen object to be destroyed</param>
    public static void DestroyKitchenObject(KitchenObject kitchenObject) {
        KitchenGameMultiplayer.Instance.DestroyKitchenObject(kitchenObject);
    }

    /// <summary>
    /// Attempts to get kitchen object as a plate
    /// </summary>
    /// <param name="plateKitchenObject">Outputs plate if this object is a plate</param>
    /// <returns>True if this object is a plate, otherwise false</returns>
    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject) {
        if (this is PlateKitchenObject) {
            plateKitchenObject = this as PlateKitchenObject;
            return true;
        }
        else {
            plateKitchenObject = null;
            return false;
        }
    }

    /// <summary>
    /// Spawns a kitchen object to the given parent
    /// </summary>
    /// <param name="kitchenObjectSO">The kitchen object SO to spawn</param>
    /// <param name="kitchenObjectParent">The parent to hold kitchen object</param>
    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent) {
        KitchenGameMultiplayer.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent);
    }

}
