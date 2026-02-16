using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarFx : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;

    private RectTransform rt;
    private Sequence seq;

    void Awake()
    {
        rt = (RectTransform)transform;
        if (group == null) group = GetComponent<CanvasGroup>();
    }

    void OnDisable()
    {
        // если объект выключили/прибрали в пул — убиваем анимации
        KillTweens();
    }

    void OnDestroy()
    {
        // если объект реально Destroy — тоже убиваем
        KillTweens();
    }

    private void KillTweens()
    {
        if (seq != null && seq.IsActive())
            seq.Kill();

        seq = null;

        if (rt != null) rt.DOKill();
        if (group != null) group.DOKill();
    }

    public void Play(Vector2 centerPos, StarBurstSettings fx)
    {
        if (rt == null) rt = (RectTransform)transform;
        KillTweens();

        Vector2 start = centerPos + Random.insideUnitCircle * fx.spawnRadius;

        rt.anchoredPosition = start;
        rt.localScale = Vector3.one * Random.Range(0.45f, 0.8f);
        rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        if (group != null) group.alpha = 1f;

        float dur = Random.Range(fx.durationMin, fx.durationMax);

        float dx = Random.Range(-45f, 45f);
        float dy = Random.Range(25f, 65f);
        float fall = Random.Range(12f, 28f);

        Vector2 p1 = start + new Vector2(dx * 0.6f, dy);
        Vector2 p2 = start + new Vector2(dx, dy - fall);

        seq = DOTween.Sequence()
            .SetUpdate(fx.unscaledTime)
            .SetLink(gameObject);

        seq.Append(rt.DOAnchorPos(p1, dur * 0.55f).SetEase(Ease.OutQuad));
        seq.Append(rt.DOAnchorPos(p2, dur * 0.45f).SetEase(Ease.InQuad));

        seq.Join(rt.DOScale(Random.Range(0.85f, 1.15f), dur).SetEase(Ease.OutBack));
        seq.Join(rt.DOLocalRotate(
            new Vector3(0, 0, rt.localEulerAngles.z + Random.Range(-120f, 120f)),
            dur,
            RotateMode.FastBeyond360).SetEase(Ease.InOutSine));

        if (group != null)
            seq.Join(group.DOFade(0f, dur).SetEase(Ease.InQuad));

        seq.OnComplete(() =>
        {
            if (this != null && gameObject) gameObject.SetActive(false);
        });
    }
}
