using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 天平HUD：监听 BalanceChanged 事件并驱动天平可视化。
/// </summary>
public class BalanceHUDPresenter : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private Transform balanceArm;
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;
    [SerializeField] private Transform leftSlot;
    [SerializeField] private Transform rightSlot;

    [Header("星星预制体")]
    [SerializeField] private GameObject calmStarPrefab;
    [SerializeField] private GameObject angerStarPrefab;
    [SerializeField] private Transform calmStarRoot;
    [SerializeField] private Transform angerStarRoot;

    [Header("星星轨道")]
    [SerializeField] private Vector3 calmOrbitLocalOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Vector3 angerOrbitLocalOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float orbitRadius = 0.45f;
    [SerializeField] private float orbitSpeed = 80f;
    [SerializeField] private float orbitLerpSpeed = 8f;

    [Header("天平旋转")]
    [SerializeField] private bool angerOnRight = true;
    [SerializeField] private float maxRotationDegrees = 15f;
    [SerializeField] private float rotateDuration = 0.25f;
    [SerializeField] private Ease rotateEase = Ease.OutCubic;

    [Header("槽位跟随")]
    [SerializeField] private float slotFollowLerpSpeed = 16f;

    [Header("星星动画")]
    [SerializeField] private float starSpawnDuration = 0.25f;
    [SerializeField] private float starDespawnDuration = 0.2f;
    [SerializeField] private Ease starSpawnEase = Ease.OutBack;
    [SerializeField] private Ease starDespawnEase = Ease.InBack;
    [SerializeField] private float starSpawnFromScale = 0.15f;

    [Header("差值文本")]
    [SerializeField] private Text differenceText;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color calmColor = new Color(0.35f, 0.72f, 1f);
    [SerializeField] private Color angerColor = new Color(1f, 0.38f, 0.38f);

    public int AngerPoint { get; private set; }
    public int CalmPoint { get; private set; }
    public int Difference { get; private set; }

    private readonly List<OrbitStar> calmStars = new();
    private readonly List<OrbitStar> angerStars = new();

    private BattleContext ctx;
    private bool isBound;
    private float calmOrbitAngle;
    private float angerOrbitAngle;
    private Tweener armRotateTween;

    private class OrbitStar
    {
        public Transform Transform;
        public Vector3 BaseScale;
        public float CurrentAngle;
        public Tween ActiveTween;
    }

    public void Initialize(BattleContext context)
    {
        if (context == null) return;

        Unbind();
        ctx = context;

        AngerPoint = Mathf.Max(0, ctx.Balance.AngerPoint);
        CalmPoint = Mathf.Max(0, ctx.Balance.CalmPoint);
        Difference = Mathf.Abs(AngerPoint - CalmPoint);

        SyncStarsToPoints(immediate: true);
        RefreshDifferenceText();
        RefreshArmRotation(immediate: true);
        SnapSlotsToAnchors();

        Bind();
    }

    private void Update()
    {
        FollowSlots();
        UpdateOrbit(Time.deltaTime);
    }

    private void OnDestroy()
    {
        Unbind();
        KillAllTweensAndDestroyStars();
    }

    private void Bind()
    {
        if (ctx == null || ctx.Events == null || isBound) return;
        ctx.Events.OnBalanceChanged.OnAfter(HandleBalanceChanged, EventPriority.Normal, this);
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || ctx == null || ctx.Events == null) return;
        ctx.Events.UnregisterAllByOwner(this);
        isBound = false;
    }

    private void HandleBalanceChanged(BalanceChangedEventArgs args)
    {
        AngerPoint = Mathf.Max(0, args.CurrentAnger);
        CalmPoint = Mathf.Max(0, args.CurrentCalm);
        Difference = Mathf.Abs(AngerPoint - CalmPoint);

        SyncStarsToPoints(immediate: false);
        RefreshDifferenceText();
        RefreshArmRotation(immediate: false);
    }

    private void FollowSlots()
    {
        if (leftSlot != null && leftAnchor != null)
        {
            leftSlot.position = Vector3.Lerp(
                leftSlot.position,
                leftAnchor.position,
                1f - Mathf.Exp(-slotFollowLerpSpeed * Time.deltaTime));
        }

        if (rightSlot != null && rightAnchor != null)
        {
            rightSlot.position = Vector3.Lerp(
                rightSlot.position,
                rightAnchor.position,
                1f - Mathf.Exp(-slotFollowLerpSpeed * Time.deltaTime));
        }
    }

    private void SnapSlotsToAnchors()
    {
        if (leftSlot != null && leftAnchor != null) leftSlot.position = leftAnchor.position;
        if (rightSlot != null && rightAnchor != null) rightSlot.position = rightAnchor.position;
    }

    private void RefreshArmRotation(bool immediate)
    {
        if (balanceArm == null || ctx == null || ctx.Balance == null) return;

        int maxDiff = Mathf.Max(1, ctx.Balance.MaxDifference);
        int warningDiff = Mathf.Max(1, maxDiff - 1);
        float t = Mathf.Clamp01((float)Difference / warningDiff);
        float targetAbsAngle = maxRotationDegrees * t;

        int sign = 0;
        if (AngerPoint > CalmPoint) sign = angerOnRight ? -1 : 1;
        else if (CalmPoint > AngerPoint) sign = angerOnRight ? 1 : -1;

        float targetY = targetAbsAngle * sign;
        armRotateTween?.Kill();

        if (immediate)
        {
            Vector3 euler = balanceArm.localEulerAngles;
            balanceArm.localEulerAngles = new Vector3(euler.x, targetY, euler.z);
        }
        else
        {
            armRotateTween = balanceArm
                .DOLocalRotate(new Vector3(0f, targetY, 0f), rotateDuration)
                .SetEase(rotateEase);
        }
    }

    private void RefreshDifferenceText()
    {
        if (differenceText == null || ctx == null || ctx.Balance == null) return;

        differenceText.text = Difference.ToString();

        if (Difference == 0)
        {
            differenceText.color = neutralColor;
            return;
        }

        int maxDiff = Mathf.Max(1, ctx.Balance.MaxDifference);
        float t = Mathf.Clamp01((float)Difference / maxDiff);
        Color target = AngerPoint > CalmPoint ? angerColor : calmColor;
        differenceText.color = Color.Lerp(neutralColor, target, t);
    }

    private void SyncStarsToPoints(bool immediate)
    {
        SyncSideStars(calmStars, CalmPoint, calmStarPrefab, GetCalmSlot(), calmStarRoot, immediate);
        SyncSideStars(angerStars, AngerPoint, angerStarPrefab, GetAngerSlot(), angerStarRoot, immediate);
    }

    private void SyncSideStars(
        List<OrbitStar> stars,
        int targetCount,
        GameObject prefab,
        Transform slotTransform,
        Transform root,
        bool immediate)
    {
        if (targetCount < 0) targetCount = 0;

        while (stars.Count < targetCount)
        {
            var newStar = SpawnStar(prefab, slotTransform, root, immediate);
            if (newStar == null) break;
            stars.Add(newStar);
        }

        while (stars.Count > targetCount)
        {
            int lastIndex = stars.Count - 1;
            DespawnStar(stars[lastIndex], immediate);
            stars.RemoveAt(lastIndex);
        }
    }

    private OrbitStar SpawnStar(GameObject prefab, Transform slotTransform, Transform root, bool immediate)
    {
        if (prefab == null || slotTransform == null) return null;

        Transform parent = root != null ? root : transform;
        GameObject starObject = Instantiate(prefab, parent);
        Transform starTransform = starObject.transform;
        starTransform.position = slotTransform.position;
        Vector3 baseScale = starTransform.localScale;
        starTransform.localScale = immediate ? baseScale : baseScale * starSpawnFromScale;

        var star = new OrbitStar
        {
            Transform = starTransform,
            BaseScale = baseScale,
            CurrentAngle = 0f
        };

        if (!immediate)
        {
            star.ActiveTween = starTransform
                .DOScale(star.BaseScale, starSpawnDuration)
                .SetEase(starSpawnEase);
        }

        return star;
    }

    private void DespawnStar(OrbitStar star, bool immediate)
    {
        if (star == null || star.Transform == null) return;

        star.ActiveTween?.Kill();
        if (immediate)
        {
            Destroy(star.Transform.gameObject);
            return;
        }

        Sequence seq = DOTween.Sequence();
        seq.Join(star.Transform.DOScale(0f, starDespawnDuration).SetEase(starDespawnEase));
        seq.Join(star.Transform.DOMove(star.Transform.position + Vector3.up * 0.25f, starDespawnDuration));
        seq.OnComplete(() =>
        {
            if (star.Transform != null)
            {
                Destroy(star.Transform.gameObject);
            }
        });
        star.ActiveTween = seq;
    }

    private void UpdateOrbit(float deltaTime)
    {
        calmOrbitAngle += orbitSpeed * deltaTime;
        angerOrbitAngle += orbitSpeed * deltaTime;

        UpdateSideOrbit(calmStars, GetCalmOrbitCenter(), calmOrbitAngle, deltaTime);
        UpdateSideOrbit(angerStars, GetAngerOrbitCenter(), angerOrbitAngle, deltaTime);
    }

    private void UpdateSideOrbit(List<OrbitStar> stars, Vector3 center, float globalAngle, float deltaTime)
    {
        int count = stars.Count;
        if (count <= 0) return;

        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            OrbitStar star = stars[i];
            if (star == null || star.Transform == null) continue;

            float targetAngle = globalAngle + i * step;
            star.CurrentAngle = Mathf.LerpAngle(
                star.CurrentAngle,
                targetAngle,
                1f - Mathf.Exp(-orbitLerpSpeed * deltaTime));

            float rad = star.CurrentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
            star.Transform.position = center + offset;
        }
    }

    private Transform GetCalmSlot() => angerOnRight ? leftSlot : rightSlot;
    private Transform GetAngerSlot() => angerOnRight ? rightSlot : leftSlot;

    private Vector3 GetCalmOrbitCenter()
    {
        Transform slot = GetCalmSlot();
        return slot != null ? slot.position + calmOrbitLocalOffset : transform.position;
    }

    private Vector3 GetAngerOrbitCenter()
    {
        Transform slot = GetAngerSlot();
        return slot != null ? slot.position + angerOrbitLocalOffset : transform.position;
    }

    private void KillAllTweensAndDestroyStars()
    {
        armRotateTween?.Kill();
        KillAndClearSideStars(calmStars);
        KillAndClearSideStars(angerStars);
    }

    private void KillAndClearSideStars(List<OrbitStar> stars)
    {
        foreach (var star in stars)
        {
            if (star == null) continue;
            star.ActiveTween?.Kill();
            if (star.Transform != null)
            {
                Destroy(star.Transform.gameObject);
            }
        }
        stars.Clear();
    }
}
