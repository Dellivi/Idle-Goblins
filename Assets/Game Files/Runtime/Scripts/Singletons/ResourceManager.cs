using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResourceGet
{
    public ResourceData resourceData;
    public double initialAmount;
}

/// <summary>
/// Менеджер ресурсов. Хранит количество каждого ResourceData.
/// Использует double для предотвращения потери точности на больших числах (idle-специфика).
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────
    public static ResourceManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Starting Resources")]
    [SerializeField] private List<ResourceGet> startingResources = new();

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private readonly Dictionary<ResourceData, double> resources = new();

    // ──────────────────────────────────────────────
    //  События
    // ──────────────────────────────────────────────

    /// <summary>Вызывается при любом изменении ресурса. (resource, newAmount, delta)</summary>
    public event Action<ResourceData, double, double> OnResourceChanged;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeResources();
    }

    // ──────────────────────────────────────────────
    //  Initialization
    // ──────────────────────────────────────────────
    private void InitializeResources()
    {
        foreach (var entry in startingResources)
        {
            if (entry.resourceData != null)
                AddResource(entry.resourceData, entry.initialAmount);
            else
                Debug.LogWarning("[ResourceManager] null ResourceData в стартовых ресурсах");
        }
    }

    // ──────────────────────────────────────────────
    //  Public API — Чтение
    // ──────────────────────────────────────────────

    /// <summary>Снимок всех ресурсов. Возвращает копию — безопасно итерировать.</summary>
    public Dictionary<ResourceData, double> GetAllResources()
        => new(resources);

    /// <summary>Текущее количество ресурса (double для точности).</summary>
    public double GetResource(ResourceData data)
    {
        if (data == null) return 0.0;
        return resources.TryGetValue(data, out double amount) ? amount : 0.0;
    }

    /// <summary>Есть ли ресурс в реестре (хотя бы 0).</summary>
    public bool HasResource(ResourceData data)
        => data != null && resources.ContainsKey(data);

    /// <summary>Можно ли потратить указанное количество.</summary>
    public bool CanAfford(ResourceData data, double amount)
        => data != null && amount >= 0.0 && GetResource(data) >= amount;

    /// <summary>Можно ли потратить набор ресурсов.</summary>
    public bool CanAfford(IReadOnlyDictionary<ResourceData, float> costs)
    {
        if (costs == null) return true;
        foreach (var kv in costs)
            if (!CanAfford(kv.Key, kv.Value)) return false;
        return true;
    }

    // ──────────────────────────────────────────────
    //  Public API — Изменение
    // ──────────────────────────────────────────────

    /// <summary>Добавить ресурс (float перегрузка для совместимости с IdleManager).</summary>
    public void AddResource(ResourceData data, float amount)
        => AddResource(data, (double)amount);

    /// <summary>Добавить ресурс.</summary>
    public void AddResource(ResourceData data, double amount)
    {
        if (data == null || amount <= 0.0) return;

        resources.TryGetValue(data, out double prev);
        resources[data] = prev + amount;

        OnResourceChanged?.Invoke(data, resources[data], amount);

        if (verboseLog)
            Debug.Log($"[ResourceManager] +{NumberFormatter.FormatSmart(amount)} {data.name} → {NumberFormatter.FormatSmart(resources[data])}");
    }

    /// <summary>Потратить ресурс. Возвращает false если недостаточно.</summary>
    public bool SpendResource(ResourceData data, double amount)
    {
        if (!CanAfford(data, amount))
        {
            if (verboseLog)
                Debug.LogWarning($"[ResourceManager] Недостаточно {data?.name}: нужно {amount:F2}, есть {GetResource(data):F2}");
            return false;
        }

        double prev = resources[data];
        resources[data] = prev - amount;

        OnResourceChanged?.Invoke(data, resources[data], -amount);

        if (verboseLog)
            Debug.Log($"[ResourceManager] -{NumberFormatter.FormatSmart(amount)} {data.name} → {NumberFormatter.FormatSmart(resources[data])}");

        return true;
    }

    /// <summary>float-перегрузка для совместимости с существующим кодом.</summary>
    public bool SpendResource(ResourceData data, float amount)
        => SpendResource(data, (double)amount);

    /// <summary>Потратить набор ресурсов. Атомарно: либо всё, либо ничего.</summary>
    public bool SpendResources(IReadOnlyDictionary<ResourceData, float> costs)
    {
        if (!CanAfford(costs)) return false;
        foreach (var kv in costs)
            SpendResource(kv.Key, kv.Value);
        return true;
    }

    /// <summary>Принудительно выставить количество ресурса.</summary>
    public void SetResource(ResourceData data, double amount)
    {
        if (data == null) return;
        amount = Math.Max(0.0, amount);

        double prev = GetResource(data);
        resources[data] = amount;

        OnResourceChanged?.Invoke(data, amount, amount - prev);
    }

    // ──────────────────────────────────────────────
    //  Save / Load  (опциональный пример)
    // ──────────────────────────────────────────────
    private const string SavePrefix = "ResourceManager_";

    public void SaveResources()
    {
        foreach (var kv in resources)
        {
            if (kv.Key == null) continue;
            PlayerPrefs.SetString(SavePrefix + kv.Key.name, kv.Value.ToString("R"));
        }
        PlayerPrefs.Save();
    }

    public void LoadResources()
    {
        foreach (var kv in new Dictionary<ResourceData, double>(resources))
        {
            if (kv.Key == null) continue;
            string key = SavePrefix + kv.Key.name;
            if (PlayerPrefs.HasKey(key) &&
                double.TryParse(PlayerPrefs.GetString(key), out double val))
            {
                resources[kv.Key] = val;
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Debug
    // ──────────────────────────────────────────────
    [ContextMenu("Log All Resources")]
    private void LogAll()
    {
        Debug.Log($"[ResourceManager] Всего: {resources.Count} ресурс(а)");
        foreach (var kv in resources)
            Debug.Log($"  {kv.Key?.name ?? "null"}: {NumberFormatter.FormatSmart(kv.Value)}");
    }

    [ContextMenu("Add 1000 to All (Debug)")]
    private void DebugAddAll()
    {
        if (!Application.isPlaying) return;
        foreach (var entry in startingResources)
            if (entry.resourceData != null)
                AddResource(entry.resourceData, 1000);
    }
}