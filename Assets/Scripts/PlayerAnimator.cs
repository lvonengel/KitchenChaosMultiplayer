using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls the player's animation. Specifically used
/// for when the player is walking.
/// </summary>
public class PlayerAnimator : NetworkBehaviour {
    
    /// <summary>
    /// Trigger for the walking animation
    /// </summary>
    private const string IS_WALKING = "IsWalking";

    /// <summary>
    /// Reference to the player to animate
    /// </summary>
    [SerializeField] private Player player;

    private Animator animator;


    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        if (!IsOwner) {
            return;
        }
        //sets animation trigger
        animator.SetBool(IS_WALKING, player.IsWalking());
    }
}
