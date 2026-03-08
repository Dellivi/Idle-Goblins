using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Расширение PurchaseSystem: поддерживает требования по уровню для разблокировки
/// дополнительных ресурсных условий покупки.
/// </summary>
public class ProducerWithRequirements : PurchaseSystem
{
    private ProducerWithRequirementsConfig RequirementsConfig
        => Config as ProducerWithRequirementsConfig;

    // Два отдельных словаря:
    // mergedCosts  — только для чтения/отображения (GetCurrentCosts)
    // spendBuffer  — копия для итерации при списании (SpendResources)
    private readonly Dictionary<ResourceData, float> mergedCosts = new();
    private readonly Dictionary<ResourceData, float> spendBuffer = new();

    // ──────────────────────────────────────────────
    //  Overrides
    // ──────────────────────────────────────────────

    protected override IReadOnlyDictionary<ResourceData, float> GetCurrentCosts()
    {
        var baseCosts = base.GetCurrentCosts();

        var cfg = RequirementsConfig;
        if (cfg == null || cfg.levelRequirements == null || cfg.levelRequirements.Count == 0)
            return baseCosts;

        mergedCosts.Clear();
        foreach (var kv in baseCosts)
            mergedCosts[kv.Key] = kv.Value;

        foreach (var req in cfg.levelRequirements)
        {
            if (req.resource == null || CurrentLevel < req.unlockLevel) continue;

            mergedCosts.TryGetValue(req.resource, out float existing);
            mergedCosts[req.resource] = existing + req.amount;
        }

        return mergedCosts;
    }

    protected override bool CanAfford()
    {
        foreach (var kv in GetCurrentCosts())
            if (!ResourceManager.Instance.CanAfford(kv.Key, kv.Value))
                return false;
        return true;
    }

    protected override void SpendResources()
    {
        // Копируем в spendBuffer ПЕРЕД итерацией.
        // SpendResource → OnResourceChanged → HandleResourceChanged → GetCurrentCosts()
        // перезаписывает mergedCosts — но мы итерируем по spendBuffer, не по mergedCosts.
        spendBuffer.Clear();
        foreach (var kv in GetCurrentCosts())
            spendBuffer[kv.Key] = kv.Value;

        // Проверка перед списанием
        foreach (var kv in spendBuffer)
        {
            if (!ResourceManager.Instance.CanAfford(kv.Key, kv.Value))
            {
                Debug.LogWarning($"[ProducerWithRequirements] SpendResources отменён: недостаточно {kv.Key?.name}");
                return;
            }
        }

        // Итерируем по stale-копии — событие не ломает цикл
        foreach (var kv in spendBuffer)
            ResourceManager.Instance.SpendResource(kv.Key, kv.Value);
    }
}