using System;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

/// <summary>
/// Пример ResourceManager для демонстрации интеграции
/// Адаптируйте под свою существующую реализацию
/// </summary>
public class ResourceManager : MonoBehaviour
{
    #region Singleton

    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ResourceManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ResourceManager");
                    _instance = go.AddComponent<ResourceManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    #endregion

    [Header("Starting Resources")]
    [SerializeField] private List<ResourceGet> startingResources = new List<ResourceGet>();

    // Словарь для хранения ресурсов по ResourceData
    private Dictionary<ResourceData, float> resources = new Dictionary<ResourceData, float>();

    // События для ResourceStorageView
    public event Action<ResourceData, float, float> OnResourceChanged;
    public event Action<ResourceData, float> OnResourceAdded;
    public event Action<ResourceData> OnResourceRemoved;

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }

        InitializeResources();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация стартовых ресурсов
    /// </summary>
    private void InitializeResources()
    {
        foreach (var resourceData in startingResources)
        {
            if (resourceData.resourceData != null)
            {
                AddResource(resourceData.resourceData,resourceData.initialAmount);
            }
            else
            {
                Debug.LogWarning("[ResourceManager] Найден null ResourceData в стартовых ресурсах");
            }
        }

        Debug.Log($"[ResourceManager] Инициализировано {resources.Count} ресурсов");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Получение количества ресурса
    /// </summary>
    public float GetResource(ResourceData resourceData)
    {
        if (resourceData == null) return 0f;
        return resources.TryGetValue(resourceData, out float amount) ? amount : 0f;
    }

    /// <summary>
    /// Получение всех ресурсов
    /// </summary>
    public Dictionary<ResourceData, float> GetAllResources()
    {
        return new Dictionary<ResourceData, float>(resources);
    }

    /// <summary>
    /// Добавление ресурса
    /// </summary>
    public void AddResource(ResourceData resourceData, float amount)
    {
        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceManager] Попытка добавить null ResourceData");
            return;
        }

        if (amount <= 0) return;

        float previousAmount = GetResource(resourceData);

        if (resources.ContainsKey(resourceData))
        {
            resources[resourceData] += amount;
        }
        else
        {
            resources[resourceData] = amount;
            OnResourceAdded?.Invoke(resourceData, amount);
            OnResourceChanged?.Invoke(resourceData, amount, 0);
            Debug.Log($"[ResourceManager] Добавлен новый ресурс {resourceData.name}: {NumberFormatter.FormatSmart(amount)}");
            return;
        }

        float newAmount = resources[resourceData];
        OnResourceChanged?.Invoke(resourceData, newAmount, previousAmount);

        Debug.Log($"[ResourceManager] Добавлено {NumberFormatter.FormatSmart(amount)} {resourceData.name}. Итого: {NumberFormatter.FormatSmart(newAmount)}");
    }

    /// <summary>
    /// Трата ресурса
    /// </summary>
    public bool SpendResource(ResourceData resourceData, float amount)
    {
        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceManager] Попытка потратить null ResourceData");
            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("[ResourceManager] Попытка потратить отрицательное количество ресурса");
            return false;
        }

        if (!CanAfford(resourceData, amount))
        {
            Debug.LogWarning($"[ResourceManager] Недостаточно {resourceData.name}. Требуется: {NumberFormatter.FormatSmart(amount)}, доступно: {NumberFormatter.FormatSmart(GetResource(resourceData))}");
            return false;
        }

        float previousAmount = resources[resourceData];
        resources[resourceData] -= amount;
        float newAmount = resources[resourceData];

        OnResourceChanged?.Invoke(resourceData, newAmount, previousAmount);

        Debug.Log($"[ResourceManager] Потрачено {NumberFormatter.FormatSmart(amount)} {resourceData.name}. Осталось: {NumberFormatter.FormatSmart(newAmount)}");
        return true;
    }

    /// <summary>
    /// Установка точного количества ресурса
    /// </summary>
    public void SetResource(ResourceData resourceData, float amount)
    {
        if (resourceData == null)
        {
            Debug.LogWarning("[ResourceManager] Попытка установить null ResourceData");
            return;
        }

        if (amount < 0)
        {
            Debug.LogWarning("[ResourceManager] Попытка установить отрицательное количество ресурса");
            amount = 0;
        }

        float previousAmount = GetResource(resourceData);

        if (!resources.ContainsKey(resourceData))
        {
            resources[resourceData] = amount;
            OnResourceAdded?.Invoke(resourceData, amount);
            Debug.Log($"[ResourceManager] Добавлен новый ресурс {resourceData.name}: {NumberFormatter.FormatSmart(amount)}");
            return;
        }

        resources[resourceData] = amount;
        OnResourceChanged?.Invoke(resourceData, amount, previousAmount);

        Debug.Log($"[ResourceManager] Установлено {resourceData.name} = {NumberFormatter.FormatSmart(amount)}");
    }

    /// <summary>
    /// Проверка достаточности ресурса
    /// </summary>
    public bool CanAfford(ResourceData resourceData, float amount)
    {
        if (resourceData == null || amount <= 0)
            return false;

        return GetResource(resourceData) >= amount;
    }

    /// <summary>
    /// Проверка достаточности нескольких ресурсов
    /// </summary>
    public bool CanAfford(Dictionary<ResourceData, float> costs)
    {
        if (costs == null || costs.Count == 0)
            return true;

        foreach (var cost in costs)
        {
            if (!CanAfford(cost.Key, cost.Value))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Трата нескольких ресурсов
    /// </summary>
    public bool SpendResources(Dictionary<ResourceData, float> costs)
    {
        if (!CanAfford(costs))
            return false;

        foreach (var cost in costs)
        {
            SpendResource(cost.Key, cost.Value);
        }

        return true;
    }

    /// <summary>
    /// Удаление ресурса
    /// </summary>
    public void RemoveResource(ResourceData resourceData)
    {
        if (resourceData == null) return;

        if (resources.Remove(resourceData))
        {
            OnResourceRemoved?.Invoke(resourceData);
            Debug.Log($"[ResourceManager] Удален ресурс: {resourceData.name}");
        }
    }

    /// <summary>
    /// Проверка существования ресурса
    /// </summary>
    public bool HasResource(ResourceData resourceData)
    {
        if (resourceData == null) return false;
        return resources.ContainsKey(resourceData);
    }

    #endregion

    #region Debug Methods

    /// <summary>
    /// Добавление ресурсов для тестирования (только в режиме разработки)
    /// </summary>
    [ContextMenu("Add Test Resources")]
    private void AddTestResources()
    {
        if (!Application.isPlaying) return;

        // Для тестирования нужно будет создать ResourceData объекты
        foreach (var resourceGet in startingResources)
        {
            if (resourceGet.resourceData != null)
            {
                AddResource(resourceGet.resourceData, 100);
            }
        }
    }

    /// <summary>
    /// Вывод всех ресурсов в консоль
    /// </summary>
    [ContextMenu("Log All Resources")]
    private void LogAllResources()
    {
        Debug.Log("=== Все ресурсы ===");
        foreach (var resource in resources)
        {
            string resourceName = resource.Key != null ? resource.Key.name : "Unknown";
            Debug.Log($"{resourceName}: {resource.Value}");
        }
    }

    #endregion
}

/// <summary>
/// Структура данных для стартовых ресурсов
/// </summary>
[System.Serializable]
public class ResourceGet
{
    public ResourceData resourceData;
    public int initialAmount;

    public ResourceGet(ResourceData data, int amount)
    {
        resourceData = data;
        initialAmount = amount;
    }

    public ResourceGet(ResourceGet resourceGet)
    {
        resourceData = resourceGet.resourceData;
        initialAmount = resourceGet.initialAmount;
    }
}