using UnityEngine;
using UnityEditor;

namespace Map
{
    [CustomEditor(typeof(HexGridBuilder))]
    public class HexGridBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HexGridBuilder builder = (HexGridBuilder)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("网格构建工具", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            // 构建按钮
            if (GUILayout.Button("🔨 构建网格", GUILayout.Height(35)))
            {
                builder.BuildGrid();
                
                // 标记场景为已修改，可以保存
                EditorUtility.SetDirty(target);
                
                // 刷新Scene视图
                SceneView.RepaintAll();
            }

            // 清除按钮
            if (GUILayout.Button("🗑️ 清除网格", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清除网格数据吗？", "确定", "取消"))
                {
                    builder.ClearGrid();
                    EditorUtility.SetDirty(target);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Gizmos设置按钮
            if (GUILayout.Button("🎨 应用Gizmos设置到所有节点", GUILayout.Height(30)))
            {
                var tempNodes = FindObjectsOfType<HexNode>();
                if (tempNodes.Length == 0)
                {
                    EditorUtility.DisplayDialog("提示", "场景中没有HexNode", "确定");
                }
                else
                {
                    // 批量设置
                    foreach (var node in tempNodes)
                    {
                        node.gizmosSphereRadius = builder.gizmosSphereRadius;
                        node.gizmosHeightOffset = builder.gizmosHeightOffset;
                        node.gizmosLabelOffset = builder.gizmosLabelOffset;
                        EditorUtility.SetDirty(node);
                    }
                    
                    EditorUtility.DisplayDialog("完成", 
                        $"已应用Gizmos设置到 {tempNodes.Length} 个节点", 
                        "确定");
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.Space(10);

            // 实时预览状态显示
            if (builder.autoRebuildInEditor)
            {
                EditorGUILayout.HelpBox(
                    "⚡ 实时预览已开启\n" +
                    "移动、添加、删除六边形时会自动重建网格\n" +
                    "如果场景复杂可以关闭此选项以提高性能",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(5);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "📝 使用说明：\n\n" +
                "1️⃣ 确保所有六边形块都挂载了 HexNode 组件\n" +
                "2️⃣ 选择一个六边形块勾选 isOrigin = true\n" +
                "3️⃣ 调整 hexApothem 为实际的内接圆半径\n" +
                "4️⃣ 点击【构建网格】按钮\n" +
                "5️⃣ 在 Scene 视图查看网格可视化\n\n" +
                "🎨 颜色说明：\n" +
                "🟢 绿色 = 正常节点\n" +
                "🔴 红色 = 异常节点（位置不对）\n" +
                "🟡 黄色 = 不可通行节点\n" +
                "🟤 棕色文字 = Axial 坐标\n\n" +
                "💡 实用技巧：\n" +
                "• alwaysShowConnections = 始终显示连接线\n" +
                "• showUnwalkableConnections = 显示不可通行节点连线\n" +
                "• ignoreUnwalkableNodes = 构建时忽略不可通行节点\n" +
                "• autoRebuildInEditor = 实时预览（自动重建）\n" +
                "• gizmosHeightOffset = 调整显示高度（避免遮挡）",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // 快捷操作
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);
            
            if (GUILayout.Button("选中所有 HexNode"))
            {
                var allNodes = FindObjectsOfType<HexNode>();
                Selection.objects = System.Array.ConvertAll(allNodes, node => node.gameObject);
            }

            if (GUILayout.Button("查找未设置原点的场景"))
            {
                var allNodes = FindObjectsOfType<HexNode>();
                bool hasOrigin = false;
                foreach (var node in allNodes)
                {
                    if (node.isOrigin)
                    {
                        hasOrigin = true;
                        break;
                    }
                }

                if (!hasOrigin && allNodes.Length > 0)
                {
                    EditorUtility.DisplayDialog("提示", 
                        $"场景中有 {allNodes.Length} 个 HexNode，但没有设置原点！\n请选择一个节点勾选 isOrigin。", 
                        "知道了");
                }
                else if (hasOrigin)
                {
                    EditorUtility.DisplayDialog("提示", "已设置原点 ✓", "知道了");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "场景中没有 HexNode", "知道了");
                }
            }
        }
    }
}

