using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PurchaseGroupConfig", menuName = "IdleGame/Purchase Group Config")]
public class PurchaseGroupConfig : ScriptableObject
{
    public string groupName;

    [Header("Elements in Group")]
    public List<PurchaseConfig> elements = new List<PurchaseConfig>();

    [Header("Global Settings (optional)")]
    public float globalProductionMultiplier = 1f;
    public float globalCostMultiplier = 1f;
}
