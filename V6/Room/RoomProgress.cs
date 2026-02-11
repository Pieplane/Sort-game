using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomProgress
{
    public enum UpgradeSource { Buy, RewardUnlock, Equip }

    public static Action<RoomItemId, int, UpgradeSource> OnRoomItemChanged;
    public static event Action OnRoomChanged;

    static RoomProgressSaveData Data => PlayerProgress.Instance?.Room;

    static RoomItemState GetOrCreate(RoomItemId id)
    {
        if (PlayerProgress.Instance == null || !PlayerProgress.Instance.IsLoaded)
            return null;

        var room = PlayerProgress.Instance.Room;
        if (room == null)
        {
            room = new RoomProgressSaveData();
            //PlayerProgress.Instance.SaveRoom(room);
        }

        string key = id.ToString();
        var list = room.items;
        for (int i = 0; i < list.Count; i++)
            if (list[i].id == key) return list[i];

        var st = new RoomItemState
        {
            id = key,
            equipped = 0,
            ownedMask = 1,     // дефолт owned
            unlockedMask = 1,  // дефолт unlocked
            seenMask = 0
        };
        list.Add(st);
        //SaveRoom();
        return st;
    }

    static bool HasBit(int mask, int idx) => (mask & (1 << idx)) != 0;
    static int SetBit(int mask, int idx, bool value)
        => value ? (mask | (1 << idx)) : (mask & ~(1 << idx));

    public static bool IsUnlocked(RoomItemId id, int idx)
    {
        if (idx == 0) return true;
        var st = GetOrCreate(id);
        if (st == null) return false;
        return HasBit(st.unlockedMask, idx);
    }

    public static void SetUnlocked(RoomItemId id, int idx, bool value)
    {
        if (idx == 0) return;
        var st = GetOrCreate(id);
        if (st == null) return;

        st.unlockedMask = SetBit(st.unlockedMask, idx, value);
        //SaveRoom();
        NotifyRoomChanged();
    }

    public static bool IsOwned(RoomItemId id, int idx)
    {
        if (idx == 0) return true;
        var st = GetOrCreate(id);
        if (st == null) return false;
        return HasBit(st.ownedMask, idx);
    }

    public static void SetOwned(RoomItemId id, int idx, bool value)
    {
        if (idx == 0) return;
        var st = GetOrCreate(id);
        if (st == null) return;

        st.ownedMask = SetBit(st.ownedMask, idx, value);
        //SaveRoom();
        NotifyRoomChanged();
    }

    public static int GetEquipped(RoomItemId id)
    {
        var st = GetOrCreate(id);
        return st != null ? st.equipped : 0;
    }

    public static void SetEquipped(RoomItemId id, int idx, UpgradeSource source = UpgradeSource.Equip)
    {
        var st = GetOrCreate(id);
        if (st == null) return;

        st.equipped = idx;
        //SaveRoom();

        OnRoomItemChanged?.Invoke(id, idx, source);
        NotifyRoomChanged();
    }

    public static bool IsSeen(RoomItemId id, int idx)
    {
        var st = GetOrCreate(id);
        if (st == null) return false;
        return HasBit(st.seenMask, idx);
    }

    public static void SetSeen(RoomItemId id, int idx, bool value)
    {
        var st = GetOrCreate(id);
        if (st == null) return;

        st.seenMask = SetBit(st.seenMask, idx, value);
        //SaveRoom();
    }

    public static void SaveRoom()
    {
        // сохраняем через общий прогресс (Editor -> PlayerPrefs, WebGL -> Cloud)
        if (PlayerProgress.Instance == null) return;
        PlayerProgress.Instance.SaveRoom(PlayerProgress.Instance.Room);
    }

    public static void NotifyRoomChanged()
    {
        OnRoomChanged?.Invoke();
    }
}
