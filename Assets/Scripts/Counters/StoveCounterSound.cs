using UnityEngine;

/// <summary>
/// Handles all the sound effects for the stove
/// </summary>
public class StoveCounterSound : MonoBehaviour {
    
    /// <summary>
    /// Reference to the stove counter that the noise will come from
    /// </summary>
    [SerializeField] private StoveCounter stoveCounter;

    // audio source that will play when the kitchen object is cooking
    private AudioSource audioSource;

    // timer amount to control how often the warning sound will play
    private float warningSoundTimer;

    // logic to control when warning sound should be played
    private bool playWarningSound;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start() {
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
        stoveCounter.OnProgressChanged += StoveCounter_OnProgressChanged;
    }

    // fires whenver the stove's state changes
    // plays whenever the food is actively cooking
    private void StoveCounter_OnStateChanged(object sender, StoveCounter.onStateChangedEventArgs e) {
        bool playSound = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;
        if (playSound) {
            audioSource.Play();
        } else {
            audioSource.Pause();
        }
    
    }

    // fires whenver the stove's cooking progress changes
    // enables whether warning sound should play based on progress
    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e) {
        float burnShowProgressAmount = .5f;
        playWarningSound = stoveCounter.IsFried() && e.progressNormalized >= burnShowProgressAmount;
    }

    private void Update() {
        if (playWarningSound) {
            warningSoundTimer -= Time.deltaTime;
            if (warningSoundTimer <= 0f) {
                float warningSoundTimerMax = .2f;
                warningSoundTimer = warningSoundTimerMax;
                SoundManager.Instance.PlayWarningSound(stoveCounter.transform.position);
            }
        }
    }

}
