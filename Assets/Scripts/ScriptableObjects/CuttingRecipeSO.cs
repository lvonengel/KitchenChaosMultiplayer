using UnityEngine;

// When a kitchen object is cut on a cutting counter
// connects output with input, and how many cuts it takes to cut
[CreateAssetMenu()]
public class CuttingRecipeSO : ScriptableObject {

    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public int cuttingProgressMax;

}
