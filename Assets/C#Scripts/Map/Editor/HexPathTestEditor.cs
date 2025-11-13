using UnityEngine;
using UnityEditor;

namespace Map
{
    [CustomEditor(typeof(HexPathTest))]
    public class HexPathTestEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            HexPathTest tester = (HexPathTest)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("寻路测试工具", EditorStyles.boldLabel);

            // 寻路测试按钮
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 测试寻路", GUILayout.Height(35)))
            {
                tester.TestPathfinding();
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("📍 测试移动范围", GUILayout.Height(35)))
            {
                tester.TestMoveRange();
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("🗑️ 清除结果", GUILayout.Height(30)))
            {
                tester.ClearResults();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(10);

            // 帮助信息
            EditorGUILayout.HelpBox(
                "📝 使用说明：\n\n" +
                "寻路测试：\n" +
                "1️⃣ 拖拽两个 HexNode 到 startNode 和 goalNode\n" +
                "2️⃣ 点击【测试寻路】按钮\n" +
                "3️⃣ Scene 视图会显示黄色路径\n" +
                "   • 绿色大球 = 起点\n" +
                "   • 红色大球 = 终点\n" +
                "   • 黄色线 = 路径\n\n" +
                "移动范围测试：\n" +
                "1️⃣ 拖拽一个 HexNode 到 centerNode\n" +
                "2️⃣ 设置 moveRange（步数）\n" +
                "3️⃣ 点击【测试移动范围】按钮\n" +
                "4️⃣ Scene 视图会显示青色范围\n" +
                "   • 蓝色大球 = 中心点\n" +
                "   • 青色圈 = 可到达范围",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // 快捷选择
            EditorGUILayout.LabelField("快捷操作", EditorStyles.boldLabel);

            if (GUILayout.Button("随机选择起点和终点"))
            {
                var allNodes = FindObjectsOfType<HexNode>();
                if (allNodes.Length >= 2)
                {
                    tester.startNode = allNodes[Random.Range(0, allNodes.Length)];
                    tester.goalNode = allNodes[Random.Range(0, allNodes.Length)];
                    EditorUtility.SetDirty(tester);
                    Debug.Log($"起点: {tester.startNode.name}, 终点: {tester.goalNode.name}");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "场景中至少需要2个HexNode", "确定");
                }
            }

            EditorGUILayout.Space(5);

            // 状态显示
            if (tester.startNode != null || tester.goalNode != null)
            {
                EditorGUILayout.LabelField("当前设置", EditorStyles.boldLabel);
                
                if (tester.startNode != null)
                    EditorGUILayout.LabelField($"起点: {tester.startNode.name}");
                
                if (tester.goalNode != null)
                    EditorGUILayout.LabelField($"终点: {tester.goalNode.name}");
            }
        }
    }
}



