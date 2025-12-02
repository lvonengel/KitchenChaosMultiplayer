using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents the player in the game.
/// Handles movement, interaction, input listening, and holding kitchen objects.
/// </summary>
public class Player : NetworkBehaviour, IKitchenObjectParent {

    /// <summary>
    /// Fired when any player is spawned
    /// </summary>
    public static event EventHandler OnAnyPlayerSpawned;

    /// <summary>
    /// Fired when any player successfully picks up a kitchen object
    /// </summary>
    public static event EventHandler OnAnyPickedSomething;

    //resets static data to prevent duplicates and memory leaks
    public static void ResetStaticData() {
        OnAnyPlayerSpawned = null;
    }

    public static Player LocalInstance { get; private set; }

    /// <summary>
    /// Fired when local player successfully picks up a kitchen object
    /// </summary>
    public event EventHandler OnPickedSomething;

    /// <summary>
    /// Fired whenever the currently selected counter changes locally
    /// </summary>
    public event EventHandler<onSelectedCounterChangedEventArgs> onSelectedCounterChanged;
    public class onSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter selectedCounter;
    }

    // the player movement speed
    [SerializeField] private float moveSpeed = 7f;
    // [SerializeField] private GameInput gameInput;

    //layer mask so player does not run into counters
    [SerializeField] private LayerMask countersLayerMask;

    //layer mask so player does not run into other players
    [SerializeField] private LayerMask collisionsLayerMask;
    
    // the transform the player holds kitchen objects
    [SerializeField] private Transform kitchenObjectHoldPoint;

    // the list where multiple players get spawned
    [SerializeField] private List<Vector3> spawnPositionList;
    [SerializeField] private PlayerVisual playerVisual;

    // whether the local player is walking
    private bool isWalking;

    //cache of the last local interaction direction
    private Vector3 lastInteractDir;

    /// <summary>
    /// The counter the local player has currently selected
    /// </summary>
    private BaseCounter selectedCounter;

    /// <summary>
    /// The kitchen object the local player currently holds
    /// </summary>
    private KitchenObject kitchenObject;


    private void Start() {
        GameInput.Instance.OnInteractAction += GameInput_OnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
        PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromClientId(OwnerClientId);
        playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
    }

    // Spawns player when network starts
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            LocalInstance = this;
        }
        transform.position = spawnPositionList[KitchenGameMultiplayer.Instance.GetPlayerDataIndexFromClientId(OwnerClientId)];
        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);

        if (IsServer) {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }

    // Fires when a player disconnects from lobby
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId) {
        if (clientId == OwnerClientId && HasKitchenObject()) {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }

    // Fires when player interacts
    private void GameInput_OnInteractAction(object sender, System.EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        
        if (selectedCounter != null) {
            selectedCounter.Interact(this);
        }
    }

    // Fires when player alternate interacts
    private void GameInput_OnInteractAlternateAction(object sender, System.EventArgs e) {
        if (!KitchenGameManager.Instance.IsGamePlaying()) return;
        
        if (selectedCounter != null) {
            selectedCounter.InteractAlternate(this);
        }
    }

    // Update is called once per frame
    private void Update() {
        if (!IsOwner) {
            return;
        }
        HandleMovement();
        HandleInteractions();
    }

    public bool IsWalking() {
        return isWalking;
    }

    /// <summary>
    /// Does raycast based on movement direction to determine which counter to interact with
    /// </summary>
    private void HandleInteractions() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        // Even if you're not actively moving forward into object, will still interact with it
        if (moveDir != Vector3.zero) {
            lastInteractDir = moveDir;
        }

        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask)) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            }
            else {
                SetSelectedCounter(null);
            }
        }
        else {
            // Raycast does not hit anything
            SetSelectedCounter(null);
        }
    }

    /// <summary>
    /// Moves the player using BoxCast and updates whether the player is walking.
    /// </summary>

    private void HandleMovement() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();

        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);

        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = .7f;
        // float playerHeight = 2f;
        bool canMove = !Physics.BoxCast(
            transform.position, Vector3.one * playerRadius, moveDir, Quaternion.identity, moveDistance, collisionsLayerMask);

        if (!canMove) {
            // Cannot move towards moveDir

            // Attempt x movement only
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > +.5f) && !Physics.BoxCast(
                transform.position, Vector3.one * playerRadius, moveDirX, Quaternion.identity, moveDistance, collisionsLayerMask);

            if (canMove) {
                // Can move only on the x
                moveDir = moveDirX;
            }
            else {
                // Cannot move only in the x

                // Attempy z movement only
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > +.5f) && !Physics.BoxCast(
                    transform.position, Vector3.one * playerRadius, moveDirZ, Quaternion.identity, moveDistance, collisionsLayerMask);

                if (canMove) {
                    // Can only move in the Z
                    moveDir = moveDirZ;
                }
                else {
                    //Cannot move in any direction
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * moveDistance;
        }

        isWalking = moveDir != Vector3.zero;

        float rotateSpeed = 13f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    #region getters/setters

    /// <summary>
    /// Sets the currently selected counter
    /// </summary>
    /// <param name="selectedCounter">The selected counter</param>
    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this.selectedCounter = selectedCounter;
        // assigns selected counter to the field onSelectedCounterChangedEventArgs
        onSelectedCounterChanged?.Invoke(this, new onSelectedCounterChangedEventArgs {
            selectedCounter = selectedCounter
        });
    }

    /// <summary>
    /// Gets the transform where held kitchen objects should follow
    /// </summary>
    /// <returns>Transform that kitchen objects are</returns>
    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    /// <summary>
    /// Sets the kitchen object to the player
    /// </summary>
    /// <param name="kitchenObject">The kitchen object the player holds</param>
    public void SetKitchenObject(KitchenObject kitchenObject) {
        this.kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnPickedSomething?.Invoke(this, EventArgs.Empty);
            OnAnyPickedSomething?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the currently held kitchen object
    /// </summary>
    /// <returns>Currently held kitchen object</returns>
    public KitchenObject GetKitchenObject() {
        return kitchenObject;
    }

    /// <summary>
    /// Clears the held kitchen object from player
    /// </summary>
    public void ClearKitchenObject() {
        kitchenObject = null;
    }
    
    /// <summary>
    /// Checks whether the player is holding a kitchen object
    /// </summary>
    /// <returns>True if the player is holding a kitchen object, false otherwise</returns>
    public bool HasKitchenObject() {
        return kitchenObject != null;
    }

    /// <summary>
    /// Gets the local player's network object
    /// </summary>
    /// <returns>Local player's network object</returns>
    public NetworkObject GetNetworkObject() {
        return NetworkObject;
    }

    #endregion
}
