using UnityEngine;

/// <summary>
/// The counter the player submits orders to.
/// </summary>
public class DeliveryCounter : BaseCounter {

    public static DeliveryCounter Instance {get; private set;}

    private void Awake() {
        Instance = this;
    }

    //validates whether order was correct, and destroys plate
    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                // only deletes plates
                DeliveryManager.Instance.DeliverRecipe(plateKitchenObject);
                KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            }
        }
    }

}
