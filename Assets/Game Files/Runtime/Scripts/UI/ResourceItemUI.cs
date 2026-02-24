using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI элемент для отображения отдельного ресурса
/// </summary>
public class ResourceItemUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI resourceNameText;
    [SerializeField] private TextMeshProUGUI resourceValueText;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.1f);
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Value Display Settings")]
    [SerializeField] private string valueFormat = "F0"; // Формат отображения числа
    [SerializeField] private bool showValueChange = true;
    [SerializeField] private Color increaseColor = Color.green;
    [SerializeField] private Color decreaseColor = Color.red;

    // Данные ресурса
    private ResourceData resourceData;
    private float currentValue;
    private float previousValue;
    private float maxValue = -1f; // -1 означает без лимита

    // Компоненты анимации
    private Coroutine updateAnimationCoroutine;
    private Coroutine valueChangeCoroutine;

    // События
    public event Action<ResourceData, float> OnValueChanged;

    #region Properties

    public ResourceData ResourceData => resourceData;
    public float CurrentValue => currentValue;
    public float MaxValue => maxValue;

    #endregion

    #region Initialization

    /// <summary>
    /// Инициализация элемента ресурса
    /// </summary>
    public void Initialize(ResourceData resource, float initialValue, float maxVal = -1f)
    {
        if (resource == null)
        {
            Debug.LogError("[ResourceItemUI] Попытка инициализации с null ResourceData");
            return;
        }

        resourceData = resource;
        currentValue = initialValue;
        previousValue = initialValue;
        maxValue = maxVal;

        UpdateDisplay();

        Debug.Log($"[ResourceItemUI] Инициализирован ресурс: {resource.name} = {initialValue}");
    }

    #endregion

    #region Value Management

    /// <summary>
    /// Обновление значения ресурса
    /// </summary>
    public void UpdateValue(float newValue)
    {
        if (Mathf.Approximately(currentValue, newValue))
            return;

        previousValue = currentValue;
        currentValue = newValue;

        UpdateDisplay();
        OnValueChanged?.Invoke(resourceData, newValue);

        // Показываем изменение значения
        if (showValueChange)
        {
            ShowValueChange();
        }
    }

    /// <summary>
    /// Установка максимального значения для прогресс-бара
    /// </summary>
    public void SetMaxValue(float maxVal)
    {
        maxValue = maxVal;
        UpdateResourceValue(); // Обновляем отображение значения
    }

    #endregion

    #region Display Updates

    /// <summary>
    /// Обновление всего отображения
    /// </summary>
    private void UpdateDisplay()
    {
        UpdateResourceName();
        UpdateResourceValue();
        UpdateIcon();
    }

    /// <summary>
    /// Обновление названия ресурса
    /// </summary>
    private void UpdateResourceName()
    {
        if (resourceNameText != null && resourceData != null)
        {
            // Используем локализованное название если доступно
            string displayName = resourceData.nameResource?.GetLocalizedString() ?? resourceData.name;
            resourceNameText.text = displayName;
        }
    }

    /// <summary>
    /// Обновление значения ресурса
    /// </summary>
    private void UpdateResourceValue()
    {
        if (resourceValueText != null)
        {
            string valueString = FormatValue(currentValue);

            // Добавляем максимальное значение если установлено
            if (maxValue > 0)
            {
                valueString += $" / {FormatValue(maxValue)}";
            }

            resourceValueText.text = valueString;
        }
    }

    /// <summary>
    /// Обновление иконки ресурса
    /// </summary>
    private void UpdateIcon()
    {
        if (resourceIcon != null && resourceData != null)
        {
            // Используем иконку из ResourceData
            if (resourceData.icon != null)
            {
                resourceIcon.sprite = resourceData.icon;
                resourceIcon.gameObject.SetActive(true);
            }
            else
            {
                // Если иконка не установлена в ResourceData, пробуем загрузить из Resources
                Sprite fallbackSprite = Resources.Load<Sprite>($"ResourceIcons/{resourceData.name}");
                if (fallbackSprite != null)
                {
                    resourceIcon.sprite = fallbackSprite;
                    resourceIcon.gameObject.SetActive(true);
                }
                else
                {
                    resourceIcon.gameObject.SetActive(false);
                }
            }
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Воспроизведение анимации обновления
    /// </summary>
    public void PlayUpdateAnimation()
    {
        if (updateAnimationCoroutine != null)
        {
            StopCoroutine(updateAnimationCoroutine);
        }

        updateAnimationCoroutine = StartCoroutine(UpdateAnimationCoroutine());
    }

    /// <summary>
    /// Корутина анимации обновления
    /// </summary>
    private IEnumerator UpdateAnimationCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Анимация масштаба
            float scaleMultiplier = scaleCurve.Evaluate(progress);
            transform.localScale = originalScale * scaleMultiplier;

            yield return null;
        }

        // Возвращаем исходное состояние
        transform.localScale = originalScale;

        updateAnimationCoroutine = null;
    }

    /// <summary>
    /// Показ изменения значения
    /// </summary>
    private void ShowValueChange()
    {
        if (valueChangeCoroutine != null)
        {
            StopCoroutine(valueChangeCoroutine);
        }

        valueChangeCoroutine = StartCoroutine(ValueChangeCoroutine());
    }

    /// <summary>
    /// Корутина показа изменения значения
    /// </summary>
    private IEnumerator ValueChangeCoroutine()
    {
        if (resourceValueText == null) yield break;

        float difference = currentValue - previousValue;
        bool isIncrease = difference > 0;

        // Меняем цвет текста
        Color originalColor = resourceValueText.color;
        resourceValueText.color = isIncrease ? increaseColor : decreaseColor;

        yield return new WaitForSeconds(0.5f);

        // Возвращаем исходный цвет
        resourceValueText.color = originalColor;

        valueChangeCoroutine = null;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Форматирование значения для отображения
    /// </summary>
    private string FormatValue(float value)
    {
        // Используем сокращения для больших чисел
        if (value >= 1000000000f) // Миллиарды
        {
            return (value / 1000000000f).ToString("F1") + "B";
        }
        else if (value >= 1000000f) // Миллионы
        {
            return (value / 1000000f).ToString("F1") + "M";
        }
        else if (value >= 1000f) // Тысячи
        {
            return (value / 1000f).ToString("F1") + "K";
        }

        return value.ToString(valueFormat);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Установка кастомной иконки
    /// </summary>
    public void SetIcon(Sprite icon)
    {
        if (resourceIcon != null)
        {
            resourceIcon.sprite = icon;
            resourceIcon.gameObject.SetActive(icon != null);
        }
    }

    /// <summary>
    /// Получение ResourceData этого UI элемента
    /// </summary>
    public ResourceData GetResourceData()
    {
        return resourceData;
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // Останавливаем все корутины при уничтожении
        if (updateAnimationCoroutine != null)
        {
            StopCoroutine(updateAnimationCoroutine);
        }

        if (valueChangeCoroutine != null)
        {
            StopCoroutine(valueChangeCoroutine);
        }
    }

    #endregion
}