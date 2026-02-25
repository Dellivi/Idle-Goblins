using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class ProgressBarWithTween : MonoBehaviour
{
    [SerializeField] private Image fillBar; // UI Image (fill type = Filled)
    [SerializeField] private TextMeshProUGUI text_time;

    private IdleAction action;
    private Tween activeTween;
    private bool isDestroyed = false;
    private bool isProcessingComplete = false; // Флаг для предотвращения рекурсии

    // Отложенные обновления для апгрейдов
    private IdleAction pendingAction;
    private bool hasPendingUpgrade = false;

    /// <summary>
    /// Привязывает прогресс-бар к действию
    /// </summary>
    public void Setup(IdleAction a)
    {
        if (a == null)
        {
            Debug.LogWarning("ProgressBarWithTween: null action setup");
            return;
        }

        // Сохраняем текущий прогресс если это обновление того же действия
        float previousProgress = 0f;
        if (action != null && action == a)
        {
            previousProgress = action.timer / action.GetDuration();
        }

        action = a;
        isDestroyed = false;
        isProcessingComplete = false;

        // Восстанавливаем прогресс если это было обновление
        if (previousProgress > 0 && previousProgress < 1f)
        {
            action.timer = previousProgress * action.GetDuration();
        }

        RefreshImmediately();
    }

    /// <summary>
    /// Обновление после завершения текущего цикла действия
    /// </summary>
    public void SetupWithCycleComplete(IdleAction a)
    {
        if (a == null) return;

        // Останавливаем текущий твин
        StopTween();

        // Обновляем действие
        action = a;

        // Сбрасываем таймер только если это НОВОЕ действие
        if (pendingAction != a)
        {
            action.timer = 0f;
        }

        isDestroyed = false;
        isProcessingComplete = false;
        pendingAction = null;
        hasPendingUpgrade = false;

        // Запускаем с нуля
        RefreshImmediately();
        StartTween();
    }

    private void OnDisable() => StopTween();

    private void OnDestroy()
    {
        isDestroyed = true;
        StopTween();
    }

    public void StopTween()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
        activeTween = null;

        pendingAction = null;
        hasPendingUpgrade = false;
        isProcessingComplete = false;

        if (fillBar != null) fillBar.fillAmount = 0f;
        if (text_time != null) text_time.text = "0.0s";
    }

    public void RefreshImmediately()
    {
        if (action == null || fillBar == null || isDestroyed) return;

        float progress = Mathf.Clamp01(action.timer / action.GetDuration());
        fillBar.fillAmount = progress;
        UpdateTimeText();
    }

    public void StartTween()
    {
        // Защита от рекурсии
        if (isProcessingComplete) return;

        if (action == null || fillBar == null || isDestroyed) return;

        StopTween();

        float currentProgress = Mathf.Clamp01(action.timer / action.GetDuration());
        float remaining = Mathf.Max(0f, action.GetDuration() - action.timer);

        // Если время истекло, просто обновляем и выходим
        if (remaining <= 0.001f)
        {
            fillBar.fillAmount = 1f;
            action.timer = action.GetDuration();
            UpdateTimeText();

            // Запускаем завершение без рекурсии
            isProcessingComplete = true;
            ProcessCycleComplete();
            isProcessingComplete = false;
            return;
        }

        fillBar.fillAmount = currentProgress;

        activeTween = fillBar.DOFillAmount(1f, remaining)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                if (action != null && !isDestroyed)
                {
                    action.timer = fillBar.fillAmount * action.GetDuration();
                    UpdateTimeText();
                }
            })
            .OnComplete(() =>
            {
                if (!isDestroyed)
                {
                    isProcessingComplete = true;
                    OnTweenComplete();
                    isProcessingComplete = false;
                }
            });

        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (text_time == null || action == null || isDestroyed) return;

        float timeRemaining = Mathf.Max(0f, action.GetDuration() - action.timer);

        if (timeRemaining <= 0.001f || action.level <= 0)
        {
            text_time.text = $"0.0s/{action.GetDuration():F1}s";
            return;
        }

        if (timeRemaining >= 3600)
        {
            int hours = (int)(timeRemaining / 3600);
            int minutes = (int)((timeRemaining % 3600) / 60);
            text_time.text = $"{hours}h {minutes}m";
        }
        else if (timeRemaining >= 60)
        {
            int minutes = (int)(timeRemaining / 60);
            int seconds = (int)(timeRemaining % 60);
            text_time.text = $"{minutes}m {seconds}s";
        }
        else
        {
            text_time.text = $"{timeRemaining:F1}s";
        }

        text_time.text += $"/{action.GetDuration():F1}s";
    }

    private void OnTweenComplete()
    {
        if (action == null || isDestroyed) return;

        // Проверяем отложенное обновление ДО сброса таймера
        if (hasPendingUpgrade && pendingAction != null)
        {
            action = pendingAction;
            pendingAction = null;
            hasPendingUpgrade = false;

            // Устанавливаем таймер в 0 для нового действия
            action.timer = 0f;

            if (fillBar != null) fillBar.fillAmount = 0f;

            // Запускаем новый твин
            if (!isDestroyed && !isProcessingComplete)
            {
                StartTween();
            }
            return;
        }

        // Сбрасываем таймер для нового цикла (только если нет отложенного обновления)
        action.timer = 0f;

        if (fillBar != null) fillBar.fillAmount = 0f;

        // Запускаем новый твин
        if (!isDestroyed && !isProcessingComplete)
        {
            StartTween();
        }
    }

    private void ProcessCycleComplete()
    {
        if (action == null || isDestroyed) return;

        // Проверяем отложенное обновление
        if (hasPendingUpgrade && pendingAction != null)
        {
            action = pendingAction;
            pendingAction = null;
            hasPendingUpgrade = false;

            action.timer = 0f;
        }
        else
        {
            action.timer = 0f;
        }

        if (fillBar != null) fillBar.fillAmount = 0f;

        // Запускаем новый твин
        if (!isDestroyed)
        {
            StartTween();
        }
    }

    public void UpdateProgress()
    {
        if (action == null || fillBar == null || isDestroyed || isProcessingComplete) return;

        if (action.timer >= action.GetDuration() - 0.001f)
        {
            isProcessingComplete = true;
            OnTweenComplete();
            isProcessingComplete = false;
        }
        else if (activeTween == null || !activeTween.IsActive())
        {
            RefreshImmediately();
        }
    }

    public void PauseTween()
    {
        activeTween?.Pause();
    }

    public void ResumeTween()
    {
        activeTween?.Play();
    }

    public float GetCurrentProgress()
    {
        return action == null ? 0f : Mathf.Clamp01(action.timer / action.GetDuration());
    }

    public void ForceUpdateProgress(float newTimer)
    {
        if (action == null || isProcessingComplete) return;

        action.timer = newTimer;
        RefreshImmediately();

        if (activeTween != null && activeTween.IsActive())
        {
            StopTween();
            StartTween();
        }
    }

    private void Awake()
    {
        if (fillBar == null)
            Debug.LogError($"ProgressBarWithTween на {gameObject.name}: fillBar не назначен!");
        if (text_time == null)
            Debug.LogError($"ProgressBarWithTween на {gameObject.name}: text_time не назначен!");
    }
}