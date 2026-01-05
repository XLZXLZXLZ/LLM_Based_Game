using UnityEngine;

/// <summary>
/// 手牌UI测试脚本
/// </summary>
public class TestHandUI : MonoBehaviour
{
    [Header("引用")]
    public HandUI handUI;

    [Header("测试配置")]
    public int initialCardCount = 5;

    private void Start()
    {
        if (handUI == null)
        {
            Debug.LogError("[TestHandUI] HandUI 未设置！");
            return;
        }

        // 监听卡牌打出请求
        handUI.OnCardPlayRequested += OnCardPlayRequested;

        // 添加初始手牌
        for (int i = 0; i < initialCardCount; i++)
        {
            AddTestCard(i);
        }
    }

    private void OnDestroy()
    {
        if (handUI != null)
        {
            handUI.OnCardPlayRequested -= OnCardPlayRequested;
        }
    }

    private void Update()
    {
        // 按空格添加卡牌
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddTestCard(Random.Range(1, 10));
            Debug.Log("[TestHandUI] 添加了一张卡牌");
        }

        // 按R刷新可打出状态
        if (Input.GetKeyDown(KeyCode.R))
        {
            handUI.RefreshPlayableStates();
            Debug.Log("[TestHandUI] 刷新了可打出状态");
        }

        // 按C清空手牌
        if (Input.GetKeyDown(KeyCode.C))
        {
            handUI.Clear();
            Debug.Log("[TestHandUI] 清空了手牌");
        }
    }

    private void AddTestCard(int index)
    {
        // 创建测试卡牌
        var cardGO = new GameObject($"TestCard_{index}");
        var card = cardGO.AddComponent<TestCard>();

        // 设置卡牌信息
        card.info = new CardInfo
        {
            cardName = $"测试卡牌 {index + 1}",
            cardDescription = $"这是第 {index + 1} 张测试卡牌\n打出时会输出Debug信息",
            baseCost = (index % 3) + 1,
            costType = index % 2 == 0 ? CostType.Anger : CostType.Calm,
            cardQuality = (CardQuality)(index % 4)
        };

        // 设置为直接使用（不需要选择目标）
        card.targetType = CardTargetType.DirectUse;

        // 添加到手牌
        handUI.AddCard(card);
    }

    private void OnCardPlayRequested(CardUI cardUI, object target)
    {
        if (cardUI.CardData == null) return;

        Debug.Log($"[TestHandUI] 请求打出卡牌: {cardUI.CardData.info.cardName}");

        // 执行卡牌效果
        cardUI.CardData.CardEffect(CardTarget.None);

        // 从手牌移除
        handUI.RemoveCard(cardUI);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== 手牌UI测试 ===");
        GUILayout.Label($"当前手牌数: {handUI?.Cards?.Count ?? 0}");
        GUILayout.Space(10);
        GUILayout.Label("操作说明:");
        GUILayout.Label("  空格 - 添加卡牌");
        GUILayout.Label("  R - 刷新可打出状态");
        GUILayout.Label("  C - 清空手牌");
        GUILayout.Label("  拖拽卡牌到上方 - 打出");
        GUILayout.EndArea();
    }
}

