using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[Serializable]
public class ResourceCost
{
    public ResourceData resource;
    public float baseCost;
}

[CreateAssetMenu(fileName = "NewPurchase", menuName = "IdleGame/Purchase Config")]
public class PurchaseConfig : ScriptableObject
{
    [Header("Информация")]
    public string categoryId;
    public LocalizedString categoryName;
    public LocalizedString categoryDescription;
    public Sprite icon;

    [Header("Idle Action")]
    public string actionId = "produce_goblins";

    [Header("Purchase Settings")]
    public float costMultiplier = 1.15f;      // Множитель стоимости на каждый следующий уровень
    public float baseProduction = 1;            // Базовое производство
    public float productionMultiplier = 1.2f; // Множитель производства на уровень

    [Header("Timing Balance")]
    public float baseDuration = 1f;
    public float minDuration = 0.1f; // Минимум 100ms
    public float minPaybackTime = 0.5f; // Минимум 500ms
    public float endgamePaybackFactor = 0.1f; // Рост после 100 уровня (10% за порядок magnitude)

    [Header("Duration Progression")]
    [Tooltip("Как часто уменьшается duration (в уровнях)")]
    public int durationStepLevels = 10; // Уменьшение каждые N уровней
    [Tooltip("Множитель уменьшения duration за шаг")]
    public float durationDecayPerStep = 0.95f; // -5% за шаг

    [Header("Resources")]
    public bool showRequirements = false;
    public List<ResourceCost> costResourceList;
    public ResourceData productionResource;

    // Получить производство для уровня
    public float GetProductionForLevel(int level)
    {
        return baseProduction * Mathf.Pow(productionMultiplier, level - 1);
    }

    // Получить стоимость для уровня
    public float GetCostForLevel(ResourceCost resourceCost, int level)
    {
        float costs = resourceCost.baseCost * Mathf.Pow(costMultiplier, level); ;
        float duration = GetDurationForLevel(level);
        float costsPerSecond = costs / duration;

        float targetPayback = GetTargetPayback(level);

        return costsPerSecond * targetPayback;
    }

    // Получить длительность для уровня (уменьшается каждые N уровней)
    public float GetDurationForLevel(int level)
    {
        // Сколько полных шагов прошло (level 1-10: steps=0, 11-20: steps=1, etc)
        int stepsCount = Mathf.FloorToInt((level - 1) / durationStepLevels);

        // Базовое уменьшение времени
        float calculatedDuration = baseDuration * Mathf.Pow(durationDecayPerStep, stepsCount);

        // Не опускаемся ниже минимума из конфига
        return Mathf.Max(calculatedDuration, minDuration);
    }

    // Получить время окупаемости для уровня
    public float GetPaybackTime(ResourceCost resourceCost,int level)
    {
        float cost = GetCostForLevel(resourceCost,level);
        float production = GetProductionForLevel(level);
        float duration = GetDurationForLevel(level);

        return GetPaybackTime(cost, production, duration, level);
    }

    public float GetTargetPayback(int level)
    {
        if (level <= 10)
            return 30f;          // ранняя игра медленная

        if (level <= 50)
            return Mathf.Lerp(30f, 10f, (level - 10) / 40f);

        if (level <= 150)
            return Mathf.Lerp(10f, 3f, (level - 50) / 100f);

        return 3f + Mathf.Log10(level - 149f); // мягкий эндгейм рост
    }

    // Получить время окупаемости (с переданными параметрами)
    public float GetPaybackTime(float cost, float production, float duration, int level)
    {
        float productionPerSecond = production / duration;
        float basePayback = cost / productionPerSecond;

        // Плавное приближение к минимуму
        float targetMinPayback = minPaybackTime;
        float approachRate = 0.1f; // Скорость приближения

        float payback = basePayback;

        if (level <= 10)
        {
            return Mathf.Max(basePayback, 15f); // Минимум 15 секунд
        }

        if (level > 150)
        {
            float endgameFactor = 1.0f + endgamePaybackFactor * Mathf.Log10(level / 150f);
            payback *= endgameFactor;
        }

        return Mathf.Max(payback, minPaybackTime);
    }

    // Получить ROI для уровня
    public float GetROI(ResourceCost resourceCost, int level)
    {
        float cost = GetCostForLevel(resourceCost,level);
        float production = GetProductionForLevel(level);

        return production / cost;
    }

    // Валидация параметров
    private void OnValidate()
    {
        // Проверка множителей
        if (costMultiplier < 1f) costMultiplier = 1f;
        if (productionMultiplier < 1f) productionMultiplier = 1f;

        // Проверка времени
        if (baseDuration < 0.1f) baseDuration = 0.1f;
        if (minDuration < 0.01f) minDuration = 0.01f;
        if (minPaybackTime < 0.1f) minPaybackTime = 0.1f;

        // Проверка duration progression
        if (durationStepLevels < 1) durationStepLevels = 1;
        if (durationDecayPerStep < 0.5f) durationDecayPerStep = 0.5f;
        if (durationDecayPerStep > 1f) durationDecayPerStep = 1f;

        // Проверка endgame factor
        endgamePaybackFactor = Mathf.Clamp(endgamePaybackFactor, 0f, 0.5f);
    }
}