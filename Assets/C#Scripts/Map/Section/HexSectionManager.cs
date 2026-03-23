using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Map.Section
{
    /// <summary>
    /// 地图 Section 管理器（单例）。
    /// 负责：
    ///   1. 游戏启动时记录相机与玩家的 Z 偏移量
    ///   2. 实例化预设 Section Prefab 并将入口节点对齐玩家当前格子前方
    ///   3. 驱动相机向前推进的 DOTween 动画
    ///   4. 驱动新 Section 所有节点的 LineRenderer 入场动画
    ///   5. 重建全图网格（BuildGrid）
    /// </summary>
    public class HexSectionManager : Singleton<HexSectionManager>
    {
        // ── 外部引用 ───────────────────────────────────────────────
        [Header("== 引用 ==")]
        [Tooltip("相机 Transform（默认取 Camera.main）")]
        public Transform cameraTransform;

        [Tooltip("玩家 Transform（默认取场景内 PlayerController）")]
        public Transform playerTransform;

        [Tooltip("场景中的 HexGridBuilder，用于重建全图")]
        public HexGridBuilder gridBuilder;

        // ── Section 参数 ──────────────────────────────────────────
        [Header("== Section 参数 ==")]
        [Tooltip("正六边形边长（默认 1）")]
        public float hexSideLength = 1f;

        [Tooltip("入口格相对玩家格的 X 方向：+1 = 右前方（x + s/2），-1 = 左前方（x - s/2）")]
        [Range(-1, 1)]
        public int entranceXSide = 1;

        // ── 动画参数 ──────────────────────────────────────────────
        [Header("== 动画参数 ==")]
        [Tooltip("相机推进动画时长（秒）")]
        public float cameraPushDuration = 0.8f;

        [Tooltip("相机推进缓动")]
        public Ease cameraPushEase = Ease.InOutQuad;

        [Tooltip("节点 LineRenderer 入场最大延迟（秒）")]
        public float revealMaxDelay = 0.6f;

        // ── 内部状态 ──────────────────────────────────────────────

        /// <summary>游戏开始时记录的 camera.z - player.z</summary>
        private float cameraZOffset;

        /// <summary>当前是否正在播放过场动画（期间锁定外部逻辑）</summary>
        public bool IsTransitioning { get; private set; }

        // ── Unity 生命周期 ─────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
        }

        void Start()
        {
            // 自动解析引用
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (playerTransform == null)
            {
                var pc = FindObjectOfType<Player.PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }

            if (gridBuilder == null)
                gridBuilder = FindObjectOfType<HexGridBuilder>();

            // 记录初始偏移
            if (cameraTransform != null && playerTransform != null)
            {
                cameraZOffset = cameraTransform.position.z - playerTransform.position.z;
                Debug.Log($"[HexSectionManager] 初始相机Z偏移 = {cameraZOffset:F3}");
            }
            else
            {
                Debug.LogWarning("[HexSectionManager] 无法解析相机或玩家引用，Z偏移未记录");
            }
        }

        // ── 公共接口 ──────────────────────────────────────────────

        /// <summary>
        /// 加载一个预设 Section 并播放入场动画。
        /// 由 HexNodeEvent_NextSection 调用。
        /// </summary>
        /// <param name="sectionPrefab">要实例化的 Section Prefab</param>
        /// <param name="playerCurrentNode">触发事件时玩家所在节点</param>
        public void LoadSection(GameObject sectionPrefab, HexNode playerCurrentNode)
        {
            if (IsTransitioning)
            {
                Debug.LogWarning("[HexSectionManager] 正在过渡中，忽略重复调用");
                return;
            }

            if (sectionPrefab == null || playerCurrentNode == null)
            {
                Debug.LogError("[HexSectionManager] sectionPrefab 或 playerCurrentNode 为 null");
                return;
            }

            IsTransitioning = true;

            // ── 步骤 1：计算新 Section 的实例化位置 ────────────────
            // 先在 (0,0,0) 实例化 Prefab，以便读取 entrance 的 localPosition
            GameObject sectionInstance = Instantiate(sectionPrefab, Vector3.zero, Quaternion.identity);

            // 找入口节点
            HexNode entranceNode = FindEntranceNode(sectionInstance);
            if (entranceNode == null)
            {
                Debug.LogError("[HexSectionManager] Section Prefab 内未找到 isEntrance=true 的 HexNode！");
                Destroy(sectionInstance);
                IsTransitioning = false;
                return;
            }

            // 边邻接前方格偏移（由实测坐标推算，边长 s = 0.866）：
            //   x = ±(1/2)·s       右前方取 +，左前方取 -
            //   z = +(√3/2)·s
            float xOffset = entranceXSide * 0.5f * hexSideLength;
            float zOffset = (Mathf.Sqrt(3f) / 2f) * hexSideLength;
            Vector3 targetEntranceWorld = new Vector3(
                playerCurrentNode.WorldPosition.x + xOffset,
                playerCurrentNode.WorldPosition.y,
                playerCurrentNode.WorldPosition.z + zOffset
            );

            // 入口节点当前的世界位置（此时 prefab 在原点，所以等于 localPos）
            Vector3 entranceCurrentWorld = entranceNode.transform.position;

            // 整体偏移 = 目标 - 当前
            Vector3 offset = targetEntranceWorld - entranceCurrentWorld;
            sectionInstance.transform.position += offset;

            // ── 步骤 2：收集新节点，初始化 LineRenderer 为 0 ────────
            List<HexNode> newNodes = new List<HexNode>(
                sectionInstance.GetComponentsInChildren<HexNode>()
            );

            // 为每个新节点确保有 HexSectionReveal 组件，并隐藏
            foreach (var node in newNodes)
            {
                var reveal = node.GetComponent<HexSectionReveal>();
                if (reveal == null)
                    reveal = node.gameObject.AddComponent<HexSectionReveal>();
                reveal.HideImmediate();
            }

            // ── 步骤 3：相机推进 ─────────────────────────────────────
            float targetCameraZ = targetEntranceWorld.z + cameraZOffset;
            Tween cameraTween = null;

            if (cameraTransform != null)
            {
                Vector3 camTarget = new Vector3(
                    cameraTransform.position.x,
                    cameraTransform.position.y,
                    targetCameraZ
                );
                cameraTween = cameraTransform
                    .DOMove(camTarget, cameraPushDuration)
                    .SetEase(cameraPushEase);
            }

            // ── 步骤 5：节点入场动画 ──────────────────────────────────
            PlayRevealAnimation(newNodes, cameraPushDuration);

            // ── 步骤 4：动画全部完成后：重建网格 → 解锁输入 ───────────
            // totalDuration = 相机推进 + 最远节点延迟 + 该节点动画时长
            float totalDuration = cameraPushDuration + revealMaxDelay
                + GetMaxRevealDuration(newNodes);

            // 动画结束后 0.1 秒再 BuildGrid，确保所有碰撞体已完成物理注册
            DOVirtual.DelayedCall(totalDuration + 0.1f, () =>
            {
                if (gridBuilder != null)
                {
                    gridBuilder.BuildGrid();
                    Debug.Log("[HexSectionManager] BuildGrid 完成");
                }
                else
                {
                    Debug.LogWarning("[HexSectionManager] gridBuilder 未设置，跳过重建");
                }

                IsTransitioning = false;
                Debug.Log("[HexSectionManager] Section 过渡完成，输入已解锁");
            });
        }

        // ── 内部工具 ──────────────────────────────────────────────

        /// <summary>
        /// 在实例化后的 Section 中查找入口节点
        /// </summary>
        private HexNode FindEntranceNode(GameObject sectionInstance)
        {
            HexNode[] nodes = sectionInstance.GetComponentsInChildren<HexNode>();
            foreach (var node in nodes)
            {
                if (node.isEntrance)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// 播放所有新节点的 LineRenderer 入场动画。
        /// Z 最小的节点延迟为 0，其余节点按 Z 值线性递增延迟。
        /// 动画整体在相机推进完成后开始（baseDelay = cameraPushDuration）。
        /// </summary>
        private void PlayRevealAnimation(List<HexNode> nodes, float baseDelay)
        {
            if (nodes == null || nodes.Count == 0) return;

            // 找 Z 范围
            float zMin = float.MaxValue;
            float zMax = float.MinValue;

            foreach (var node in nodes)
            {
                float z = node.WorldPosition.z;
                if (z < zMin) zMin = z;
                if (z > zMax) zMax = z;
            }

            float zRange = zMax - zMin;

            foreach (var node in nodes)
            {
                var reveal = node.GetComponent<HexSectionReveal>();
                if (reveal == null) continue;

                // 节点 Z 在范围内的比例 → 延迟
                float t = (zRange > 0.001f)
                    ? (node.WorldPosition.z - zMin) / zRange
                    : 0f;

                float delay = baseDelay + t * revealMaxDelay;
                reveal.PlayReveal(delay);
            }
        }

        /// <summary>
        /// 获取所有节点中最大的 revealDuration（用于计算总动画时长）
        /// </summary>
        private float GetMaxRevealDuration(List<HexNode> nodes)
        {
            float max = 0f;
            foreach (var node in nodes)
            {
                var reveal = node.GetComponent<HexSectionReveal>();
                if (reveal != null && reveal.revealDuration > max)
                    max = reveal.revealDuration;
            }
            return max;
        }
    }
}
