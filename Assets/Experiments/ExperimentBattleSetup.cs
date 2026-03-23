using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 实验用战斗入口（类似 <see cref="BattleTestSetup"/>）。
/// 在 Inspector 中选择「第几套卡组」，对应 <c>Assets/LuaCards/&lt;模型目录&gt;/</c> 下全部 <c>*.lua</c>，
/// 每张 Lua 动态生成一张 <see cref="CardData"/>（Lua_Card），组成整副牌组后开始战斗；由你手动打出验证。
/// </summary>
public class ExperimentBattleSetup : MonoBehaviour
{
    /// <summary>三套卡组与下方三个「模型目录名」一一对应。</summary>
    public enum ExperimentDeckPreset
    {
        Model1 = 0,
        Model2 = 1,
        Model3 = 2
    }

    [Header("引用")]
    public BattleManager battleManager;

    [Header("预配置 SO")]
    [SerializeField] private EnemyData enemyData;
    [Tooltip("可选：从该 CardData 复制 cardType/costType/baseCost/quality 等默认值；脚本路径仍由下方模型目录下的 .lua 决定。")]
    [SerializeField] private CardData optionalLuaCardTemplate;

    [Header("实验卡组：选择使用哪一套")]
    [SerializeField] private ExperimentDeckPreset deckPreset = ExperimentDeckPreset.Model1;

    [Header("三套卡组对应的模型目录名（位于 Assets/LuaCards/<目录名>/）")]
    [SerializeField] private string modelFolder1 = "claude-opus-4-6";
    [SerializeField] private string modelFolder2 = "gemini-3-pro-preview";
    [SerializeField] private string modelFolder3 = "gpt-4o-mini";

    private BattleContext Ctx => battleManager != null ? battleManager.Context : null;

    private void Start()
    {
        Card_0_Lua.RegisterExecutor(new MoonSharpLuaCardExecutor());

        if (battleManager == null)
        {
            Debug.LogError("[ExperimentBattleSetup] BattleManager 未设置！");
            return;
        }

        if (enemyData == null)
        {
            Debug.LogError("[ExperimentBattleSetup] EnemyData 未设置！");
            return;
        }

        string folder = GetSelectedModelFolderName();
        List<CardData> deck = BuildDeckFromModelLuaFolder(folder);
        if (deck == null || deck.Count == 0)
        {
            Debug.LogError($"[ExperimentBattleSetup] 未能从目录构建卡组：LuaCards/{folder}（请确认该目录存在且含 .lua 文件）。");
            return;
        }

        Debug.Log($"[ExperimentBattleSetup] 卡组「{deckPreset}」→ 目录 {folder}，共 {deck.Count} 张 Lua 卡。");
        battleManager.StartBattle(enemyData, deck);
    }

    private void Update()
    {
        if (battleManager != null && Input.GetKeyDown(KeyCode.E))
            battleManager.EndPlayerTurn();
    }

    private string GetSelectedModelFolderName()
    {
        return deckPreset switch
        {
            ExperimentDeckPreset.Model1 => modelFolder1,
            ExperimentDeckPreset.Model2 => modelFolder2,
            ExperimentDeckPreset.Model3 => modelFolder3,
            _ => modelFolder1
        };
    }

    /// <summary>
    /// 扫描 <c>Assets/LuaCards/&lt;folderName&gt;/</c> 下所有 .lua，每张生成一个运行时 CardData。
    /// </summary>
    private List<CardData> BuildDeckFromModelLuaFolder(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return null;

        string safeFolder = folderName.Trim().Replace("\\", "/").Trim('/');
        string absDir = Path.Combine(Application.dataPath, "LuaCards", safeFolder);
        if (!Directory.Exists(absDir))
            return null;

        string[] files = Directory.GetFiles(absDir, "*.lua", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
            return null;

        var sorted = files.OrderBy(Path.GetFileName, System.StringComparer.Ordinal).ToArray();
        var list = new List<CardData>(sorted.Length);

        foreach (string absFile in sorted)
        {
            string fileName = Path.GetFileName(absFile);
            string relativeFromAssets = Path.Combine("LuaCards", safeFolder, fileName).Replace("\\", "/");

            var cardData = ScriptableObject.CreateInstance<CardData>();
            if (optionalLuaCardTemplate != null)
            {
                cardData.cardType = optionalLuaCardTemplate.cardType;
                cardData.costType = optionalLuaCardTemplate.costType;
                cardData.baseCost = optionalLuaCardTemplate.baseCost;
                cardData.quality = optionalLuaCardTemplate.quality;
                cardData.artwork = optionalLuaCardTemplate.artwork;
            }
            else
            {
                cardData.cardType = CardType.Lua_Card;
                cardData.costType = CostType.Calm;
                cardData.baseCost = 1;
                cardData.quality = CardQuality.Common;
            }

            cardData.cardName = Path.GetFileNameWithoutExtension(fileName);
            cardData.descriptionTemplate = $"@lua:{relativeFromAssets}";

            list.Add(cardData);
        }

        return list;
    }

    private void OnGUI()
    {
        if (Ctx == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
        var boldStyle = new GUIStyle(style) { fontStyle = FontStyle.Bold, fontSize = 18 };

        GUILayout.BeginArea(new Rect(10, 10, 420, 340));
        GUILayout.Label($"=== 实验卡组：{deckPreset}（{GetSelectedModelFolderName()}）===", boldStyle);
        GUILayout.Space(5);
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

        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 60));
        if (!Ctx.Enemy.IsDead)
        {
            GUILayout.Label($"敌人意图: 攻击 {Ctx.Enemy.Attack} (倒计时 {Ctx.Enemy.Timer})", style);
        }
        GUILayout.EndArea();
    }
}
