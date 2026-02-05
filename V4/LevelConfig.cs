using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "MemeShelf/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    public int shelvesCount = 2;
    public int slotsPerShelf = 3;
    public int emptySlotsTotal = 1; // сколько пустых слотов на всю доску

    public ItemType[] allowedTypes = new[] { ItemType.A, ItemType.B, ItemType.C, ItemType.D, ItemType.E, ItemType.F, ItemType.G, ItemType.H, ItemType.I, ItemType.J, ItemType.K, ItemType.L, ItemType.M, ItemType.N, ItemType.O, ItemType.P, ItemType.Q, ItemType.R, ItemType.S, ItemType.T, ItemType.U, ItemType.V};

    [Header("Visuals for this level")]
    public ItemVisualConfig visuals;   // ✅ добавь сюда


    [Header("Start items total = shelvesCount*slotsPerShelf - 1 empty")]
    public bool randomStart = true;

    // если randomStart=false, используем ручную раскладку:
    // длина массива должна быть shelvesCount*slotsPerShelf, пустой слот = None
    public ItemTypeOrEmpty[] manualLayout;

    public int layoutIndex = 0;
}

public enum ItemTypeOrEmpty
{
    Empty = -1,
    A = 0,
    B = 1,
    C = 2,
    D = 3,
    E = 4,
    F = 5,
    G = 6,
    H = 7,
    I = 8,
    J = 9,
    K = 10,
    L = 11,
    M = 12,
    N = 13,
    O = 14,
    P = 15,
    Q = 16,
    R = 17,
    S = 18,
    T = 19,
    U = 20,
    V = 21
}
