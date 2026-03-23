using UnityEngine;

/// <summary>
/// 手牌UI测试脚本（不依赖 BattleManager，独立创建 BattleContext 进行测试）
/// </summary>
public class TestHandUI : MonoBehaviour
{
    [Header("引用")]
    public HandUI handUI;

    [Header("测试配置")]
    public int initialCardCount = 5;

    private BattleContext ctx;

    private void Start()
    {
        if (handUI == null)
        {
            Debug.LogError("[TestHandUI] HandUI 未设置！");
            return;
        }

        var playerState = new PlayerState(3, 30);
        var enemyState = new EnemyState(100, 5, 3);
        var balanceState = new BalanceState(5);
        var deckState = new DeckState();

        ctx = new BattleContext(playerState, enemyState, balanceState, deckState);

        handUI.Initialize(ctx);

        for (int i = 0; i < initialCardCount; i++)
        {
            AddTestCard(i);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddTestCard(Random.Range(1, 10));
            Debug.Log("[TestHandUI] 添加了一张卡牌");
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            handUI.RefreshPlayableStates();
            Debug.Log("[TestHandUI] 刷新了可打出状态");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ctx.Deck.Hand.Clear();
            handUI.Clear();
            Debug.Log("[TestHandUI] 清空了手牌");
        }
    }

    private void AddTestCard(int index)
    {
        var data = ScriptableObject.CreateInstance<CardData>();
        data.cardType = CardType.Test;
        data.cardName = $"测试卡牌 {index + 1}";
        data.descriptionTemplate = $"这是第 {index + 1} 张测试卡牌\n打出时会输出Debug信息";
        data.baseCost = (index % 3) + 1;
        data.costType = index % 2 == 0 ? CostType.Anger : CostType.Calm;
        data.quality = (CardQuality)(index % 4);

        var card = new Card_99_Test(data);
        ctx.Deck.AddToHand(card);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label("=== 手牌UI测试 ===");
        GUILayout.Label($"当前手牌数: {ctx?.Deck?.Hand?.Count ?? 0}");
        GUILayout.Label($"天平: 狂热={ctx?.Balance?.AngerPoint ?? 0} / 寂静={ctx?.Balance?.CalmPoint ?? 0}");
        GUILayout.Label($"玩家: HP={ctx?.Player?.Hp ?? 0} / 命={ctx?.Player?.Life ?? 0}");
        GUILayout.Label($"敌人: HP={ctx?.Enemy?.Hp ?? 0}");
        GUILayout.Space(10);
        GUILayout.Label("操作说明:");
        GUILayout.Label("  空格 - 添加卡牌");
        GUILayout.Label("  R - 刷新可打出状态");
        GUILayout.Label("  C - 清空手牌");
        GUILayout.Label("  拖拽卡牌到上方 - 打出");
        GUILayout.EndArea();
    }
}
