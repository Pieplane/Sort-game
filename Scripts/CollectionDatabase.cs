using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Collection Database")]
public class CollectionDatabase : ScriptableObject
{
    public List<CollectionItem> items = new();
}

[System.Serializable]
public class CollectionItem
{
    public string id;       // "item_01"
    public Sprite sprite;   // ОДИН цветной спрайт
}
