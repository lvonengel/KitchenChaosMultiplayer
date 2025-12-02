using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual for the plates counter. Displays the number of plates on the counter.
/// </summary>
public class PlatesCounterVisual : MonoBehaviour {

    /// <summary>
    /// Reference to plates counter to listen to the player interacting with the plates counter.
    /// </summary>
    [SerializeField] private PlatesCounter platesCounter;

    // the top point where the plate is going to spawn
    [SerializeField] private Transform counterTopPoint;

    // reference to the plate that will be spawned
    [SerializeField] private Transform plateVisualPrefab;

    // the list of plates that are on the counter
    private List<GameObject> plateVisualGameObjectList;

    private void Awake() {
        plateVisualGameObjectList = new List<GameObject>();
    }

    private void Start() {
        //Subscribes to plate spawn/remove events from the PlatesCounter
        // so visual stack updates whenever plates are added or removed.
        platesCounter.OnPlateSpawned += PlatesCounter_OnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounter_OnPlateRemoved;
    }

    /// <summary>
    /// Triggers when a plate is spawned. Creates a new plate visual, 
    /// positions it in a stack and stores it in the list.
    /// </summary>
    private void PlatesCounter_OnPlateSpawned(object sender, System.EventArgs e) {
        Transform plateVisualTransform = Instantiate(plateVisualPrefab, counterTopPoint);
        float plateOffsetY = .1f;
        plateVisualTransform.localPosition = new Vector3(0, plateOffsetY * plateVisualGameObjectList.Count, 0);
        plateVisualGameObjectList.Add(plateVisualTransform.gameObject);
    }

    /// <summary>
    /// Triggers when a plate is removed. Removes the top plate 
    /// visual from the stack and destroys it.
    /// </summary>
    private void PlatesCounter_OnPlateRemoved(object sender, System.EventArgs e) {
        GameObject plateGameObject = plateVisualGameObjectList[plateVisualGameObjectList.Count - 1];
        plateVisualGameObjectList.Remove(plateGameObject);
        Destroy(plateGameObject);

    }  
}

