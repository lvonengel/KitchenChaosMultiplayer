using UnityEngine;

// A kitchen object that can used in the game.
// Connects a prefab kitchen object, the visual 2D sprite, 
// and the name
[CreateAssetMenu()]
public class KitchenObjectSO : ScriptableObject {

    public Transform prefab;
    public Sprite sprite;
    public string objectName;

}
