using UnityEngine;

/// <summary>
/// Handles the visual animation for the container counter.
/// </summary>
public class ContainerCounterVisual : MonoBehaviour {

    /// <summary>
    /// The trigger for animating the container counter.
    /// </summary>
    private const string OPEN_CLOSE = "OpenClose";

    /// <summary>
    /// Reference to container counter to listen to the player interacting with the container.
    /// </summary>
    [SerializeField] private ContainerCounter containerCounter;

    // reference to animator component that controls container animation/visual
    private Animator animator;

    private void Awake() {
        //gets animator component
        animator = GetComponent<Animator>();
    }

    private void Start() {
        //subscribes to when player interacts with container
        containerCounter.OnPlayerGrabbedObject += ContainerCounter_OnPlayerGrabbedObject;
    }

    // sets the trigger to start animation
    public void ContainerCounter_OnPlayerGrabbedObject(object sender, System.EventArgs e) {
        animator.SetTrigger(OPEN_CLOSE);   
    }

}
