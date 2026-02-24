using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "LocationLayoutData_", menuName = "Game/Data/new LocationLayoutData")]
public class LocationLayoutData : ScriptableObject
{
    public LocalizedString localizedStringGather;
    public List<LocationData> locationDataList;
    public List<PurchaseConfig> purchaseConfigList;
}
