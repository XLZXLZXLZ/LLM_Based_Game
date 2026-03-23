using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Lua 脚本侧声明的卡牌展示信息（与 CardData 合并：脚本未填的字段回退到 SO）。
/// </summary>
public readonly struct LuaCardMetadata
{
    public string Name { get; }
    public CostType CostType { get; }
    public int CostValue { get; }

    public LuaCardMetadata(string name, CostType costType, int costValue)
    {
        Name = name ?? "";
        CostType = costType;
        CostValue = costValue;
    }

    public static LuaCardMetadata FromData(CardData data)
    {
        if (data == null) return new LuaCardMetadata("", CostType.Calm, 0);
        return new LuaCardMetadata(data.cardName ?? "", data.costType, data.baseCost);
    }
}

/// <summary>
/// Lua 特殊卡（索引 0）。
/// 通过可注入执行器将 Card 的行为转发给 Lua；执行器未注册时走安全降级逻辑。
/// </summary>
public class Card_0_Lua : Card
{
    /// <summary>
    /// Lua 卡执行器接口：由具体 Lua 运行时桥接层实现并注册。
    /// </summary>
    public interface ILuaCardExecutor
    {
        void OnInitialize(string scriptPath, CardData data, BattleContext ctx, Card owner);
        bool CanPlay(string scriptPath, CardData data, BattleContext ctx);
        void OnPlay(string scriptPath, CardData data, BattleContext ctx);
        string GetDescription(string scriptPath, CardData data, BattleContext ctx);
        CostType GetCostType(string scriptPath, CardData data, BattleContext ctx);
        int GetCostValue(string scriptPath, CardData data, BattleContext ctx);
        /// <summary>
        /// 从 Lua 脚本的 lua_card 表解析并与 CardData 合并后的展示用元数据。
        /// </summary>
        LuaCardMetadata GetLuaCardMetadata(string scriptPath, CardData data);
    }

    private static ILuaCardExecutor executor;
    private readonly string scriptPath;

    /// <summary>
    /// 注册全局 Lua 执行器（例如 MoonSharp/xLua 适配器）。
    /// </summary>
    public static void RegisterExecutor(ILuaCardExecutor newExecutor)
    {
        executor = newExecutor;
    }

    public Card_0_Lua(CardData data) : base(data)
    {
        scriptPath = ResolveScriptPath(data);
    }

    public override string Name => ResolveLuaMetadata().Name;

    public override CostType CostType => ResolveLuaMetadata().CostType;

    public override int CostValue => ResolveLuaMetadata().CostValue;

    public override CostType GetCostType(BattleContext ctx)
    {
        if (executor == null || ctx == null)
            return CostType;

        try
        {
            return executor.GetCostType(scriptPath, Data, ctx);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Card_0_Lua] GetCostType 执行异常，回退静态费用侧。path={scriptPath}, err={ex.Message}");
            return CostType;
        }
    }

    public override int GetCostValue(BattleContext ctx)
    {
        if (executor == null || ctx == null)
            return CostValue;

        try
        {
            return executor.GetCostValue(scriptPath, Data, ctx);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Card_0_Lua] GetCostValue 执行异常，回退静态费用值。path={scriptPath}, err={ex.Message}");
            return CostValue;
        }
    }

    private LuaCardMetadata ResolveLuaMetadata()
    {
        if (executor == null)
            return LuaCardMetadata.FromData(Data);

        try
        {
            return executor.GetLuaCardMetadata(scriptPath, Data);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Card_0_Lua] GetLuaCardMetadata 异常，回退 CardData。path={scriptPath}, err={ex.Message}");
            return LuaCardMetadata.FromData(Data);
        }
    }

    public override bool CanPlay(BattleContext ctx)
    {
        if (!base.CanPlay(ctx)) return false;

        if (executor == null) return true;

        try
        {
            return executor.CanPlay(scriptPath, Data, ctx);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Card_0_Lua] CanPlay 执行异常，回退为可打出。card={Data?.cardName}, path={scriptPath}, err={ex.Message}");
            return true;
        }
    }

    public override void OnInitialize(BattleContext ctx)
    {
        base.OnInitialize(ctx);
        if (executor == null || ctx == null) return;

        try
        {
            executor.OnInitialize(scriptPath, Data, ctx, this);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Card_0_Lua] OnInitialize 执行异常，跳过初始化。card={Data?.cardName}, path={scriptPath}, err={ex.Message}");
        }
    }

    public override void OnPlay(BattleContext ctx)
    {
        if (executor != null)
        {
            try
            {
                executor.OnPlay(scriptPath, Data, ctx);
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Card_0_Lua] OnPlay Lua 执行失败，触发降级效果。card={Data?.cardName}, path={scriptPath}, err={ex}");
            }
        }

        // 安全降级：避免因为 Lua 环节异常中断整局战斗。
        int fallbackDamage = Mathf.Max(1, CostValue);
        ctx.DealDamageToEnemy(fallbackDamage, this);
    }

    public override string GetDescription(BattleContext ctx)
    {
        if (executor != null)
        {
            try
            {
                string desc = executor.GetDescription(scriptPath, Data, ctx);
                if (!string.IsNullOrEmpty(desc)) return desc;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Card_0_Lua] GetDescription 执行异常，回退模板描述。card={Data?.cardName}, path={scriptPath}, err={ex.Message}");
            }
        }

        return base.GetDescription(ctx);
    }

    //TODO: 这个函数需要优化，现在只是简单的将descriptionTemplate的第一行作为脚本路径，应该更智能的解析。
    private static string ResolveScriptPath(CardData data)
    {
        // 约定：descriptionTemplate 第一行可写 @lua:relative/path.lua
        // 示例：@lua:GeneratedCards/card_0.lua
        string template = data != null ? data.descriptionTemplate : null;
        if (!string.IsNullOrEmpty(template))
        {
            string[] lines = template.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            if (lines.Length > 0)
            {
                const string Prefix = "@lua:";
                string firstLine = lines[0].Trim();
                if (firstLine.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string relativePath = firstLine.Substring(Prefix.Length).Trim();
                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        return Path.Combine(Application.dataPath, relativePath);
                    }
                }
            }
        }

        // 默认路径：Assets/LuaCards/{cardName}.lua
        string safeName = (data != null && !string.IsNullOrWhiteSpace(data.cardName))
            ? data.cardName.Trim()
            : "lua_card_0";
        return Path.Combine(Application.dataPath, "LuaCards", $"{safeName}.lua");
    }
}
