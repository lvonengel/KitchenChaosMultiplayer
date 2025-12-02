using UnityEngine;

/// <summary>
/// Handles game object following the position of a target transform.
/// Used for kitchen object to follow a player
/// </summary>
public class FollowTransform : MonoBehaviour {
    
    /// <summary>
    /// The transform that the object should follow
    /// </summary>
    private Transform targetTransform;

    /// <summary>
    /// Assigns given target transform to this object
    /// </summary>
    /// <param name="targetTransform">The transform to follow</param>
    public void SetTargetTransform(Transform targetTransform) {
        this.targetTransform = targetTransform;
    }

    private void LateUpdate() {
        if (targetTransform == null) {
            return;
        } 
        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
    }

}