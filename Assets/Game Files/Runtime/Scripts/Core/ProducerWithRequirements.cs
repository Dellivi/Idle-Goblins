using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Универсальный продюсер с поддержкой требований на уровне.
/// Наследуется от PurchaseSystem.
/// </summary>
public class ProducerWithRequirements : PurchaseSystem
{
    // Приведение к конфигу с требованиями
    public ProducerWithRequirementsConfig ProducerConfig => Config as ProducerWithRequirementsConfig;

    private void Start()
    {
        Setup(Config);
    }

    /// <summary>
    /// Расширяем базовый метод GetCurrentCosts добавлением ресурсов из LevelRequirements
    /// </summary>
    protected override Dictionary<ResourceData, float> GetCurrentCosts()
    {
        // Берём базовые ресурсы
        var baseCosts = base.GetCurrentCosts();

        if (ProducerConfig == null) return baseCosts;

        // Добавляем ресурсы из levelRequirements на текущем уровне
        foreach (var req in ProducerConfig.levelRequirements)
        {
            if (CurrentLevel < req.unlockLevel)
            {
                if (baseCosts.ContainsKey(req.resource))
                    baseCosts.Remove(req.resource);
            }
        }

        return baseCosts;
    }

    /// <summary>
    /// Проверка возможности покупки с учётом всех ресурсов
    /// </summary>
    protected override bool CanAfford()
    {
        var costs = GetCurrentCosts();
        foreach (var kvp in costs)
        {
            if (!ResourceManager.Instance.CanAfford(kvp.Key, kvp.Value))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Списание всех ресурсов при улучшении
    /// </summary>
    protected override void SpendResources()
    {
        var costs = GetCurrentCosts();
        foreach (var kvp in costs)
        {
            ResourceManager.Instance.SpendResource(kvp.Key, kvp.Value);
        }
    }
}