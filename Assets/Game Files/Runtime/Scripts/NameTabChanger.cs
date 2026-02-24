using TMPro;
using UnityEngine;

public class NameTabChanger : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    [Space]
    public TabManager tabManager;

    public void OnEnable()
    {
        SetText();
    }

    private void SetText()
    {
        if (tabManager.CurrentTab == null) return;
        nameText.text = tabManager.CurrentTab.GetData().nameTab.GetLocalizedString();
    }
}
