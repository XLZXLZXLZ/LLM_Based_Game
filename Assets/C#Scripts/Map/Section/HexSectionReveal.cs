using DG.Tweening;
using UnityEngine;

namespace Map.Section
{
    /// <summary>
    /// 挂载在每个 HexNode 上，负责管理其 LineRenderer 的入场动画。
    /// Awake 时自动缓存 LineRenderer 的原始宽度；
    /// 动画由 HexSectionManager 统一调度，调用 PlayReveal() 驱动。
    /// </summary>
    [RequireComponent(typeof(HexNode))]
    public class HexSectionReveal : MonoBehaviour
    {
        [Header("动画参数")]
        [Tooltip("线宽从0变到目标宽度所需的时间（秒）")]
        public float revealDuration = 0.4f;

        /// <summary>节点持有的 LineRenderer（可为空，若无则跳过动画）</summary>
        private LineRenderer lineRenderer;

        /// <summary>Prefab 原始 widthMultiplier，Awake 时缓存</summary>
        private float originalWidthMultiplier;

        private Tween revealTween;

        void Awake()
        {
            // LineRenderer 与 HexNode 同属一个父物体的不同子物体
            // 先尝试自身，再尝试子物体，最后向父物体的其他子物体查找
            lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer == null)
                lineRenderer = GetComponentInChildren<LineRenderer>();

            if (lineRenderer == null && transform.parent != null)
                lineRenderer = transform.parent.GetComponentInChildren<LineRenderer>();

            if (lineRenderer != null)
                originalWidthMultiplier = lineRenderer.widthMultiplier;
        }

        /// <summary>
        /// 立即将 LineRenderer 宽度设为 0（在 Instantiate 后、动画前调用）
        /// </summary>
        public void HideImmediate()
        {
            if (lineRenderer == null) return;
            lineRenderer.widthMultiplier = 0f;
        }

        /// <summary>
        /// 播放入场动画：lineWidth 从 0 渐变到原始宽度
        /// </summary>
        /// <param name="delay">延迟时间（秒），由 HexSectionManager 根据 Z 位置计算</param>
        public void PlayReveal(float delay)
        {
            if (lineRenderer == null) return;

            revealTween?.Kill();
            lineRenderer.widthMultiplier = 0f;

            revealTween = DOTween
                .To(
                    () => lineRenderer.widthMultiplier,
                    x => lineRenderer.widthMultiplier = x,
                    originalWidthMultiplier,
                    revealDuration
                )
                .SetDelay(delay)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// 立即还原到原始宽度（跳过动画）
        /// </summary>
        public void ShowImmediate()
        {
            revealTween?.Kill();
            if (lineRenderer == null) return;
            lineRenderer.widthMultiplier = originalWidthMultiplier;
        }

        void OnDestroy()
        {
            revealTween?.Kill();
        }
    }
}
