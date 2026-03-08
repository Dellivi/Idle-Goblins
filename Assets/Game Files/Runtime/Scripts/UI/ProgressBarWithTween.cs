using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Прогресс-бар для IdleAction.
/// 
/// АРХИТЕКТУРА ДЛЯ 50+ ПАНЕЛЕЙ:
/// - fillAmount обновляется напрямую из action.GetProgress() в Update() — без DOTween-тика
/// - DOTween используется ТОЛЬКО для flash-эффекта при завершении цикла (короткий, автоубивается)
/// - Текст времени обновляется через throttle — не каждый кадр
/// - Подписывается на IdleManager.OnCycleComplete — знает когда сбросить цикл
/// </summary>
public class ProgressBarWithTween : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private Image fillBar;
    [SerializeField] private TextMeshProUGUI textTime;

    [Header("Inactive State")]
    [SerializeField] private GameObject inactiveOverlay;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color activeColor = Color.white;

    [Header("Cycle Flash")]
    [SerializeField] private bool flashOnComplete = true;
    [SerializeField] private Color flashColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Performance")]
    [Tooltip("Обновлять текст раз в N секунд. 0.1 = 10 раз/с, достаточно для читаемости.")]
    [SerializeField, Min(0.05f)] private float textUpdateInterval = 0.1f;

    // ──────────────────────────────────────────────
    //  Состояние
    // ──────────────────────────────────────────────
    private IdleAction cachedAction;
    private bool isActive;
    private float textUpdateTimer;
    private Tween flashTween;

    // Флаг: был ли прогресс в прошлом кадре близко к 1
    // Используется для детектирования завершения цикла без подписки
    private float prevProgress = -1f;

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    public void Setup(IdleAction action)
    {
        if (action == null)
        {
            Debug.LogWarning($"[ProgressBar] {gameObject.name}: Setup получил null action");
            return;
        }

        // Отписываемся от старого action если был
        UnsubscribeCycleEvent();

        cachedAction = action;
        isActive = true;
        prevProgress = action.GetProgress();
        textUpdateTimer = 0f;

        SetFillColor(activeColor);
        if (inactiveOverlay != null) inactiveOverlay.SetActive(false);

        if (fillBar != null)
            fillBar.fillAmount = prevProgress;

        RefreshTimeText();
        SubscribeCycleEvent();
    }

    public void SetInactive()
    {
        UnsubscribeCycleEvent();

        isActive = false;
        cachedAction = null;

        flashTween?.Kill();

        if (fillBar != null)
        {
            fillBar.fillAmount = 0f;
            SetFillColor(inactiveColor);
        }

        if (textTime != null)
            textTime.text = "—";

        if(inactiveOverlay != null) inactiveOverlay.SetActive(true);
    }

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────

    private void OnEnable()
    {
        if (isActive && cachedAction != null)
        {
            SubscribeCycleEvent();
            prevProgress = cachedAction.GetProgress();
            textUpdateTimer = 0f;

            if (fillBar != null)
                fillBar.fillAmount = prevProgress;

            RefreshTimeText();
        }
    }

    private void OnDisable()
    {
        UnsubscribeCycleEvent();
        flashTween?.Kill();
    }

    private void OnDestroy()
    {
        UnsubscribeCycleEvent();
        flashTween?.Kill();
    }

    /// <summary>
    /// Единственное место обновления визуала — прямое чтение из action.
    /// Никакого DOTween для прогресса. Один Update = одна операция fillAmount.
    /// </summary>
    private void Update()
    {
        if (!isActive || cachedAction == null || fillBar == null) return;

        float progress = cachedAction.GetProgress();
        fillBar.fillAmount = progress;

        // Текст — с throttle, не каждый кадр
        textUpdateTimer -= Time.deltaTime;
        if (textUpdateTimer <= 0f)
        {
            textUpdateTimer = textUpdateInterval;
            RefreshTimeText();
        }
    }

    // ──────────────────────────────────────────────
    //  Cycle Complete
    // ──────────────────────────────────────────────

    private void SubscribeCycleEvent()
    {
        if (IdleManager.Instance != null)
            IdleManager.Instance.OnCycleComplete += HandleCycleComplete;
    }

    private void UnsubscribeCycleEvent()
    {
        if (IdleManager.Instance != null)
            IdleManager.Instance.OnCycleComplete -= HandleCycleComplete;
    }

    private void HandleCycleComplete(IdleAction action, float produced)
    {
        if (action != cachedAction) return;

        PlayFlash();
    }

    // ──────────────────────────────────────────────
    //  Flash (единственное место где нужен DOTween)
    // ──────────────────────────────────────────────

    private void PlayFlash()
    {
        if (!flashOnComplete || fillBar == null) return;

        flashTween?.Kill();

        // SetColor → flashColor → activeColor. AutoKill = true по умолчанию.
        flashTween = fillBar
            .DOColor(flashColor, flashDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() =>
            {
                SetFillColor(activeColor);
                flashTween = null;
            });
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────

    private void RefreshTimeText()
    {
        if (textTime == null || cachedAction == null) return;

        float remain = cachedAction.GetRemainingTime();
        float duration = cachedAction.GetDuration();

        textTime.text = remain >= 60f
            ? $"{remain / 60f:F1}м / {duration / 60f:F1}м"
            : $"{remain:F1}с / {duration:F1}с";
    }

    private void SetFillColor(Color color)
    {
        if (fillBar != null) fillBar.color = color;
    }
}