using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Отображает все ресурсы в контейнере и реактивно обновляется через события ResourceManager.
/// Не использует Update — только event-driven обновления.
/// </summary>
public class ResourceStorageView : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private Transform resourceContainer;
    [SerializeField] private GameObject resourceItemPrefab;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private readonly Dictionary<ResourceData, ResourceItemUI> itemMap = new();
    private readonly Dictionary<ResourceData, double> cachedValues = new();

    // ──────────────────────────────────────────────
    //  События
    // ──────────────────────────────────────────────

    /// <summary>Вызывается после обновления UI конкретного ресурса.</summary>
    public event Action<ResourceData, double> OnResourceUIUpdated;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        if (!ValidateReferences()) enabled = false;
    }

    private void Start()
    {
        SubscribeEvents();
        BuildInitialUI();
    }

    private void OnEnable()
    {
        // Переподписываемся при повторном включении (после OnDisable)
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ──────────────────────────────────────────────
    //  Initialization
    // ──────────────────────────────────────────────
    private bool ValidateReferences()
    {
        if (resourceContainer == null)
        {
            Debug.LogError($"[ResourceStorageView] resourceContainer не назначен!", this);
            return false;
        }
        if (resourceItemPrefab == null)
        {
            Debug.LogError($"[ResourceStorageView] resourceItemPrefab не назначен!", this);
            return false;
        }
        if (resourceItemPrefab.GetComponent<ResourceItemUI>() == null)
        {
            Debug.LogError($"[ResourceStorageView] В префабе отсутствует ResourceItemUI!", this);
            return false;
        }
        return true;
    }

    private void BuildInitialUI()
    {
        if (ResourceManager.Instance == null) return;

        // GetAllResources возвращает снимок — итерируем безопасно
        var snapshot = ResourceManager.Instance.GetAllResources();
        foreach (var kv in snapshot)
        {
            if (kv.Key != null)
                CreateItem(kv.Key, kv.Value);
        }
    }

    // ──────────────────────────────────────────────
    //  Event Subscription
    // ──────────────────────────────────────────────
    private bool _subscribed;

    private void SubscribeEvents()
    {
        if (_subscribed || ResourceManager.Instance == null) return;
        ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
        _subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_subscribed || ResourceManager.Instance == null) return;
        ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
        _subscribed = false;
    }

    // ──────────────────────────────────────────────
    //  Event Handlers
    // ──────────────────────────────────────────────

    // Сигнатура совпадает с ResourceManager: (ResourceData, double newAmount, double delta)
    private void HandleResourceChanged(ResourceData data, double newAmount, double delta)
    {
        if (data == null) return;

        if (!itemMap.ContainsKey(data))
        {
            // Ресурс появился впервые — создаём UI
            CreateItem(data, newAmount);
            return;
        }

        UpdateItem(data, newAmount);
    }

    // ──────────────────────────────────────────────
    //  UI Management
    // ──────────────────────────────────────────────
    private void CreateItem(ResourceData data, double value)
    {
        if (itemMap.ContainsKey(data)) return;

        var go = Instantiate(resourceItemPrefab, resourceContainer);
        var ui = go.GetComponent<ResourceItemUI>();

        if (ui == null)
        {
            Debug.LogError("[ResourceStorageView] ResourceItemUI не найден в созданном объекте!", go);
            Destroy(go);
            return;
        }

        ui.Initialize(data, value);
        itemMap[data] = ui;
        cachedValues[data] = value;
    }

    private void UpdateItem(ResourceData data, double newValue)
    {
        if (!itemMap.TryGetValue(data, out var ui)) return;

        // Обновляем только если значение реально изменилось
        if (cachedValues.TryGetValue(data, out double prev) && prev == newValue) return;

        cachedValues[data] = newValue;
        ui.UpdateValue(newValue);
        ui.PlayUpdateAnimation();

        OnResourceUIUpdated?.Invoke(data, newValue);
    }

    private void RemoveItem(ResourceData data)
    {
        if (!itemMap.TryGetValue(data, out var ui)) return;

        itemMap.Remove(data);
        cachedValues.Remove(data);

        if (ui != null)
            Destroy(ui.gameObject);
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>Принудительно перерисовать все элементы из ResourceManager.</summary>
    public void RefreshAll()
    {
        if (ResourceManager.Instance == null) return;
        var snapshot = ResourceManager.Instance.GetAllResources();
        foreach (var kv in snapshot)
        {
            if (kv.Key == null) continue;
            if (itemMap.ContainsKey(kv.Key))
                UpdateItem(kv.Key, kv.Value);
            else
                CreateItem(kv.Key, kv.Value);
        }
    }

    /// <summary>Уничтожить все UI-элементы и очистить состояние.</summary>
    public void Clear()
    {
        foreach (var ui in itemMap.Values)
            if (ui != null) Destroy(ui.gameObject);

        itemMap.Clear();
        cachedValues.Clear();
    }

    /// <summary>Получить UI-элемент для конкретного ресурса.</summary>
    public ResourceItemUI GetItem(ResourceData data)
    {
        itemMap.TryGetValue(data, out var ui);
        return ui;
    }

    /// <summary>Показать / скрыть UI конкретного ресурса.</summary>
    public void SetVisible(ResourceData data, bool visible)
    {
        if (itemMap.TryGetValue(data, out var ui))
            ui.gameObject.SetActive(visible);
    }

    /// <summary>Передать максимальное значение в UI-элемент (для прогресс-баров внутри).</summary>
    public void SetMaxValue(ResourceData data, double maxValue)
    {
        if (itemMap.TryGetValue(data, out var ui))
            ui.SetMaxValue(maxValue);
    }
}