using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IAPItemView : MonoBehaviour
{
    public enum ProductKind { Consumable, NonConsumable }

    [Header("Config")]
    [SerializeField] private string productId = "noads_popup";
    [SerializeField] private ProductKind productKind = ProductKind.NonConsumable;

    [Header("UI")]
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text priceText;

    void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        SetPriceLoading();
    }

    void OnEnable()
    {
        if (PaymentsBridge.Instance != null)
        {
            PaymentsBridge.Instance.CatalogJson += OnCatalogJson;
            PaymentsBridge.Instance.PurchaseSucceeded += OnPurchaseSucceeded;
            PaymentsBridge.Instance.PurchaseFailed += OnPurchaseFailed;
        }

        // применим каталог, если уже пришел
        if (!string.IsNullOrEmpty(PaymentsBridge.LastCatalogJson))
            OnCatalogJson(PaymentsBridge.LastCatalogJson);

        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnInfiniteBoostersChanged += OnInfiniteChanged;
        // отрисуем состояние
        RefreshOwnedState();
    }

    void OnDisable()
    {
        if (PaymentsBridge.Instance != null)
        {
            PaymentsBridge.Instance.CatalogJson -= OnCatalogJson;
            PaymentsBridge.Instance.PurchaseSucceeded -= OnPurchaseSucceeded;
            PaymentsBridge.Instance.PurchaseFailed -= OnPurchaseFailed;
        }
        if (PlayerProgress.Instance != null)
            PlayerProgress.Instance.OnInfiniteBoostersChanged -= OnInfiniteChanged;
    }
    void OnInfiniteChanged()
    {
        RefreshOwnedState();
    }

    void OnBuyClicked()
    {
        if (PaymentsBridge.Instance == null)
        {
            Debug.LogWarning("PaymentsBridge not found");
            return;
        }

        AudioManager.Instance?.Play("Click");

        if (buyButton != null) buyButton.interactable = false;
        PaymentsBridge.Instance.Purchase(productId);
    }

    // ---------- catalog ----------
    void OnCatalogJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        // JsonUtility не умеет массив в корне
        if (json.TrimStart().StartsWith("["))
            json = "{\"products\":" + json + "}";

        var catalog = JsonUtility.FromJson<CatalogDto>(json);
        if (catalog?.products == null) return;

        foreach (var p in catalog.products)
        {
            if (p == null) continue;
            if (p.id != productId) continue;

            if (priceText != null)
                priceText.text = string.IsNullOrEmpty(p.price) ? "—" : p.price;

            RefreshButtonState();
            return;
        }
    }

    // ---------- purchase result ----------
    void OnPurchaseSucceeded(string id, string token)
    {
        if (id != productId) return;

        // consumable -> просто вернуть кнопку сразу
        if (productKind == ProductKind.Consumable)
        {
            if (buyButton != null) buyButton.interactable = true;
            return;
        }

        // non-consumable -> скрыть кнопку, но баннер оставить
        RefreshOwnedState();
    }

    void OnPurchaseFailed(string id, string err)
    {
        // если id пустой — на всякий случай возвращаем кнопку только если это наш item
        if (!string.IsNullOrEmpty(id) && id != productId) return;

        // вернуть кнопку если это не owned
        if (!(productKind == ProductKind.NonConsumable && IsAlreadyOwned()))
            if (buyButton != null) buyButton.interactable = true;
    }

    // ---------- states ----------
    void SetPriceLoading()
    {
        if (priceText != null) priceText.text = "...";
        if (buyButton != null) buyButton.interactable = false;
    }

    void RefreshOwnedState()
    {
        // 1) блок по infinite (для consumable и super_deal)
        if (IsBlockedByInfinite())
        {
            if (buyButton != null) buyButton.interactable = false;
            if (buyButton != null) buyButton.gameObject.SetActive(false); // или true + interactable=false, как хочешь
            //if (priceText != null) priceText.text = "Included"; // или "∞"
            return;
        }

        if (productKind == ProductKind.NonConsumable && IsAlreadyOwned())
        {
            // ✅ скрываем только кнопку
            if (buyButton != null) buyButton.gameObject.SetActive(false);
            if (priceText != null) priceText.text = "Purchased";
        }
        else
        {
            if (buyButton != null) buyButton.gameObject.SetActive(true);
            RefreshButtonState();
        }
    }

    void RefreshButtonState()
    {
        if (buyButton == null) return;

        if (productKind == ProductKind.NonConsumable && IsAlreadyOwned())
        {
            buyButton.gameObject.SetActive(false);
            return;
        }

        // кнопку можно включить только если цена уже есть
        if (priceText != null && priceText.text != "..." && priceText.text != "—")
            buyButton.interactable = true;
    }

    bool IsAlreadyOwned()
    {
        if (PlayerProgress.Instance == null) return false;

        return productId switch
        {
            "noads_popup" => PlayerProgress.Instance.NoAds,
            "infinite_boosters" => PlayerProgress.Instance.InfiniteBoosters,
            _ => false
        };
    }
    bool IsBlockedByInfinite()
    {
        if (PlayerProgress.Instance == null) return false;
        if (!PlayerProgress.Instance.InfiniteBoosters) return false;

        // всё, что даёт конечные бусты, блокируем
        return productId == "buff_clear"
            || productId == "buff_shuffle"
            || productId == "buff_hint"
            || productId == "super_deal";
    }

    // ---- DTO ----
    [Serializable] class CatalogDto { public ProductDto[] products; }
    [Serializable] class ProductDto { public string id; public string price; }
}
