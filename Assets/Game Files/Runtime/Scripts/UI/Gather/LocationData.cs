using UnityEngine;

[CreateAssetMenu(fileName = "LocationData_", menuName = "Game/Data/new LocationData")]
public class LocationData : ScriptableObject
{
    public string locationName;
    public int maxChapterIndex;
    [Space]
    public ResourceData resource;
    public int startValue;

    /// <summary>
    /// Curve resources get for complete current chapter
    /// </summary>
    public AnimationCurve curveResourceMultiplier;

    public int GetMaximumValue(int chapterIndex)
    {
        int maxValue = Mathf.FloorToInt((float)startValue * curveResourceMultiplier.Evaluate(chapterIndex));
        Debug.Log($"[LocationData] curveResourceMultiplier is {curveResourceMultiplier.Evaluate(chapterIndex)}");
        Debug.Log($"[LocationData] maxValue is {maxValue}");
        return maxValue;
    }
}
