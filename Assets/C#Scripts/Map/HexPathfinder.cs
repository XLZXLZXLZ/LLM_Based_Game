using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    /// <summary>
    /// 六边形网格BFS寻路器
    /// </summary>
    public class HexPathfinder
    {
        /// <summary>
        /// 使用BFS查找从起点到终点的最短路径
        /// </summary>
        /// <param name="start">起点节点</param>
        /// <param name="goal">终点节点</param>
        /// <returns>路径节点列表（从起点到终点），如果无法到达则返回null</returns>
        public static List<HexNode> FindPath(HexNode start, HexNode goal)
        {
            // 参数检查
            if (start == null || goal == null)
            {
                Debug.LogWarning("起点或终点为null");
                return null;
            }

            // 起点或终点不可通行
            if (!start.isWalkable || !goal.isWalkable)
            {
                Debug.LogWarning($"起点或终点不可通行: start={start.isWalkable}, goal={goal.isWalkable}");
                return null;
            }

            // 起点和终点相同
            if (start == goal)
            {
                return new List<HexNode> { start };
            }

            // BFS核心数据结构
            Queue<HexNode> queue = new Queue<HexNode>();
            Dictionary<HexNode, HexNode> cameFrom = new Dictionary<HexNode, HexNode>();

            // 初始化
            queue.Enqueue(start);
            cameFrom[start] = start; // 起点的来源是自己

            // BFS主循环
            while (queue.Count > 0)
            {
                HexNode current = queue.Dequeue();

                // 找到目标，提前退出
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, start, goal);
                }

                // 遍历所有邻居
                for (int i = 0; i < 6; i++)
                {
                    HexNode neighbor = current.neighbors[i];

                    // 跳过无效邻居
                    if (neighbor == null)
                        continue;

                    // 跳过不可通行的节点
                    if (!neighbor.isWalkable)
                        continue;

                    // 跳过已访问的节点
                    if (cameFrom.ContainsKey(neighbor))
                        continue;

                    // 将邻居加入队列
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }

            // 无法到达目标
            Debug.LogWarning($"无法从 {start.name} 到达 {goal.name}");
            return null;
        }

        /// <summary>
        /// 从cameFrom字典重建路径
        /// </summary>
        private static List<HexNode> ReconstructPath(Dictionary<HexNode, HexNode> cameFrom, HexNode start, HexNode goal)
        {
            List<HexNode> path = new List<HexNode>();
            HexNode current = goal;

            // 从终点回溯到起点
            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Add(start);
            path.Reverse(); // 反转得到从起点到终点的路径

            return path;
        }

        /// <summary>
        /// 获取从起点出发，在指定步数内能到达的所有节点
        /// （用于显示移动范围）
        /// </summary>
        /// <param name="start">起点节点</param>
        /// <param name="maxSteps">最大步数</param>
        /// <returns>可到达的节点集合</returns>
        public static HashSet<HexNode> GetReachableNodes(HexNode start, int maxSteps)
        {
            HashSet<HexNode> reachable = new HashSet<HexNode>();

            if (start == null || !start.isWalkable)
                return reachable;

            Queue<(HexNode node, int steps)> queue = new Queue<(HexNode, int)>();
            HashSet<HexNode> visited = new HashSet<HexNode>();

            queue.Enqueue((start, 0));
            visited.Add(start);
            reachable.Add(start);

            while (queue.Count > 0)
            {
                var (current, steps) = queue.Dequeue();

                // 超出最大步数，不再扩展
                if (steps >= maxSteps)
                    continue;

                // 遍历邻居
                for (int i = 0; i < 6; i++)
                {
                    HexNode neighbor = current.neighbors[i];

                    if (neighbor == null || !neighbor.isWalkable)
                        continue;

                    if (visited.Contains(neighbor))
                        continue;

                    visited.Add(neighbor);
                    reachable.Add(neighbor);
                    queue.Enqueue((neighbor, steps + 1));
                }
            }

            return reachable;
        }

        /// <summary>
        /// 打印路径信息（用于调试）
        /// </summary>
        public static void DebugPrintPath(List<HexNode> path)
        {
            if (path == null || path.Count == 0)
            {
                Debug.Log("路径为空");
                return;
            }

            Debug.Log($"路径长度: {path.Count} 步");
            for (int i = 0; i < path.Count; i++)
            {
                Debug.Log($"  步骤 {i}: {path[i].name} 坐标({path[i].axialCoord.x}, {path[i].axialCoord.y})");
            }
        }
    }
}



