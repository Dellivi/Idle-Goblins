using UnityEditor;
using UnityEngine;
using System.Linq;

public class PurchaseConfigROIWindow : EditorWindow
{
    private PurchaseConfig targetConfig;

    private int maxLevelToAnalyze = 25;
    private int selectedResourceIndex = 0;
    private Vector2 scrollPos;

    private bool showGraph = true;
    private bool showStats = true;
    private bool showRecommendations = true;

    [MenuItem("Tools/Purchase ROI Analyzer")]
    public static void ShowWindow()
    {
        GetWindow<PurchaseConfigROIWindow>("ROI Analyzer");
    }

    private void OnGUI()
    {
        DrawHeader();

        targetConfig = (PurchaseConfig)EditorGUILayout.ObjectField(
            "Purchase Config",
            targetConfig,
            typeof(PurchaseConfig),
            false);

        if (targetConfig == null)
        {
            EditorGUILayout.HelpBox("Assign PurchaseConfig to analyze.", MessageType.Info);
            return;
        }

        if (targetConfig.costResourceList == null ||
            targetConfig.costResourceList.Count == 0)
        {
            EditorGUILayout.HelpBox("No cost resources defined!", MessageType.Warning);
            return;
        }

        DrawResourceSelector();

        maxLevelToAnalyze = EditorGUILayout.IntSlider(
            "Max Level",
            maxLevelToAnalyze,
            1,
            200);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawLevelTable();

        if (showGraph)
            DrawGraph();

        if (showStats)
            DrawStats();

        if (showRecommendations)
            DrawRecommendations();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("ROI / Payback Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
    }

    private void DrawResourceSelector()
    {
        EditorGUILayout.LabelField("Analyze Resource", EditorStyles.boldLabel);

        string[] names = targetConfig.costResourceList
            .Select(r => r.resource != null ? r.resource.name : "Null")
            .ToArray();

        selectedResourceIndex = EditorGUILayout.Popup(
            selectedResourceIndex,
            names);
    }

    private void DrawLevelTable()
    {
        var costData = targetConfig.costResourceList[selectedResourceIndex];

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Data", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float cost = targetConfig.GetCostForLevel(costData, level);
            float production = targetConfig.GetProductionForLevel(level);
            float roi = targetConfig.GetROI(costData, level);
            float payback = targetConfig.GetPaybackTime(costData, level);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"Lv {level}", GUILayout.Width(40));
            EditorGUILayout.LabelField($"Cost: {FormatNumber(cost)}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Prod: {FormatNumber(production)}", GUILayout.Width(100));

            // Цвет ROI
            Color originalColor = GUI.color;
            GUI.color = roi >= 1.5f ? Color.green : (roi >= 1f ? Color.yellow : Color.red);
            EditorGUILayout.LabelField($"ROI: {roi:F2}", GUILayout.Width(70));

            // Цвет Payback
            GUI.color = payback <= 15f ? Color.green : (payback <= 30f ? Color.yellow : Color.red);
            EditorGUILayout.LabelField($"Pay: {FormatTime(payback)}", GUILayout.Width(80));

            GUI.color = originalColor;

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawGraph()
    {
        var costData = targetConfig.costResourceList[selectedResourceIndex];

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Cost & Production Graph", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(400, 250);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        // Собираем данные
        float[] costs = new float[maxLevelToAnalyze];
        float[] productions = new float[maxLevelToAnalyze];
        float maxValue = 0f;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            costs[level - 1] = targetConfig.GetCostForLevel(costData, level);
            productions[level - 1] = targetConfig.GetProductionForLevel(level);

            maxValue = Mathf.Max(maxValue, costs[level - 1], productions[level - 1]);
        }

        if (maxValue <= 0f)
            return;

        // Рисуем сетку
        Handles.BeginGUI();
        Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // Горизонтальные линии
        for (int i = 0; i <= 5; i++)
        {
            float y = rect.y + (i * rect.height / 5f);
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));

