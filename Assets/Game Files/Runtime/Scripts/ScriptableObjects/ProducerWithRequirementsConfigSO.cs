using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewProducerWithRequirements", menuName = "IdleGame/Producer With Requirements Config")]
public class ProducerWithRequirementsConfig : PurchaseConfig
{
    [Header("Level Requirements")]
    [Tooltip("Ресурсы, которые становятся необходимыми для улучшений на определённых уровнях")]
    public List<ResourceLevelRequirement> levelRequirements = new List<ResourceLevelRequirement>();
}