using UnityEngine;
using UnityEditor;

namespace Player
{
    [CustomEditor(typeof(PlayerController))]
    public class PlayerControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlayerController player = (PlayerController)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("玩家控制工具", EditorStyles.boldLabel);

            // 当前状态显示
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("当前状态", EditorStyles.boldLabel);
                
                var currentNode = player.GetCurrentNode();
                if (currentNode != null)
                {
                    EditorGUILayout.LabelField($"当前位置: {currentNode.name}");
                    EditorGUILayout.LabelField($"坐标: ({currentNode.axialCoord.x}, {currentNode.axialCoord.y})");
                }
                else
                {
                    EditorGUILayout.LabelField("当前位置: 未初始化");
                }
                
                string movingStatus = player.IsMoving() ? "移动中 🏃" : "静止 🧍";
                EditorGUILayout.LabelField($"状态: {movingStatus}");
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
            }

            // 工具按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 重新初始化", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    // 使用反射调用私有方法
                    var method = typeof(PlayerController).GetMethod("InitializePlayer", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(player, null);
                    Debug.Log("已重新初始化Player");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时使用此功能", "确定");
                }
            }

            if (GUILayout.Button("⏹️ 强制停止", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    player.ForceStop();
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请在运行时使用此功能", "确定");
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 传送功能
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("快捷传送", EditorStyles.boldLabel);
                
                if (GUILayout.Button("传送到Origin"))
                {
                    var allNodes = FindObjectsOfType<Map.HexNode>();
                    foreach (var node in allNodes)
                    {
                        if (node.isOrigin)
                        {
                            player.TeleportTo(node);
                            SceneView.RepaintAll();
                            break;
                        }
                    }
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.Space(10);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "📝 使用说明：\n\n" +
                "🖱️ 输入控制：\n" +
                "• 左键点击HexNode = 寻路移动\n" +
                "• 右键点击 = 取消当前移动\n" +
                "• 移动中左键点击新目标 = 完成当前跳跃后转向新目标\n\n" +
                "⚙️ 必要配置：\n" +
                "1️⃣ 确保场景中有一个HexNode的isOrigin=true\n" +
                "2️⃣ 设置Player Transform（不设置则使用当前物体）\n" +
                "3️⃣ 配置HexNode Layer（在LayerMask中选择）\n" +
                "4️⃣ 调整positionOffset设置Player高度\n\n" +
                "🎨 参数说明：\n" +
                "• positionOffset = Player相对节点的高度偏移\n" +
                "• jumpHeight = 跳跃高度\n" +
                "• jumpDuration = 每跳耗时\n\n" +
                "🔌 扩展接口：\n" +
                "可以继承PlayerController并重写以下方法添加特效：\n" +
                "• OnMovementStart() - 移动开始\n" +
                "• OnStepStart() - 单步开始\n" +
                "• OnStepFinished() - 单步完成\n" +
                "• OnMovementEnd() - 移动完成\n" +
                "• OnMovementCancelled() - 移动取消\n" +
                "• OnMovementFailed() - 移动失败",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // 依赖检查
            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField("场景检查", EditorStyles.boldLabel);
                
                if (GUILayout.Button("检查场景配置"))
                {
                    CheckSceneSetup();
                }
            }
        }

        /// <summary>
        /// 检查场景配置
        /// </summary>
        private void CheckSceneSetup()
        {
            var allNodes = FindObjectsOfType<Map.HexNode>();
            
            if (allNodes.Length == 0)
            {
                EditorUtility.DisplayDialog("场景检查", 
                    "❌ 场景中没有HexNode！\n请先创建六边形网格。", 
                    "知道了");
                return;
            }

            bool hasOrigin = false;
            int walkableCount = 0;
            int meshColliderCount = 0;

            foreach (var node in allNodes)
            {
                if (node.isOrigin) hasOrigin = true;
                if (node.isWalkable) walkableCount++;
                
                // 检查是否有MeshCollider
                var meshFilter = node.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null)
                {
                    var meshCollider = meshFilter.GetComponent<MeshCollider>();
                    if (meshCollider != null) meshColliderCount++;
                }
            }

            string message = $"场景检查结果：\n\n";
            message += $"✅ HexNode总数: {allNodes.Length}\n";
            message += hasOrigin ? "✅ 已设置Origin\n" : "❌ 未设置Origin！\n";
            message += $"✅ 可通行节点: {walkableCount}\n";
            message += $"✅ 已配置MeshCollider: {meshColliderCount}/{allNodes.Length}\n\n";

            if (!hasOrigin)
            {
                message += "⚠️ 请选择一个HexNode勾选isOrigin！\n";
            }

            if (meshColliderCount < allNodes.Length)
            {
                message += $"⚠️ 有{allNodes.Length - meshColliderCount}个节点未配置MeshCollider\n";
                message += "建议：在HexNode上勾选autoSetupMeshCollider\n";
            }

            EditorUtility.DisplayDialog("场景检查", message, "知道了");
        }
    }
}



