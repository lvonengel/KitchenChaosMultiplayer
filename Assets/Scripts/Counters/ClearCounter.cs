using UnityEngine;

public class ClearCounter : BaseCounter {

    [SerializeField] private KitchenObjectSO kitchenObjectSO;


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
                        GetKitchenObject().DestroySelf();
                    }
                } else {
                    //player is not holding a plate but something else
                    if (GetKitchenObject().TryGetPlate(out plateKitchenObject)) {
                        // counter is holding a plate
                        if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO())) {
                            player.GetKitchenObject().DestroySelf();
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
