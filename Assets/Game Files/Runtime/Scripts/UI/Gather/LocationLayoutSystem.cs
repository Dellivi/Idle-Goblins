using UnityEngine;

public class LocationLayoutSystem : MonoBehaviour
{
    [SerializeField] private LocationLayoutData locationLayoutData;
    [SerializeField] private LocationSystem locationSystem;
    [SerializeField] private ActionPurchaseLayoutSpawner actionAssignmentLayoutSpawner;

    private int currentLocationIndex = 0; // 0 - default [saveable]

    private void OnEnable()
    {
        locationSystem.Initialize(locationLayoutData.locationDataList[currentLocationIndex]);
        actionAssignmentLayoutSpawner.Spawn(locationLayoutData.purchaseConfigList);
    }

    public void NextLocation()
    {
        currentLocationIndex++;
        locationSystem.Initialize(locationLayoutData.locationDataList[currentLocationIndex]);
    }
}
