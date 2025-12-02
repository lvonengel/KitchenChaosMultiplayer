using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The counter that the player can cook kitchen objects on
/// </summary>
public class StoveCounter : BaseCounter, IHasProgress {

    // reference to frying recipe from uncooked to cooked
    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    
    // reference to frying recipe from cooked to burned
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;

    /// <summary>
    /// Fired whenever stove cooking progress changes
    /// </summary>
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    
    /// <summary>
    /// Fired whenever stove state changes 
    /// </summary>
    public event EventHandler<onStateChangedEventArgs> OnStateChanged;
    public class onStateChangedEventArgs : EventArgs {
        public State state;
    }

    /// <summary>
    /// States that stove can be in
    /// </summary>
    public enum State {
        Idle,
        Frying,
        Fried,
        Burned,
    }

    //current time that the kitchen object has been cooking
    private NetworkVariable<float> fryingTimer = new NetworkVariable<float>(0f);
    
    //current time that the kitchen object has been burning
    private NetworkVariable<float> burningTimer = new NetworkVariable<float>(0f);
    
    // the frying recipe used for the currently cooking object
    private FryingRecipeSO fryingRecipeSO;

    // the frying recipe used for the currently burned object
    private BurningRecipeSO burningRecipeSO;

    // current stove state
    private NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);


    public override void OnNetworkSpawn() {
        fryingTimer.OnValueChanged += FryingTime_OnValueChanged;
        burningTimer.OnValueChanged += BurningTime_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    }

    private void FryingTime_OnValueChanged(float previousValue, float newValue) {
        float fryingTimerMax = fryingRecipeSO != null ? fryingRecipeSO.fryingTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = fryingTimer.Value / fryingTimerMax
        });
    }

    private void BurningTime_OnValueChanged(float previousValue, float newValue) {
        float burningTimerMax = burningRecipeSO != null ? burningRecipeSO.burningTimerMax : 1f;

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
            progressNormalized = burningTimer.Value / burningTimerMax
        });
    }

    private void State_OnValueChanged(State previousState, State newState) {
        OnStateChanged?.Invoke(this, new onStateChangedEventArgs {
            state = state.Value
        });

        if (state.Value == State.Burned || state.Value == State.Idle) {
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs {
                progressNormalized = 0f
            });
        }
    }

    // logic for changing the state for the stove
    public void Update() {
        if (!IsServer) {
            return;
        }
        if (HasKitchenObject()) {
            switch (state.Value) {
                case State.Idle:
                    break;
                case State.Frying:
                    //updates current frying timer
                    fryingTimer.Value += Time.deltaTime;

                    if (fryingTimer.Value > fryingRecipeSO.fryingTimerMax) {
                        // Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);
                        state.Value = State.Fried;
                        burningTimer.Value = 0f;
                        SetBurningRecipeSOClientRpc(
                            KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO())
                        );
                    }
                    break;
                case State.Fried:
                    //updates current burned timer
                    burningTimer.Value += Time.deltaTime;

                    if (burningTimer.Value > burningRecipeSO.burningTimerMax) {
                        // Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);
                        state.Value = State.Burned;

                    }
                    break;
                case State.Burned:
                    break;
            }
        }

    }

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            // Does not have kitchen object on it
            if (player.HasKitchenObject()) {
                // Player is holding something that can be fried
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);

                    InteractLogicPlaceObjectOnCounterServerRpc(
                        KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO()));
                }
            } else {
                // Player not carring anything
            }
        }
        else {
            // There is a kitchen object here
            if (player.HasKitchenObject()) {
                // Player is holding something
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    //player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        SetStateIdleServerRpc();
                    }
                }
            }
            else {
                // Player is not holding anything
                GetKitchenObject().SetKitchenObjectParent(player);
                SetStateIdleServerRpc();
            }
        }
    }

    /// <summary>
    /// Server to start frying a placed kitchen object
    /// </summary>
    /// <param name="kitchenObjectSOIndex">The index of the kitchenobjectSO List of kitchen objects</param>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void InteractLogicPlaceObjectOnCounterServerRpc(int kitchenObjectSOIndex) {
        // resets frying timer and sets state to frying
        fryingTimer.Value = 0f;
        state.Value = State.Frying;
        SetFryingRecipeSOClientRpc(kitchenObjectSOIndex);
    }

    #region getters/setters

    /// <summary>
    /// Server to set the stove state back to idle.
    /// Used if kitchen object is finished cooking or if nothing is on stove
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetStateIdleServerRpc() {
        state.Value = State.Idle;
    }

    /// <summary>
    /// Client that assigns current frying recipe based on index.
    /// </summary>
    /// <param name="kitchenObjectSOIndex">The index of the kitchen object SO</param>
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetFryingRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }

    /// <summary>
    /// Client that assigns the current burning recipe based on the kitchen object index
    /// makes sure all clients track burn progression
    /// </summary>
    /// <param name="kitchenObjectSOIndex">The index of the kitchen object SO</param>
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetBurningRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
    }


    /// <summary>
    /// Checks if the given kitchen object has a CuttingRecipeSO (if it can be cut)
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object SO to be checked</param>
    /// <returns>True if the given kitchen object has a frying recipe</returns>
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        return fryingRecipeSO != null;
    }

    /// <summary>
    /// Goes through list of Cutting Recipes to find the one that matches
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object SO to be checked</param>
    /// <returns>Kitchen object out with the given input</returns>
    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        if (fryingRecipeSO != null) {
            return fryingRecipeSO.output;
        }
        return null;
    }

    /// <summary>
    /// Goes through all frying recipes to find the one whose input matches the given kitchen object
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object SO to be checked</param>
    /// <returns>Frying recipe for that given kitchen object</returns>
    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray) {
            if (fryingRecipeSO.input == inputKitchenObjectSO) {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    /// <summary>
    /// Goes through all burned recipes to find the one whose input matches the given
    /// </summary>
    /// <param name="inputKitchenObjectSO">The kitchen object SO to be checked</param>
    /// <returns>Burning recipe for that given kitchen object</returns>
    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray) {
            if (burningRecipeSO.input == inputKitchenObjectSO) {
                return burningRecipeSO;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if the stove state is fried
    /// </summary>
    /// <returns>True if the stove state is fried</returns>
    public bool IsFried() {
        return state.Value == State.Fried;
    }

    #endregion

}