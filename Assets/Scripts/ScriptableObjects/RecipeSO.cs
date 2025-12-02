using System.Collections.Generic;
using UnityEngine;

//A single recipe with the kitchen objects needed to make it
[CreateAssetMenu()]
public class RecipeSO : ScriptableObject {
    
    public List<KitchenObjectSO> kitchenObjectSOList;
    public string recipeName;

}
