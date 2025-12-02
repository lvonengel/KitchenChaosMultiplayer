using UnityEngine;

/// <summary>
/// Handles the player's footstep sounds based on if the player is walking.
/// </summary>
public class PlayerSounds : MonoBehaviour {
    
    // reference to the player to animate
    private Player player;

    // Current time between each footstep
    private float footstepTimer;

    // Max time between each footprint
    private float footstepTimerMax = .1f;


    private void Awake() {
        player = GetComponent<Player>();
    }

    private void Update() {
        footstepTimer -= Time.deltaTime;
        if (footstepTimer < 0f) {
            footstepTimer = footstepTimerMax;

            if (player.IsWalking()) {
                float volume = 1f;
                SoundManager.Instance.PlayFootstepsSound(player.transform.position, volume);
            }
        }
    }

}
