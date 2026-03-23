using DG.Tweening;
using UnityEngine;

/// <summary>
/// 全局相机特效组件（单例）。
/// 当前提供镜头震动能力，可在任意位置通过 CameraEffects.Instance 调用。
/// </summary>
public class CameraEffects : Singleton<CameraEffects>
{
    [Header("镜头震动默认参数")]
    [SerializeField] private float defaultShakeAmplitude = 0.12f;
    [SerializeField] private float defaultShakeDuration = 0.18f;
    [SerializeField] private int vibrato = 20;
    [SerializeField] private float randomness = 90f;

    private Vector3 originalLocalPosition;
    private Tween shakeTween;

    protected override void Awake()
    {
        base.Awake();
        originalLocalPosition = transform.localPosition;
    }

    private void OnDestroy()
    {
        shakeTween?.Kill();
    }

    /// <summary>
    /// 使用默认参数触发镜头震动（默认值偏小，适合频繁触发）。
    /// </summary>
    public void Shake()
    {
        Shake(defaultShakeAmplitude, defaultShakeDuration);
    }

    /// <summary>
    /// 触发镜头震动。
    /// </summary>
    /// <param name="amplitude">震动幅度（建议 0.05 ~ 0.4）</param>
    /// <param name="duration">震动时长（秒）</param>
    public void Shake(float amplitude, float duration)
    {
        float safeAmplitude = Mathf.Max(0f, amplitude);
        float safeDuration = Mathf.Max(0.01f, duration);

        shakeTween?.Kill();
        transform.localPosition = originalLocalPosition;

        shakeTween = transform
            .DOShakePosition(
                safeDuration,
                safeAmplitude,
                vibrato,
                randomness,
                false,
                true)
            .SetUpdate(true)
            .OnComplete(() => transform.localPosition = originalLocalPosition)
            .OnKill(() => transform.localPosition = originalLocalPosition);
    }
}
