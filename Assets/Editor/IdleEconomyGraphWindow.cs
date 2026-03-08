using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class IdleEconomyAnalyzer : EditorWindow
{
    #region Window Setup
    [MenuItem("Tools/Idle Game/Economy Analyzer")]
    public static void ShowWindow()
    {
        var window = GetWindow<IdleEconomyAnalyzer>("Economy Analyzer");
        window.minSize = new Vector2(1000, 600);
    }
    #endregion

    #region Data Classes
    [System.Serializable]
    private class SimulationConfig
    {
        public ProductionConfig config;
        public int panelCount = 1;
        public float panelMultiplier = 3f;
    }

    private class ResourceState
    {
        public float amount;
        public float totalProduction;
        public float totalCost;
    }

    private class PanelData
    {
        public ProductionConfig config;
        public int panelIndex;
        public int level;
        public float cost;
        public float production;
        public float duration;
        public float roi;
        public float paybackTime;
        public float productionPerSecond;
    }
    #endregion

    #region Fields
    private List<SimulationConfig> simulationConfigs = new List<SimulationConfig>();
    private ProductionConfig primaryResource;

    private int maxLevel = 50;
    private float simulationTime = 1800f;
    private bool useTimeMode = false;

    private enum ROIMode { AddProduce, TotalProduce }
    private ROIMode roiMode = ROIMode.AddProduce; // переключатель ROI

    private Dictionary<ResourceData, List<PanelData>> panelDataByResource = new Dictionary<ResourceData, List<PanelData>>();
    private Dictionary<ResourceData, ResourceState> resourceStates = new Dictionary<ResourceData, ResourceState>();

    private Vector2 scrollPosition;
    private Vector2 graphScrollPosition;
    private int selectedTab = 0;
    private string[] tabs = { "Setup", "Simulation", "Metrics", "Graphs" };

    private bool showCostLine = true;
    private bool showProductionLine = true;
    private bool showROILine = true;
    private Rect graphRect = new Rect(0, 0, 800, 400);

    private Color costColor = new Color(1f, 0.3f, 0.3f);
    private Color productionColor = new Color(0.3f, 1f, 0.3f);
    private Color roiColor = new Color(1f, 0.9f, 0.3f);
    private Color warningColor = new Color(1f, 0.5f, 0f);
    #endregion

    #region GUI
    private void OnGUI()
    {
        DrawHeader();
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);
        EditorGUILayout.Space(10);

        switch (selectedTab)
        {
            case 0: DrawSetupTab(); break;
            case 1: DrawSimulationTab(); break;
            case 2: DrawMetricsTab(); break;
            case 3: DrawGraphsTab(); break;
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Idle Economy Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Multi-cycle resource production analysis", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Setup Tab
    private void DrawSetupTab()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.LabelField("Primary Resource", EditorStyles.boldLabel);
        primaryResource = (ProductionConfig)EditorGUILayout.ObjectField("Main Resource", primaryResource, typeof(ProductionConfig), false);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Production Configs", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Config")) simulationConfigs.Add(new SimulationConfig());
        if (GUILayout.Button("Clear All")) simulationConfigs.Clear();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);

        for (int i = 0; i < simulationConfigs.Count; i++)
            DrawSimulationConfig(i);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Simulation Settings", EditorStyles.boldLabel);
        useTimeMode = EditorGUILayout.Toggle("Use Time Mode", useTimeMode);
        if (useTimeMode) simulationTime = EditorGUILayout.FloatField("Simulation Time (seconds)", simulationTime);
        else maxLevel = EditorGUILayout.IntSlider("Max Level", maxLevel, 1, 200);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("ROI Settings", EditorStyles.boldLabel);
        roiMode = (ROIMode)EditorGUILayout.EnumPopup("ROI Formula", roiMode);

        EditorGUILayout.Space(10);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Run Simulation", GUILayout.Height(40))) RunSimulation();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void DrawSimulationConfig(int index)
    {
        var config = simulationConfigs[index];
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        config.config = (ProductionConfig)EditorGUILayout.ObjectField($"Config {index + 1}", config.config, typeof(ProductionConfig), false);
        if (GUILayout.Button("X", GUILayout.Width(25))) { simulationConfigs.RemoveAt(index); return; }
        EditorGUILayout.EndHorizontal();

        if (config.config != null)
        {
            config.panelCount = EditorGUILayout.IntSlider("Panel Count", config.panelCount, 1, 10);
            config.panelMultiplier = EditorGUILayout.Slider("Panel Multiplier", config.panelMultiplier, 1.5f, 5f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Produces:", GUILayout.Width(70));
            if (config.config.productionResource != null)
                EditorGUILayout.LabelField(config.config.productionResource.nameResource.GetLocalizedString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (config.config.costResourceList != null && config.config.costResourceList.Count > 0)
            {
                EditorGUILayout.LabelField("Requires:");
                EditorGUI.indentLevel++;
                foreach (var cost in config.config.costResourceList)
                    if (cost.resource != null)
                        EditorGUILayout.LabelField($"• {cost.resource.nameResource.GetLocalizedString()}: {cost.baseCost}");
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }
    #endregion

    #region Simulation Tab
    private void DrawSimulationTab()
    {
        if (panelDataByResource.Count == 0)
        {
            EditorGUILayout.HelpBox("No simulation data. Run simulation first.", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (var kvp in panelDataByResource) DrawResourceSimulation(kvp.Key, kvp.Value);
        EditorGUILayout.EndScrollView();
    }

    private void DrawResourceSimulation(ResourceData resource, List<PanelData> panels)
    {
        if (resource == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(resource.nameResource.GetLocalizedString(), EditorStyles.boldLabel);

        if (resourceStates.TryGetValue(resource, out var state))
        {
            EditorGUILayout.LabelField($"Total Amount: {FormatNumber(state.amount)}");
            EditorGUILayout.LabelField($"Total Production/s: {FormatNumber(state.totalProduction)}/s");
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Panel", EditorStyles.boldLabel, GUILayout.Width(50));
        GUILayout.Label("Level", EditorStyles.boldLabel, GUILayout.Width(50));
        GUILayout.Label("Cost", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("Production", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label("ROI", EditorStyles.boldLabel, GUILayout.Width(80));
        GUILayout.Label("Payback", EditorStyles.boldLabel, GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();

        foreach (var panel in panels.Take(20)) DrawPanelRow(panel);
        if (panels.Count > 20) EditorGUILayout.LabelField($"... and {panels.Count - 20} more panels");

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);
    }

    private void DrawPanelRow(PanelData panel)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"#{panel.panelIndex + 1}", GUILayout.Width(50));
        GUILayout.Label(panel.level.ToString(), GUILayout.Width(50));

        GUI.color = costColor;
        GUILayout.Label(FormatNumber(panel.cost), GUILayout.Width(100));
        GUI.color = Color.white;

        GUI.color = productionColor;
        GUILayout.Label(FormatNumber(panel.production), GUILayout.Width(100));
        GUI.color = Color.white;

        Color roiColorActual = panel.roi < 1f ? warningColor : roiColor;
        GUI.color = roiColorActual;
        GUILayout.Label(panel.roi.ToString("F2"), GUILayout.Width(80));
        GUI.color = Color.white;

        GUILayout.Label(FormatTime(panel.paybackTime), GUILayout.Width(100));
        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region Metrics Tab
    private void DrawMetricsTab()
    {
        if (panelDataByResource.Count == 0)
        {
            EditorGUILayout.HelpBox("No metrics data. Run simulation first.", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (var kvp in panelDataByResource) DrawResourceMetrics(kvp.Key, kvp.Value);
        DrawWarnings();
        EditorGUILayout.EndScrollView();
    }

    private void DrawResourceMetrics(ResourceData resource, List<PanelData> panels)
    {
        if (resource == null || panels.Count == 0) return;
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(resource.nameResource.GetLocalizedString(), EditorStyles.boldLabel);

        float avgROI = panels.Average(p => p.roi);
        float minROI = panels.Min(p => p.roi);
        float maxROI = panels.Max(p => p.roi);
        float avgPayback = panels.Average(p => p.paybackTime);
        float totalProduction = panels.Sum(p => p.productionPerSecond);

        EditorGUILayout.LabelField($"Average ROI: {avgROI:F2}");
        EditorGUILayout.LabelField($"ROI Range: {minROI:F2} - {maxROI:F2}");
        EditorGUILayout.LabelField($"Average Payback: {FormatTime(avgPayback)}");
        EditorGUILayout.LabelField($"Total Production/s: {FormatNumber(totalProduction)}/s");

        if (panels.Count > 1)
        {
            float firstROI = panels.First().roi;
            float lastROI = panels.Last().roi;
            string trend = lastROI > firstROI ? "↑ Increasing" : "↓ Decreasing";
            Color trendColor = lastROI > firstROI ? Color.green : Color.red;

            GUI.color = trendColor;
            EditorGUILayout.LabelField($"ROI Trend: {trend}");
            GUI.color = Color.white;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawWarnings()
    {
        EditorGUILayout.LabelField("Warnings & Recommendations", EditorStyles.boldLabel);

        foreach (var kvp in panelDataByResource)
        {
            if (kvp.Key == null) continue;
            var panels = kvp.Value;

            var lowROI = panels.Where(p => p.roi < 1f).ToList();
            if (lowROI.Count > 0)
            {
                EditorGUILayout.HelpBox($"{kvp.Key.nameResource.GetLocalizedString()}: {lowROI.Count} panels have ROI < 1.0 (unprofitable)", MessageType.Warning);
            }

            var highROI = panels.Where(p => p.roi > 5f).ToList();
            if (highROI.Count > 0)
            {
                EditorGUILayout.HelpBox($"{kvp.Key.nameResource.GetLocalizedString()}: {highROI.Count} panels have ROI > 5.0 (too easy)", MessageType.Info);
            }

            float avgPayback = panels.Average(p => p.paybackTime);
            if (avgPayback < 10f)
                EditorGUILayout.HelpBox($"{kvp.Key.nameResource.GetLocalizedString()}: Average payback < 10s (too fast)", MessageType.Warning);
            else if (avgPayback > 300f)
                EditorGUILayout.HelpBox($"{kvp.Key.nameResource.GetLocalizedString()}: Average payback > 5min (too slow)", MessageType.Warning);
        }
    }
    #endregion

    #region Graphs Tab
    private void DrawGraphsTab()
    {
        if (panelDataByResource.Count == 0)
        {
            EditorGUILayout.HelpBox("No graph data. Run simulation first.", MessageType.Info);
            return;
        }

        graphScrollPosition = EditorGUILayout.BeginScrollView(graphScrollPosition);
        EditorGUILayout.BeginHorizontal();
        showCostLine = EditorGUILayout.Toggle("Show Cost", showCostLine);
        showProductionLine = EditorGUILayout.Toggle("Show Production", showProductionLine);
        showROILine = EditorGUILayout.Toggle("Show ROI", showROILine);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        foreach (var kvp in panelDataByResource)
        {
            if (kvp.Key == null) continue;
            DrawGraph(kvp.Key, kvp.Value);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawGraph(ResourceData resource, List<PanelData> panels)
    {
        EditorGUILayout.LabelField(resource.nameResource.GetLocalizedString(), EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(graphRect.width, graphRect.height);

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f));
            DrawGrid(rect);

            if (panels.Count > 1)
            {
                if (showCostLine) DrawLine(rect, panels, p => p.cost, costColor);
                if (showProductionLine) DrawLine(rect, panels, p => p.production, productionColor);
                if (showROILine) DrawROILine(rect, panels);
            }

            DrawLegend(rect);
        }

        EditorGUILayout.Space(10);
    }

    private void DrawGrid(Rect rect)
    {
        Handles.color = new Color(0.3f, 0.3f, 0.3f);
        for (int i = 0; i <= 10; i++)
        {
            float x = rect.x + (rect.width / 10f) * i;
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));

            float y = rect.y + (rect.height / 10f) * i;
            Handles.DrawLine(new Vector3(rect.x, y), new Vector3(rect.xMax, y));
        }
    }

    private void DrawLine(Rect rect, List<PanelData> panels, System.Func<PanelData, float> getValue, Color color)
    {
        if (panels.Count < 2) return;
        float maxValue = panels.Max(getValue);
        if (maxValue == 0) return;

        Handles.color = color;
        for (int i = 0; i < panels.Count - 1; i++)
        {
            float x1 = rect.x + (rect.width / (panels.Count - 1)) * i;
            float y1 = rect.yMax - (getValue(panels[i]) / maxValue) * rect.height;
            float x2 = rect.x + (rect.width / (panels.Count - 1)) * (i + 1);
            float y2 = rect.yMax - (getValue(panels[i + 1]) / maxValue) * rect.height;
            Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
        }
    }

    private void DrawROILine(Rect rect, List<PanelData> panels)
    {
        Handles.color = roiColor;
        float maxROI = Mathf.Min(panels.Max(p => p.roi), 10f);
        for (int i = 0; i < panels.Count - 1; i++)
        {
            float x1 = rect.x + (rect.width / (panels.Count - 1)) * i;
            float y1 = rect.yMax - (Mathf.Min(panels[i].roi, 10f) / maxROI) * rect.height;
            float x2 = rect.x + (rect.width / (panels.Count - 1)) * (i + 1);
            float y2 = rect.yMax - (Mathf.Min(panels[i + 1].roi, 10f) / maxROI) * rect.height;
            Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
        }
    }

    private void DrawLegend(Rect rect)
    {
        float legendX = rect.xMax - 120;
        float legendY = rect.y + 10;
        float lineHeight = 20;
        Rect legendRect = new Rect(legendX, legendY, 110, lineHeight * 4);
        EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

        if (showCostLine)
        {
            EditorGUI.DrawRect(new Rect(legendX + 5, legendY + 5, 20, 2), costColor);
            GUI.Label(new Rect(legendX + 30, legendY, 80, lineHeight), "Cost");
            legendY += lineHeight;
        }
        if (showProductionLine)
        {
            EditorGUI.DrawRect(new Rect(legendX + 5, legendY + 5, 20, 2), productionColor);
            GUI.Label(new Rect(legendX + 30, legendY, 80, lineHeight), "Production");
            legendY += lineHeight;
        }
        if (showROILine)
        {
            EditorGUI.DrawRect(new Rect(legendX + 5, legendY + 5, 20, 2), roiColor);
            GUI.Label(new Rect(legendX + 30, legendY, 80, lineHeight), "ROI");
        }
    }
    #endregion

    #region Simulation Logic
    private void RunSimulation()
    {
        panelDataByResource.Clear();
        resourceStates.Clear();
        if (simulationConfigs.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Add at least one production config.", "OK");
            return;
        }

        foreach (var simConfig in simulationConfigs)
        {
            if (simConfig.config == null || simConfig.config.productionResource == null) continue;
            var resource = simConfig.config.productionResource;
            if (!resourceStates.ContainsKey(resource)) resourceStates[resource] = new ResourceState();
        }

        foreach (var simConfig in simulationConfigs) if (simConfig.config != null) SimulateConfig(simConfig);

        Debug.Log("Simulation complete!");
        selectedTab = 1;
        Repaint();
    }

    private void SimulateConfig(SimulationConfig simConfig)
    {
        var config = simConfig.config;
        var resource = config.productionResource;
        if (resource == null) return;
        if (!panelDataByResource.ContainsKey(resource)) panelDataByResource[resource] = new List<PanelData>();

        for (int panelIndex = 0; panelIndex < simConfig.panelCount; panelIndex++)
        {
            float panelCostMultiplier = Mathf.Pow(simConfig.panelMultiplier, panelIndex);
            float panelProdMultiplier = Mathf.Pow(simConfig.panelMultiplier, panelIndex);
            int levels = useTimeMode ? CalculateLevelsForTime(config, simulationTime) : maxLevel;

            float previousTotalProduction = 0f;

            for (int level = 1; level <= levels; level++)
            {
                var panelData = new PanelData { config = config, panelIndex = panelIndex, level = level };

                float cost = 0f;
                if (config.costResourceList != null && config.costResourceList.Count > 0)
                {
                    var firstCost = config.costResourceList[0];
                    cost = config.GetCostForLevel(firstCost, level) * panelCostMultiplier;
                }
                panelData.cost = cost;

                panelData.production = config.GetProductionForLevel(level) * panelProdMultiplier;
                panelData.duration = config.GetDurationForLevel(level);
                panelData.productionPerSecond = panelData.production / panelData.duration;

                // ROI calculation
                float productionForROI = roiMode == ROIMode.AddProduce
                    ? panelData.productionPerSecond
                    : panelData.productionPerSecond + previousTotalProduction;

                panelData.roi = productionForROI > 0f ? panelData.cost / productionForROI : float.MaxValue;
                previousTotalProduction += panelData.productionPerSecond;

                // Payback
                panelData.paybackTime = panelData.productionPerSecond > 0f ? panelData.cost / panelData.productionPerSecond : float.MaxValue;

                panelDataByResource[resource].Add(panelData);

                // Update resource state
                if (resourceStates.TryGetValue(resource, out var state))
                {
                    state.totalProduction += panelData.productionPerSecond;
                    state.totalCost += panelData.cost;
                }
            }
        }
    }

    private int CalculateLevelsForTime(ProductionConfig config, float time) => Mathf.Min((int)(time / config.baseDuration), maxLevel);
    #endregion

    #region Utility
    private string FormatNumber(float number)
    {
        return NumberFormatter.FormatSmart(number);
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 1f) return $"{seconds * 1000:F0}ms";
        if (seconds < 60f) return $"{seconds:F1}s";
        if (seconds < 3600f) return $"{seconds / 60:F1}m";
        return $"{seconds / 3600:F1}h";
    }
    #endregion
}
