using UnityEngine;

/// <summary>
/// Handles changing the UI's orientation to the camera.
/// </summary>
public class LookAtCamera : MonoBehaviour {

    /// <summary>
    /// The different ways the UI looks at the camera
    /// </summary>
    private enum Mode {
        LookAt, // Looks directly at camera
        LookAtInverted, //Faces away from camera
        CameraForward, // Sets object forward direction equal to camera's forward direction
        CameraForwardInverted, // Sets object forward direction opposite to camera's forward direction
    }

    /// <summary>
    /// The orientation the UI is using
    /// </summary>
    [SerializeField] private Mode mode;

    private void LateUpdate() {
        switch (mode) {
            case Mode.LookAt:
                transform.LookAt(Camera.main.transform);
                break;
            case Mode.LookAtInverted:
                Vector3 dirFromCamera = transform.position - Camera.main.transform.position;
                transform.LookAt(transform.position + dirFromCamera);
                break;
            case Mode.CameraForward:
                transform.forward = Camera.main.transform.forward;
                break;
            case Mode.CameraForwardInverted:
                transform.forward = -Camera.main.transform.forward;
                break;
        }
    }
}
