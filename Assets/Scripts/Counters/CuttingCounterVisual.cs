using UnityEngine;

/// <summary>
/// Visual for the cutting counter.
/// </summary>
public class CuttingCounterVisual : MonoBehaviour {

    /// <summary>
    /// The trigger for animating the container counter.
    /// </summary>
    private const string CUT = "Cut";

    /// <summary>
    /// Reference to cutting counter to listen to the player interacting with the cutting counter.
    /// </summary>
    [SerializeField] private CuttingCounter cuttingCounter;

    // reference to animator component that controls cutting animation/visual
    private Animator animator;

    private void Awake() {
        //gets animator component
        animator = GetComponent<Animator>();
    }

    private void Start() {
        //subscribes to when player interacts with cutting counter
        cuttingCounter.OnCut += CuttingCounter_OnCut;
    }

    // sets the trigger to start animation
    public void CuttingCounter_OnCut(object sender, System.EventArgs e) {
        animator.SetTrigger(CUT);   
    }

}
