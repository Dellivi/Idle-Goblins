using System;
using UnityEngine;

/// <summary>
/// Данные одного idle-производителя.
/// Производство и скорость модифицируются через ModifierSystem.
/// </summary>
[Serializable]
public class IdleAction
{
    public string actionId;
    public ResourceData resource;
    public ProductionConfig config;
    public int level;

    [NonSerialized] public float timer;
    [NonSerialized] public bool isActive;

    // ──────────────────────────────────────────────
    //  Производство
    // ──────────────────────────────────────────────

    /// <summary>
    /// Базовое производство за цикл БЕЗ модификаторов.
    /// IdleManager применяет модификаторы снаружи.
    /// </summary>
    public float GetBaseProductionPerCycle()
        => config != null && level > 0 ? config.GetProductionForLevel(level) : 0f;

    /// <summary>
    /// Итоговое производство за цикл с учётом globalMultiplier и ModifierSystem.
    /// </summary>
    public float GetProductionPerCycle(float globalMultiplier)
    {
        float modifier = ModifierSystem.Instance != null
            ? ModifierSystem.Instance.GetProductionMultiplier(this)
            : 1f;
        return GetBaseProductionPerCycle() * globalMultiplier * modifier;
    }

    /// <summary>Производство в секунду с учётом всех модификаторов.</summary>
    public float GetProductionPerSecond(float globalMultiplier)
    {
        float d = GetDuration();
        return d > 0f ? GetProductionPerCycle(globalMultiplier) / d : 0f;
    }

    // ──────────────────────────────────────────────
    //  Длительность
    // ──────────────────────────────────────────────

    /// <summary>
    /// Базовая длительность цикла из конфига.
    /// </summary>
    public float GetBaseDuration()
        => config != null ? config.GetDurationForLevel(level) : 0f;

    /// <summary>
    /// Итоговая длительность с учётом SpeedMultiplier.
    /// SpeedMultiplier > 1 = быстрее (duration уменьшается).
    /// </summary>
    public float GetDuration()
    {
        float baseDur = GetBaseDuration();
        if (baseDur <= 0f) return 0f;

        float speedMult = ModifierSystem.Instance != null
            ? ModifierSystem.Instance.GetSpeedMultiplier(this)
            : 1f;

        return speedMult > 0f ? baseDur / speedMult : baseDur;
    }

    // ──────────────────────────────────────────────
    //  Прогресс
    // ──────────────────────────────────────────────

    public float GetProgress()
    {
        float d = GetDuration();
        return d > 0f ? Mathf.Clamp01(timer / d) : 0f;
    }

    public float GetRemainingTime()
        => Mathf.Max(0f, GetDuration() - timer);

    // ──────────────────────────────────────────────
    //  Тик
    // ──────────────────────────────────────────────

    /// <summary>
    /// Двигает таймер. Возвращает циклы и базовое produced (без globalMultiplier).
    /// Модификаторы применяются в IdleManager при диспатче.
    /// </summary>
    public int Tick(float deltaTime, out float basedProduced)
    {
        basedProduced = 0f;
        if (!isActive) return 0;

        float duration = GetDuration();
        if (duration <= 0f) return 0;

        timer += deltaTime;
        int cycles = 0;

        while (timer >= duration)
        {
            timer -= duration;
            basedProduced += GetBaseProductionPerCycle();
            cycles++;
        }

        return cycles;
    }

    public float SimulateOffline(float seconds, float globalMultiplier)
        => GetProductionPerSecond(globalMultiplier) * seconds;
}