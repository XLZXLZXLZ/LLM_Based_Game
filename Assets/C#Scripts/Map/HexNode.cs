using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// 六边形节点 - 挂载在每个六边形块上
    /// </summary>
    [ExecuteAlways]  // 在编辑器模式下也运行
    [SelectionBase]  // 点击子物体时自动选中这个父物体
    [RequireComponent(typeof(SphereCollider))]
    public class HexNode : MonoBehaviour
    {
        [Header("节点属性")]
        [Tooltip("是否是原点（坐标0,0）")]
        public bool isOrigin = false;
        
        [Tooltip("是否可通行")]
        public bool isWalkable = true;

        [Header("坐标（运行时分配）")]
        [Tooltip("Axial坐标(q, r)")]
        public Vector2Int axialCoord;
        
        [Tooltip("坐标是否已分配")]
        public bool hasCoordinate = false;

        [Header("邻居关系（运行时分配）")]
        [Tooltip("6个方向的邻居节点")]
        public HexNode[] neighbors = new HexNode[6];

        [Header("调试信息")]
        [Tooltip("异常邻居（距离近但角度不对）")]
        public List<HexNode> abnormalNeighbors = new List<HexNode>();

        [Header("Gizmos显示设置")]
        [Tooltip("球体半径")]
        public float gizmosSphereRadius = 0.3f;
        
        [Tooltip("Gizmos高度偏移（避免被模型遮挡）")]
        public float gizmosHeightOffset = 1.0f;
        
        [Tooltip("坐标文字高度偏移")]
        public float gizmosLabelOffset = 1.5f;

        [Header("碰撞体设置")]
        [Tooltip("自动为子物体添加MeshCollider（用于点击检测）")]
        public bool autoSetupMeshCollider = true;

        /// <summary>
        /// 世界坐标
        /// </summary>
        public Vector3 WorldPosition => transform.position;

        private SphereCollider centerCollider;

        void OnEnable()
        {
            // 自动添加/配置中心球Collider（用于邻居检测）
            centerCollider = GetComponent<SphereCollider>();
            if (centerCollider == null)
            {
                centerCollider = gameObject.AddComponent<SphereCollider>();
            }
            centerCollider.isTrigger = true;
            centerCollider.radius = 0.1f;

            // 自动为子物体配置MeshCollider（用于点击检测）
            if (autoSetupMeshCollider)
            {
                SetupMeshColliderForChild();
            }
        }

        /// <summary>
        /// 为子物体自动配置MeshCollider（用于点击检测）
        /// </summary>
        private void SetupMeshColliderForChild()
        {
            // 查找第一个带MeshFilter的子物体
            MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
            if (meshFilter != null && meshFilter.gameObject != gameObject)
            {
                GameObject meshObject = meshFilter.gameObject;
                
                // 检查是否已有MeshCollider
                MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
                if (meshCollider == null)
                {
                    meshCollider = meshObject.AddComponent<MeshCollider>();
                }
                
                // 配置MeshCollider
                // 注意：凹面网格不能同时设置 convex=false 和 isTrigger=true
                // 射线检测不需要 trigger，所以设为 false
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;   // 精确碰撞（保持六边形形状）
                meshCollider.isTrigger = false; // 不设为trigger（射线检测仍然有效）
            }
        }

        /// <summary>
        /// 获取所有有效邻居
        /// </summary>
        public List<HexNode> GetNeighbors()
        {
            var result = new List<HexNode>();
            for (int i = 0; i < 6; i++)
            {
                if (neighbors[i] != null)
                    result.Add(neighbors[i]);
            }
            return result;
        }

        #region Gizmos可视化

        void OnDrawGizmos()
        {
            // 计算Gizmos位置（添加高度偏移）
            Vector3 gizmosPosition = transform.position + Vector3.up * gizmosHeightOffset;
            
            // 根据状态选择颜色
            if (!isWalkable)
            {
                Gizmos.color = Color.yellow;  // 黄色：不可通行
            }
            else if (abnormalNeighbors.Count > 0)
            {
                Gizmos.color = Color.red;  // 红色：异常节点
            }
            else
            {
                Gizmos.color = Color.green;  // 绿色：正常节点
            }

            // 绘制中心球（WireSphere）
            Gizmos.DrawWireSphere(gizmosPosition, gizmosSphereRadius);

            // 绘制坐标文本
            #if UNITY_EDITOR
            if (hasCoordinate)
            {
                var style = new GUIStyle
                {
                    normal = new GUIStyleState { textColor = new Color(0.6f, 0.4f, 0.2f) },  // 棕色
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                };
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * gizmosLabelOffset,
                    $"({axialCoord.x}, {axialCoord.y})",
                    style
                );
            }
            #endif
        }

        void OnDrawGizmosSelected()
        {
            // 计算连接线的起点和终点（添加高度偏移）
            Vector3 startPos = transform.position + Vector3.up * gizmosHeightOffset;
            
            // 绘制正常邻居连接（绿色）
            Gizmos.color = Color.green;
            for (int i = 0; i < 6; i++)
            {
                if (neighbors[i] != null)
                {
                    Vector3 endPos = neighbors[i].transform.position + Vector3.up * neighbors[i].gizmosHeightOffset;
                    Gizmos.DrawLine(startPos, endPos);
                }
            }

            // 绘制异常连接（红色）
            Gizmos.color = Color.red;
            foreach (var abnormal in abnormalNeighbors)
            {
                if (abnormal != null)
                {
                    Vector3 endPos = abnormal.transform.position + Vector3.up * abnormal.gizmosHeightOffset;
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
        }

        #endregion
    }
}

