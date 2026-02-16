using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VariantCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image icon;
    public TMP_Text titleText;

    public TMP_Text priceText;

    public TMP_Text lockText;
    public GameObject lockRoot;     // объект с замком

    public GameObject ownedRoot;    // например "Куплено"
    public GameObject equippedRoot; // например "Установлено"

    [Header("Actions (4 buttons)")]
    public GameObject btnInstalledRoot; public Button btnInstalled;   // "Установлено" (не кликается)
    public GameObject btnEquipRoot; public Button btnEquip;       // "Установить"
    public GameObject btnBuyRoot; public Button btnBuy;         // "Купить"
    public GameObject btnUnlockAdRoot; public Button btnUnlockAd;    // "Открыть"

    public GameObject newBadgeRoot; // объект с восклицательным знаком
}
