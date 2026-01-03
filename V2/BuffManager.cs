using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    BoardManager board;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        board = FindFirstObjectByType<BoardManager>();
    }

    // ---------- BUFFS ----------

    /// 🧨 Удалить N предметов одного типа
    public void Buff_ClearSomeOfType(int count = 3)
    {
        if (board == null) return;
        board.ClearSomeOfSameType(count);
    }

    /// 🔄 Перемешать все предметы
    public void Buff_Shuffle()
    {
        if (board == null) return;
        board.ShuffleBoard();
    }
}
