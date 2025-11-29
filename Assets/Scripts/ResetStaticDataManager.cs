using UnityEngine;

public class ResetStaticDataManager : MonoBehaviour {

    // Clears all listeners, must do for static functions
    private void Awake() {
        CuttingCounter.ResetStaticData();
        BaseCounter.ResetStaticData();
        TrashCounter.ResetStaticData();
    }

}