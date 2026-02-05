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
    public void Buff_HintThreeSame()
    {
        if (board == null) return;
        board.Buff_Hint_ThreeOrTwo(1.2f);
    }
    public void UseBoost(BoostType type)
    {
        switch (type)
        {
            case BoostType.ClearSomeOfType:
                Buff_ClearSomeOfType(3);
                break;
            case BoostType.ShuffleButton:
                Buff_Shuffle();
                break;
            case BoostType.HintThreeSame:
                Buff_HintThreeSame();
                break;
        }
    }
}
