using UnityEngine;

/// <summary>
/// Visual for the stove counter. Displays particle and lights up stove
/// </summary>
public class StoveCounterVisual : MonoBehaviour {

    /// <summary>
    /// Reference to stove counter to check if its cooking
    /// </summary>
    [SerializeField] private StoveCounter stoveCounter;

    /// <summary>
    /// Reference to light up stove surface.
    /// </summary>
    [SerializeField] private GameObject stoveOnGameObject;

    /// <summary>
    /// Reference to particles that shoot out.
    /// </summary>
    [SerializeField] private GameObject particlesGameObject;

    private void Start() {
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
    }

    // fires when the state of the stove changes.
    // Shows cooking visuals when it is actively cooking
    private void StoveCounter_OnStateChanged(object sender, StoveCounter.onStateChangedEventArgs e) {
        bool showVisual = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;
        stoveOnGameObject.SetActive(showVisual);
        particlesGameObject.SetActive(showVisual);
    }

}
