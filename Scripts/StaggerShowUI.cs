using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaggerShowUI : MonoBehaviour
{
    [Header("Targets (order matters)")]
    [SerializeField] private List<RectTransform> items = new List<RectTransform>();

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.15f;
    [SerializeField] private float stagger = 0.06f;     // задержка между кнопками
    [SerializeField] private float duration = 0.22f;    // длительность по€влени€

    [Header("Scale")]
    [SerializeField] private float fromScale = 0.0f;    // можно 0.6f если хочешь не с нул€
    [SerializeField] private float toScale = 1.0f;

    [Header("Ease")]
    [SerializeField] private Ease ease = Ease.OutBack;

    [Header("Optional")]
    [SerializeField] private bool disableRaycastsUntilDone = true;
    [SerializeField] private CanvasGroup blockGroup; // если хочешь блокировать клики на врем€

    Sequence seq;

    void Reset()
    {
        // автозаполнение: возьмЄт всех детей-RectTransform кроме себ€
        items.Clear();
        foreach (Transform ch in transform)
        {
            if (ch is RectTransform rt)
                items.Add(rt);
        }
    }

    void OnEnable()
    {
        Play();
    }

    public void Play()
    {
        seq?.Kill();

        if (disableRaycastsUntilDone && blockGroup != null)
            blockGroup.blocksRaycasts = false;

        // стартовые значени€
        for (int i = 0; i < items.Count; i++)
        {
            var rt = items[i];
            if (rt == null) continue;

            rt.DOKill();
            rt.localScale = Vector3.one * fromScale;
            rt.gameObject.SetActive(true);
        }

        seq = DOTween.Sequence().SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        float t = startDelay;
        for (int i = 0; i < items.Count; i++)
        {
            var rt = items[i];
            if (rt == null) { t += stagger; continue; }

            seq.Insert(t,
                rt.DOScale(toScale, duration)
                  .SetEase(ease)
                  .SetUpdate(true) // если меню на паузе (Time.timeScale=0) Ч всЄ равно анимируетс€
            );

            t += stagger;
        }

        seq.OnComplete(() =>
        {
            if (disableRaycastsUntilDone && blockGroup != null)
                blockGroup.blocksRaycasts = true;
        });

        seq.Play();
    }

    void OnDisable()
    {
        seq?.Kill();
    }
}
