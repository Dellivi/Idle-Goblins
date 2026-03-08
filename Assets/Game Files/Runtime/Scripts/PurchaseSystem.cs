using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Контроллер панели покупки/улучшения idle-производителя.
///
/// ОПТИМИЗАЦИИ ДЛЯ 50+ ПАНЕЛЕЙ:
/// - HandleResourceChanged фильтрует по relevantResources (HashSet, O(1) вместо foreach O(N))
/// - RefreshBuyButton обновляет кнопку только если affordability изменилась
/// - Подписка на ResourceManager только на нужные ресурсы через фильтр
/// </summary>
public class PurchaseSystem : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("References")]
    [SerializeField] private ProgressBarWithTween progressBar;
    [SerializeField] private Button buyButton;
    [SerializeField] private MultiCostView multiCostView;
    [SerializeField] private ResourceGetView resourceProduceView;
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private Image icon;

    [Header("Config (опционально, можно задать через Setup())")]
    [SerializeField] private ProductionConfig config;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private int currentLevel;
    private string saveKey;
    private IdleAction cachedAction;
    private bool isInitialized;

    // Кэш affordability — обновляем кнопку только при изменении
    private bool cachedCanAfford;

    // Кэш стоимостей — не аллоцируем словарь каждый вызов
    private readonly Dictionary<ResourceData, float> costCache = new();

    // HashSet для O(1) проверки relevance вместо foreach O(N) по costList
    private readonly HashSet<ResourceData> relevantResources = new();

    // ──────────────────────────────────────────────
    //  Свойства
    // ──────────────────────────────────────────────
    public ProductionConfig Config => config;
    public int CurrentLevel => currentLevel;
    public IdleAction CachedAction => cachedAction;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        buyButton?.onClick.AddListener(OnBuyClicked);
    }

    private void OnEnable()
    {
        SubscribeEvents();
        if (!isInitialized) return;
        if (progressBar != null && cachedAction != null)
            progressBar.Setup(cachedAction);
        RefreshUI();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────
    public void Setup(ProductionConfig cfg)
    {
        if (cfg == null)
        {
            Debug.LogError($"[PurchaseSystem] {gameObject.name}: null конфиг!", this);
            return;
        }

        config = cfg;
        saveKey = $"producer_{cfg.categoryId}_{cfg.actionId}_level";

        // Строим HashSet один раз — O(1) проверка в горячем пути
        relevantResources.Clear();
        foreach (var cost in cfg.costResourceList)
            if (cost.resource != null)
                relevantResources.Add(cost.resource);

        LoadLevel();
        ApplyToIdleManager();
        BuildUI();

        isInitialized = true;
    }

    // ──────────────────────────────────────────────
    //  Event Subscription
    // ──────────────────────────────────────────────
    private void SubscribeEvents()
    {
        if (IdleManager.Instance != null)
        {
            IdleManager.Instance.OnCycleComplete += HandleCycleComplete;
            IdleManager.Instance.OnMultiplierChanged += HandleMultiplierChanged;
        }

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged += HandleResourceChanged;
    }

    private void UnsubscribeEvents()
    {
        if (IdleManager.Instance != null)
        {
            IdleManager.Instance.OnCycleComplete -= HandleCycleComplete;
            IdleManager.Instance.OnMultiplierChanged -= HandleMultiplierChanged;
        }

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.OnResourceChanged -= HandleResourceChanged;
    }

    // ──────────────────────────────────────────────
    //  Button Handler
    // ──────────────────────────────────────────────
    private void OnBuyClicked()
    {
        if (!CanAfford()) return;

        SpendResources();
        currentLevel++;
        SaveLevel();

        // Обновляем relevantResources если стоимость могла измениться
        relevantResources.Clear();
        foreach (var cost in config.costResourceList)
            if (cost.resource != null)
                relevantResources.Add(cost.resource);

        cachedAction = IdleManager.Instance.RegisterOrUpdateAction(
            config.actionId, config.productionResource, config, currentLevel);

        progressBar?.Setup(cachedAction);
        RefreshUI();
    }

    // ──────────────────────────────────────────────
    //  Event Handlers
    // ──────────────────────────────────────────────
    private void HandleCycleComplete(IdleAction action, float produced)
    {
        if (action != cachedAction) return;
        resourceProduceView?.Show(produced);
    }

    private void HandleMultiplierChanged(float newMultiplier)
    {
        RefreshProductionView();
        ForceRefreshBuyButton();
    }

    /// <summary>
    /// Горячий путь — вызывается часто.
    /// HashSet.Contains = O(1). Ранний выход если ресурс нерелевантен.
    /// Обновляем кнопку только если affordability реально изменилась.
    /// </summary>
    private void HandleResourceChanged(ResourceData data, double newAmount, double delta)
    {
        // O(1) — HashSet вместо foreach по списку
        if (!relevantResources.Contains(data)) return;

        RefreshCostView();

        // Пересчитываем и обновляем кнопку только при изменении состояния
        bool canAffordNow = CanAfford();
        if (canAffordNow == cachedCanAfford) return;

        cachedCanAfford = canAffordNow;
        if (buyButton != null)
            buyButton.interactable = cachedCanAfford;
    }

    // ──────────────────────────────────────────────
    //  Idle Manager Integration
    // ──────────────────────────────────────────────
    private void ApplyToIdleManager()
    {
        if (currentLevel <= 0)
        {
            cachedAction = null;
            progressBar?.SetInactive();
            return;
        }

        cachedAction = IdleManager.Instance.RegisterOrUpdateAction(
            config.actionId, config.productionResource, config, currentLevel);

        progressBar?.Setup(cachedAction);
    }

    // ──────────────────────────────────────────────
    //  Economy
    // ──────────────────────────────────────────────
    protected virtual bool CanAfford()
    {
        if (config == null) return false;
        foreach (var cost in config.costResourceList)
            if (!ResourceManager.Instance.CanAfford(cost.resource, config.GetCostForLevel(cost, currentLevel)))
                return false;
        return true;
    }

    protected virtual void SpendResources()
    {
        foreach (var cost in config.costResourceList)
            ResourceManager.Instance.SpendResource(cost.resource, config.GetCostForLevel(cost, currentLevel));
    }

    protected virtual IReadOnlyDictionary<ResourceData, float> GetCurrentCosts()
    {
        costCache.Clear();
        foreach (var cost in config.costResourceList)
            costCache[cost.resource] = config.GetCostForLevel(cost, currentLevel);
        return costCache;
    }

    private float GetCurrentProduction()
    {
        if (cachedAction != null)
            return cachedAction.GetProductionPerCycle(IdleManager.Instance.GlobalMultiplier);
        return currentLevel > 0 ? config.GetProductionForLevel(currentLevel) : 0f;
    }

    // ──────────────────────────────────────────────
    //  UI
    // ──────────────────────────────────────────────
    private void BuildUI()
    {
        if (icon != null) icon.sprite = config.icon;
        if (textDescription != null) textDescription.text = config.categoryDescription.GetLocalizedString();
        if (resourceProduceView != null) resourceProduceView.Setup(config.productionResource);
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshNameText();
        RefreshCostView();
        RefreshProductionView();
        ForceRefreshBuyButton();
    }

    private void RefreshNameText()
    {
        if (textName == null) return;
        textName.text = config.categoryName.GetLocalizedString() +
                        (currentLevel > 0 ? $" lv.{currentLevel}" : " (не куплено)");
    }

    private void RefreshCostView()
        => multiCostView?.ShowCosts(GetCurrentCosts(), config.showRequirements);

    private void RefreshProductionView()
        => resourceProduceView?.Show(GetCurrentProduction());

    /// <summary>Полный пересчёт без проверки кэша — для явных вызовов.</summary>
    private void ForceRefreshBuyButton()
    {
        cachedCanAfford = CanAfford();
        if (buyButton != null)
            buyButton.interactable = cachedCanAfford;
    }

    // ──────────────────────────────────────────────
    //  Save / Load
    // ──────────────────────────────────────────────
    private void SaveLevel()
    {
        PlayerPrefs.SetInt(saveKey, currentLevel);
        PlayerPrefs.Save();
    }

    private void LoadLevel()
        => currentLevel = PlayerPrefs.GetInt(saveKey, 0);

    // ──────────────────────────────────────────────
    //  Editor
    // ──────────────────────────────────────────────
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (config != null && string.IsNullOrEmpty(saveKey))
            saveKey = $"producer_{config.categoryId}_{config.actionId}_level";
    }

    [ContextMenu("Reset Level (Debug)")]
    private void DebugResetLevel()
    {
        if (!Application.isPlaying) return;
        currentLevel = 0;
        PlayerPrefs.DeleteKey(saveKey);
        ApplyToIdleManager();
        RefreshUI();
    }

    [ContextMenu("Add Level (Debug)")]
    private void DebugAddLevel()
    {
        if (!Application.isPlaying) return;
        OnBuyClicked();
    }
#endif
}