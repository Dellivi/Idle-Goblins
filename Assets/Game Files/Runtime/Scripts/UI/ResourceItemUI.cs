using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-элемент одного ресурса в хранилище.
/// Отображает иконку, название и текущее количество.
/// </summary>
public class ResourceItemUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textAmount;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string updateTrigger = "OnUpdate";

    [Header("Optional: Progress Bar")]
    [SerializeField] private Slider progressBar;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private ResourceData resourceData;
    private double currentValue;
    private double maxValue = -1; // -1 = не задано

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public void Initialize(ResourceData data, double value)
    {
        resourceData = data;
        currentValue = value;

        if (icon != null && data != null)
            icon.sprite = data.icon;

        if (textName != null && data != null)
            textName.text = data.nameResource.GetLocalizedString();

        RefreshAmountText();
    }

    public void UpdateValue(double newValue)
    {
        currentValue = newValue;
        RefreshAmountText();
        RefreshProgressBar();
    }

    public void SetMaxValue(double max)
    {
        maxValue = max;
        RefreshProgressBar();
    }

    public void PlayUpdateAnimation()
    {
        if (animator != null && !string.IsNullOrEmpty(updateTrigger))
            animator.SetTrigger(updateTrigger);
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────
    private void RefreshAmountText()
    {
        if (textAmount == null) return;
        textAmount.text = NumberFormatter.FormatSmart(currentValue);
    }

    private void RefreshProgressBar()
    {
        if (progressBar == null || maxValue <= 0) return;
        progressBar.value = (float)(currentValue / maxValue);
    }
}