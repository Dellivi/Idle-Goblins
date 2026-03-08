// ══════════════════════════════════════════════════════════════════════════════
//  Типы модификаторов
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Как модификатор применяется к значению.
/// </summary>
public enum ModifierType
{
    /// <summary>Прибавка к итоговому множителю: value += bonus (1.0 = +100%)</summary>
    Additive = 0,

    /// <summary>Перемножается с итоговым: value *= bonus (1.5 = +50%)</summary>
    Multiplicative = 1,
}

/// <summary>
/// На что нацелен модификатор.
/// </summary>
public enum ModifierTarget
{
    /// <summary>Производство конкретного ресурса (все action'ы этого ресурса)</summary>
    ResourceProduction = 0,

    /// <summary>Производство конкретного action (по actionId)</summary>
    ActionProduction = 1,

    /// <summary>Скорость конкретного action (уменьшает duration)</summary>
    ActionSpeed = 2,

    /// <summary>Глобальное производство (все ресурсы, все action'ы)</summary>
    GlobalProduction = 3,
}

// ══════════════════════════════════════════════════════════════════════════════
//  Modifier — единица эффекта
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Один модификатор. Создаётся и хранится источником (зданием, апгрейдом и т.д.).
/// Источник сам отвечает за добавление и удаление.
/// </summary>
public class Modifier
{
    public readonly string id;           // уникальный идентификатор
    public readonly ModifierTarget target;
    public readonly ModifierType type;
    public readonly float value;

    // Для ResourceProduction — на какой ресурс влияет
    public readonly ResourceData targetResource;

    // Для ActionProduction / ActionSpeed — на какой action влияет
    public readonly string targetActionId;

    // Опционально: источник для отладки
    public readonly string sourceLabel;

    // ── Конструкторы ────────────────────────────────────────────────────────

    /// <summary>Модификатор на конкретный ресурс.</summary>
    public Modifier(string id, ModifierTarget target, ModifierType type,
                    float value, ResourceData resource, string sourceLabel = "")
    {
        this.id = id;
        this.target = target;
        this.type = type;
        this.value = value;
        this.targetResource = resource;
        this.sourceLabel = sourceLabel;
    }

    /// <summary>Модификатор на конкретный action.</summary>
    public Modifier(string id, ModifierTarget target, ModifierType type,
                    float value, string actionId, string sourceLabel = "")
    {
        this.id = id;
        this.target = target;
        this.type = type;
        this.value = value;
        this.targetActionId = actionId;
        this.sourceLabel = sourceLabel;
    }

    /// <summary>Глобальный модификатор.</summary>
    public Modifier(string id, ModifierType type, float value, string sourceLabel = "")
    {
        this.id = id;
        this.target = ModifierTarget.GlobalProduction;
        this.type = type;
        this.value = value;
        this.sourceLabel = sourceLabel;
    }
}
