using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class HostDisconnectUI : MonoBehaviour {
    
    [SerializeField] private Button playAgainButton;


    private void Start() {
        NetworkManager.Singleton.OnClientStopped += NetworkManager_OnClientStopped;
        Hide();
    }

    private void NetworkManager_OnClientStopped(bool success) {
        if (!NetworkManager.Singleton.IsHost) {
            //Server is shutting down
            Show();
        }
    }


    private void Show() {
        gameObject.SetActive(true);
        playAgainButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        NetworkManager.Singleton.OnClientStopped -= NetworkManager_OnClientStopped;
    }

}
