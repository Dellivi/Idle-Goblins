using System.Collections.Generic;
using UnityEngine;

public class MultiCostView : MonoBehaviour
{
    [SerializeField] private ResourceCostView costViewPrefab;
    [SerializeField] private Transform container;

    private readonly List<ResourceCostView> activeViews = new();

    /// <summary>
    /// Отобразить список стоимостей
    /// </summary>
    public void ShowCosts(Dictionary<ResourceData, float> costs, bool showRequired)
    {
        Clear();

        foreach (var pair in costs)
        {
            var view = Instantiate(costViewPrefab, container);
            view.Setup(pair.Key, pair.Value, showRequired);

            activeViews.Add(view);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < activeViews.Count; i++)
        {
            if (activeViews[i] != null)
                Destroy(activeViews[i].gameObject);
        }

        activeViews.Clear();
    }
}


