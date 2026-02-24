using System.Collections.Generic;
using UnityEngine;


public class PopUpManager : MonoBehaviour
{
    public static PopUpManager Instance;

    public GameObject back;
    public TabManager tabManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        CloseViews();
    }

    public void CloseViews()
    {
        back.SetActive(false);
        tabManager.CloseViews();
    }

    public void OpenView(Tab obj)
    {
        back.SetActive(true);
        tabManager.OpenView(obj);
    }
}
