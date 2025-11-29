using UnityEngine;
using System.Collections.Generic;
using System;

public class PlateKitchenObject : KitchenObject{

    public event EventHandler<onIngredientAddedEventArgs> onIngredientAdded;
    public class onIngredientAddedEventArgs : EventArgs {
        public KitchenObjectSO kitchenObjectSO;
    }

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;

    private List<KitchenObjectSO> KitchenObjectSOList;

    private void Awake() {
        KitchenObjectSOList = new List<KitchenObjectSO>();
    }
    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO) {
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO)) {
            // Not a valid ingredient
            return false;
        }
        if (KitchenObjectSOList.Contains(kitchenObjectSO)) {
            //Plate already has ingredient, not allowing duplicates
            return false;
        } else {
            KitchenObjectSOList.Add(kitchenObjectSO);
            onIngredientAdded?.Invoke(this, new onIngredientAddedEventArgs {
                kitchenObjectSO = kitchenObjectSO
            });
            return true;
        }
    }

    public List<KitchenObjectSO> GetKitchenObjectSOList() {
        return KitchenObjectSOList;
    }
}
