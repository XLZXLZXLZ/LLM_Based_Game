using Map.Section;
using Player;
using UnityEngine;

namespace Map.NodeEvent
{
    /// <summary>
    /// 节点事件：加载下一个预设 Section。
    /// 将此组件挂载到需要触发"进入新区段"效果的 HexNode 上，
    /// 并在 Inspector 中指定要加载的 Section Prefab。
    /// </summary>
    public class HexNodeEvent_NextSection : HexNodeEvent
    {
        [Header("== 配置 ==")]
        [Tooltip("要加载的下一个 Section 的 Prefab")]
        public GameObject nextSectionPrefab;

        public override void Trigger(PlayerController player)
        {
            if (nextSectionPrefab == null)
            {
                Debug.LogError($"[HexNodeEvent_NextSection] {gameObject.name} 未设置 nextSectionPrefab！");
                return;
            }

            HexNode currentNode = player.GetCurrentNode();
            if (currentNode == null)
            {
                Debug.LogError("[HexNodeEvent_NextSection] 无法获取玩家当前节点");
                return;
            }

            HexSectionManager.Instance.LoadSection(nextSectionPrefab, currentNode);
        }
    }
}
