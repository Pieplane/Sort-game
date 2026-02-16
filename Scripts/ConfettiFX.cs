using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfettiFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] RectTransform root;
    [SerializeField] Image confettiPrefab;

    [Header("Spawn")]
    [SerializeField] int count = 30;
    [SerializeField] Vector2 spawnPos = new Vector2(0f, 0f);   // точка взрыва в root-координатах

    [Header("Ballistics")]
    [SerializeField] float minSpeed = 900f;    // px/sec
    [SerializeField] float maxSpeed = 1500f;   // px/sec
    [SerializeField] float gravity = 2200f;    // px/sec^2 (вниз)
    [SerializeField] float drag = 0.08f;       // 0..1 (сопротивление)

    [Header("Cone Up (degrees)")]
    [SerializeField] float minAngle = 20f;     // ширина веера
    [SerializeField] float maxAngle = 160f;    // 0..180 это верхняя полуплоскость

    [Header("Lifetime")]
    [SerializeField] float minLife = 1.1f;
    [SerializeField] float maxLife = 1.8f;

    [Header("Visuals")]
    [SerializeField] Color[] colors;
    [SerializeField] Sprite[] shapes;
    [SerializeField] float minSize = 18f;
    [SerializeField] float maxSize = 36f;

    public void Play()
    {
        if (root == null || confettiPrefab == null) return;

        for (int i = 0; i < count; i++)
            SpawnOne();
    }
    void OnDisable()
    {
        DOTween.Kill(gameObject);   // убьёт все твины, залинкованные на этот GO
        transform.DOKill();         // на всякий
        GetComponent<CanvasGroup>()?.DOKill();
    }

    void SpawnOne()
    {
        Image piece = Instantiate(confettiPrefab, root);
        RectTransform rt = piece.rectTransform;

        // sprite/цвет
        if (shapes != null && shapes.Length > 0)
            piece.sprite = shapes[Random.Range(0, shapes.Length)];

        if (colors != null && colors.Length > 0)
            piece.color = colors[Random.Range(0, colors.Length)];

        float size = Random.Range(minSize, maxSize);
        rt.sizeDelta = new Vector2(size, size);

        // старт
        rt.anchoredPosition = spawnPos;

        // скорость: ВЕЕР ВВЕРХ
        float angleDeg = Random.Range(minAngle, maxAngle);
        float a = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)).normalized;

        float speed = Random.Range(minSpeed, maxSpeed);
        Vector2 vel = dir * speed;

        // жизнь
        float life = Random.Range(minLife, maxLife);

        // вращение
        rt.DORotate(
                new Vector3(0, 0, Random.Range(-720f, 720f)),
                life,
                RotateMode.FastBeyond360
            )
            .SetLink(piece.gameObject); // 🔥 ВАЖНО


        // фейд
        piece.DOFade(0f, 0.35f)
            .SetDelay(life - 0.35f)
            .SetLink(piece.gameObject)
            .OnComplete(() =>
            {
                if (piece != null)
                    Destroy(piece.gameObject);
            });

        // движение физикой
        StartCoroutine(BallisticRoutine(piece, rt, vel, life));
    }

    IEnumerator BallisticRoutine(Image piece, RectTransform rt, Vector2 vel, float life)
    {
        float t = 0f;

        while (t < life)
        {
            // если объект уже уничтожили извне — выходим
            if (rt == null) yield break;

            float dt = Time.unscaledDeltaTime;
            t += dt;

            // гравитация
            vel.y -= gravity * dt;

            // лёгкое сопротивление
            vel *= (1f - drag * dt);

            // интеграция позиции
            rt.anchoredPosition += vel * dt;

            yield return null;
        }

        if (piece != null) Destroy(piece.gameObject);
    }
}
