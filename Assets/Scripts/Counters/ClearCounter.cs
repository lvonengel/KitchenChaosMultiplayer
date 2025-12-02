using UnityEngine;

/// <summary>
/// Class for all empty counters.
/// Kitchen objects can be placed on it.
/// </summary>
public class ClearCounter : BaseCounter {

    /// <summary>
    /// Interaction for placing and picking kitchen objects onto it.
    /// Can also combine existing kitchen object with plate.
    /// </summary>
    /// <param name="player">The player interacting with the counter</param>
    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // Does not have kitchen object on it
            if (player.HasKitchenObject()) {
                // Player is holding something
                player.GetKitchenObject().SetKitchenObjectParent(this);
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
                } else {
                    //player is not holding a plate but something else
                    if (GetKitchenObject().TryGetPlate(out plateKitchenObject)) {
                        // counter is holding a plate
                        if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO())) {
                            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
                        }
                    }
                }
            } else {
                // Player is not holding anything
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }
    
}
