using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Центральный менеджер idle-производства.
///
/// ОПТИМИЗАЦИИ ДЛЯ 50+ ДЕЙСТВИЙ:
/// - Все AddResource за кадр буферизируются, событие OnResourceChanged стреляет
///   один раз в LateUpdate — вместо N раз в Update
/// - OnCycleComplete собирается в список за кадр, диспатчится после тиков —
///   без вызовов из середины цикла
/// - Никаких аллокаций в горячем пути (Update/LateUpdate)
/// </summary>
public class IdleManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────
    public static IdleManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("Balance")]
    [SerializeField, Min(0.01f)] private float globalMultiplier = 1f;

    [Header("Offline")]
    [SerializeField] private bool applyOfflineProgress = true;
    [SerializeField, Min(0f)] private float maxOfflineSeconds = 8f * 3600f;
    [SerializeField, Range(0f, 1f)] private float offlineEfficiency = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private readonly Dictionary<string, IdleAction> actionMap = new();
    private readonly List<IdleAction> actionList = new();

    private float _globalMultiplier;

    // ── Буферы кадра (нет аллокаций — переиспользуются каждый кадр) ──────────

    // Накопленное производство по ресурсу за кадр
    // ResourceManager.AddResource вызовется ОДИН РАЗ на ресурс в LateUpdate
    private readonly Dictionary<ResourceData, float> frameProductionBuffer = new();

    // Завершённые циклы за кадр: (action, totalProduced)
    // Диспатч событий — после тиков, не во время
    private readonly List<(IdleAction action, float produced)> frameCycleEvents = new();

    // ──────────────────────────────────────────────
    //  События
    // ──────────────────────────────────────────────

    /// <summary>Цикл завершился. Вызывается в LateUpdate, не в Update.</summary>
    public event Action<IdleAction, float> OnCycleComplete;

    /// <summary>Зарегистрировано новое действие или обновлён уровень.</summary>
    public event Action<IdleAction> OnActionRegistered;

    /// <summary>Глобальный множитель изменился.</summary>
    public event Action<float> OnMultiplierChanged;

    // ──────────────────────────────────────────────
    //  Свойства
    // ──────────────────────────────────────────────
    public float GlobalMultiplier
    {
        get => globalMultiplier;
        set
        {
            globalMultiplier = Mathf.Max(0.01f, value);
            _globalMultiplier = globalMultiplier;
            OnMultiplierChanged?.Invoke(_globalMultiplier);
        }
    }

    public IReadOnlyList<IdleAction> Actions => actionList;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _globalMultiplier = globalMultiplier;
    }

    private void Start()
    {
        if (applyOfflineProgress)
            ProcessOfflineProgress();
    }

    private void OnApplicationPause(bool pause) { if (pause) SaveLastOnlineTime(); }
    private void OnApplicationQuit() { SaveLastOnlineTime(); }

    /// <summary>
    /// Только тики и накопление буферов. Никаких событий, никаких AddResource.
    /// </summary>
    private void Update()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < actionList.Count; i++)
        {
            var action = actionList[i];
            if (!action.isActive) continue;

            int cycles = action.Tick(dt, out float produced);
            if (cycles <= 0) continue;

            float totalProduced = action.GetProductionPerCycle(_globalMultiplier) * cycles;

            // Буферизируем производство — не вызываем AddResource сейчас
            if (frameProductionBuffer.TryGetValue(action.resource, out float existing))
                frameProductionBuffer[action.resource] = existing + totalProduced;
            else
                frameProductionBuffer[action.resource] = totalProduced;

            // Буферизируем событие завершения цикла
            frameCycleEvents.Add((action, totalProduced));
        }
    }

    /// <summary>
    /// Диспатч всего накопленного за кадр:
    /// - AddResource вызывается ОДИН РАЗ на уникальный ресурс
    /// - OnCycleComplete вызывается для каждого завершённого цикла
    /// - OnResourceChanged (из ResourceManager) стреляет минимальное число раз
    /// </summary>
    private void LateUpdate()
    {
        // Один AddResource на ресурс → один OnResourceChanged на ресурс
        foreach (var kv in frameProductionBuffer)
            ResourceManager.Instance.AddResource(kv.Key, kv.Value);

        frameProductionBuffer.Clear();

        // Диспатч событий завершения циклов
        for (int i = 0; i < frameCycleEvents.Count; i++)
        {
            var (action, produced) = frameCycleEvents[i];
            OnCycleComplete?.Invoke(action, produced);

            if (verboseLog)
                Debug.Log($"[IdleManager] {action.actionId} → +{produced:F2} {action.resource?.name}");
        }

        frameCycleEvents.Clear();
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public IdleAction RegisterOrUpdateAction(
        string actionId,
        ResourceData resource,
        ProductionConfig config,
        int level)
    {
        if (string.IsNullOrEmpty(actionId))
        {
            Debug.LogError("[IdleManager] actionId не может быть пустым");
            return null;
        }

        if (!actionMap.TryGetValue(actionId, out var action))
        {
            action = new IdleAction { actionId = actionId, timer = 0f };
            actionMap[actionId] = action;
            actionList.Add(action);
        }

        action.resource = resource;
        action.config = config;
        action.level = level;
        action.isActive = level > 0 && config != null && resource != null;

        OnActionRegistered?.Invoke(action);

        if (verboseLog)
            Debug.Log($"[IdleManager] Registered '{actionId}' lv={level} active={action.isActive}");

        return action;
    }

    public IdleAction GetAction(string actionId)
    {
        actionMap.TryGetValue(actionId, out var action);
        return action;
    }

    public float GetTotalProductionPerSecond(ResourceData resource)
    {
        float total = 0f;
        for (int i = 0; i < actionList.Count; i++)
        {
            var a = actionList[i];
            if (a.isActive && a.resource == resource)
                total += a.GetProductionPerSecond(_globalMultiplier);
        }
        return total;
    }

    public void AddTemporaryBoost(float multiplierAdd, float durationSeconds)
        => StartCoroutine(TemporaryBoostRoutine(multiplierAdd, durationSeconds));

    // ──────────────────────────────────────────────
    //  Offline Progress
    // ──────────────────────────────────────────────
    private const string LastOnlineKey = "IdleManager_LastOnlineTime";

    private void SaveLastOnlineTime()
    {
        PlayerPrefs.SetString(LastOnlineKey, DateTime.UtcNow.ToBinary().ToString());
        PlayerPrefs.Save();
    }

    private void ProcessOfflineProgress()
    {
        string saved = PlayerPrefs.GetString(LastOnlineKey, string.Empty);
        if (string.IsNullOrEmpty(saved)) return;
        if (!long.TryParse(saved, out long binary)) return;

        float offlineSeconds = (float)(DateTime.UtcNow - DateTime.FromBinary(binary)).TotalSeconds;
        offlineSeconds = Mathf.Clamp(offlineSeconds, 0f, maxOfflineSeconds);
        if (offlineSeconds < 1f) return;

        float effective = offlineSeconds * offlineEfficiency;

        // Буферизируем оффлайн так же как онлайн
        for (int i = 0; i < actionList.Count; i++)
        {
            var action = actionList[i];
            if (!action.isActive) continue;

            float produced = action.SimulateOffline(effective, _globalMultiplier);
            if (produced <= 0f) continue;

            if (frameProductionBuffer.TryGetValue(action.resource, out float existing))
                frameProductionBuffer[action.resource] = existing + produced;
            else
                frameProductionBuffer[action.resource] = produced;
        }

        // Сбрасываем сразу — Start() вызывается до первого Update/LateUpdate
        foreach (var kv in frameProductionBuffer)
            ResourceManager.Instance.AddResource(kv.Key, kv.Value);

        frameProductionBuffer.Clear();

        Debug.Log($"[IdleManager] Оффлайн: {offlineSeconds:F0}с × {offlineEfficiency * 100f:F0}%");
    }

    // ──────────────────────────────────────────────
    //  Coroutines
    // ──────────────────────────────────────────────
    private System.Collections.IEnumerator TemporaryBoostRoutine(float add, float duration)
    {
        GlobalMultiplier += add;
        yield return new WaitForSeconds(duration);
        GlobalMultiplier -= add;
    }

    // ──────────────────────────────────────────────
    //  Debug
    // ──────────────────────────────────────────────
    [ContextMenu("Log All Actions")]
    private void LogAllActions()
    {
        Debug.Log($"[IdleManager] Всего: {actionList.Count}");
        foreach (var a in actionList)
            Debug.Log($"  [{a.actionId}] lv={a.level} active={a.isActive} " +
                      $"timer={a.timer:F2}/{a.GetDuration():F2} " +
                      $"prod/s={a.GetProductionPerSecond(_globalMultiplier):F3}");
    }
}