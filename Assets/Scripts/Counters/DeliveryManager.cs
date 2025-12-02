using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages the recipe orders that need to be deilvered.
/// Spawns new orders and checks delivered recipes against the current request recipes.
/// </summary>
public class DeliveryManager : NetworkBehaviour {

    public static DeliveryManager Instance {get; private set;}

    /// <summary>
    /// List of all possible recipes that can be ordered
    /// </summary>
    [SerializeField] private RecipeListSO recipeListSO;


    /// <summary>
    /// Fired whenever a new recipe is created
    /// </summary>
    public event EventHandler OnRecipeSpawned;

    /// <summary>
    /// Fired whenever a recipe is completed
    /// </summary>
    public event EventHandler OnRecipeCompleted;

    /// <summary>
    /// Fired whenever a recipe is delivered correctly
    /// </summary>
    public event EventHandler OnRecipeSuccess;

    /// <summary>
    /// Fired whenever a recipe is delivered incorrectly
    /// </summary>
    public event EventHandler OnRecipeFailed;
    
    /// <summary>
    /// Current list of all recipes that are currently ordered
    /// </summary>
    private List<RecipeSO> waitingRecipeSOList;

    // timer for current time between recipes
    private float spawnRecipeTimer = 4f;

    // max time between recipes spawning
    private float spawnRecipeTimerMax = 4f;

    // max recipes that can be ordered at one time
    private int waitingRecipesMax = 4;

    // number of recipes that are successfully delivred
    private int successfulRecipesAmount;

    private void Awake() {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Update() {
        if (!IsServer) {
            return;
        }
        //spawns new recipe if the max recipes ordered is not met
        spawnRecipeTimer -= Time.deltaTime;
        if (spawnRecipeTimer <= 0f) {
            spawnRecipeTimer = spawnRecipeTimerMax;
            
            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipesMax) {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    /// <summary>
    /// Client that spawns new recipes based on the index of the given recipe
    /// </summary>
    /// <param name="waitingRecipeSOIndex">Index of the recipe in the recipe list</param>
    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex) {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];
        waitingRecipeSOList.Add(waitingRecipeSO);
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Checks if the delivered recipe is correct or not.
    /// Checks all the ingredients to each ordered recipe.
    /// </summary>
    /// <param name="plateKitchenObject">The plate that is being delivered</param>
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject) {
        for (int i = 0; i < waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) {
                // has the same number of ingredients
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // going through all ingredients in the recipe
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // going through all ingredients in the plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            //Ingredient matches!
                            ingredientFound = true;
                            break;
                        }
                    }
                    //
                    if (!ingredientFound) {
                        // this recipe ingredient was not found on the plate
                        plateContentsMatchesRecipe = false;
                    }
                }
                if (plateContentsMatchesRecipe) {
                    //player delivered the correct recipe
                    DeliverCorrectRecipeServerRpc(i);
                    return;
                }
            }
        }

        // No matches found
        //player did not deliver correct recipe
        DeliverInCorrectRecipeServerRpc();
    }

    /// <summary>
    /// Server that handles if an incorrect recipe was delivered
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DeliverInCorrectRecipeServerRpc() {
        DeliverInCorrectRecipeClientRpc();
    }

    /// <summary>
    /// Client so all clients known when an incorrect recipe was delivered.
    /// </summary>
    [Rpc(SendTo.ClientsAndHost)]
    private void DeliverInCorrectRecipeClientRpc() {
        Debug.Log("Player did not deliver a correct recipe");
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Server that handles if an correct recipe was delivered for the given index
    /// </summary>
    /// <param name="waitingRecipeSOListIndex">Index of the completed recipe in the list</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOListIndex) {
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    /// <summary>
    /// Client that removes the recipe from the ordered list and manages success
    /// </summary>
    /// <param name="waitingRecipeSOListIndex"></param>
    [Rpc(SendTo.ClientsAndHost)]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex) {
        successfulRecipesAmount++;
        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    #region setters/getters

    /// <summary>
    /// Gets the list of ordered recipes
    /// </summary>
    /// <returns>List of ordered recipes</returns>
    public List<RecipeSO> GetWaitingRecipeSOList() {
        return waitingRecipeSOList;
    }

    /// <summary>
    /// Gets the number of sucessful deliveries
    /// </summary>
    /// <returns>Number of successful deliveres</returns>
    public int GetSuccessfulRecipesAmount() {
        return successfulRecipesAmount;
    }

    #endregion

}
