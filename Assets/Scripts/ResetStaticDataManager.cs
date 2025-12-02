using UnityEngine;

/// <summary>
/// Resets static data across game when a scene loads
/// </summary>
public class ResetStaticDataManager : MonoBehaviour {

    // Clears all listeners, must do for static functions
    private void Awake() {
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
        Player.ResetStaticData();
    }

}
