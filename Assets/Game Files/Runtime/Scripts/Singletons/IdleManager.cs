using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IdleAction
{
    public string actionId;
    public string categoryId;
    public ResourceData resource;
    public PurchaseConfig config;
    public int level;

    [NonSerialized] public float timer; // внутренний таймер

    public float GetProductionPerCycle(float globalMultiplier)
    {
        return config.GetProductionForLevel(level) * globalMultiplier;
    }

    public float GetDuration()
    {
        return config.GetDurationForLevel(level);
    }

    public float GetProductionPerSecond(float globalMultiplier)
    {
        return GetProductionPerCycle(globalMultiplier) / GetDuration();
    }
}

[Serializable]
public class IdleCategory
{
    public string categoryId;
    public List<IdleAction> actions = new();
    public Dictionary<string, IdleAction> actionDict = new();

    public IdleCategory(string id)
    {
        categoryId = id;
        actions = new List<IdleAction>();
        actionDict = new Dictionary<string, IdleAction>();
    }

    public void AddAction(IdleAction action)
    {
        actions.Add(action);
        actionDict[action.actionId] = action;
    }

    public void RemoveAction(string actionId)
    {
        if (actionDict.TryGetValue(actionId, out var action))
        {
            actions.Remove(action);
            actionDict.Remove(actionId);
        }
    }

    public IdleAction GetAction(string actionId)
    {
        actionDict.TryGetValue(actionId, out var action);
        return action;
    }
}


public class IdleManager : MonoBehaviour
{
    public static IdleManager Instance;

    public float globalProductionMultiplier = 1f;
    public float tickInterval = 0.1f;

    private Dictionary<string, IdleCategory> categories = new();
    private List<IdleAction> activeActions = new();

    private float tickTimer;

    // События для UI
    public event Action<IdleAction> OnActionCycleComplete;
    public event Action<IdleAction> OnActionPaybackComplete;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval)
        {
            ProcessActions(tickTimer);
            tickTimer = 0f;
        }
    }

    private void ProcessActions(float deltaTime)
    {
        for (int i = activeActions.Count - 1; i >= 0; i--)
        {
            var action = activeActions[i];
            if (action == null) continue;

            float duration = action.GetDuration();

            // Таймер производства
            action.timer += deltaTime;


            // Проверка завершения цикла производства
            if (action.timer >= duration)
            {
                int cycles = Mathf.FloorToInt(action.timer / duration);
                float production = action.GetProductionPerCycle(globalProductionMultiplier);

                ResourceManager.Instance?.AddResource(action.resource, production * cycles);

                action.timer -= cycles * duration;

                // Вызываем событие для UI
                OnActionCycleComplete?.Invoke(action);
            }
        }
    }

    #region CATEGORY SYSTEM

    public IdleCategory GetOrCreateCategory(string categoryId)
    {
        if (!categories.TryGetValue(categoryId, out var category))
        {
            category = new IdleCategory(categoryId);
            categories.Add(categoryId, category);
        }

        return category;
    }

    public IdleAction GetAction(string categoryId, string actionId)
    {
        if (!categories.TryGetValue(categoryId, out var category))
            return null;

        return category.GetAction(actionId);
    }

    #endregion

    #region ACTION CONTROL

    public void RegisterOrUpdateAction(
        string categoryId,
        string actionId,
        ResourceData resource,
        PurchaseConfig config,
        int level)
    {
        var category = GetOrCreateCategory(categoryId);
        var action = category.GetAction(actionId);

        if (action == null)
        {
            action = new IdleAction
            {
                categoryId = categoryId,
                actionId = actionId,
                resource = resource,
                config = config,
                level = level,
                timer = 0f,
            };

            category.AddAction(action);

            if (level > 0)
                activeActions.Add(action);
        }
        else
        {
            action.resource = resource;
            action.config = config;
            action.level = level;

            if (level > 0 && !activeActions.Contains(action))
                activeActions.Add(action);
            else if (level <= 0 && activeActions.Contains(action))
                activeActions.Remove(action);
        }
    }

    public void UpgradeAction(string categoryId, string actionId, int newLevel)
    {
        var action = GetAction(categoryId, actionId);
        if (action == null) return;

        action.level = newLevel;
    }

    public void RemoveAction(string categoryId, string actionId)
    {
        var action = GetAction(categoryId, actionId);
        if (action == null) return;

        activeActions.Remove(action);

        if (categories.TryGetValue(categoryId, out var category))
            category.RemoveAction(actionId);
    }

    #endregion

    #region ANALYTICS

    public Dictionary<ResourceData, float> GetTotalProductionPerSecond(string categoryId = null)
    {
        Dictionary<ResourceData, float> result = new();

        if (string.IsNullOrEmpty(categoryId))
        {
            // По всем категориям
            foreach (var category in categories.Values)
            {
                foreach (var action in category.actions)
                {
                    float perSecond = action.GetProductionPerSecond(globalProductionMultiplier);

                    if (result.ContainsKey(action.resource))
                        result[action.resource] += perSecond;
                    else
                        result[action.resource] = perSecond;
                }
            }
        }
        else if (categories.TryGetValue(categoryId, out var category))
        {
            foreach (var action in category.actions)
            {
                float perSecond = action.GetProductionPerSecond(globalProductionMultiplier);

                if (result.ContainsKey(action.resource))
                    result[action.resource] += perSecond;
                else
                    result[action.resource] = perSecond;
            }
        }

        return result;
    }

    #endregion

    #region OFFLINE EARNINGS

    public void ApplyOfflineEarnings(float offlineTime)
    {
        foreach (var action in activeActions)
        {
            if (action == null) continue;

            action.timer += offlineTime;

            float duration = action.GetDuration();

            if (action.timer >= duration)
            {
                int cycles = Mathf.FloorToInt(action.timer / duration);
                float production = action.GetProductionPerCycle(globalProductionMultiplier);

                ResourceManager.Instance?.AddResource(action.resource, production * cycles);
                action.timer -= cycles * duration;
            }
        }
    }

    #endregion
}