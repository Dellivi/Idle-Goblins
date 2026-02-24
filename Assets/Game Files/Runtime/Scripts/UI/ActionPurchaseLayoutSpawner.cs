using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class ActionPurchaseLayoutSpawner : MonoBehaviour
{
    [SerializeField] private GameObject actionPrefab;
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private float delayBetweenSpawns = 0.1f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    private Stack<PurchaseSystem> pool = new();
    private List<PurchaseSystem> activeObjects = new();
    private Sequence spawnSequence;

    #region Initialization

    private void Awake()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            pool.Push(CreateNew());
        }
    }

    private PurchaseSystem CreateNew()
    {
        var obj = Instantiate(actionPrefab, transform)
            .GetComponent<PurchaseSystem>();

        var canvasGroup = obj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = obj.gameObject.AddComponent<CanvasGroup>();

        obj.gameObject.SetActive(false);

        return obj;
    }

    #endregion

    #region Pool

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
        var canvasGroup = obj.GetComponent<CanvasGroup>();

        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;

        obj.gameObject.SetActive(false);
        pool.Push(obj);
    }

    #endregion

    #region Public API

    public void Spawn(List<PurchaseConfig> configs)
    {
        Clear();

        spawnSequence?.Kill();
        spawnSequence = DOTween.Sequence();

        foreach (var config in configs)
        {
            spawnSequence.AppendCallback(() =>
            {
                var purchase = GetFromPool();
                purchase.Setup(config);
                activeObjects.Add(purchase);

                var canvasGroup = purchase.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup
                    .DOFade(1f, fadeDuration)
                    .SetEase(fadeEase);
            });

            spawnSequence.AppendInterval(delayBetweenSpawns);
        }
    }

    public void Clear()
    {
        spawnSequence?.Kill();

        foreach (var obj in activeObjects)
        {
            ReturnToPool(obj);
        }

        activeObjects.Clear();
    }

    private void OnDisable()
    {
        Clear();
    }

    #endregion
}