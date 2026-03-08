// LocationView.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationView : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textChapter;
    [SerializeField] private TextMeshProUGUI textResourceCount;
    [SerializeField] private Image icon;
    [SerializeField] private Button btnNextLocation;

    // ─── State ────────────────────────────────────────────────
    private LocationSystem _locationSystem;

    // ─── Setup ────────────────────────────────────────────────
    public void Setup(LocationSystem locationSystem)
    {
        if (_locationSystem != null)
            UnsubscribeFromEvents();

        _locationSystem = locationSystem;

        SubscribeToEvents();
        RenderLocation(_locationSystem.LocationData, _locationSystem.CurrentChapter);
        RenderProgress(_locationSystem.CurrentValue, _locationSystem.MaxValue);
        RenderNextButton(_locationSystem.CanAdvance());

        btnNextLocation.onClick.RemoveAllListeners();
        btnNextLocation.onClick.AddListener(_locationSystem.TryNextLocation);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        btnNextLocation.onClick.RemoveAllListeners();
    }

    // ─── Event Subscriptions ──────────────────────────────────
    private void SubscribeToEvents()
    {
        _locationSystem.OnLocationChanged += RenderLocation;
        _locationSystem.OnProgressChanged += RenderProgress;
        _locationSystem.OnNextLocationAvailabilityChanged += RenderNextButton;
        ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (_locationSystem == null) return;

        _locationSystem.OnLocationChanged -= RenderLocation;
        _locationSystem.OnProgressChanged -= RenderProgress;
        _locationSystem.OnNextLocationAvailabilityChanged -= RenderNextButton;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    // ─── Render Methods ───────────────────────────────────────
    private void RenderLocation(LocationData data, int chapter)
    {
        textName.text = data.locationName;
        textChapter.text = $"часть {chapter}";
        if(data.icon) icon.sprite = data.icon; // если есть в LocationData
    }

    private void RenderProgress(double current, double max)
    {
        textResourceCount.text = $"{NumberFormatter.FormatSmart(current)}/{NumberFormatter.FormatSmart(max)}";
    }

    private void RenderNextButton(bool isAvailable)
    {
        btnNextLocation.gameObject.SetActive(isAvailable);
    }

    // ─── Resource Callback ────────────────────────────────────
    private void OnResourceChanged(ResourceData data, double newValue, double previousValue)
    {
        _locationSystem.AddProgress(newValue - previousValue);
    }
}