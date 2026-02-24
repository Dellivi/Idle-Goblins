using UnityEngine;

public class Tab : MonoBehaviour
{
    [SerializeField] private TabData tabData;

    public TabData GetData() => tabData;
}
