using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PurchaseSystem : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private ProgressBarWithTween progressBar;
    [SerializeField] private Button buyButton;
    [SerializeField] private MultiCostView multiCostView;
    [SerializeField] private ResourceGetView resourceProduceView;
    [SerializeField] private TextMeshProUGUI textName;
    [SerializeField] private TextMeshProUGUI textDescription;
    [SerializeField] private Image icon;

    [SerializeField] private PurchaseConfig config;

    private int currentLevel;
    private string saveKey;
    private IdleAction cachedAction;

    public PurchaseConfig Config { get => config; protected set => config = value; }
    public int CurrentLevel { get => currentLevel; protected set => currentLevel = value; }

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        // Подписываемся на события IdleManager
        if (IdleManager.Instance != null)
        {
            IdleManager.Instance.OnActionCycleComplete += OnCycleComplete;
        }
    }

    private void OnDisable()
    {
        if (IdleManager.Instance != null)
        {
            IdleManager.Instance.OnActionCycleComplete -= OnCycleComplete;
        }
    }

    public void Setup(PurchaseConfig config)
    {
        this.Config = config;

        if (config == null)
        {
            Debug.LogError("PurchaseSystem: Config not assigned!");
            return;
        }

        //LoadLevel();

        saveKey = $"{config.categoryId}_{config.actionId}_level";

        // Загружаем уровень, если еще не загружен
        if (CurrentLevel == 0)
        {
           // LoadLevel();
        }

        SetupUI();
        ApplyIdleAction();
    }

 private void OnClick()
{
    if (!CanAfford()) return;

    SpendResources();

    CurrentLevel++;
    SaveLevel();

    // ✅ ИСПРАВЛЕНО: Используем RegisterOrUpdateAction вместо UpgradeAction
    IdleManager.Instance.RegisterOrUpdateAction(
        Config.categoryId,
        Config.actionId,
        Config.productionResource,
        Config,
        CurrentLevel
    );

    // Обновляем кэш
    cachedAction = IdleManager.Instance.GetAction(Config.categoryId, Config.actionId);

    // Обновляем UI
    if (progressBar != null && cachedAction != null)
    {
        progressBar.SetupWithCycleComplete(cachedAction);
    }

    UpdateUI();
}

    private void OnCycleComplete(IdleAction action)
    {
        // Проверяем, относится ли событие к нашему действию
        if (action == cachedAction)
        {
            // Обновляем UI при завершении цикла
            if (resourceProduceView != null)
            {
                double production = action.GetProductionPerCycle(1f);
                resourceProduceView.Show(production);
            }

            // Прогресс-бар автоматически обновится через SetupWithCycleComplete
            if (progressBar != null)
            {
                progressBar.SetupWithCycleComplete(action);
            }
        }
    }

    #region Idle Integration

    private void ApplyIdleAction()
    {

        IdleManager.Instance.RegisterOrUpdateAction(
            Config.categoryId,
            Config.actionId,
            Config.productionResource,
            Config,
            CurrentLevel
        );

        cachedAction = IdleManager.Instance.GetAction(Config.categoryId, Config.actionId);

        // Настраиваем прогресс-бар
        if (progressBar != null && cachedAction != null)
        {
            progressBar.Setup(cachedAction); // Используем Setup вместо SetupWithCycleComplete для первого запуска
        }

        // Обновляем отображение производства
        if (resourceProduceView != null && cachedAction != null)
        {
            float production = cachedAction.GetProductionPerCycle(1f);
            resourceProduceView.Show(Mathf.RoundToInt(production));
        }
    }

    #endregion

    #region Economy

    protected virtual bool CanAfford()
    {
        foreach (var cost in Config.costResourceList)
        {
            float amount = Config.GetCostForLevel(cost, CurrentLevel);
            if (!ResourceManager.Instance.CanAfford(cost.resource, amount))
                return false;
        }

        return true;
    }

    protected virtual void SpendResources()
    {
        foreach (var cost in Config.costResourceList)
        {
            float amount = Config.GetCostForLevel(cost, CurrentLevel);
            ResourceManager.Instance.SpendResource(cost.resource, amount);
        }
    }

    private double GetCurrentProduction()
    {
        if (cachedAction != null)
            return cachedAction.GetProductionPerCycle(1f);

        return Config.GetProductionForLevel(CurrentLevel);
    }

    protected virtual Dictionary<ResourceData, float> GetCurrentCosts()
    {
        Dictionary<ResourceData, float> result = new();

        foreach (var cost in Config.costResourceList)
        {
            result[cost.resource] = Config.GetCostForLevel(cost, CurrentLevel);
        }

        return result;
    }

    #endregion

    #region UI

    private void SetupUI()
    {
        if (textName != null)
            textName.text = $"{Config.categoryName.GetLocalizedString()} lvl {CurrentLevel}";

        if (textDescription != null)
            textDescription.text = Config.categoryDescription.GetLocalizedString();

        if (icon != null)
            icon.sprite = Config.icon;

        if (resourceProduceView != null)
            resourceProduceView.Setup(Config.productionResource);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (multiCostView != null)
            multiCostView.ShowCosts(GetCurrentCosts(), config.showRequirements);

        if (resourceProduceView != null)
            resourceProduceView.Show(GetCurrentProduction());

        if (buyButton != null)
            buyButton.interactable = CanAfford();

        if (textName != null)
            textName.text = $"{Config.categoryName.GetLocalizedString()} lvl {CurrentLevel}";
    }

    #endregion

    #region Save / Load

    private void SaveLevel()
    {
        PlayerPrefs.SetInt(saveKey, CurrentLevel);
        PlayerPrefs.Save();
        Debug.Log($"Saved level {CurrentLevel} for {Config?.name}");
    }

    private void LoadLevel()
    {
        if (!string.IsNullOrEmpty(saveKey))
        {
            CurrentLevel = PlayerPrefs.GetInt(saveKey, 0);
            Debug.Log($"Loaded level {CurrentLevel} for {Config?.name}");
        }
    }

    #endregion

    // Для отладки
    private void OnValidate()
    {
        if (Config != null && string.IsNullOrEmpty(saveKey))
        {
            saveKey = $"{Config.categoryId}_{Config.actionId}_level";
        }
    }
}