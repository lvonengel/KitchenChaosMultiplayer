using System.Collections.Generic;
using UnityEngine;

//List of all kitchen object SOs
[CreateAssetMenu()]
public class KitchenObjectListSO : ScriptableObject {
    
    public List<KitchenObjectSO> kitchenObjectSOList;
}