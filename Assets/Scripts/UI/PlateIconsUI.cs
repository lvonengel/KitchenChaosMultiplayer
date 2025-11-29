using Unity.VisualScripting;
using UnityEngine;

public class PlateIconsUI : MonoBehaviour{

    [SerializeField] private PlateKitchenObject PlateKitchenObject;
    [SerializeField] private Transform iconTemplate;

    private void Awake() {
        iconTemplate.gameObject.SetActive(false);
    }

    private void Start() {
        PlateKitchenObject.onIngredientAdded += PlateKitchenObject_OnIngredientAdded;
    }

    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.onIngredientAddedEventArgs e) {
        UpdateVisual();
    }

    private void UpdateVisual() {
        foreach (Transform child in transform) {
            if (child == iconTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach (KitchenObjectSO kitchenObjectSO in PlateKitchenObject.GetKitchenObjectSOList()) {
            Transform IconTransform = Instantiate(iconTemplate, transform);
            IconTransform.gameObject.SetActive(true);
            IconTransform.GetComponent<PlateIconsSingleUI>().SetKitchenObjectSO(kitchenObjectSO);
        }
    }

}
