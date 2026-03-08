// LocationSystem.cs
using System;
using UnityEngine;

public class LocationSystem : MonoBehaviour
{
    // ─── Events ───────────────────────────────────────────────
    public event Action<LocationData, int> OnLocationChanged;   // data, chapter
    public event Action<double, double> OnProgressChanged;   // current, max
    public event Action<bool> OnNextLocationAvailabilityChanged;
    public event Action<int> OnLocationIndexRequested;

    // ─── Inspector ────────────────────────────────────────────
    [SerializeField] private LocationSettingsDataSO locationSettingsData;
    [SerializeField] private CustomBar bar;

    // ─── State ────────────────────────────────────────────────
    public LocationData LocationData { get; private set; }
    public int CurrentChapter { get; private set; } = 1;
    public double CurrentValue { get; private set; }
    public double MaxValue { get; private set; }

    private int _maxLocationLevel;

    // ─── Public API ───────────────────────────────────────────
    public void Initialize(LocationData data, int maxLocationIndex)
    {
        LocationData = data;
        _maxLocationLevel = maxLocationIndex;

        LocationSaveData save = SaveSystem.Load().location;
        CurrentChapter = save.currentLocationLevel;

        ResetProgress();
    }

    public void AddProgress(double amount)
    {
        CurrentValue = Math.Min(CurrentValue + amount, MaxValue);

        bar.AddFillCurrent((float)amount);
        OnProgressChanged?.Invoke(CurrentValue, MaxValue);
        OnNextLocationAvailabilityChanged?.Invoke(CanAdvance());
    }

    public bool CanAdvance() =>
        CurrentValue >= MaxValue && CurrentChapter < _maxLocationLevel;

    public void TryNextLocation()
    {
        if (!CanAdvance()) return;

        CurrentChapter++;

        SaveSystem.Load().location.currentLocationLevel = CurrentChapter;
        SaveSystem.Save();

        ResetProgress();

        OnLocationIndexRequested.Invoke(CurrentChapter);
        OnLocationChanged?.Invoke(LocationData, CurrentChapter);
    }

    // ─── Private ──────────────────────────────────────────────
    private void ResetProgress()
    {
        CurrentValue = 0;
        MaxValue = CalculateMaxValue();

        bar.SetBar(CurrentValue, MaxValue);
        OnProgressChanged?.Invoke(CurrentValue, MaxValue);
        OnNextLocationAvailabilityChanged?.Invoke(CanAdvance());
    }

    private double CalculateMaxValue() =>
        locationSettingsData.baseAmount *
        Mathf.Pow(locationSettingsData.baseMultiplier,
                  CurrentChapter);
}