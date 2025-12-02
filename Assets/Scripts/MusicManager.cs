using UnityEngine;

/// <summary>
/// Manages the volume control for the game music.
/// </summary>
public class MusicManager : MonoBehaviour {

    //Playerprefs key to save the music volume
    private const string PLAYER_PREFS_MUSIC_VOLUME = "MusicVolume";
    
    public static MusicManager Instance {get; private set;}

    // Reference to component that plays the audio source
    private AudioSource audioSource;

    //current music volume
    private float volume = .3f;

    private void Awake() {
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        volume = PlayerPrefs.GetFloat(PLAYER_PREFS_MUSIC_VOLUME, .3f);
        audioSource.volume = volume;
    }

    /// <summary>
    /// Changes volume by increments of .1. Goes back to 0 when
    /// the volume is over 1.
    /// </summary>
    public void ChangeVolume() {
        volume += .1f;
        if (volume > 1f) {
            volume = 0f;
        }
        audioSource.volume = volume;
        PlayerPrefs.SetFloat(PLAYER_PREFS_MUSIC_VOLUME, volume);
        PlayerPrefs.Save();
    }

    public float GetVolume() {
        return volume;
    }


}