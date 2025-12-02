using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logic for the player showing/selecting a character in the character select screen
/// Shows player name, color, ready state, and lets host kick them
/// </summary>
public class CharacterSelectPlayer : MonoBehaviour {

    /// <summary>
    /// The index of the player in the character UI
    /// Used to map player data for multiplayer
    /// </summary>
    [SerializeField] private int playerIndex;

    /// <summary>
    /// Indicated whether player is ready (gameobject above head)
    /// </summary>
    [SerializeField] private GameObject readyGameObject;

    /// <summary>
    /// Reference to change player's color
    /// </summary>
    [SerializeField] private PlayerVisual playerVisual;

    /// <summary>
    /// Reference to button so host can kick players from the lobby
    /// </summary>
    [SerializeField] private Button kickButton;

    /// <summary>
    /// Reference to player names (text above head)
    /// </summary>
    [SerializeField] private TextMeshPro playerNameText;

    private void Awake() {
        kickButton.onClick.AddListener(() => {
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.playerId.ToString());
            KitchenGameMultiplayer.Instance.KickPlayer(playerData.clientId);
        });
    }
    
    private void Start() {
        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;
        kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
        UpdatePlayer();
    }

    //Fired when players join/leave the lobby
    // mostly for UI
    private void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e) {
        UpdatePlayer();
    }

    // Fired when a player changes their ready state
    // mostly for UI
    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e) {
        UpdatePlayer();
    }

    /// <summary>
    /// Updates the visual for player (name, color, ready state)
    /// </summary>
    private void UpdatePlayer() {
        if (KitchenGameMultiplayer.Instance.IsPlayerIndexConnected(playerIndex)) {
            Show();
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));
            playerNameText.text = playerData.playerName.ToString();
            playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    //unsubscribes to prevent multiple listeners
    private void OnDestroy() {
        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
    }
}
