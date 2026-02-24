using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> viewList;
    [SerializeField] private List<CustomButton> btnList;
    [Space]
    [SerializeField] private GameObject startView;

    private GameObject currentView;

    private void Start()
    {
        for(int i = 0; i < btnList.Count; i++)
        {
            GameObject a = viewList[i];
            btnList[i].Btn.onClick.AddListener(() => 
            { OpenView(a); });
        }

        //OpenView(startView);
    }

    private void OpenView(GameObject view)
    {
        for(int i = 0; i < viewList.Count; i++)
        {
            if (viewList[i] == view)
            {
                view.SetActive(true);
                btnList[i].Select();

                currentView = view;
            }
            else
            {
                viewList[i].SetActive(false);
                btnList[i].Diselect();
            }
        }
    }
}
