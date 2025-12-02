using UnityEngine;

// When a kitchen object is cut on a cutting counter
// connects output with input, and how long it takes to burn
[CreateAssetMenu()]
public class BurningRecipeSO : ScriptableObject {

    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float burningTimerMax;

}
