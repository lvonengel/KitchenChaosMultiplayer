using UnityEngine;

/// <summary>
/// Handles the players appearance, specifically changing the player's
/// color for multiplayer
/// </summary>
public class PlayerVisual : MonoBehaviour {

    //player's head
    [SerializeField] private MeshRenderer headMeshRenderer;

    //player's body
    [SerializeField] private MeshRenderer bodyMeshRenderer;

    //the material that will be applied to head and body
    private Material material;

    private void Awake() {
        // creates a new material to not affect other player's color
        material = new Material(headMeshRenderer.material);
        headMeshRenderer.material = material;
        bodyMeshRenderer.material = material;
    }

    /// <summary>
    /// Sets the player color with the given one
    /// </summary>
    /// <param name="color">The color to give the player</param>
    public void SetPlayerColor(Color color) {
        material.color = color;
    }
    
}