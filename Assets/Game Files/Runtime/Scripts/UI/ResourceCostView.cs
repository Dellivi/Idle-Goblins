using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отображает стоимость одного ресурса: иконка + количество.
/// Подписывается на ResourceManager для актуального цвета.
/// </summary>
public class ResourceCostView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI textAmount;

    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = Color.red;

    private ResourceData resource;
    private double requiredAmount;
    private bool showRequired;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void OnEnable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
    }

    private void OnDisable()
    {
        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────
    public void Setup(ResourceData res, double required, bool showReq)
    {
        resource = res;
        requiredAmount = required;
        showRequired = showReq;

        if (icon != null && res != null)
            icon.sprite = res.icon;

        Refresh();
    }

    public void Refresh()
    {
        if (resource == null || textAmount == null) return;

        double current = ResourceManager.Instance.GetResource(resource);

        textAmount.text = showRequired
            ? $"{NumberFormatter.FormatSmart(current)} / {NumberFormatter.FormatSmart(requiredAmount)}"
            : NumberFormatter.FormatSmart(requiredAmount);

        textAmount.color = current >= requiredAmount ? enoughColor : notEnoughColor;
    }

    // ──────────────────────────────────────────────
    //  Event Handler
    // ──────────────────────────────────────────────
    private void OnResourceChanged(ResourceData changed, double newAmount, double delta)
    {
        if (changed == resource)
            Refresh();
    }
}