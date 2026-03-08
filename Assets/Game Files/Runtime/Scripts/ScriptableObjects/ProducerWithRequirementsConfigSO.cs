using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Конфиг производителя с поддержкой дополнительных ресурсных требований по уровню.
/// Наследует все базовые настройки ProductionConfig.
/// </summary>
[CreateAssetMenu(
    fileName = "NewProducerWithRequirements",
    menuName = "IdleGame/Producer With Requirements Config")]
public class ProducerWithRequirementsConfig : ProductionConfig
{
    [Header("Level Requirements")]
    [Tooltip("Дополнительные ресурсы, которые добавляются к стоимости апгрейда начиная с unlockLevel")]
    public List<ResourceLevelRequirement> levelRequirements = new();

    // ──────────────────────────────────────────────
    //  Editor Validation
    // ──────────────────────────────────────────────
    private new void OnValidate()
    {
        base.OnValidate();

        if (levelRequirements == null) return;

        // Сортируем по unlockLevel для удобства в Inspector
        levelRequirements.Sort((a, b) => a.unlockLevel.CompareTo(b.unlockLevel));
    }
}