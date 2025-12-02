using UnityEngine;

// When a kitchen object is placed on a stove
// connects output with input, and how long it takes to cook
[CreateAssetMenu()]
public class FryingRecipeSO : ScriptableObject {

    public KitchenObjectSO input;
    public KitchenObjectSO output;
    public float fryingTimerMax;

}
