using UnityEngine;

/// <summary>
/// Visual to highlight the counter that the player can interact with it.
/// </summary>
public class SelectedCounterVisual : MonoBehaviour {

    /// <summary>
    /// Reference to the counter that will be highlighted
    /// </summary>
    [SerializeField] private BaseCounter baseCounter;

    //the gameobjects in the counter that need to be highlighted
    [SerializeField] private GameObject[] visualGameObjectArray;

    private void Start() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.onSelectedCounterChanged += Player_OnSelectedCounterChanged;
        } else {
            Player.OnAnyPlayerSpawned += Player_OnAnyPlayerSpawned;
        }
    }

    /// <summary>
    /// Calls when players current selected counter changes
    /// </summary>
    private void Player_OnAnyPlayerSpawned(object sender, System.EventArgs e) {
        if (Player.LocalInstance != null) {
            // ensures there are only 1 listener
            Player.LocalInstance.onSelectedCounterChanged -= Player_OnSelectedCounterChanged;
            Player.LocalInstance.onSelectedCounterChanged += Player_OnSelectedCounterChanged;
        }
    }

    private void Player_OnSelectedCounterChanged(object sender, Player.onSelectedCounterChangedEventArgs e) {
        if (e.selectedCounter == baseCounter) {
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        foreach(GameObject visualGameObject in visualGameObjectArray) {
            visualGameObject.SetActive(true);
        }
    }
    
    private void Hide() {
        foreach(GameObject visualGameObject in visualGameObjectArray) {
            visualGameObject.SetActive(false);
        }
    }

}
