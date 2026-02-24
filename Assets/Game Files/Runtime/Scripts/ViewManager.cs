using UnityEngine;
using UnityEngine.Events;

public class ViewManager : MonoBehaviour
{
    public UnityEvent OnEnableEvent;
    public UnityEvent OnDisableEvent;

    public TabManager closedTabManager;
    public TabManager tabManager;

    private void OnEnable()
    {
        OnEnableEvent.Invoke();
    }

    private void OnDisable()
    {
        OnDisableEvent.Invoke();
        CloseView();
    }

    public void Open(Tab obj)
    {
        closedTabManager.gameObject.SetActive(false);
        tabManager.gameObject.SetActive(true);
        tabManager.OpenView(obj);
    }

    public void CloseView()
    {
        closedTabManager.gameObject.SetActive(true);
        tabManager.gameObject.SetActive(false);
    }
}
