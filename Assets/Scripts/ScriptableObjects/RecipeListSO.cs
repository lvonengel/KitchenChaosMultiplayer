using System.Collections.Generic;
using UnityEngine;

//List of all recipes that the player can deliver
// [CreateAssetMenu()] //commented out to be safe since only need one 
public class RecipeListSO : ScriptableObject {
    
    public List<RecipeSO> recipeSOList;
}
