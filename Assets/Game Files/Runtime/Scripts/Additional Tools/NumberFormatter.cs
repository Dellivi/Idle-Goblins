using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Утилита для форматирования больших чисел с поддержкой суффиксов и научной нотации
/// </summary>
public static class NumberFormatter
{
    private static readonly string[] Suffixes = {
        "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc",
        "Ud", "Dd", "Td", "Qad", "Qid", "Sxd", "Spd", "Ocd", "Nod", "Vg"
    };

    private static readonly string[] ScientificSuffixes = {
        "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
    };

    /// <summary>
    /// Форматирует число с суффиксом (K, M, B, T, etc.)
    /// </summary>
    public static string FormatWithSuffix(double num, int decimalPlaces = 2, bool useShortScale = true)
    {
        if (double.IsInfinity(num) || double.IsNaN(num) || num < 0)
            return "0";

        if (num < 1000)
            return num.ToString("F" + decimalPlaces);

        string[] suffixes = useShortScale ? ScientificSuffixes : Suffixes;

        int suffixIndex = 0;
        double tempNum = num;

        while (tempNum >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            tempNum /= 1000;
            suffixIndex++;
        }

        string format = tempNum >= 100 ? "F0" : (tempNum >= 10 ? "F1" : "F" + decimalPlaces);
        return tempNum.ToString(format) + suffixes[suffixIndex];
    }

    /// <summary>
    /// Форматирует число в научной нотации (e.g., 1.23e15)
    /// </summary>
    public static string FormatScientific(double num, int decimalPlaces = 2)
    {
        if (double.IsInfinity(num) || double.IsNaN(num) || num < 0)
            return "0";

        if (num < 1000)
            return num.ToString("F" + decimalPlaces);

        int exponent = (int)Mathf.Floor(Mathf.Log10((float)num));
        double mantissa = num / Mathf.Pow(10, exponent);

        return mantissa.ToString("F" + decimalPlaces) + "e" + exponent;
    }

    /// <summary>
    /// Умное форматирование: для маленьких чисел - суффиксы, для огромных - научная нотация
    /// </summary>
    public static string FormatSmart(double num, int decimalPlaces = 2, double scientificThreshold = 1e15)
    {
        if (double.IsInfinity(num) || double.IsNaN(num) || num < 0)
            return "0";

        if (num < 1000)
            return num.ToString("F" + decimalPlaces);

        if (num > scientificThreshold)
            return FormatScientific(num, decimalPlaces);

        return FormatWithSuffix(num, decimalPlaces);
    }

    /// <summary>
    /// Форматирует время в удобочитаемый формат
    /// </summary>
    public static string FormatTime(double seconds)
    {
        if (seconds < 0)
            return "0s";

        if (seconds < 1)
            return $"{(seconds * 1000):F0}ms";

        if (seconds < 60)
            return $"{seconds:F1}s";

        if (seconds < 3600)
        {
            int minutes = (int)(seconds / 60);
            int secs = (int)(seconds % 60);
            return $"{minutes}m {secs}s";
        }

        if (seconds < 86400)
        {
            int hours = (int)(seconds / 3600);
            int minutes = (int)((seconds % 3600) / 60);
            return $"{hours}h {minutes}m";
        }

        int days = (int)(seconds / 86400);
        int hoursLeft = (int)((seconds % 86400) / 3600);
        return $"{days}d {hoursLeft}h";
    }

    /// <summary>
    /// Форматирует валюту с соответствующим суффиксом
    /// </summary>
    public static string FormatCurrency(double num, string currencySymbol = "", int decimalPlaces = 2)
    {
        return currencySymbol + FormatSmart(num, decimalPlaces);
    }

    /// <summary>
    /// Получает цвет для числа на основе его величины
    /// </summary>
    public static Color GetColorForValue(double value, Color normalColor, Color highColor, Color veryHighColor,
                                        double highThreshold = 1e6, double veryHighThreshold = 1e9)
    {
        if (value >= veryHighThreshold)
            return veryHighColor;
        if (value >= highThreshold)
            return highColor;
        return normalColor;
    }
}