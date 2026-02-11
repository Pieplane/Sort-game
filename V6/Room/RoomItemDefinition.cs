using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomItemId
{
    Carpet,
    Chair,
    Door,
    Floor,
    Flower,
    Refrigerator,
    Sofa,
    Table,
    Wall,
    Window
    // ... добавишь свои 10
}

[System.Serializable]
public struct RoomItemVariant
{
    public string title;

    [Header("UI (panel)")]
    public Sprite previewSprite;     // что показываем в карточке

    [Header("Scene (room object)")]
    public Sprite sceneSprite;       // что ставим на объект в комнате

    [Header("Unlock & Buy")]
    public int unlockLevel;          // 0 = доступен сразу
    public int priceCoins;           // цена покупки
}

[CreateAssetMenu(menuName = "Room/Room Item Definition")]
public class RoomItemDefinition : ScriptableObject
{
    public RoomItemId id;
    public string displayName;

    [Header("Exactly 3 variants: 0=default, 1..2 unlock by level")]
    public RoomItemVariant[] variants = new RoomItemVariant[3];
}
