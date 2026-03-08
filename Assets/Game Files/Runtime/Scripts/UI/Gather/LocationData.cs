using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocationData_", menuName = "Game/Data/Gather/new LocationData")]
public class LocationData : ScriptableObject
{
    public string locationName;
    public Sprite icon;
}
