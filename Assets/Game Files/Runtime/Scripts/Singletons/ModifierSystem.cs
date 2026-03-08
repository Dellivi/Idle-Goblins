using System;
using System.Collections.Generic;
using UnityEngine;
// ══════════════════════════════════════════════════════════════════════════════
//  ModifierSystem — реестр и вычисление итоговых множителей
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Хранит все активные модификаторы и вычисляет итоговые множители
/// производства/скорости для IdleManager.
///
/// Формула применения (стандарт idle-игр):
///   finalMultiplier = (1 + sumAdditive) * productMultiplicative
///
/// Пример:
///   Здание A: +50% additive на золото  → additive += 0.5
///   Здание B: ×2 multiplicative на золото → multiplicative *= 2
///   Итог: (1 + 0.5) × 2 = 3.0x
/// </summary>
public class ModifierSystem : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────
    public static ModifierSystem Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Хранилище модификаторов
    // ──────────────────────────────────────────────
    private readonly Dictionary<string, Modifier> modifiers = new();

    // Кэш вычисленных множителей — пересчитывается только при изменении модификаторов
    // Ключ для кэша: строка вида "res:{resourceId}" / "action:{actionId}" / "global"
    private readonly Dictionary<string, float> productionCache = new();
    private readonly Dictionary<string, float> speedCache = new();
    private bool cacheDirty = true;

    // ──────────────────────────────────────────────
    //  События
    // ──────────────────────────────────────────────

    /// <summary>Любой модификатор был добавлен или удалён.</summary>
    public event Action OnModifiersChanged;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ──────────────────────────────────────────────
    //  Public API — управление модификаторами
    // ──────────────────────────────────────────────

    /// <summary>Добавить модификатор. Если id уже существует — заменяет.</summary>
    public void Add(Modifier modifier)
    {
        if (modifier == null || string.IsNullOrEmpty(modifier.id))
        {
            Debug.LogError("[ModifierSystem] Modifier с пустым id");
            return;
        }

        modifiers[modifier.id] = modifier;
        InvalidateCache();
    }

    /// <summary>Удалить модификатор по id.</summary>
    public bool Remove(string modifierId)
    {
        if (!modifiers.Remove(modifierId)) return false;
        InvalidateCache();
        return true;
    }

    /// <summary>Удалить все модификаторы от источника (по префиксу id).</summary>
    public void RemoveBySource(string sourcePrefix)
    {
        var toRemove = new List<string>();
        foreach (var kv in modifiers)
            if (kv.Key.StartsWith(sourcePrefix))
                toRemove.Add(kv.Key);

        if (toRemove.Count == 0) return;

        foreach (var key in toRemove)
            modifiers.Remove(key);

        InvalidateCache();
    }

    public bool Has(string modifierId) => modifiers.ContainsKey(modifierId);

    // ──────────────────────────────────────────────
    //  Public API — получение множителей
    // ──────────────────────────────────────────────

    /// <summary>
    /// Итоговый множитель производства для конкретного action.
    /// Учитывает: глобальные, пер-ресурсные, пер-экшн модификаторы.
    /// </summary>
    public float GetProductionMultiplier(IdleAction action)
    {
        if (action == null) return 1f;
        RebuildCacheIfDirty();

        float result = 1f;

        // Глобальный
        if (productionCache.TryGetValue("global", out float g))
            result *= g;

        // Пер-ресурсный
        if (action.resource != null)
        {
            string resKey = $"res:{action.resource.name}";
            if (productionCache.TryGetValue(resKey, out float r))
                result *= r;
        }

        // Пер-экшн
        if (!string.IsNullOrEmpty(action.actionId))
        {
            string actKey = $"action:{action.actionId}";
            if (productionCache.TryGetValue(actKey, out float a))
                result *= a;
        }

        return result;
    }

    /// <summary>
    /// Итоговый множитель скорости для конкретного action.
    /// Значение > 1 означает ускорение (duration делится на этот множитель).
    /// </summary>
    public float GetSpeedMultiplier(IdleAction action)
    {
        if (action == null) return 1f;
        RebuildCacheIfDirty();

        float result = 1f;

        if (!string.IsNullOrEmpty(action.actionId))
        {
            string actKey = $"action:{action.actionId}";
            if (speedCache.TryGetValue(actKey, out float s))
                result *= s;
        }

        return result;
    }

    // ──────────────────────────────────────────────
    //  Cache
    // ──────────────────────────────────────────────

    private void InvalidateCache()
    {
        cacheDirty = true;
        OnModifiersChanged?.Invoke();
    }

    private void RebuildCacheIfDirty()
    {
        if (!cacheDirty) return;
        cacheDirty = false;

        productionCache.Clear();
        speedCache.Clear();

        // Для каждой группы: сначала суммируем additive, потом перемножаем multiplicative
        // Используем временные структуры
        var additive = new Dictionary<string, float>();
        var multiplicative = new Dictionary<string, float>();

        var speedAdd = new Dictionary<string, float>();
        var speedMult = new Dictionary<string, float>();

        foreach (var mod in modifiers.Values)
        {
            string key = GetCacheKey(mod);
            if (string.IsNullOrEmpty(key)) continue;

            if (mod.target == ModifierTarget.ActionSpeed)
            {
                Accumulate(speedAdd, speedMult, key, mod);
            }
            else
            {
                Accumulate(additive, multiplicative, key, mod);
            }
        }

        // Финализируем: (1 + sumAdditive) * productMultiplicative
        FinalizeCache(additive, multiplicative, productionCache);
        FinalizeCache(speedAdd, speedMult, speedCache);
    }

    private static void Accumulate(
        Dictionary<string, float> add,
        Dictionary<string, float> mult,
        string key, Modifier mod)
    {
        if (mod.type == ModifierType.Additive)
        {
            add.TryGetValue(key, out float cur);
            add[key] = cur + mod.value;
        }
        else
        {
            mult.TryGetValue(key, out float cur);
            mult[key] = cur == 0f ? mod.value : cur * mod.value;
        }
    }

    private static void FinalizeCache(
        Dictionary<string, float> add,
        Dictionary<string, float> mult,
        Dictionary<string, float> cache)
    {
        var keys = new HashSet<string>(add.Keys);
        keys.UnionWith(mult.Keys);

        foreach (var key in keys)
        {
            add.TryGetValue(key, out float a);
            mult.TryGetValue(key, out float m);
            if (m == 0f) m = 1f;
            cache[key] = (1f + a) * m;
        }
    }

    private static string GetCacheKey(Modifier mod)
    {
        return mod.target switch
        {
            ModifierTarget.GlobalProduction => "global",
            ModifierTarget.ResourceProduction => mod.targetResource != null
                                                    ? $"res:{mod.targetResource.name}"
                                                    : null,
            ModifierTarget.ActionProduction => !string.IsNullOrEmpty(mod.targetActionId)
                                                    ? $"action:{mod.targetActionId}"
                                                    : null,
            ModifierTarget.ActionSpeed => !string.IsNullOrEmpty(mod.targetActionId)
                                                    ? $"action:{mod.targetActionId}"
                                                    : null,
            _ => null
        };
    }

    // ──────────────────────────────────────────────
    //  Debug
    // ──────────────────────────────────────────────
    [ContextMenu("Log All Modifiers")]
    private void LogAll()
    {
        Debug.Log($"[ModifierSystem] Активных модификаторов: {modifiers.Count}");
        foreach (var m in modifiers.Values)
            Debug.Log($"  [{m.id}] target={m.target} type={m.type} value={m.value:F3} " +
                      $"src='{m.sourceLabel}'");
    }
}