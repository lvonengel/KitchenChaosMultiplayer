using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles cleaning up of network. Makes sure that the
/// NetworkManager, KitchenGameMultiplayer, and KitchenGameLobby
/// get destroyed to avoid duplication
/// </summary>
public class MainMenuCleanup : MonoBehaviour {
    private void Awake() {
        if (NetworkManager.Singleton != null) {
            Destroy(NetworkManager.Singleton.gameObject);
        }
        if (KitchenGameMultiplayer.Instance != null) {
            Destroy(KitchenGameMultiplayer.Instance.gameObject);
        }
        if (KitchenGameLobby.Instance != null) {
            Destroy(KitchenGameLobby.Instance.gameObject);
        }
    }
}