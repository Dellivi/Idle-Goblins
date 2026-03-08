using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "TabLayoutData_", menuName = "IdleGame/UI/new TabLayoutData")]
public class TabLayoutDataSO : ScriptableObject
{
    public LocalizedString nameTab;
    public ProducerWithRequirementsConfig producer;
    public ProductionConfigDataListSO productionConfigDataListSO;
}
