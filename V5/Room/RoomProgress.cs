using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RoomProgress
{
    public enum UpgradeSource
    {
        Buy,
        RewardUnlock,
        Equip
    }
    public static System.Action<RoomItemId, int, UpgradeSource> OnRoomItemChanged;
    // пример ключей
    //static string KeyOpened(RoomItemId id, int v) => $"room_opened_{(int)id}_{v}";
    static string KeyUnlocked(RoomItemId id, int idx) => $"room_unlocked_{id}_{idx}";
    static string KeyOwned(RoomItemId id, int idx) => $"room_owned_{id}_{idx}";
    static string KeyEquipped(RoomItemId id) => $"room_equipped_{id}";
    static string KeySeen(RoomItemId id, int idx) => $"room_seen_{id}_{idx}";

    public static event System.Action OnRoomChanged;

    public static bool IsUnlocked(RoomItemId id, int idx)
        => idx == 0 || PlayerPrefs.GetInt(KeyUnlocked(id, idx), 0) == 1;

    public static void SetUnlocked(RoomItemId id, int idx, bool value)
    {
        PlayerPrefs.SetInt(KeyUnlocked(id, idx), value ? 1 : 0);
        NotifyRoomChanged();
    }

    public static bool IsOwned(RoomItemId id, int idx)
        => idx == 0 || PlayerPrefs.GetInt(KeyOwned(id, idx), 0) == 1;

    public static void SetOwned(RoomItemId id, int idx, bool value)
    {
        PlayerPrefs.SetInt(KeyOwned(id, idx), value ? 1 : 0);
        NotifyRoomChanged();
    }

    public static int GetEquipped(RoomItemId id)
        => PlayerPrefs.GetInt(KeyEquipped(id), 0);

    public static void SetEquipped(RoomItemId id, int idx, UpgradeSource source = UpgradeSource.Equip)
    {
        PlayerPrefs.SetInt(KeyEquipped(id), idx);

        // ✅ УВЕДОМЛЯЕМ СЦЕНУ
        OnRoomItemChanged?.Invoke(id, idx, source);
        NotifyRoomChanged();
    }
    public static bool IsSeen(RoomItemId id, int idx)
    => PlayerPrefs.GetInt(KeySeen(id, idx), 0) == 1;

    public static void SetSeen(RoomItemId id, int idx, bool value)
        => PlayerPrefs.SetInt(KeySeen(id, idx), value ? 1 : 0);

    public static void NotifyRoomChanged()
    {
        OnRoomChanged?.Invoke();
    }
}
