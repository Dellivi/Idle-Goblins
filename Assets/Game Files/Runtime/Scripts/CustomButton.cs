using UnityEngine;
using UnityEngine.UI;

public class CustomButton : MonoBehaviour
{
    public GameObject original;
    public GameObject selected;

    private Button btn;

    public Button Btn { get => btn; set => btn = value; }

    private void Awake()
    {
        Btn = GetComponent<Button>();
        Diselect();
    }

    public void Select()
    {
        original.SetActive(false);
        selected.SetActive(true);
    }

    public void Diselect()
    {
        original.SetActive(true);
        selected.SetActive(false);
    }
}
