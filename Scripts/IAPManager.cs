using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance;

    public bool debugIap = true;
    void Log(string s) { if (debugIap) Debug.Log("[IAP] " + s); }
    void LogW(string s) { if (debugIap) Debug.LogWarning("[IAP] " + s); }

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
        Log("OnPurchaseJson raw = " + json);

        var p = JsonUtility.FromJson<PurchaseDto>(json);
        if (p == null) { LogW("PurchaseDto null"); return; }

        var id = GetProductId(p);
        Log($"purchase parsed id={id} token={p.purchaseToken}");

        if (string.IsNullOrEmpty(id)) { LogW("productId empty"); return; }

        ApplyPurchase(id);
        RefreshSticky();

        if (!string.IsNullOrEmpty(p.purchaseToken))
        {
            Log("consume -> " + p.purchaseToken);
            PaymentsBridge.Instance.Consume(p.purchaseToken);
        }
        else
        {
            LogW("token empty -> cannot consume");
        }
    }
    void RefreshSticky()
    {
        var pp = PlayerProgress.Instance;
        if (pp != null && pp.NoAds) StickyAds.Hide();
        else StickyAds.Show();
    }

    // ===== restore =====
    [Serializable]
    class PurchasesArrayDto { public PurchaseDto[] items; }
    [Serializable]
    class PurchasesResponseDto
    {
        public PurchaseDto[] items;
        public string signature;
    }

    void OnPurchasesJson(string json)
    {
        Log("OnPurchasesJson raw = " + json);
        if (string.IsNullOrEmpty(json)) { RefreshSticky(); return; }

        var resp = JsonUtility.FromJson<PurchasesResponseDto>(json);
        Log("parsed items = " + (resp?.items?.Length ?? -1));

        if (resp?.items != null)
        {
            foreach (var p in resp.items)
            {
                if (p == null) continue;
                var id = GetProductId(p);
                Log($"restore item id={id} token={p.purchaseToken}");

                if (string.IsNullOrEmpty(id)) continue;

                ApplyPurchase(id);

                if (!string.IsNullOrEmpty(p.purchaseToken))
                {
                    Log("consume -> " + p.purchaseToken);
                    PaymentsBridge.Instance.Consume(p.purchaseToken);
                }
                else
                {
                    LogW("token empty -> cannot consume");
                }
            }
        }

        RefreshSticky();
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
                pp.SaveBoughtBoosts();
                break;

            case "buff_shuffle":
                BoostInventory.Add(BoostType.ShuffleButton, 5);
                pp.SaveBoughtBoosts();
                break;

            case "buff_hint":
                BoostInventory.Add(BoostType.HintThreeSame, 5);
                pp.SaveBoughtBoosts();
                break;

            case "super_deal":
                BoostInventory.Add(BoostType.ClearSomeOfType, 5);
                BoostInventory.Add(BoostType.ShuffleButton, 5);
                BoostInventory.Add(BoostType.HintThreeSame, 5);
                pp.SaveBoughtBoosts();
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
