using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceCostView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI textAmount;

    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = Color.red;

    private ResourceData resource;
    private double requiredAmount;
    private bool showRequiredResource = false;

    public void Setup(ResourceData resource, double requiredAmount, bool showRequired)
    {
        this.resource = resource;
        this.requiredAmount = requiredAmount;
        this.showRequiredResource = showRequired;

        if (icon != null)
            icon.sprite = resource.icon;

        Refresh(showRequiredResource);
    }

    public void Refresh(bool showRequired)
    {
        if (resource == null) return;

        double current = ResourceManager.Instance.GetResource(resource);

        if (showRequired)
        {
            textAmount.text = $"{NumberFormatter.FormatSmart(current)} / {NumberFormatter.FormatSmart(requiredAmount)}";
        }
        else
        {
            textAmount.text = $"{NumberFormatter.FormatSmart(requiredAmount)}";
        }

            textAmount.color = current >= requiredAmount ? enoughColor : notEnoughColor;
    }
}