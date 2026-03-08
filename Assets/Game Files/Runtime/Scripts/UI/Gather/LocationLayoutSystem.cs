// LocationLayoutSystem.cs
using UnityEngine;

public class LocationLayoutSystem : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────
    [SerializeField] private LocationLayoutData locationLayoutData;
    [SerializeField] private LocationSystem locationSystem;
    [SerializeField] private LocationView locationView;
    [SerializeField] private ActionPurchaseLayoutSpawner actionAssignmentLayoutSpawner;

    // ─── State ────────────────────────────────────────────────
    private int CurrentLocationIndex
    {
        get => PlayerPrefs.GetInt(SaveKey, 0);
        set => PlayerPrefs.SetInt(SaveKey, value);
    }

    private const string SaveKey = "CurrentLocationIndex";

    private int MaxLocationIndex => locationLayoutData.locationDataList.Count - 1;

    // ─── Lifecycle ────────────────────────────────────────────
    private void OnEnable()
    {
        locationSystem.OnLocationIndexRequested += HandleLocationIndexRequested;
        InitializeLocation(CurrentLocationIndex);
        actionAssignmentLayoutSpawner.Spawn(locationLayoutData.purchaseConfigList);
    }

    private void OnDisable()
    {
        locationSystem.OnLocationIndexRequested -= HandleLocationIndexRequested;
    }

    // ─── Private ──────────────────────────────────────────────
    private void InitializeLocation(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"[LocationLayoutSystem] Invalid location index: {index}");
            return;
        }

        Debug.Log($"[LocationLayoutSystem] Location index: {index}");

        locationSystem.Initialize(
            locationLayoutData.locationDataList[index],
            locationLayoutData.locationDataList.Count
        );

        locationView.Setup(locationSystem);
    }

    private void HandleLocationIndexRequested(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.Log("[LocationLayoutSystem] All locations completed.");
            return;
        }

        CurrentLocationIndex = index;
        InitializeLocation(CurrentLocationIndex);
    }

    private bool IsValidIndex(int index) =>
        index >= 0 && index <= MaxLocationIndex;
}