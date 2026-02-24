using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationView : MonoBehaviour
{
    private LocationSystem locationSystem;
    [Space]
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textChapter;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI textResourceCount;
    [SerializeField] private Button btnNextChapter;
    [SerializeField] private Button btnNextLocation;

    public Button BtnNextChapter { get => btnNextChapter; set => btnNextChapter = value; }
    public Button BtnNextLocation { get => btnNextLocation; set => btnNextLocation = value; }

    public void Setup(LocationSystem locationSystem)
    {
        this.locationSystem = locationSystem;
        Initialize();
    }

    public void Initialize()
    {
        textName.text = locationSystem.LocationData.locationName;
        textChapter.text = $"часть {locationSystem.CurrentChapterIndex}";
        textResourceCount.text = $"{locationSystem.CurrentValue}/{locationSystem.MaxValue}";
    }
}
