using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "ResourceData", menuName = "Scriptable Objects/ResourceData")]
public class ResourceData : ScriptableObject
{
    public LocalizedString nameResource;
    public Sprite icon;
}
