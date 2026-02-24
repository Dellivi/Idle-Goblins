using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Показывает сколько получаемого ресурса будет.
/// </summary>
public class ResourceGetView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI textResourceGet;

    [Header("Format Settings")]
    [SerializeField] private NumberFormatMode formatMode = NumberFormatMode.Smart;
    [SerializeField] private int decimalPlaces = 2;
    [SerializeField] private bool colorizeByValue = false;
    [SerializeField] private bool showPrefix = true;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highValueColor = Color.yellow;
    [SerializeField] private Color veryHighValueColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private double highValueThreshold = 1e6; // 1M
    [SerializeField] private double veryHighValueThreshold = 1e9; // 1B

    public enum NumberFormatMode
    {
        Smart,      // Автоматический выбор
        Suffix,     // K, M, B, T
        Scientific, // 1.23e15
        Raw         // Без форматирования
    }

    private string customPrefix = "";

    public void Setup(ResourceData data)
    {
        if (icon != null)
            icon.sprite = data.icon;
    }

    public void Show(int amount)
    {
        Show((double)amount);
    }

    public void Show(float amount)
    {
        Show((double)amount);
    }

    public void Show(double amount)
    {
        if (textResourceGet == null) return;

        string formattedAmount = FormatNumber(amount);
        string prefix = showPrefix ? "+" : customPrefix;

        textResourceGet.text = $"{prefix}{formattedAmount}";

        if (colorizeByValue)
        {
            textResourceGet.color = NumberFormatter.GetColorForValue(
                amount, normalColor, highValueColor, veryHighValueColor,
                highValueThreshold, veryHighValueThreshold
            );
        }
    }

    /// <summary>
    /// Устанавливает кастомный префикс (вместо стандартного + или пустоты)
    /// </summary>
    public void SetCustomPrefix(string prefix)
    {
        customPrefix = prefix;
        showPrefix = false;
    }

    private string FormatNumber(double num)
    {
        switch (formatMode)
        {
            case NumberFormatMode.Smart:
                return NumberFormatter.FormatSmart(num, decimalPlaces);
            case NumberFormatMode.Suffix:
                return NumberFormatter.FormatWithSuffix(num, decimalPlaces);
            case NumberFormatMode.Scientific:
                return NumberFormatter.FormatScientific(num, decimalPlaces);
            case NumberFormatMode.Raw:
                return num.ToString("F" + decimalPlaces);
            default:
                return NumberFormatter.FormatSmart(num, decimalPlaces);
        }
    }

    public void Clear()
    {
        if (textResourceGet != null)
            textResourceGet.text = "";
    }

    public void ShowSpecial(string message)
    {
        if (textResourceGet != null)
            textResourceGet.text = message;
    }

    private void Awake()
    {
        if (textResourceGet == null)
            Debug.LogError($"ResourceGetView на {gameObject.name}: textResourceGet не назначен!");
    }
}


