using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the visual for the Plates by showing ingredients
/// when those ingredients are added to the plate.
/// </summary>
public class PlateCompleteVisual : MonoBehaviour {

    /// <summary>
    /// Maps a Kitchen object SO to its corresponding game object
    /// </summary>
    [Serializable]
    public struct KitchenObjectSO_GameObject {
        public KitchenObjectSO kitchenObjectSO;
        public GameObject gameObject;
    }

    /// <summary>
    /// Reference to the plate kitchen object
    /// </summary>
    [SerializeField] private PlateKitchenObject plateKitchenObject;

    /// <summary>
    /// List of ingredient mappings to enable the ingredients on the plate
    /// </summary>
    [SerializeField] private List<KitchenObjectSO_GameObject> kitchenObjectSOGameObjectList;

    private void Start() {
        plateKitchenObject.onIngredientAdded += PlateKitchenObject_OnIngredientAdded;
        foreach(KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            kitchenObjectSOGameObject.gameObject.SetActive(false);
        }
    }

    // Fires when an ingredient is added to the plate
    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.onIngredientAddedEventArgs e) {
        foreach(KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            if (kitchenObjectSOGameObject.kitchenObjectSO == e.kitchenObjectSO) {
                kitchenObjectSOGameObject.gameObject.SetActive(true);
            }
        }
        // e.kitchenObjectSO
    }
}
