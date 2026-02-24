using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отображает все ресурсы и автоматически обновляется при их изменении
/// </summary>
public class ResourceStorageView : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Transform resourceContainer;
    [SerializeField] private GameObject resourceItemPrefab;

    // Словарь для быстрого доступа к UI элементам ресурсов
    private Dictionary<ResourceData, ResourceItemUI> resourceUIItems = new Dictionary<ResourceData, ResourceItemUI>();

    // Кэш предыдущих значений для оптимизации обновлений
    private Dictionary<ResourceData, float> previousResourceValues = new Dictionary<ResourceData, float>();

    // Ссылка на менеджер ресурсов
    private ResourceManager resourceManager;

    /// <summary>
    /// Событие, вызываемое при обновлении UI ресурса
    /// </summary>
    public event Action<ResourceData, float> OnResourceUIUpdated;

    #region Unity Lifecycle

    private void OnEnable()
    {
        if (resourceManager == null) return;
        SubscribeToResourceEvents();
    }

    private void Awake()
    {
        ValidateComponents();
        InitializeResourceManager();
    }

    private void Start()
    {
        SetupInitialResources();
    }

    private void OnDestroy()
    {
        UnsubscribeFromResourceEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromResourceEvents();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Проверка необходимых компонентов
    /// </summary>
    private void ValidateComponents()
    {
        if (resourceContainer == null)
        {
            Debug.LogError($"[{nameof(ResourceStorageView)}] Resource Container не назначен!");
            return;
        }

        if (resourceItemPrefab == null)
        {
            Debug.LogError($"[{nameof(ResourceStorageView)}] Resource Item Prefab не назначен!");
            return;
        }

        // Проверяем, что в префабе есть компонент ResourceItemUI
        ResourceItemUI prefabComponent = resourceItemPrefab.GetComponent<ResourceItemUI>();
        if (prefabComponent == null)
        {
            Debug.LogError($"[{nameof(ResourceStorageView)}] В префабе resourceItemPrefab отсутствует компонент ResourceItemUI!");
        }
    }

    /// <summary>
    /// Инициализация менеджера ресурсов
    /// </summary>
    private void InitializeResourceManager()
    {
        resourceManager = ResourceManager.Instance;
        if (resourceManager == null)
        {
            Debug.LogError($"[{nameof(ResourceStorageView)}] ResourceManager не найден!");
        }
    }

    /// <summary>
    /// Настройка начальных ресурсов
    /// </summary>
    private void SetupInitialResources()
    {
        if (resourceManager == null) return;

        var allResources = resourceManager.GetAllResources();

        foreach (var resource in allResources)
        {
            if (resource.Key != null)
            {
                CreateResourceUI(resource.Key, resource.Value);
                previousResourceValues[resource.Key] = resource.Value;
            }
            else
            {
                Debug.LogWarning($"[{nameof(ResourceStorageView)}] Найден null ResourceData в начальных ресурсах!");
            }
        }

        Debug.Log($"[{nameof(ResourceStorageView)}] Инициализировано {resourceUIItems.Count} UI элементов ресурсов");
    }

    #endregion

    #region Event Handling

    /// <summary>
    /// Подписка на события изменения ресурсов
    /// </summary>
    private void SubscribeToResourceEvents()
    {
        if (resourceManager == null) return;

        resourceManager.OnResourceChanged += HandleResourceChanged;
        resourceManager.OnResourceAdded += HandleResourceAdded;
        resourceManager.OnResourceRemoved += HandleResourceRemoved;

        Debug.Log($"[{nameof(ResourceStorageView)}] Подписались на события ResourceManager");
    }

    /// <summary>
    /// Отписка от событий
    /// </summary>
    private void UnsubscribeFromResourceEvents()
    {
        if (resourceManager == null) return;

        resourceManager.OnResourceChanged -= HandleResourceChanged;
        resourceManager.OnResourceAdded -= HandleResourceAdded;
        resourceManager.OnResourceRemoved -= HandleResourceRemoved;

        Debug.Log($"[{nameof(ResourceStorageView)}] Отписались от событий ResourceManager");
    }

    /// <summary>
    /// Обработка изменения ресурса
    /// </summary>
    private void HandleResourceChanged(ResourceData resourceData, float newValue, float previousValue)
    {
        if (resourceData == null)
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] Получен null ResourceData в HandleResourceChanged");
            return;
        }

        UpdateResourceUI(resourceData, newValue);
        OnResourceUIUpdated?.Invoke(resourceData, newValue);
    }

    /// <summary>
    /// Обработка добавления нового ресурса
    /// </summary>
    private void HandleResourceAdded(ResourceData resourceData, float initialValue)
    {
        if (resourceData == null)
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] Получен null ResourceData в HandleResourceAdded");
            return;
        }

        CreateResourceUI(resourceData, initialValue);
    }

    /// <summary>
    /// Обработка удаления ресурса
    /// </summary>
    private void HandleResourceRemoved(ResourceData resourceData)
    {
        if (resourceData == null)
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] Получен null ResourceData в HandleResourceRemoved");
            return;
        }

        RemoveResourceUI(resourceData);
    }

    #endregion

    #region UI Management

    /// <summary>
    /// Создание UI элемента для ресурса
    /// </summary>
    private void CreateResourceUI(ResourceData resourceData, float value)
    {
        if (resourceData == null)
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] Попытка создать UI для null ResourceData");
            return;
        }

        if (resourceUIItems.ContainsKey(resourceData))
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] UI для ресурса {resourceData.name} уже существует!");
            return;
        }

        GameObject itemObject = Instantiate(resourceItemPrefab, resourceContainer);
        ResourceItemUI itemUI = itemObject.GetComponent<ResourceItemUI>();

        if (itemUI == null)
        {
            Debug.LogError($"[{nameof(ResourceStorageView)}] В префабе отсутствует компонент ResourceItemUI!");
            Destroy(itemObject);
            return;
        }

        itemUI.Initialize(resourceData, value);
        resourceUIItems[resourceData] = itemUI;

        // Подписываемся на событие изменения значения UI элемента
        itemUI.OnValueChanged += OnResourceItemValueChanged;

        Debug.Log($"[{nameof(ResourceStorageView)}] Создан UI для ресурса: {resourceData.name}");
    }

    /// <summary>
    /// Обновление UI элемента ресурса
    /// </summary>
    private void UpdateResourceUI(ResourceData resourceData, float newValue)
    {
        if (resourceData == null) return;

        if (!resourceUIItems.TryGetValue(resourceData, out ResourceItemUI itemUI))
        {
            Debug.LogWarning($"[{nameof(ResourceStorageView)}] UI для ресурса {resourceData.name} не найден! Создаем новый.");
            CreateResourceUI(resourceData, newValue);
            return;
        }

        // Обновляем только если значение изменилось
        if (!previousResourceValues.TryGetValue(resourceData, out float previousValue) ||
            !Mathf.Approximately(previousValue, newValue))
        {
            itemUI.UpdateValue(newValue);
            previousResourceValues[resourceData] = newValue;

            // Анимация изменения (опционально)
            itemUI.PlayUpdateAnimation();
        }
    }

    /// <summary>
    /// Удаление UI элемента ресурса
    /// </summary>
    private void RemoveResourceUI(ResourceData resourceData)
    {
        if (resourceData == null) return;

        if (!resourceUIItems.TryGetValue(resourceData, out ResourceItemUI itemUI))
        {
            return;
        }

        // Отписываемся от события
        if (itemUI != null)
        {
            itemUI.OnValueChanged -= OnResourceItemValueChanged;
        }

        resourceUIItems.Remove(resourceData);
        previousResourceValues.Remove(resourceData);

        if (itemUI != null && itemUI.gameObject != null)
        {
            Destroy(itemUI.gameObject);
        }

        Debug.Log($"[{nameof(ResourceStorageView)}] Удален UI для ресурса: {resourceData.name}");
    }

    /// <summary>
    /// Обработка изменения значения в UI элементе
    /// </summary>
    private void OnResourceItemValueChanged(ResourceData resourceData, float newValue)
    {
        // Можно добавить дополнительную логику при изменении значения в UI
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Принудительное обновление всех UI элементов
    /// </summary>
    public void RefreshAllResourceUI()
    {
        if (resourceManager == null) return;

        var allResources = resourceManager.GetAllResources();

        foreach (var resource in allResources)
        {
            if (resource.Key != null)
            {
                UpdateResourceUI(resource.Key, resource.Value);
            }
        }

        Debug.Log($"[{nameof(ResourceStorageView)}] Обновлены все UI элементы ресурсов");
    }

    /// <summary>
    /// Очистка всех UI элементов
    /// </summary>
    public void ClearAllResourceUI()
    {
        foreach (var item in resourceUIItems.Values)
        {
            if (item != null)
            {
                item.OnValueChanged -= OnResourceItemValueChanged;

                if (item.gameObject != null)
                {
                    Destroy(item.gameObject);
                }
            }
        }

        resourceUIItems.Clear();
        previousResourceValues.Clear();

        Debug.Log($"[{nameof(ResourceStorageView)}] Очищены все UI элементы ресурсов");
    }

    /// <summary>
    /// Получение UI элемента ресурса по ResourceData
    /// </summary>
    public ResourceItemUI GetResourceUI(ResourceData resourceData)
    {
        if (resourceData == null) return null;

        resourceUIItems.TryGetValue(resourceData, out ResourceItemUI itemUI);
        return itemUI;
    }

    /// <summary>
    /// Проверка существования UI для ресурса
    /// </summary>
    public bool HasResourceUI(ResourceData resourceData)
    {
        if (resourceData == null) return false;
        return resourceUIItems.ContainsKey(resourceData);
    }

    /// <summary>
    /// Получение всех UI элементов ресурсов
    /// </summary>
    public Dictionary<ResourceData, ResourceItemUI> GetAllResourceUI()
    {
        return new Dictionary<ResourceData, ResourceItemUI>(resourceUIItems);
    }

    /// <summary>
    /// Установка видимости конкретного ресурса
    /// </summary>
    public void SetResourceUIVisible(ResourceData resourceData, bool visible)
    {
        if (resourceData == null) return;

        if (resourceUIItems.TryGetValue(resourceData, out ResourceItemUI itemUI))
        {
            itemUI.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Установка максимального значения для конкретного ресурса (для прогресс-бара)
    /// </summary>
    public void SetResourceMaxValue(ResourceData resourceData, float maxValue)
    {
        if (resourceData == null) return;

        if (resourceUIItems.TryGetValue(resourceData, out ResourceItemUI itemUI))
        {
            itemUI.SetMaxValue(maxValue);
        }
    }

    #endregion
}