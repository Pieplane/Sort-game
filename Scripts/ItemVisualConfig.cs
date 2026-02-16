using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MemeShelf/ItemVisualConfig")]
public class ItemVisualConfig : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public ItemType type;
        public Sprite sprite;
    }

    public Entry[] entries;

    public Sprite GetSprite(ItemType type)
    {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].type == type)
                return entries[i].sprite;
        return null;
    }
}
