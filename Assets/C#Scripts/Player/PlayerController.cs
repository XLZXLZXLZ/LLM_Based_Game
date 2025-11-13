using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Map;

namespace Player
{
    /// <summary>
    /// 玩家控制器 - 处理移动、寻路和输入
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("== 引用 ==")]
        [Tooltip("玩家物体的Transform")]
        public Transform playerTransform;

        [Header("== 移动参数 ==")]
        [Tooltip("Player相对于HexNode的高度偏移")]
        public Vector3 positionOffset = new Vector3(0, 0.1f, 0);
        
        [Tooltip("跳跃高度")]
        public float jumpHeight = 0.5f;
        
        [Tooltip("每跳耗时（秒）")]
        public float jumpDuration = 0.3f;

        [Header("== 输入检测 ==")]
        [Tooltip("HexNode所在的Layer")]
        public LayerMask hexNodeLayer = -1;
        
        [Tooltip("射线检测距离")]
        public float raycastDistance = 100f;

        [Header("== 调试 ==")]
        [Tooltip("显示调试日志")]
        public bool showDebugLog = true;

        [Header("== 状态（只读）==")]
        [SerializeField] private bool isMoving = false;
        [SerializeField] private int currentStepIndex = 0;
        [SerializeField] private int totalSteps = 0;

        // 私有状态
        private HexNode currentNode;
        private List<HexNode> currentPath = null;
        private bool hasCancelRequest = false;
        private HexNode pendingTargetNode = null;
        private Tweener currentTween = null;

        #region Unity生命周期

        void Start()
        {
            InitializePlayer();
        }

        void Update()
        {
            HandleMouseInput();
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化玩家位置
        /// </summary>
        private void InitializePlayer()
        {
            // 自动获取playerTransform
            if (playerTransform == null)
            {
                playerTransform = transform;
            }

            // 查找Origin节点
            HexNode[] allNodes = FindObjectsOfType<HexNode>();
            HexNode originNode = null;
            
            foreach (var node in allNodes)
            {
                if (node.isOrigin)
                {
                    originNode = node;
                    break;
                }
            }
            
            if (originNode == null)
            {
                Debug.LogError("未找到Origin节点！请确保场景中有一个HexNode的isOrigin为true");
                return;
            }
            
            // 设置Player位置
            currentNode = originNode;
            playerTransform.position = GetNodePosition(currentNode);
            
            if (showDebugLog)
                Debug.Log($"Player初始化在 {currentNode.name} 坐标({currentNode.axialCoord.x}, {currentNode.axialCoord.y})");
        }

        /// <summary>
        /// 获取节点的实际放置位置（加上偏移）
        /// </summary>
        private Vector3 GetNodePosition(HexNode node)
        {
            return node.WorldPosition + positionOffset;
        }

        #endregion

        #region 输入检测

        /// <summary>
        /// 处理鼠标输入
        /// </summary>
        private void HandleMouseInput()
        {
            // 左键点击 - 新寻路
            if (Input.GetMouseButtonDown(0))
            {
                HexNode clickedNode = GetClickedHexNode();
                if (clickedNode != null)
                {
                    TryStartNewPath(clickedNode);
                }
            }
            
            // 右键点击 - 取消移动
            if (Input.GetMouseButtonDown(1))
            {
                RequestCancel();
            }
        }

        /// <summary>
        /// 射线检测获取点击的HexNode
        /// </summary>
        private HexNode GetClickedHexNode()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, raycastDistance, hexNodeLayer))
            {
                // 先尝试直接获取HexNode
                HexNode node = hit.collider.GetComponent<HexNode>();
                if (node != null) return node;
                
                // 如果hit到子物体的MeshCollider，向上查找父物体的HexNode
                node = hit.collider.GetComponentInParent<HexNode>();
                if (node != null) return node;
            }
            
            return null;
        }

        #endregion

        #region 寻路模块

        /// <summary>
        /// 尝试开始新的寻路
        /// </summary>
        private void TryStartNewPath(HexNode targetNode)
        {
            // 基础检查
            if (targetNode == null) return;
            
            // 点击当前节点，忽略
            if (targetNode == currentNode)
            {
                if (showDebugLog)
                    Debug.Log("已经在目标位置");
                return;
            }
            
            // 如果正在移动，设置待处理目标
            if (isMoving)
            {
                pendingTargetNode = targetNode;
                if (showDebugLog)
                    Debug.Log($"移动中，新目标已记录: {targetNode.name}");
                return;
            }
            
            // 寻路
            List<HexNode> path = HexPathfinder.FindPath(currentNode, targetNode);
            
            if (path == null || path.Count == 0)
            {
                OnPathNotFound(targetNode);
                return;
            }
            
            // 开始移动
            StartMovement(path);
        }

        /// <summary>
        /// 路径未找到回调
        /// </summary>
        private void OnPathNotFound(HexNode targetNode)
        {
            if (showDebugLog)
                Debug.LogWarning($"无法到达 {targetNode.name}");
            
            // 触发移动失败事件（扩展接口）
            OnMovementFailed(targetNode);
        }

        #endregion

        #region 移动执行模块

        /// <summary>
        /// 开始移动
        /// </summary>
        private void StartMovement(List<HexNode> path)
        {
            currentPath = path;
            currentStepIndex = 1; // 从1开始，因为0是起点（当前位置）
            totalSteps = path.Count;
            isMoving = true;
            
            // 清除旧的中断标志
            hasCancelRequest = false;
            
            if (showDebugLog)
                Debug.Log($"开始移动，路径长度: {path.Count}");
            
            // 触发移动开始事件（扩展接口）
            OnMovementStart(path);
            
            MoveToNextStep();
        }

        /// <summary>
        /// 移动到下一步
        /// </summary>
        private void MoveToNextStep()
        {
            // 检查是否完成
            if (currentStepIndex >= currentPath.Count)
            {
                OnMovementComplete();
                return;
            }
            
            // 获取下一个节点
            HexNode nextNode = currentPath[currentStepIndex];
            Vector3 targetPos = GetNodePosition(nextNode);
            
            if (showDebugLog)
                Debug.Log($"跳向 {nextNode.name} (步数 {currentStepIndex}/{totalSteps-1})");
            
            // 触发单步开始事件（扩展接口）
            OnStepStart(nextNode, currentStepIndex);
            
            // DOTween跳跃动画
            currentTween = playerTransform.DOJump(
                targetPos,
                jumpHeight,
                1,  // 跳跃次数
                jumpDuration
            ).SetEase(Ease.Linear)
             .OnComplete(OnStepComplete);
        }

        /// <summary>
        /// 单步完成回调
        /// </summary>
        private void OnStepComplete()
        {
            // 更新当前节点
            currentNode = currentPath[currentStepIndex];
            currentStepIndex++;
            
            // 触发单步完成事件（扩展接口）
            OnStepFinished(currentNode);
            
            // 检查中断请求
            if (hasCancelRequest)
            {
                HandleCancelRequest();
                return;
            }
            
            // 检查新目标
            if (pendingTargetNode != null)
            {
                HandlePendingTarget();
                return;
            }
            
            // 继续下一步
            MoveToNextStep();
        }

        /// <summary>
        /// 移动完成
        /// </summary>
        private void OnMovementComplete()
        {
            isMoving = false;
            currentPath = null;
            
            if (showDebugLog)
                Debug.Log($"移动完成，到达 {currentNode.name}");
            
            // 触发移动完成事件（扩展接口）
            OnMovementEnd(currentNode);
            
            // 检查是否有待处理的新目标
            if (pendingTargetNode != null)
            {
                HandlePendingTarget();
            }
        }

        #endregion

        #region 中断处理模块

        /// <summary>
        /// 请求取消移动（右键）
        /// </summary>
        private void RequestCancel()
        {
            if (!isMoving)
            {
                if (showDebugLog)
                    Debug.Log("当前未在移动");
                return;
            }
            
            hasCancelRequest = true;
            pendingTargetNode = null; // 清除待处理目标
            
            if (showDebugLog)
                Debug.Log("已请求取消移动");
        }

        /// <summary>
        /// 处理取消请求
        /// </summary>
        private void HandleCancelRequest()
        {
            if (showDebugLog)
                Debug.Log($"取消移动，停在 {currentNode.name}");
            
            // 停止移动
            isMoving = false;
            currentPath = null;
            hasCancelRequest = false;
            
            // 触发移动取消事件（扩展接口）
            OnMovementCancelled(currentNode);
        }

        /// <summary>
        /// 处理待定目标
        /// </summary>
        private void HandlePendingTarget()
        {
            HexNode target = pendingTargetNode;
            pendingTargetNode = null;
            
            if (showDebugLog)
                Debug.Log($"处理待定目标: {target.name}");
            
            // 重新寻路
            TryStartNewPath(target);
        }

        #endregion

        #region 扩展接口（可添加视觉/音效特效）

        /// <summary>
        /// 移动开始事件
        /// </summary>
        protected virtual void OnMovementStart(List<HexNode> path)
        {
            // TODO: 显示路径高亮、播放音效等
        }

        /// <summary>
        /// 单步开始事件
        /// </summary>
        protected virtual void OnStepStart(HexNode targetNode, int stepIndex)
        {
            // TODO: 播放跳跃音效等
        }

        /// <summary>
        /// 单步完成事件
        /// </summary>
        protected virtual void OnStepFinished(HexNode arrivedNode)
        {
            // TODO: 播放落地音效、粒子特效等
        }

        /// <summary>
        /// 移动完成事件
        /// </summary>
        protected virtual void OnMovementEnd(HexNode finalNode)
        {
            // TODO: 播放完成音效、特效等
        }

        /// <summary>
        /// 移动取消事件
        /// </summary>
        protected virtual void OnMovementCancelled(HexNode stoppedNode)
        {
            // TODO: 播放取消音效、特效等
        }

        /// <summary>
        /// 移动失败事件（无法到达目标）
        /// </summary>
        protected virtual void OnMovementFailed(HexNode targetNode)
        {
            // 占位函数，子类可以重写添加特效
            Debug.Log($"[接口] 移动失败: {targetNode.name}");
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 获取当前所在节点
        /// </summary>
        public HexNode GetCurrentNode()
        {
            return currentNode;
        }

        /// <summary>
        /// 获取是否正在移动
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }

        /// <summary>
        /// 强制停止移动
        /// </summary>
        public void ForceStop()
        {
            if (currentTween != null)
            {
                currentTween.Kill();
                currentTween = null;
            }
            
            isMoving = false;
            currentPath = null;
            hasCancelRequest = false;
            pendingTargetNode = null;
            
            if (showDebugLog)
                Debug.Log("强制停止移动");
        }

        /// <summary>
        /// 传送到指定节点（不播放动画）
        /// </summary>
        public void TeleportTo(HexNode node)
        {
            if (node == null)
            {
                Debug.LogWarning("传送目标节点为null");
                return;
            }
            
            ForceStop();
            currentNode = node;
            playerTransform.position = GetNodePosition(node);
            
            if (showDebugLog)
                Debug.Log($"传送到 {node.name}");
        }

        #endregion
    }
}



