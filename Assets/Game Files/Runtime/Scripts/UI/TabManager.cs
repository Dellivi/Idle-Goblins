using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [SerializeField] private List<Tab> tabList = new List<Tab>();
    private Tab currentTab;
    public Tab CurrentTab { get => currentTab; protected set => currentTab = value; }

    private void Awake()
    {
        Debug.Log($"Start: tabList count = {tabList.Count}");

        // Удаляем null ссылки
        tabList.RemoveAll(tab => tab == null);
        Debug.Log($"After cleanup: tabList count = {tabList.Count}");

        // Если список пуст, попробуем найти табы автоматически
        if (tabList.Count == 0)
        {
            RefreshTabList();
        }

        CurrentTab = tabList[0];
    }

    [ContextMenu("Refresh Tab List")]
    public void RefreshTabList()
    {
        tabList.Clear();

        // Ищем в дочерних объектах
        Tab[] childTabs = GetComponentsInChildren<Tab>(true);
        tabList.AddRange(childTabs);

        Debug.Log($"RefreshTabList: Found {tabList.Count} tabs");
    }

    public void CloseViews()
    {
        foreach (Tab tab in tabList)
        {
            if (tab != null)
                tab.gameObject.SetActive(false);
        }
    }

    public void OpenView(Tab obj)
    {
        foreach (Tab tab in tabList)
        {
            if (tab != null)
            {
                if (tab.name == obj.name)
                {
                    tab.gameObject.SetActive(true);
                    CurrentTab = tab;
                }
                else
                {
                    tab.gameObject.SetActive(false);
                }
            }
        }
    }
}