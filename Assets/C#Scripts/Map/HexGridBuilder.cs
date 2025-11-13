using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// 六边形网格构建器
    /// 负责扫描场景中的HexNode，检测邻居关系，分配坐标
    /// </summary>
    [ExecuteAlways]  // 在编辑器模式下也运行
    public class HexGridBuilder : MonoBehaviour
    {
        [Header("六边形参数")]
        [Tooltip("六边形内接圆半径（中心到边的垂直距离）")]
        public float hexApothem = 1.0f;

        [Header("检测参数")]
        [Tooltip("邻居检测半径（建议2倍半径）")]
        public float detectionRadius = 2.0f;
        
        [Tooltip("角度容差（度）")]
        [Range(5f, 20f)]
        public float angleTolerance = 12f;
        
        [Tooltip("异常节点距离阈值")]
        public float abnormalThreshold = 2.5f;

        [Header("Gizmos显示设置")]
        [Tooltip("球体半径")]
        public float gizmosSphereRadius = 0.3f;
        
        [Tooltip("Gizmos高度偏移（避免被模型遮挡）")]
        public float gizmosHeightOffset = 1.0f;
        
        [Tooltip("坐标文字高度偏移")]
        public float gizmosLabelOffset = 1.5f;

        [Header("调试")]
        public bool showDebugLog = true;
        
        [Tooltip("构建时自动应用Gizmos设置到所有节点")]
        public bool autoApplyGizmosSettings = true;
        
        [Tooltip("在编辑器模式下自动每帧重建（实时预览）")]
        public bool autoRebuildInEditor = false;
        
        [Tooltip("始终显示所有连接线（不只是选中时）")]
        public bool alwaysShowConnections = true;
        
        [Tooltip("显示不可通行节点的连接线")]
        public bool showUnwalkableConnections = false;
        
        [Tooltip("构建时忽略不可通行节点（不建立邻居关系）")]
        public bool ignoreUnwalkableNodes = false;

        // Pointy-top六边形的6个标准方向角度（度）
        // 0°=东, 60°=东南, 120°=西南, 180°=西, 240°=西北, 300°=东北
        private readonly float[] standardAngles = { 0f, 60f, 120f, 180f, 240f, 300f };
        
        // Pointy-top的Axial坐标偏移
        // 对应6个方向：东、东南、西南、西、西北、东北
        private readonly Vector2Int[] axialOffsets = {
            new Vector2Int(+1,  0),   // 0: 东
            new Vector2Int( 0, +1),   // 1: 东南
            new Vector2Int(-1, +1),   // 2: 西南
            new Vector2Int(-1,  0),   // 3: 西
            new Vector2Int( 0, -1),   // 4: 西北
            new Vector2Int(+1, -1),   // 5: 东北
        };

        private List<HexNode> allNodes = new List<HexNode>();
        private HexNode originNode;
        
        private float lastRebuildTime = 0f;
        private const float rebuildInterval = 0.1f; // 每0.1秒重建一次，避免太频繁

        #region 公共方法

        /// <summary>
        /// 构建网格（扫描节点、检测邻居、分配坐标）
        /// </summary>
        [ContextMenu("构建网格")]
        public void BuildGrid()
        {
            BuildGrid(true);
        }

        /// <summary>
        /// 构建网格（可选是否打印日志）
        /// </summary>
        private void BuildGrid(bool printLog)
        {
            if (printLog && showDebugLog)
                Debug.Log("=== 开始构建六边形网格 ===");

            // 1. 收集所有节点
            CollectNodes(printLog);

            // 2. 应用Gizmos设置
            if (autoApplyGizmosSettings)
            {
                ApplyGizmosSettings(printLog);
            }

            // 3. 检测邻居关系
            DetectNeighbors(printLog);

            // 4. 分配坐标
            AssignCoordinates(printLog);

            if (printLog && showDebugLog)
                Debug.Log($"=== 构建完成！共 {allNodes.Count} 个节点 ===");
        }

        /// <summary>
        /// 应用Gizmos设置到所有节点
        /// </summary>
        [ContextMenu("应用Gizmos设置")]
        public void ApplyGizmosSettings()
        {
            ApplyGizmosSettings(true);
        }

        private void ApplyGizmosSettings(bool printLog)
        {
            int count = 0;
            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    node.gizmosSphereRadius = gizmosSphereRadius;
                    node.gizmosHeightOffset = gizmosHeightOffset;
                    node.gizmosLabelOffset = gizmosLabelOffset;
                    count++;
                }
            }
            if (printLog && showDebugLog)
                Debug.Log($"已应用Gizmos设置到 {count} 个节点");
        }

        /// <summary>
        /// 清除网格数据
        /// </summary>
        [ContextMenu("清除网格")]
        public void ClearGrid()
        {
            foreach (var node in allNodes)
            {
                if (node != null)
                {
                    for (int i = 0; i < 6; i++)
                        node.neighbors[i] = null;
                    node.abnormalNeighbors.Clear();
                    node.hasCoordinate = false;
                }
            }
            allNodes.Clear();
            originNode = null;
            Debug.Log("网格已清除");
        }

        #endregion

        #region 构建步骤

        /// <summary>
        /// 步骤1：收集所有HexNode
        /// </summary>
        private void CollectNodes(bool printLog = true)
        {
            allNodes.Clear();
            allNodes.AddRange(FindObjectsOfType<HexNode>());

            if (printLog && showDebugLog)
                Debug.Log($"收集到 {allNodes.Count} 个节点");

            // 查找原点
            originNode = null;
            foreach (var node in allNodes)
            {
                if (node.isOrigin)
                {
                    if (originNode != null)
                        Debug.LogWarning($"发现多个原点！保留第一个: {originNode.name}");
                    else
                        originNode = node;
                }
            }

            // 如果没有原点，自动选第一个
            if (originNode == null && allNodes.Count > 0)
            {
                originNode = allNodes[0];
                originNode.isOrigin = true;
                Debug.LogWarning($"未指定原点，自动选择: {originNode.name}");
            }
        }

        /// <summary>
        /// 步骤2：检测邻居关系
        /// </summary>
        private void DetectNeighbors(bool printLog = true)
        {
            if (printLog && showDebugLog)
                Debug.Log("检测邻居关系...");

            int normalCount = 0;
            int abnormalCount = 0;

            foreach (var node in allNodes)
            {
                // 清空
                for (int i = 0; i < 6; i++)
                    node.neighbors[i] = null;
                node.abnormalNeighbors.Clear();

                // 物理检测周围的Collider
                Collider[] hits = Physics.OverlapSphere(
                    node.WorldPosition,
                    detectionRadius,
                    ~0,
                    QueryTriggerInteraction.Collide
                );

                foreach (var hit in hits)
                {
                    var other = hit.GetComponent<HexNode>();
                    if (other == null || other == node)
                        continue;

                    // 可选：忽略不可通行节点
                    if (ignoreUnwalkableNodes && (!node.isWalkable || !other.isWalkable))
                        continue;

                    // 计算方向（忽略Y轴，只看XZ平面）
                    Vector3 dir = other.WorldPosition - node.WorldPosition;
                    float distance = new Vector2(dir.x, dir.z).magnitude;
                    
                    // 计算角度
                    float angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // 找最接近的标准方向
                    int closestDir = FindClosestDirection(angle, out float angleDiff);

                    if (Mathf.Abs(angleDiff) <= angleTolerance)
                    {
                        // 正常邻居
                        node.neighbors[closestDir] = other;
                        normalCount++;
                    }
                    else if (distance < abnormalThreshold)
                    {
                        // 异常节点（角度不对但很近）
                        if (!node.abnormalNeighbors.Contains(other))
                        {
                            node.abnormalNeighbors.Add(other);
                            abnormalCount++;
                        }
                    }
                }
            }

            if (printLog && showDebugLog)
                Debug.Log($"正常连接: {normalCount / 2} 对，异常节点: {abnormalCount} 个");
        }

        /// <summary>
        /// 步骤3：分配坐标（BFS从原点传播）
        /// </summary>
        private void AssignCoordinates(bool printLog = true)
        {
            if (originNode == null)
            {
                Debug.LogError("未找到原点！");
                return;
            }

            if (printLog && showDebugLog)
                Debug.Log($"从原点 {originNode.name} 开始分配坐标");

            // 重置
            foreach (var node in allNodes)
                node.hasCoordinate = false;

            // 原点坐标(0, 0)
            originNode.axialCoord = Vector2Int.zero;
            originNode.hasCoordinate = true;

            // BFS传播
            Queue<HexNode> queue = new Queue<HexNode>();
            queue.Enqueue(originNode);

            int assignedCount = 1;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // 遍历6个方向
                for (int dir = 0; dir < 6; dir++)
                {
                    var neighbor = current.neighbors[dir];
                    if (neighbor == null || neighbor.hasCoordinate)
                        continue;

                    // 计算邻居坐标
                    neighbor.axialCoord = current.axialCoord + axialOffsets[dir];
                    neighbor.hasCoordinate = true;
                    queue.Enqueue(neighbor);
                    assignedCount++;
                }
            }

            if (printLog && showDebugLog)
            {
                Debug.Log($"坐标分配: {assignedCount} / {allNodes.Count}");
                if (assignedCount < allNodes.Count)
                    Debug.LogWarning($"{allNodes.Count - assignedCount} 个节点未分配（孤立节点）");
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 找到最接近的标准方向
        /// </summary>
        private int FindClosestDirection(float angle, out float angleDiff)
        {
            int closestDir = 0;
            float minDiff = float.MaxValue;

            for (int i = 0; i < 6; i++)
            {
                float diff = Mathf.DeltaAngle(angle, standardAngles[i]);
                float absDiff = Mathf.Abs(diff);

                if (absDiff < minDiff)
                {
                    minDiff = absDiff;
                    closestDir = i;
                }
            }

            angleDiff = Mathf.DeltaAngle(angle, standardAngles[closestDir]);
            return closestDir;
        }

        #endregion

        #region Unity生命周期

        void Update()
        {
            // 编辑器模式下自动重建
            #if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && autoRebuildInEditor)
            {
                // 限制重建频率
                if (Time.realtimeSinceStartup - lastRebuildTime > rebuildInterval)
                {
                    lastRebuildTime = Time.realtimeSinceStartup;
                    BuildGrid(false); // 自动重建时不打印日志
                }
            }
            #endif
        }

        #endregion

        #region Gizmos

        void OnDrawGizmos()
        {
            // 原点标记
            if (originNode != null)
            {
                Vector3 originPos = originNode.WorldPosition + Vector3.up * gizmosHeightOffset;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(originPos, gizmosSphereRadius * 1.5f);
            }

            // 显示所有连接线
            if (alwaysShowConnections)
            {
                foreach (var node in allNodes)
                {
                    if (node == null) continue;

                    // 跳过不可通行节点的连接（可选）
                    if (!showUnwalkableConnections && !node.isWalkable)
                        continue;

                    Vector3 startPos = node.WorldPosition + Vector3.up * gizmosHeightOffset;

                    // 绘制正常邻居连接
                    for (int i = 0; i < 6; i++)
                    {
                        if (node.neighbors[i] != null)
                        {
                            // 跳过连向不可通行节点的线（可选）
                            if (!showUnwalkableConnections && !node.neighbors[i].isWalkable)
                                continue;

                            // 根据通行性选择颜色
                            if (node.isWalkable && node.neighbors[i].isWalkable)
                                Gizmos.color = Color.green;  // 都可通行：绿色
                            else
                                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);  // 有不可通行：灰色半透明

                            Vector3 endPos = node.neighbors[i].WorldPosition + Vector3.up * gizmosHeightOffset;
                            Gizmos.DrawLine(startPos, endPos);
                        }
                    }

                    // 绘制异常连接（红色）
                    if (node.abnormalNeighbors.Count > 0)
                    {
                        Gizmos.color = Color.red;
                        foreach (var abnormal in node.abnormalNeighbors)
                        {
                            if (abnormal != null)
                            {
                                Vector3 endPos = abnormal.WorldPosition + Vector3.up * gizmosHeightOffset;
                                Gizmos.DrawLine(startPos, endPos);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}

