using UnityEngine;
using System;
using Unity.Netcode;

/// <summary>
/// Counter that player can cut kitchen objects on.
/// </summary>
public class CuttingCounter : BaseCounter, IHasProgress {

    /// <summary>
    /// Fires when any cutting counter does a cut.
    /// Used mostly for sound effects.
    /// </summary>
    public static event EventHandler OnAnyCut;

    // Resets static event to prevent listener duplication
    new public static void ResetStaticData() {
        OnAnyCut = null;
    }

    /// <summary>
    /// Array of all valid kitchen objects that can be placed on the counter.
    /// Takes in the input of SO, and spawns the output.
    /// </summary>
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;

    /// <summary>
    /// Fires when the cutting progress on counter updates.
    /// </summary>
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    /// <summary>
    /// Fires when counter performs an actual cut.
    /// Used specifically for cutting animation.
    /// </summary>
    public event EventHandler OnCut;

    // tracks how many cuts were done on the current object
    private int cuttingProgress;

    /// <summary>
    /// Handles placing kitchen object on the counter and once cuts have reached
    /// max, spawns the cut version of that from cuttingRecipeSO
    /// </summary>
    /// <param name="player">The player interacting with counter</param>
    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // Does not have kitchen object on it
            if (player.HasKitchenObject()) {
                // Player is holding something
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);
                    InteractLogicPlaceObjectOnCounterServerRpc();
                }
            }
        }
        else {
            // There is a kitchen object here
            if (player.HasKitchenObject()) {
                // Player is holding something
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    //player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
            }
            else {
                // Player is not holding anything
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }

    /// <summary>
    /// Sends the logic to server so all clients stay synchronized with starting cutting progress
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicPlaceObjectOnCounterServerRpc() {
        InteractLogicPlaceObjectOnCounterClientRpc();
    }

    /// <summary>
    /// Runs on all clients when a valid object is placed on the counter.
    /// Resets local cutting progress and updates the progress UI.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void InteractLogicPlaceObjectOnCounterClientRpc() {
        cuttingProgress = 0;
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = 0f
        });
    }

    // Cuts a kitchen object if its here
    public override void InteractAlternate(Player player) {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            // There is a kitchen object here and it can be cut
            CutObjectServerRpc();
            TestCuttingProgressDoneServerRpc();
        }
    }

    /// <summary>
    /// Sends request to server to cut kitchen object.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CutObjectServerRpc() {
        // placed here in case client has delay and functions do not line up,
        // avoids null exception for finding kitcheonbject
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            // There is a kitchen object here and it can be cut
            CutObjectClientRpc();
        }
    }

    /// <summary>
    /// Executs cut on all clients and updates progress
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void CutObjectClientRpc() {
        cuttingProgress++;
        OnCut?.Invoke(this, EventArgs.Empty);
        OnAnyCut?.Invoke(this, EventArgs.Empty);

        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = (float)cuttingProgress / cuttingRecipeSO.cuttingProgressMax
        });
    }

    /// <summary>
    /// Server side logic to check whether the cutting progress has reached the
    /// recipe's max cut count. If yes, replaces the current kitchen
    /// object with the recipe's output.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TestCuttingProgressDoneServerRpc() {
        // placed here in case client has delay and functions do not line up,
        // avoids null exception for finding kitcheonbject (since validation is client side)
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            // There is a kitchen object here and it can be cut
            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        
            if (cuttingProgress >= cuttingRecipeSO.cuttingProgressMax) {
                KitchenObjectSO outputKitchenObjectSO = GetOutputForInput(GetKitchenObject().GetKitchenObjectSO());
                
                KitchenObject.DestroyKitchenObject(GetKitchenObject());
                KitchenObject.SpawnKitchenObject(outputKitchenObjectSO, this);
            }
        }
    }

    /// <summary>
    /// // Checks if the given kitchen object has a CuttingRecipeSO (if it can be cut)
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object attempted to place on the counter</param>
    /// <returns>True if it can be cut, else false</returns>
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        return cuttingRecipeSO != null;
    }

    #region getters/setters

    /// <summary>
    /// Goes through list of Cutting Recipes to find the one that matches
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object placed on the counter</param>
    /// <returns>The output of the kitchen object</returns>
    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        if (cuttingRecipeSO != null) {
            return cuttingRecipeSO.output;
        }
        return null;
    }

    /// <summary>
    /// Searches through all cutting recipes and returns the one whose input matches
    /// the given kitchen object. Returns null if no recipe is found.
    /// </summary>
    /// <param name="inputKitchenObjectSO"The kitchen object to get the cutting recipe</param>
    /// <returns>The cutting recipe SO</returns>
    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray) {
            if (cuttingRecipeSO.input == inputKitchenObjectSO) {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    #endregion
}
