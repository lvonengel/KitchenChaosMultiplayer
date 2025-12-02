using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the user input for moving, interacting, alternate interacting, and pausing.
/// Also manages key rebindings using Playerprefs
/// </summary>
public class GameInput : MonoBehaviour {

    //player pref to store bindings
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    public static GameInput Instance {get; private set;}

    /// <summary>
    /// Fired when the player interacts with something (default E)
    /// </summary>
    public event EventHandler OnInteractAction;

    /// <summary>
    /// Fired when the player alternate interacts with something (default F)
    /// </summary>
    public event EventHandler OnInteractAlternateAction;

    /// <summary>
    /// Fired when the player pauses the game (default escape)
    /// </summary>
    public event EventHandler OnPauseAction;

    /// <summary>
    /// Fired when the player rebinds a key
    /// Used mostly for UI
    /// </summary>
    public event EventHandler OnBindingRebind;

    /// <summary>
    /// For auto-generated InputActions class
    /// </summary>
    private PlayerInputActions playerInputActions;

    /// <summary>
    /// Keys that the player can rebind
    /// </summary>
    public enum Binding {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause,
    }

    private void Awake() {
        Instance = this;
        
        //if player has saved bindings, then load them
        playerInputActions = new PlayerInputActions();
        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS)) {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }
        playerInputActions.Player.Enable();

        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.InteractAlternate.performed += InteractAlternate_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;
    }

    // unsubscribes from input to prevene memory leaks
    private void OnDestroy() {
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;

        playerInputActions.Dispose();
    }

    // When the player pauses, sends signal
    public void Pause_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        // Checks if null, if not calls function
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    // When the player places/grabs objects, sends signal
    public void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        // Checks if null, if not calls function
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    // When the player cuts objects, sends signal
    public void InteractAlternate_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
        // Checks if null, if not calls function
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    //gets current movement input vector and normalizes it
    public Vector2 GetMovementVectorNormalized() {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();
        
        inputVector = inputVector.normalized;
        return inputVector;
    }

    /// <summary>
    /// Returns a readable string from the current binding
    /// </summary>
    /// <param name="binding">The binding to check</param>
    /// <returns>The readable name for the binding</returns>
    public string GetBindingText(Binding binding) {
        switch (binding) {
            default:
            case Binding.Move_Up:
                return playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return playerInputActions.Player.Interact.bindings[1].ToDisplayString();
            case Binding.Gamepad_InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString();
            case Binding.Gamepad_Pause:
                return playerInputActions.Player.Pause.bindings[1].ToDisplayString();
        }
    }

    /// <summary>
    /// Rebinds a key for the user by saving to PlayerPrefs
    /// </summary>
    /// <param name="binding">The binding to reset</param>
    /// <param name="onActionRebound">Callback when rebinding finishes</param>
    public void RebindBinding(Binding binding, Action onActionRebound) {
        playerInputActions.Player.Disable();
        InputAction inputAction;
        int bindingIndex;
        switch (binding) {
            default:
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;
        }

        //starts rebinding for the binding index
        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {
                callback.Dispose();
                playerInputActions.Player.Enable();
                onActionRebound();
                
                // saves to player prefs so can be loaded when screen changes/game exits
                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }
}
