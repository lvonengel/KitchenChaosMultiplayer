using UnityEngine;

/// <summary>
/// Manager for all sound effects in the game
/// </summary>
public class SoundManager : MonoBehaviour {

    //Player Pfefs to store sound effect volume setting
    private const string PLAYER_PREFS_SOUND_EFFECTS_VOLUME = "SoundEffectsVolume";
    public static SoundManager Instance {get; private set;}

    //Reference of all audio clips
    [SerializeField] private AudioClipsRefSO audioClipsRefSO;

    // the volume of the sound effects
    private float volume = 1f;

    private void Awake() {
        Instance = this;
        volume = PlayerPrefs.GetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, 1f);
    }

    private void Start() {
        DeliveryManager.Instance.OnRecipeSuccess += DeliveryManager_OnRecipeSuccess;
        DeliveryManager.Instance.OnRecipeFailed += DeliveryManager_OnRecipeFailed;
        CuttingCounter.OnAnyCut += CuttingCounter_OnAnyCut;
        Player.OnAnyPickedSomething += Player_OnAnyPickedSomething;
        BaseCounter.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
        TrashCounter.OnAnyObjectTrashed += TrashCounter_OnAnyObjectTrashed;
    }

    // plays a trash sound when an object is thrown away
    private void TrashCounter_OnAnyObjectTrashed(object sender, System.EventArgs e) {
        TrashCounter trashCounter = sender as TrashCounter;
        PlaySound(audioClipsRefSO.trash, trashCounter.transform.position);
    }

    // plays a drop sound when an object is placed on a counter
    private void BaseCounter_OnAnyObjectPlacedHere(object sender, System.EventArgs e) {
        BaseCounter baseCounter = sender as BaseCounter;
        PlaySound(audioClipsRefSO.objectDrop, baseCounter.transform.position);
    }

    // plays a pick up sound when an object is picked up
    private void Player_OnAnyPickedSomething(object sender, System.EventArgs e) {
        Player player = sender as Player;
        PlaySound(audioClipsRefSO.objectPickup, player.transform.position);
    }

    // plays a cutting sound when an object is cut
    private void CuttingCounter_OnAnyCut(object sender, System.EventArgs e) {
        CuttingCounter cuttingCounter = sender as CuttingCounter;
        PlaySound(audioClipsRefSO.chop, cuttingCounter.transform.position);
    }

    // plays a success sound when a recipe is delivered correctly
    private void DeliveryManager_OnRecipeSuccess(object sender, System.EventArgs e) {
        DeliveryCounter deliveryCounter = DeliveryCounter.Instance;
        PlaySound(audioClipsRefSO.deliverySuccess, deliveryCounter.transform.position);
    }

    // plays a failed sound when a recipe is delivered incorrectly
    private void DeliveryManager_OnRecipeFailed(object sender, System.EventArgs e) {
        DeliveryCounter deliveryCounter = DeliveryCounter.Instance;
        PlaySound(audioClipsRefSO.deliveryFail, deliveryCounter.transform.position);
    }

    /// <summary>
    /// Plays a random clip from the audio clip array
    /// </summary>
    /// <param name="audioClipArray">Array of clips to choose from</param>
    /// <param name="position">Worl position to play the sound</param>
    /// <param name="volume">The sound volume</param>
    private void PlaySound(AudioClip[] audioClipArray, Vector3 position, float volume = 1f) {
        AudioSource.PlayClipAtPoint(audioClipArray[Random.Range(0, audioClipArray.Length)], position, volume);
    }

    /// <summary>
    /// Plays a single audio clip from the audio clip array
    /// </summary>
    /// <param name="audioClipArray">The clip to play</param>
    /// <param name="position">Worl position to play the sound</param>
    /// <param name="volume">The sound volume</param>
    private void PlaySound(AudioClip audioClip, Vector3 position, float volumeMultiplier = 1f) {
        AudioSource.PlayClipAtPoint(audioClip, position, volumeMultiplier * volume);
    }

    /// <summary>
    /// Plays footprint sound for the player
    /// </summary>
    /// <param name="position">Position of the player</param>
    /// <param name="volume">The sound volume</param>
    public void PlayFootstepsSound(Vector3 position, float volume) {
        PlaySound(audioClipsRefSO.footstep, position, volume);
    }

    /// <summary>
    /// Plays a countdown sound at the origin
    /// </summary>
    public void PlayCountdownSound() {
        PlaySound(audioClipsRefSO.warning, Vector3.zero);
    }

    /// <summary>
    /// Plays a warning sound at the given world position
    /// </summary>
    /// <param name="position">World position to play sound</param>
    public void PlayWarningSound(Vector3 position) {
        PlaySound(audioClipsRefSO.warning, position);
    }

    /// <summary>
    /// Changes the global sound effect volume in increments of .1.
    /// Starts over at 0 when 1 is reached.
    /// </summary>
    public void ChangeVolume() {
        volume += .1f;
        if (volume > 1f) {
            volume = 0f;
        }
        PlayerPrefs.SetFloat(PLAYER_PREFS_SOUND_EFFECTS_VOLUME, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Gets the current sound effects volume
    /// </summary>
    /// <returns></returns>
    public float GetVolume() {
        return volume;
    }

}
