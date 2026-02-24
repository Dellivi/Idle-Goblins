using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "NewAssignment", menuName = "IdleGame/Assignment Config")]
public class AssignmentConfig : ScriptableObject
{
    [Header("Информация")]
    public Sprite icon;
    public string categoryName;
    public LocalizedString assignmentName;
    public LocalizedString description;
    [Space]
    [Header("Стоимость")]
    public ResourceGet unitCost;
    public int maxCount;

    [Header("Idle Action")]
    public string actionId = "produce_goblins";
    public List<ResourceGet> resourceAddList;
    public float duration = 5f;
}