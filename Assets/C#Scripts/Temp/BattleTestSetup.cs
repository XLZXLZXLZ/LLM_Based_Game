using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最小化战斗循环测试。
/// 挂到场景后通过 Inspector 直接配置 EnemyData 与起始牌组 CardData 列表，运行即可开始战斗。
/// 按 E 结束回合，OnGUI 显示完整战斗状态。
/// </summary>
public class BattleTestSetup : MonoBehaviour
{
    [Header("引用")]
    public BattleManager battleManager;

    [Header("预配置SO")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private List<CardData> startDeck = new();

    private BattleContext Ctx => battleManager != null ? battleManager.Context : null;

    private void Start()
    {
        Card_0_Lua.RegisterExecutor(new MoonSharpLuaCardExecutor());

        if (battleManager == null)
        {
            Debug.LogError("[BattleTestSetup] BattleManager 未设置！");
            return;
        }

        if (enemyData == null)
        {
            Debug.LogError("[BattleTestSetup] EnemyData 未设置！");
            return;
        }

        if (startDeck == null || startDeck.Count == 0)
        {
            Debug.LogError("[BattleTestSetup] 起始牌组为空，请在 Inspector 中配置至少一张 CardData。");
            return;
        }

        // 过滤空引用，避免传入 BattleManager 后在工厂处抛出难定位的空对象异常。
        var validDeck = new List<CardData>(startDeck.Count);
        for (int i = 0; i < startDeck.Count; i++)
        {
            if (startDeck[i] == null)
            {
                Debug.LogWarning($"[BattleTestSetup] startDeck[{i}] 为空，已跳过。");
                continue;
            }
            validDeck.Add(startDeck[i]);
        }

        if (validDeck.Count == 0)
        {
            Debug.LogError("[BattleTestSetup] 起始牌组过滤后为空，请检查 CardData 配置。");
            return;
        }

        battleManager.StartBattle(enemyData, validDeck);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            battleManager.EndPlayerTurn();
        }
    }

    // ==================== 战斗状态显示 ====================

    private void OnGUI()
    {
        if (Ctx == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        var boldStyle = new GUIStyle(style) { fontStyle = FontStyle.Bold, fontSize = 18 };

        // 左上 - 玩家信息
        GUILayout.BeginArea(new Rect(10, 10, 350, 300));
        GUILayout.Label($"=== 回合 {Ctx.CurrentTurn} / {battleManager.CurrentState} ===", boldStyle);
        GUILayout.Space(5);
        GUILayout.Label($"玩家:  HP {Ctx.Player.Hp}/{Ctx.Player.MaxHp}  |  命 {Ctx.Player.Life}/{Ctx.Player.MaxLife}  |  护盾 {Ctx.Player.Shield}", style);
        GUILayout.Label($"敌人:  HP {Ctx.Enemy.Hp}/{Ctx.Enemy.MaxHp}  |  攻击 {Ctx.Enemy.Attack}  |  计时器 {Ctx.Enemy.Timer}/{Ctx.Enemy.MaxTimer}", style);
        GUILayout.Label($"天平:  狂热 {Ctx.Balance.AngerPoint}  /  寂静 {Ctx.Balance.CalmPoint}  (差值 {Ctx.Balance.Difference}/{Ctx.Balance.MaxDifference})", style);
        GUILayout.Label($"牌组:  抽牌堆 {Ctx.Deck.DrawPile.Count}  |  手牌 {Ctx.Deck.Hand.Count}  |  弃牌堆 {Ctx.Deck.DiscardPile.Count}", style);
        GUILayout.Space(10);

        if (battleManager.CurrentState == BattleState.PlayerTurn)
        {
            GUILayout.Label("拖拽卡牌到上方 → 打出", style);
            GUILayout.Label("按 E → 结束回合", style);
        }
        else if (battleManager.CurrentState == BattleState.EnemyTurn)
        {
            GUILayout.Label("敌人回合中...", style);
        }
        else if (battleManager.CurrentState == BattleState.Victory)
        {
            GUILayout.Label(">>> 战斗胜利！ <<<", boldStyle);
        }
        else if (battleManager.CurrentState == BattleState.Defeat)
        {
            GUILayout.Label(">>> 战斗失败... <<<", boldStyle);
        }

        GUILayout.EndArea();

        // 右上 - 敌人意图
        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 60));
        if (!Ctx.Enemy.IsDead)
        {
            GUILayout.Label($"敌人意图: 攻击 {Ctx.Enemy.Attack} (倒计时 {Ctx.Enemy.Timer})", style);
        }
        GUILayout.EndArea();
    }
}
