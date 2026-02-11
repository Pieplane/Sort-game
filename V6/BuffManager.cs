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
    public void Buff_ClearSomeOfType()
    {
        if (board == null) return;

        int triplets = GetClearTripletsNow();
        board.Buff_RemoveTripletsOfMostCommonType(triplets);

        AudioManager.Instance?.Play("Collect");
        //if (board == null) return;

        //board.ClearSomeOfSameType(count);
        //if (AudioManager.Instance != null)
        //{
        //    // ================================== 🎧 AUDIO MANAGER CALL ==================================
        //    AudioManager.Instance.Play("Collect");
        //}
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
    public bool UseBoost(BoostType type)
    {
        if (board == null) return false;

        bool applied = false;

        switch (type)
        {
            case BoostType.ClearSomeOfType:
                applied = TryBuff_ClearSomeOfType();
                break;

            case BoostType.ShuffleButton:
                Buff_Shuffle();
                applied = true;
                break;

            case BoostType.HintThreeSame:
                Buff_HintThreeSame();
                applied = true;
                break;
        }

        if (!applied)
            return false;

        // ✅ если infinite — НЕ ТРАТИМ
        if (PlayerProgress.Instance != null && PlayerProgress.Instance.InfiniteBoosters)
        {
            board.OnBoostUsed();
            return true;
        }

        // ✅ обычный режим — тратим
        if (!BoostInventory.TryConsume(type, 1))
            return false;

        board.OnBoostUsed();
        return true;
    }
    int GetClearTripletsNow()
    {
        int gl = GameFlow.GameLevel;

        if (gl <= 5) return 1;   // удалить 1 тройку (3 предмета)
        if (gl <= 10) return 2;   // 2 тройки (6 предметов)
        if (gl <= 20) return 2;   // 2 тройки (6 предметов)
        return 3;                 // 3 тройки (9 предметов)
    }
    public bool TryBuff_ClearSomeOfType()
    {
        if (board == null) return false;

        int desired = GetClearTripletsNow();                 // сколько хочешь по уровню
        int removable = board.GetRemovableTriplets(desired); // сколько реально можно

        if (removable <= 0)
            return false;

        board.Buff_RemoveTripletsOfMostCommonType(removable);

        return true;
    }
}
