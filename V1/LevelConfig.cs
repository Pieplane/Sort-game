using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MemeShelf/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    public int shelvesCount = 2;
    public int slotsPerShelf = 3;
    public int emptySlotsTotal = 1; // сколько пустых слотов на всю доску

    public ItemType[] allowedTypes = new[] { ItemType.A, ItemType.B };

    [Header("Start items total = shelvesCount*slotsPerShelf - 1 empty")]
    public bool randomStart = true;

    // если randomStart=false, используем ручную раскладку:
    // длина массива должна быть shelvesCount*slotsPerShelf, пустой слот = None
    public ItemTypeOrEmpty[] manualLayout;
}

public enum ItemTypeOrEmpty
{
    Empty = -1,
    A = 0,
    B = 1
}
