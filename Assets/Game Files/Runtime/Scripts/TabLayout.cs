using UnityEngine;

public class TabLayout : MonoBehaviour
{
    [SerializeField] private TabLayoutDataSO tabData;
    [Space]
    [SerializeField] private ProducerWithRequirements producerWithRequirements;
    [SerializeField] private ActionPurchaseLayoutSpawner actionPurchaseLayoutSpawner;

    public TabLayoutDataSO GetData() => tabData;

    private void OnEnable()
    {
        producerWithRequirements.Setup(tabData.producer);
        actionPurchaseLayoutSpawner.Spawn(tabData.productionConfigDataListSO.list);
    }
}
