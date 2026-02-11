using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.name = "IAPManager";
    }

    IEnumerator Start()
    {
        while (PaymentsBridge.Instance == null)
            yield return null;

        PaymentsBridge.Instance.PurchaseJson += OnPurchaseJson;
        PaymentsBridge.Instance.PurchasesJson += OnPurchasesJson;
        PaymentsBridge.Instance.Error += OnIapError;

        PaymentsBridge.Instance.GetCatalog();
        PaymentsBridge.Instance.GetPurchases();
    }

    void OnDestroy()
    {
        if (PaymentsBridge.Instance == null) return;

        PaymentsBridge.Instance.PurchaseJson -= OnPurchaseJson;
        PaymentsBridge.Instance.PurchasesJson -= OnPurchasesJson;
        PaymentsBridge.Instance.Error -= OnIapError;
    }

    // ===== purchase callback =====
    [Serializable]
    class PurchaseDto
    {
        public string productID;
        public string productId;
        public string id;
        public string purchaseToken;
    }

    string GetProductId(PurchaseDto p)
    {
        if (!string.IsNullOrEmpty(p.productID)) return p.productID;
        if (!string.IsNullOrEmpty(p.productId)) return p.productId;
        if (!string.IsNullOrEmpty(p.id)) return p.id;
        return null;
    }

    void OnPurchaseJson(string json)
    {
        var p = JsonUtility.FromJson<PurchaseDto>(json);
        if (p == null) return;

        var id = GetProductId(p);
        if (string.IsNullOrEmpty(id)) return;

        ApplyPurchase(id);

        if (IsConsumable(id))
        {
            if (!string.IsNullOrEmpty(p.purchaseToken))
                PaymentsBridge.Instance.Consume(p.purchaseToken);
        }
    }

    // ===== restore =====
    [Serializable]
    class PurchasesArrayDto { public PurchaseDto[] items; }

    void OnPurchasesJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        // JsonUtility не умеет массив в корне
        var wrapped = $"{{\"items\":{json}}}";
        var list = JsonUtility.FromJson<PurchasesArrayDto>(wrapped);
        if (list?.items == null) return;

        foreach (var p in list.items)
        {
            if (p == null) continue;

            var id = GetProductId(p);
            if (string.IsNullOrEmpty(id)) continue;

            ApplyPurchase(id);

            if (IsConsumable(id))
            {
                if (!string.IsNullOrEmpty(p.purchaseToken))
                    PaymentsBridge.Instance.Consume(p.purchaseToken);
            }
        }
    }

    void OnIapError(string err)
    {
        Debug.LogWarning("[IAP] " + err);
    }

    // ===== apply to your game =====
    public void ApplyPurchase(string productId)
    {
        var pp = PlayerProgress.Instance;
        if (pp == null) return;

        switch (productId)
        {
            case "noads_popup":
                pp.SetNoAds(true);

                //var offer = FindObjectOfType<NoAdsOfferManager>();
                //if (offer != null) offer.DisableForever();
                break;

            case "buff_clear":
                BoostInventory.Add(BoostType.ClearSomeOfType, 5);
                break;

            case "buff_shuffle":
                BoostInventory.Add(BoostType.ShuffleButton, 5);
                break;

            case "buff_hint":
                BoostInventory.Add(BoostType.HintThreeSame, 5);
                break;

            case "super_deal":
                BoostInventory.Add(BoostType.ClearSomeOfType, 5);
                BoostInventory.Add(BoostType.ShuffleButton, 5);
                BoostInventory.Add(BoostType.HintThreeSame, 5);
                break;

            case "infinite_boosters":
                pp.SetInfiniteBoosters(true);
                pp.SetNoAds(true);
                break;

            default:
                Debug.Log($"[IAP] Unknown productId: {productId}");
                break;
        }
    }

    bool IsConsumable(string id)
    {
        return id == "buff_clear"
            || id == "buff_shuffle"
            || id == "buff_hint"
            || id == "super_deal";
    }
}
