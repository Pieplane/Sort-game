using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomVisualManager : MonoBehaviour
{
    public static RoomVisualManager Instance { get; private set; }

    [System.Serializable]
    public struct Binding
    {
        public RoomItemId id;
        public Image[] uiImages;
    }

    [SerializeField] private Binding[] bindings;

    private Dictionary<RoomItemId, Binding> map;

    [SerializeField] private RoomItemDefinition[] definitions; // все предметы комнаты
    private Dictionary<RoomItemId, RoomItemDefinition> defMap;

    //[SerializeField] private Camera roomCamera;
    [SerializeField] private StarFxPool starFxPool;
    [SerializeField] private Canvas fxCanvas;

    void Awake()
    {
        Instance = this;

        map = new Dictionary<RoomItemId, Binding>(bindings.Length);
        foreach (var b in bindings)
        {
            if (map.ContainsKey(b.id))
                Debug.LogWarning($"RoomVisualManager: duplicate binding for {b.id}. Last one will be used.");

            map[b.id] = b;
        }
    }
    void Start()
    {
        // построим мапу дефинишенов
        defMap = new Dictionary<RoomItemId, RoomItemDefinition>();
        if (definitions != null)
            foreach (var d in definitions)
                if (d != null) defMap[d.id] = d;

        ApplyAllFromProgress();
    }
    void OnEnable()
    {
        RoomProgress.OnRoomItemChanged += HandleRoomItemChanged;
    }

    void OnDisable()
    {
        RoomProgress.OnRoomItemChanged -= HandleRoomItemChanged;
    }
    void HandleRoomItemChanged(RoomItemId id, int variantIndex, RoomProgress.UpgradeSource source)
    {
        // 0) найти деф
        if (defMap == null || !defMap.TryGetValue(id, out var def) || def == null)
            return;

        // ✅ 1) СПРАЙТ ОБНОВЛЯЕМ ВСЕГДА
        Apply(def, variantIndex);

        // ✅ 2) FX ТОЛЬКО после покупки или рекламы
        bool needFx = (source == RoomProgress.UpgradeSource.Buy ||
                       source == RoomProgress.UpgradeSource.RewardUnlock);

        if (!needFx)
            return;

        if (starFxPool == null || fxCanvas == null) return;
        if (!map.TryGetValue(id, out var bind)) return;
        var images = bind.uiImages;
        if (images == null || images.Length == 0) return;

        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null) continue;
            sum += images[i].transform.position;
            count++;
        }
        if (count == 0) return;

        Vector3 center = sum / count;

        var preset = (source == RoomProgress.UpgradeSource.RewardUnlock)
            ? StarBurstPreset.Unlock
            : StarBurstPreset.Big;

        starFxPool.PlayBurstFromWorld(center, fxCanvas, preset);
    }

    public void ApplyAllFromProgress()
    {
        if (bindings == null) return;

        foreach (var b in bindings)
        {
            // ✅ пропускаем пустые биндинги
            if (b.uiImages == null || b.uiImages.Length == 0)
                continue;

            if (defMap == null || !defMap.TryGetValue(b.id, out var def) || def == null)
            {
                Debug.LogWarning($"RoomVisualManager: no definition for {b.id} (add it to definitions[])");
                continue;
            }

            int equipped = RoomProgress.GetEquipped(def.id);
            Apply(def, equipped); // Apply сам проставит спрайт всем renderers внутри биндинга
        }
    }

    public void Apply(RoomItemDefinition def, int variantIndex)
    {
        if (def == null) return;
        if (!map.TryGetValue(def.id, out var bind))
        {
            Debug.LogWarning($"RoomVisualManager: no binding for {def.id}");
            return;
        }

        if (def.variants == null || variantIndex < 0 || variantIndex >= def.variants.Length)
        {
            Debug.LogWarning($"RoomVisualManager: wrong variantIndex {variantIndex} for {def.id}");
            return;
        }

        var v = def.variants[variantIndex];
        var sprite = v.sceneSprite != null ? v.sceneSprite : v.previewSprite;
        if (sprite == null) return;

        // UI sprites
        if (bind.uiImages != null)
            for (int i = 0; i < bind.uiImages.Length; i++)
                if (bind.uiImages[i] != null)
                    bind.uiImages[i].sprite = sprite;
    }
}