            // Подписи значений
            float value = maxValue * (1f - i / 5f);
            EditorGUI.LabelField(
                new Rect(rect.x - 50, y - 10, 45, 20),
                FormatNumber(value),
                EditorStyles.miniLabel
            );
        }

        // Вертикальные линии
        for (int i = 0; i <= 5; i++)
        {
            float x = rect.x + (i * rect.width / 5f);
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));

            // Подписи уровней
            int level = Mathf.RoundToInt(i * (maxLevelToAnalyze - 1) / 5f) + 1;
            EditorGUI.LabelField(
                new Rect(x - 15, rect.yMax + 2, 30, 20),
                $"L{level}",
                EditorStyles.miniLabel
            );
        }

        Handles.EndGUI();

        // Рисуем график
        Handles.BeginGUI();

        // Линия стоимости (красная)
        Handles.color = Color.red;
        Vector2 prevCostPoint = Vector2.zero;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float cost = costs[level - 1];
            float normalizedCost = cost / maxValue;

            float x = rect.x + (level - 1) / (float)(maxLevelToAnalyze - 1) * rect.width;
            float y = rect.yMax - normalizedCost * rect.height;

            Vector2 point = new Vector2(x, y);

            if (level > 1)
                Handles.DrawLine(prevCostPoint, point);

            prevCostPoint = point;
        }

        // Линия производства (зеленая)
        Handles.color = Color.green;
        Vector2 prevProdPoint = Vector2.zero;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float production = productions[level - 1];
            float normalizedProd = production / maxValue;

            float x = rect.x + (level - 1) / (float)(maxLevelToAnalyze - 1) * rect.width;
            float y = rect.yMax - normalizedProd * rect.height;

            Vector2 point = new Vector2(x, y);

            if (level > 1)
                Handles.DrawLine(prevProdPoint, point);

            prevProdPoint = point;
        }

        Handles.EndGUI();

        // Легенда
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(" ", GUILayout.Width(20));
        DrawColorLegend("Cost", Color.red);
        DrawColorLegend("Production", Color.green);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // ROI Graph
        EditorGUILayout.LabelField("ROI Graph", EditorStyles.boldLabel);
        DrawROIGraph(costData);
    }

    private void DrawROIGraph(ResourceCost costData)
    {
        Rect rect = GUILayoutUtility.GetRect(400, 150);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        float[] rois = new float[maxLevelToAnalyze];
        float maxROI = 0f;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            rois[level - 1] = targetConfig.GetROI(costData, level);
            maxROI = Mathf.Max(maxROI, rois[level - 1]);
        }

        if (maxROI <= 0f)
            return;

        Handles.BeginGUI();

        // Рисуем сетку
        Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        for (int i = 0; i <= 5; i++)
        {
            float y = rect.y + (i * rect.height / 5f);
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
        }

        // Линия ROI (желтая)
        Handles.color = Color.yellow;
        Vector2 prevROIPoint = Vector2.zero;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float roi = rois[level - 1];
            float normalizedROI = roi / maxROI;

            float x = rect.x + (level - 1) / (float)(maxLevelToAnalyze - 1) * rect.width;
            float y = rect.yMax - normalizedROI * rect.height;

            Vector2 point = new Vector2(x, y);

            if (level > 1)
                Handles.DrawLine(prevROIPoint, point);

            prevROIPoint = point;
        }

        // Целевая линия ROI = 1
        Handles.color = Color.white;
        float targetY = rect.yMax - (1f / maxROI) * rect.height;
        Handles.DrawLine(new Vector3(rect.x, targetY), new Vector3(rect.xMax, targetY));

        Handles.EndGUI();

        // Подписи
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Max ROI: {maxROI:F2}x", GUILayout.Width(100));
        EditorGUILayout.LabelField("ROI=1 line", GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawColorLegend(string label, Color color)
    {
        Rect colorRect = GUILayoutUtility.GetRect(20, 20);
        EditorGUI.DrawRect(colorRect, color);
        EditorGUILayout.LabelField(label, GUILayout.Width(80));
    }

    private void DrawStats()
    {
        var costData = targetConfig.costResourceList[selectedResourceIndex];

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        float bestROI = 0f;
        int bestLevel = 1;
        float worstROI = float.MaxValue;
        int worstLevel = 1;
        float avgROI = 0f;

        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float roi = targetConfig.GetROI(costData, level);

            if (roi > bestROI)
            {
                bestROI = roi;
                bestLevel = level;
            }

            if (roi < worstROI)
            {
                worstROI = roi;
                worstLevel = level;
            }

            avgROI += roi;
        }

        avgROI /= maxLevelToAnalyze;

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Best ROI: Level {bestLevel} → {bestROI:F2}x");
        EditorGUILayout.LabelField($"Worst ROI: Level {worstLevel} → {worstROI:F2}x");
        EditorGUILayout.LabelField($"Average ROI: {avgROI:F2}x");

        // Тренд
        float firstROI = targetConfig.GetROI(costData, 1);
        float lastROI = targetConfig.GetROI(costData, maxLevelToAnalyze);
        float trend = (lastROI - firstROI) / firstROI * 100f;

        EditorGUILayout.LabelField($"Trend: {trend:F1}% {(trend >= 0 ? "↑" : "↓")}");
        EditorGUILayout.EndVertical();
    }

    private void DrawRecommendations()
    {
        var costData = targetConfig.costResourceList[selectedResourceIndex];

        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Recommendations", EditorStyles.boldLabel);

        float roiLevel1 = targetConfig.GetROI(costData, 1);
        float paybackLevel1 = targetConfig.GetPaybackTime(costData, 1);
        float roiTrend = targetConfig.GetROI(costData, maxLevelToAnalyze) - roiLevel1;

        if (roiLevel1 < 1.2f)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Level 1 ROI is too low (<1.2x). Consider reducing cost or increasing production.",
                MessageType.Warning);
        }
        else if (roiLevel1 > 3f)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Level 1 ROI is very high (>3x). May be too easy.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "✅ Level 1 ROI is good (1.2x-3x).",
                MessageType.Info);
        }

        if (paybackLevel1 > 60f)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Level 1 payback is too long (>60s). Early game may feel slow.",
                MessageType.Warning);
        }
        else if (paybackLevel1 < 5f)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Level 1 payback is very short (<5s). May feel too fast.",
                MessageType.Warning);
        }

        if (roiTrend < 0)
        {
            EditorGUILayout.HelpBox(
                "⚠️ ROI is decreasing over time. Consider increasing productionMultiplier relative to costMultiplier.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "✅ ROI is growing over time - good for progression!",
                MessageType.Info);
        }

        // Проверка пересечения cost и production
        bool costExceedsProduction = false;
        for (int level = 1; level <= maxLevelToAnalyze; level++)
        {
            float cost = targetConfig.GetCostForLevel(costData, level);
            float production = targetConfig.GetProductionForLevel(level);

            if (cost > production)
            {
                costExceedsProduction = true;
                break;
            }
        }

        if (costExceedsProduction)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Cost exceeds production at some levels. ROI < 1x - players won't buy.",
                MessageType.Warning);
        }
    }

    private string FormatNumber(float num)
    {
        if (num >= 1e9f)
            return (num / 1e9f).ToString("F2") + "B";
        if (num >= 1e6f)
            return (num / 1e6f).ToString("F2") + "M";
        if (num >= 1e3f)
            return (num / 1e3f).ToString("F2") + "K";
        return num.ToString("N0");
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 1f)
            return $"{(seconds * 1000):F0}ms";
        if (seconds < 60f)
            return $"{seconds:F1}s";
        if (seconds < 3600f)
        {
            float minutes = seconds / 60f;
            return $"{minutes:F1}m";
        }
        return $"{seconds / 3600f:F1}h";
    }
}