using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// 六边形寻路测试组件
    /// </summary>
    [ExecuteAlways]
    public class HexPathTest : MonoBehaviour
    {
        [Header("寻路测试")]
        [Tooltip("起点节点")]
        public HexNode startNode;
        
        [Tooltip("终点节点")]
        public HexNode goalNode;

        [Header("移动范围测试")]
        [Tooltip("中心节点")]
        public HexNode centerNode;
        
        [Tooltip("移动步数")]
        public int moveRange = 3;

        [Header("可视化")]
        [Tooltip("显示路径")]
        public bool showPath = true;
        
        [Tooltip("显示移动范围")]
        public bool showMoveRange = false;
        
        [Tooltip("路径线宽")]
        public float pathLineWidth = 0.1f;
        
        [Tooltip("路径颜色")]
        public Color pathColor = Color.yellow;
        
        [Tooltip("移动范围颜色")]
        public Color rangeColor = new Color(0, 1, 1, 0.3f); // 半透明青色

        private List<HexNode> currentPath;
        private HashSet<HexNode> currentRange;

        /// <summary>
        /// 测试寻路
        /// </summary>
        [ContextMenu("测试寻路")]
        public void TestPathfinding()
        {
            if (startNode == null || goalNode == null)
            {
                Debug.LogWarning("请设置起点和终点");
                return;
            }

            Debug.Log($"=== 寻路测试：从 {startNode.name} 到 {goalNode.name} ===");

            currentPath = HexPathfinder.FindPath(startNode, goalNode);

            if (currentPath != null)
            {
                Debug.Log($"✓ 找到路径！");
                HexPathfinder.DebugPrintPath(currentPath);
            }
            else
            {
                Debug.LogWarning("✗ 无法找到路径");
            }
        }

        /// <summary>
        /// 测试移动范围
        /// </summary>
        [ContextMenu("测试移动范围")]
        public void TestMoveRange()
        {
            if (centerNode == null)
            {
                Debug.LogWarning("请设置中心节点");
                return;
            }

            Debug.Log($"=== 移动范围测试：从 {centerNode.name} 出发，{moveRange} 步内 ===");

            currentRange = HexPathfinder.GetReachableNodes(centerNode, moveRange);

            Debug.Log($"可到达 {currentRange.Count} 个节点");
        }

        /// <summary>
        /// 清除测试结果
        /// </summary>
        [ContextMenu("清除结果")]
        public void ClearResults()
        {
            currentPath = null;
            currentRange = null;
            Debug.Log("已清除测试结果");
        }

        #region Gizmos可视化

        void OnDrawGizmos()
        {
            // 绘制路径
            if (showPath && currentPath != null && currentPath.Count > 1)
            {
                DrawPath(currentPath);
            }

            // 绘制移动范围
            if (showMoveRange && currentRange != null)
            {
                DrawMoveRange(currentRange);
            }
        }

        /// <summary>
        /// 绘制路径
        /// </summary>
        private void DrawPath(List<HexNode> path)
        {
            Gizmos.color = pathColor;

            // 绘制路径线
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 from = path[i].WorldPosition + Vector3.up * path[i].gizmosHeightOffset;
                Vector3 to = path[i + 1].WorldPosition + Vector3.up * path[i + 1].gizmosHeightOffset;
                
                // 绘制粗线（用多条线模拟）
                DrawThickLine(from, to, pathLineWidth);
            }

            // 绘制路径节点
            foreach (var node in path)
            {
                Vector3 pos = node.WorldPosition + Vector3.up * node.gizmosHeightOffset;
                Gizmos.DrawWireSphere(pos, node.gizmosSphereRadius * 0.8f);
            }

            // 标记起点和终点
            if (path.Count > 0)
            {
                // 起点（绿色大球）
                Gizmos.color = Color.green;
                Vector3 startPos = path[0].WorldPosition + Vector3.up * path[0].gizmosHeightOffset;
                Gizmos.DrawWireSphere(startPos, path[0].gizmosSphereRadius * 1.5f);

                // 终点（红色大球）
                Gizmos.color = Color.red;
                Vector3 endPos = path[path.Count - 1].WorldPosition + Vector3.up * path[path.Count - 1].gizmosHeightOffset;
                Gizmos.DrawWireSphere(endPos, path[path.Count - 1].gizmosSphereRadius * 1.5f);
            }
        }

        /// <summary>
        /// 绘制移动范围
        /// </summary>
        private void DrawMoveRange(HashSet<HexNode> range)
        {
            Gizmos.color = rangeColor;

            foreach (var node in range)
            {
                if (node == null) continue;

                Vector3 pos = node.WorldPosition + Vector3.up * node.gizmosHeightOffset;
                
                // 绘制半透明球体（用WireSphere模拟）
                Gizmos.DrawWireSphere(pos, node.gizmosSphereRadius * 1.2f);
                Gizmos.DrawWireSphere(pos, node.gizmosSphereRadius * 1.1f);
            }

            // 标记中心点
            if (centerNode != null && range.Contains(centerNode))
            {
                Gizmos.color = Color.blue;
                Vector3 centerPos = centerNode.WorldPosition + Vector3.up * centerNode.gizmosHeightOffset;
                Gizmos.DrawWireSphere(centerPos, centerNode.gizmosSphereRadius * 1.5f);
            }
        }

        /// <summary>
        /// 绘制粗线
        /// </summary>
        private void DrawThickLine(Vector3 from, Vector3 to, float thickness)
        {
            Vector3 direction = (to - from).normalized;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up) * thickness / 2;

            // 绘制多条平行线模拟粗线
            Gizmos.DrawLine(from, to);
            Gizmos.DrawLine(from + perpendicular, to + perpendicular);
            Gizmos.DrawLine(from - perpendicular, to - perpendicular);
        }

        #endregion
    }
}

