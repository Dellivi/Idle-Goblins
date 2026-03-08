using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

// ──────────────────────────────────────────────────────────────
//  Вспомогательные типы
// ──────────────────────────────────────────────────────────────

[Serializable]
public class ResourceCost
{
    public ResourceData resource;
    [Min(1f)] public float baseCost = 10f;
}

// ──────────────────────────────────────────────────────────────
//  ProductionConfig
// ──────────────────────────────────────────────────────────────

/// <summary>
/// ScriptableObject с настройками одного производителя.
/// Все формулы собраны здесь — легко тестировать и балансировать.
/// </summary>
[CreateAssetMenu(fileName = "New ProductionConfig", menuName = "IdleGame/Production Config")]
public class ProductionConfig : ScriptableObject
{
    // ──────────────────────────────────────────────
    //  Идентификация
    // ──────────────────────────────────────────────
    [Header("Identity")]
    public string categoryId;
    public string actionId = "produce_resource";
    public LocalizedString categoryName;
    public LocalizedString categoryDescription;
    public Sprite icon;

    // ──────────────────────────────────────────────
    //  Ресурсы
    // ──────────────────────────────────────────────
    [Header("Resources")]
    public ResourceData productionResource;
    public List<ResourceCost> costResourceList = new();
    public bool showRequirements;

    // ──────────────────────────────────────────────
    //  Экономика производства
    // ──────────────────────────────────────────────
    [Header("Production")]
    [Tooltip("Базовое производство на 1-м уровне")]
    [Min(0.001f)] public float baseProduction = 1f;

    [Tooltip("Множитель производства за уровень (1.2 = +20%/уровень)")]
    [Range(1f, 3f)] public float productionMultiplier = 1.2f;

    // ──────────────────────────────────────────────
    //  Стоимость
    // ──────────────────────────────────────────────
    [Header("Cost")]
    [Tooltip("Множитель роста стоимости за уровень")]
    [Range(1f, 2f)] public float costMultiplier = 1.15f;

    // ──────────────────────────────────────────────
    //  Длительность цикла
    // ──────────────────────────────────────────────
    [Header("Duration")]
    [Tooltip("Базовая длительность цикла (секунды)")]
    [Min(0.1f)] public float baseDuration = 5f;

    [Tooltip("Минимальная длительность цикла (секунды)")]
    [Min(0.05f)] public float minDuration = 0.5f;

    [Tooltip("Каждые N уровней длительность уменьшается")]
    [Min(1)] public int durationStepLevels = 10;

    [Tooltip("Множитель сокращения длительности за шаг (0.95 = -5%)")]
    [Range(0.5f, 0.99f)] public float durationDecayPerStep = 0.95f;

    // ──────────────────────────────────────────────
    //  Формулы
    // ──────────────────────────────────────────────

    /// <summary>Производство за цикл для уровня <paramref name="level"/>.</summary>
    public float GetProductionForLevel(int level)
    {
        if (level <= 0) return 0f;
        return baseProduction * Mathf.Pow(productionMultiplier, level - 1);
    }

    /// <summary>Стоимость апгрейда для уровня <paramref name="level"/> (следующая покупка).</summary>
    public float GetCostForLevel(ResourceCost cost, int level)
    {
        if (cost == null) return 0f;
        return cost.baseCost * Mathf.Pow(costMultiplier, level);
    }

    /// <summary>Длительность цикла для уровня <paramref name="level"/>.</summary>
    public float GetDurationForLevel(int level)
    {
        if (level <= 0) return baseDuration;
        int steps = Mathf.FloorToInt((level - 1f) / durationStepLevels);
        return Mathf.Max(baseDuration * Mathf.Pow(durationDecayPerStep, steps), minDuration);
    }

    /// <summary>Производство в секунду для уровня.</summary>
    public float GetProductionPerSecond(int level)
    {
        float d = GetDurationForLevel(level);
        return d > 0f ? GetProductionForLevel(level) / d : 0f;
    }

    /// <summary>ROI: производство / стоимость (выгодность апгрейда).</summary>
    public float GetROI(ResourceCost cost, int level)
    {
        float c = GetCostForLevel(cost, level);
        return c > 0f ? GetProductionForLevel(level) / c : 0f;
    }

    /// <summary>Время окупаемости в секундах для данного уровня.</summary>
    public float GetPaybackSeconds(ResourceCost cost, int level)
    {
        float pps = GetProductionPerSecond(level);
        if (pps <= 0f) return float.MaxValue;
        return GetCostForLevel(cost, level) / pps;
    }

    // ──────────────────────────────────────────────
    //  Editor Validation
    // ──────────────────────────────────────────────
    protected void OnValidate()
    {
        costMultiplier = Mathf.Max(1f, costMultiplier);
        productionMultiplier = Mathf.Max(1f, productionMultiplier);
        baseDuration = Mathf.Max(0.1f, baseDuration);
        minDuration = Mathf.Clamp(minDuration, 0.05f, baseDuration);
        durationStepLevels = Mathf.Max(1, durationStepLevels);
        durationDecayPerStep = Mathf.Clamp(durationDecayPerStep, 0.5f, 0.99f);
    }
}