using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class PaymentsBridge : MonoBehaviour
{
    public static PaymentsBridge Instance;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void IAP_GetCatalog();
    [DllImport("__Internal")] private static extern void IAP_Purchase(string productId);
    [DllImport("__Internal")] private static extern void IAP_Consume(string purchaseToken);
    [DllImport("__Internal")] private static extern void IAP_GetPurchases();
#endif

    // Raw events (как у тебя было)
    public event Action<string> CatalogJson;
    public event Action<string> PurchaseJson;
    public event Action<string> PurchasesJson;
    public event Action<string> Error;

    // ✅ UI-friendly purchase result events
    public event Action<string, string> PurchaseSucceeded; // (productId, purchaseToken)
    public event Action<string, string> PurchaseFailed;    // (productIdOrEmpty, error)

    public static string LastCatalogJson { get; private set; }
    public string PendingProductId { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        gameObject.name = "PaymentsBridge";
    }

    void Start()
    {
#if UNITY_EDITOR
        Editor_SimulateCatalog();
        Editor_SimulatePurchasesRestore();
#endif
    }

    public void GetCatalog()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_GetCatalog();
#endif
#if UNITY_EDITOR
        Editor_SimulateCatalog();
#endif
    }

    public void GetPurchases()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_GetPurchases();
#endif
#if UNITY_EDITOR
        Editor_SimulatePurchasesRestore();
#endif
    }

    public void Purchase(string productId)
    {
        PendingProductId = productId;

#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_Purchase(productId);
#endif
#if UNITY_EDITOR
        Editor_SimulatePurchase(productId);
#endif
    }

    public void Consume(string token)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        IAP_Consume(token);
#endif
    }

    // ---------- callbacks from JS ----------
    public void OnCatalogOk(string json)
    {
        LastCatalogJson = json;
        CatalogJson?.Invoke(json);
    }

    public void OnCatalogFail(string err)
    {
        Error?.Invoke("Catalog: " + err);
    }

    [Serializable]
    class PurchaseDto
    {
        public string productID;
        public string productId;
        public string id;
        public string purchaseToken;
    }

    string ExtractProductId(PurchaseDto p)
    {
        if (!string.IsNullOrEmpty(p.productID)) return p.productID;
        if (!string.IsNullOrEmpty(p.productId)) return p.productId;
        if (!string.IsNullOrEmpty(p.id)) return p.id;
        return null;
    }

    public void OnPurchaseOk(string json)
    {
        PurchaseJson?.Invoke(json);

        PurchaseDto dto = null;
        try { dto = JsonUtility.FromJson<PurchaseDto>(json); } catch { }

        var id = dto != null ? ExtractProductId(dto) : null;
        if (string.IsNullOrEmpty(id)) id = PendingProductId;

        var token = dto != null ? dto.purchaseToken : null;

        PurchaseSucceeded?.Invoke(id ?? "", token ?? "");
        PendingProductId = null;
    }

    public void OnPurchaseFail(string err)
    {
        Error?.Invoke("Purchase: " + err);
        PurchaseFailed?.Invoke(PendingProductId ?? "", err);
        PendingProductId = null;
    }

    public void OnConsumeOk(string _) { }
    public void OnConsumeFail(string err) => Error?.Invoke("Consume: " + err);

    public void OnGetPurchasesOk(string json) => PurchasesJson?.Invoke(json);
    public void OnGetPurchasesFail(string err) => Error?.Invoke("GetPurchases: " + err);

#if UNITY_EDITOR
    public void Editor_SimulateCatalog()
    {
        var fake = "[{\"id\":\"noads_popup\",\"price\":\"35 ₽\"},{\"id\":\"buff_clear\",\"price\":\"15 ₽\"},{\"id\":\"super_deal\",\"price\":\"30 ₽\"}]";
        OnCatalogOk(fake);
    }

    public void Editor_SimulatePurchase(string productId)
    {
        var fakePurchase = $"{{\"productID\":\"{productId}\",\"purchaseToken\":\"TOKEN_{productId}\"}}";
        OnPurchaseOk(fakePurchase);
    }

    public void Editor_SimulatePurchasesRestore()
    {
        var fake = "[{\"productID\":\"noads_popup\",\"purchaseToken\":\"TOKEN_noads\"}]";
        OnGetPurchasesOk(fake);
    }
#endif
}
