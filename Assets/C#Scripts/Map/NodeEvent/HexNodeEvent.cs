using UnityEngine;
using Player;

namespace Map.NodeEvent
{
    /// <summary>
    /// 六边形节点事件抽象基类。
    /// 挂载到含有 HexNode 的 GameObject 上，当玩家踩上该节点时触发。
    /// 具体事件逻辑由子类覆写 Trigger() 实现。
    /// </summary>
    public abstract class HexNodeEvent : MonoBehaviour
    {
        /// <summary>
        /// 玩家踩上节点时被调用
        /// </summary>
        /// <param name="player">触发事件的玩家控制器</param>
        public abstract void Trigger(PlayerController player);
    }
}
