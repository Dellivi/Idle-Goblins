using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Спаунер панелей покупки с объектным пулом.
/// Порядок объектов в иерархии гарантируется через SetSiblingIndex
/// после каждого спавна — без переинициализации или пересоздания.
/// </summary>
public class ActionPurchaseLayoutSpawner : MonoBehaviour
{
    [SerializeField] private GameObject actionPrefab;
    [SerializeField] private int initialPoolSize = 10;
    [Space]
    [SerializeField] private List<ProductionConfig> productionConfigList;

    // Пул хранит объекты в порядке "LIFO не важен" — порядок задаётся sibling index
    private readonly Stack<PurchaseSystem> pool = new();
    private readonly List<PurchaseSystem> activeObjects = new();

    // ──────────────────────────────────────────────
    //  Initialization
    // ──────────────────────────────────────────────
    private void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
            pool.Push(CreateNew());
    }

    private void Start()
    {
        if (productionConfigList is { Count: > 0 })
            Spawn(productionConfigList);
    }

    private PurchaseSystem CreateNew()
    {
        var obj = Instantiate(actionPrefab, transform).GetComponent<PurchaseSystem>();

        if (obj == null)
        {
            Debug.LogError("[ActionPurchaseLayoutSpawner] actionPrefab не содержит PurchaseSystem!", this);
            return null;
        }

        obj.gameObject.SetActive(false);
        return obj;
    }

    // ──────────────────────────────────────────────
    //  Pool
    // ──────────────────────────────────────────────
    private PurchaseSystem GetFromPool()
    {
        if (pool.Count == 0)
            pool.Push(CreateNew());

        var obj = pool.Pop();
        obj.gameObject.SetActive(true);
        return obj;
    }

    private void ReturnToPool(PurchaseSystem obj)
    {
        obj.gameObject.SetActive(false);
        pool.Push(obj);
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Спаунит панели в том порядке, в каком переданы configs.
    /// Порядок в иерархии гарантирован через SetSiblingIndex.
    /// </summary>
    public void Spawn(List<ProductionConfig> configs)
    {
        if (configs == null || configs.Count == 0) return;

        for (int i = 0; i < configs.Count; i++)
        {
            var config = configs[i];
            if (config == null)
            {
                Debug.LogWarning($"[ActionPurchaseLayoutSpawner] null config на индексе {i}, пропускаем");
                continue;
            }

            var purchase = GetFromPool();
            purchase.Setup(config);

            // Гарантируем порядок в иерархии — Layout Group учитывает sibling index
            purchase.transform.SetSiblingIndex(i);

            activeObjects.Add(purchase);
        }
    }

    /// <summary>Убрать все активные панели в пул.</summary>
    public void Clear()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
            ReturnToPool(activeObjects[i]);

        activeObjects.Clear();
    }

    /// <summary>Переспаунить с новым списком конфигов.</summary>
    public void Respawn(List<ProductionConfig> configs)
    {
        Clear();
        Spawn(configs);
    }

    private void OnDisable() => Clear();
}