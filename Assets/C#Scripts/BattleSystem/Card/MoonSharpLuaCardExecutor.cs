using System;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;
using UnityEngine;

/// <summary>
/// 使用 MoonSharp 运行 Lua 卡脚本的执行器。
/// </summary>
public class MoonSharpLuaCardExecutor : Card_0_Lua.ILuaCardExecutor
{
    private readonly Dictionary<string, Script> scriptCache = new Dictionary<string, Script>();
    private readonly Dictionary<string, LuaCardOverrides> luaCardOverridesCache = new Dictionary<string, LuaCardOverrides>();

    private struct LuaCardOverrides
    {
        public bool HasLuaTable;
        public bool HasName;
        public string Name;
        public bool HasCostType;
        public CostType CostType;
        public bool HasCostValue;
        public int CostValue;
    }

    public bool CanPlay(string scriptPath, CardData data, BattleContext ctx)
    {
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("can_play");
        if (fn.Type != DataType.Function)
            return true;

        var api = new LuaBattleContextApi(ctx);
        DynValue ret = script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
        if (ret.Type == DataType.Boolean)
            return ret.Boolean;
        return true;
    }

    public void OnInitialize(string scriptPath, CardData data, BattleContext ctx, Card owner)
    {
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("on_initialize");
        if (fn.Type != DataType.Function)
            return;

        var api = new LuaBattleContextApi(ctx, owner);
        script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
    }

    public void OnPlay(string scriptPath, CardData data, BattleContext ctx)
    {
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("on_play");
        if (fn.Type != DataType.Function)
            throw new InvalidOperationException($"Lua 脚本未定义 on_play: {scriptPath}");

        var api = new LuaBattleContextApi(ctx);
        script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
    }

    public string GetDescription(string scriptPath, CardData data, BattleContext ctx)
    {
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("get_description");
        if (fn.Type != DataType.Function)
            return GetFallbackDescription(data);

        var api = new LuaBattleContextApi(ctx);
        DynValue ret = script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
        return ret.CastToString();
    }

    public CostType GetCostType(string scriptPath, CardData data, BattleContext ctx)
    {
        var fallback = GetLuaCardMetadata(scriptPath, data).CostType;
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("get_cost_type");
        if (fn.Type != DataType.Function || ctx == null)
            return fallback;

        var api = new LuaBattleContextApi(ctx);
        DynValue ret = script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
        return TryParseCostType(ret, out CostType parsed) ? parsed : fallback;
    }

    public int GetCostValue(string scriptPath, CardData data, BattleContext ctx)
    {
        var fallback = GetLuaCardMetadata(scriptPath, data).CostValue;
        Script script = GetScript(scriptPath);
        DynValue fn = script.Globals.Get("get_cost_value");
        if (fn.Type != DataType.Function || ctx == null)
            return fallback;

        var api = new LuaBattleContextApi(ctx);
        DynValue ret = script.Call(fn, UserData.Create(api), UserData.Create(new LuaCardDataApi(data)));
        if (ret.Type != DataType.Number)
            return fallback;

        return Mathf.Max(0, (int)ret.Number);
    }

    public LuaCardMetadata GetLuaCardMetadata(string scriptPath, CardData data)
    {
        var fallback = LuaCardMetadata.FromData(data);
        var ov = GetLuaCardOverrides(scriptPath);

        if (!ov.HasLuaTable)
            return fallback;

        return new LuaCardMetadata(
            ov.HasName ? ov.Name : fallback.Name,
            ov.HasCostType ? ov.CostType : fallback.CostType,
            ov.HasCostValue ? ov.CostValue : fallback.CostValue);
    }

    private LuaCardOverrides GetLuaCardOverrides(string scriptPath)
    {
        GetScript(scriptPath);
        return luaCardOverridesCache.TryGetValue(scriptPath, out var ov) ? ov : default;
    }

    private static bool typesRegistered;

    private static void EnsureTypesRegistered()
    {
        if (typesRegistered) return;
        typesRegistered = true;

        UserData.RegisterType<LuaBattleContextApi>();
        UserData.RegisterType<LuaCardDataApi>();

        UserData.RegisterType<BuffInstance>();
        UserData.RegisterType<BuffDefinition>();

        UserData.RegisterType<BattleEventArgs>();
        UserData.RegisterType<BattleStartedEventArgs>();
        UserData.RegisterType<DamageEventArgs>();
        UserData.RegisterType<TurnEventArgs>();
        UserData.RegisterType<CardPlayedEventArgs>();
        UserData.RegisterType<CardDrawnEventArgs>();
        UserData.RegisterType<BalanceChangedEventArgs>();
        UserData.RegisterType<BalanceThresholdEventArgs>();
        UserData.RegisterType<HealEventArgs>();
        UserData.RegisterType<LifeLostEventArgs>();
        UserData.RegisterType<EnemyActionEventArgs>();
        UserData.RegisterType<EnemyDiedEventArgs>();

        UserData.RegisterType<DamageTarget>();
        UserData.RegisterType<CostType>();
    }

    private Script GetScript(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new ArgumentException("Lua 脚本路径为空。", nameof(scriptPath));

        if (scriptCache.TryGetValue(scriptPath, out var cached))
            return cached;

        if (!File.Exists(scriptPath))
            throw new FileNotFoundException($"Lua 脚本不存在: {scriptPath}");

        EnsureTypesRegistered();

        var script = new Script(CoreModules.Preset_Complete);
        script.Globals["DamageTarget"] = UserData.CreateStatic<DamageTarget>();
        script.Globals["CostType"] = UserData.CreateStatic<CostType>();
        script.DoString(File.ReadAllText(scriptPath));
        luaCardOverridesCache[scriptPath] = ParseLuaCardTable(script, scriptPath);
        scriptCache[scriptPath] = script;
        return script;
    }

    private static LuaCardOverrides ParseLuaCardTable(Script script, string scriptPath)
    {
        var ov = new LuaCardOverrides();
        DynValue lv = script.Globals.Get("lua_card");
        if (lv.Type != DataType.Table)
        {
            Debug.LogWarning(
                $"[MoonSharpLuaCardExecutor] Lua 卡牌必须在脚本顶层声明全局表 lua_card（含 name、cost_type、cost_value）。path={scriptPath}");
            return ov;
        }

        ov.HasLuaTable = true;
        Table t = lv.Table;

        DynValue nameVal = t.Get("name");
        if (nameVal.Type != DataType.Nil && nameVal.Type != DataType.Void)
        {
            string n = nameVal.CastToString();
            if (!string.IsNullOrEmpty(n))
            {
                ov.HasName = true;
                ov.Name = n;
            }
        }

        DynValue costTypeVal = t.Get("cost_type");
        if (TryParseCostType(costTypeVal, out CostType ct))
        {
            ov.HasCostType = true;
            ov.CostType = ct;
        }

        DynValue costVal = t.Get("cost_value");
        if (costVal.Type == DataType.Number)
        {
            ov.HasCostValue = true;
            ov.CostValue = (int)costVal.Number;
        }

        return ov;
    }

    private static bool TryParseCostType(DynValue v, out CostType costType)
    {
        costType = default;
        if (v.Type == DataType.Nil || v.Type == DataType.Void)
            return false;

        if (v.Type == DataType.UserData)
        {
            try
            {
                var o = v.ToObject();
                if (o is CostType ct)
                {
                    costType = ct;
                    return true;
                }
            }
            catch
            {
                // ignore
            }
        }

        if (v.Type == DataType.String && Enum.TryParse(v.String, true, out CostType parsed))
        {
            costType = parsed;
            return true;
        }

        return false;
    }

    private static string GetFallbackDescription(CardData data)
    {
        if (data == null || string.IsNullOrEmpty(data.descriptionTemplate))
            return "Lua 卡牌";

        string[] lines = data.descriptionTemplate.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length > 0 && lines[0].Trim().StartsWith("@lua:", StringComparison.OrdinalIgnoreCase))
        {
            return string.Join("\n", lines, 1, Math.Max(0, lines.Length - 1)).Trim();
        }
        return data.descriptionTemplate;
    }
}

/// <summary>
/// 暴露给 Lua 的战斗上下文白名单接口。
/// </summary>
[MoonSharpUserData]
public class LuaBattleContextApi
{
    private readonly BattleContext ctx;
    private readonly object cardEventOwner;

    public LuaBattleContextApi(BattleContext ctx)
        : this(ctx, null)
    {
    }

    public LuaBattleContextApi(BattleContext ctx, object cardEventOwner)
    {
        this.ctx = ctx;
        this.cardEventOwner = cardEventOwner;
    }

    public void DealDamageToEnemy(int amount) => ctx.DealDamageToEnemy(amount, "lua");
    public void DealDamageToPlayer(int amount) => ctx.DealDamageToPlayer(amount, "lua");
    public void DrawCards(int count) => ctx.DrawCards(count);
    public void GainShield(int amount) => ctx.GainShield(amount);
    public void HealPlayer(int amount) => ctx.HealPlayer(amount, "lua");

    public int PlayerHp => ctx.Player.Hp;
    public int PlayerLife => ctx.Player.Life;
    public int PlayerShield => ctx.Player.Shield;
    public int EnemyHp => ctx.Enemy.Hp;
    public int HandCount => ctx.Deck.Hand.Count;

    // ==================== 天平 ====================

    public int AngerPoint => ctx.Balance.AngerPoint;
    public int CalmPoint => ctx.Balance.CalmPoint;

    public void AdjustBalance(string side, int value)
    {
        var costType = (side ?? "").ToLower() == "anger" ? CostType.Anger : CostType.Calm;
        ctx.AdjustBalance(costType, value);
    }

    // ==================== Buff ====================

    public void RegisterBuff(string id, string name, string desc, int maxStacks,
        string stackMode, string durationType, int duration)
    {
        var sm = (stackMode ?? "").ToLower() switch
        {
            "stack" => BuffStackMode.Stack,
            "keep_higher" => BuffStackMode.KeepHigher,
            "refresh" => BuffStackMode.Refresh,
            "ignore" => BuffStackMode.Ignore,
            _ => BuffStackMode.Stack
        };
        var dt = (durationType ?? "").ToLower() switch
        {
            "permanent" => BuffDurationType.Permanent,
            "turn_based" => BuffDurationType.TurnBased,
            _ => BuffDurationType.Permanent
        };

        var def = new BuffDefinition
        {
            Id = id,
            DisplayName = name,
            Description = desc,
            MaxStacks = maxStacks,
            StackMode = sm,
            DurationType = dt,
            DefaultDuration = duration
        };
        BuffRegistry.Register(id, def, new LuaBuffBehavior());
    }

    public void RegisterBuffHandler(string buffId, string eventName, string phase, DynValue callback)
    {
        if (BuffRegistry.TryGet(buffId, out _, out var behavior) && behavior is LuaBuffBehavior luaBehavior)
        {
            luaBehavior.AddHandler(eventName, phase, callback);
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                $"[LuaBattleContextApi] RegisterBuffHandler: Buff未注册或非Lua Buff: {buffId}");
        }
    }

    public void ApplyBuff(string buffId, int stacks) => ctx.Buffs.ApplyBuff(buffId, stacks, "lua");
    public void RemoveBuff(string buffId) => ctx.Buffs.RemoveBuff(buffId);
    public int GetBuffStacks(string buffId) => ctx.Buffs.GetStacks(buffId);
    public bool HasBuff(string buffId) => ctx.Buffs.HasBuff(buffId);
    public void ModifyBuffStacks(string buffId, int delta) => ctx.Buffs.ModifyStacks(buffId, delta);

    public void RegisterCardEventHandler(string eventName, string phase, DynValue callback)
    {
        if (callback.Type != DataType.Function)
        {
            Debug.LogWarning("[LuaBattleContextApi] RegisterCardEventHandler: callback 必须是函数。");
            return;
        }

        if (cardEventOwner == null)
        {
            Debug.LogWarning("[LuaBattleContextApi] RegisterCardEventHandler: 当前上下文无卡牌 owner。");
            return;
        }

        var parsedPhase = (phase ?? "").ToLower() switch
        {
            "before" => EventPhase.Before,
            "resolve" => EventPhase.Resolve,
            "after" => EventPhase.After,
            _ => EventPhase.After
        };

        BindToEvent(ctx.Events, eventName, parsedPhase, args =>
        {
            try
            {
                callback.Function.Call(
                    UserData.Create(new LuaBattleContextApi(ctx, cardEventOwner)),
                    UserData.Create(args));
            }
            catch (Exception e)
            {
                Debug.LogError($"[LuaBattleContextApi] Card事件回调执行异常: {e.Message}");
            }
        }, cardEventOwner);
    }

    private static void BindToEvent(BattleEventManager events, string eventName,
        EventPhase phase, Action<BattleEventArgs> handler, object owner)
    {
        switch (eventName)
        {
            case "OnBattleStarted":
                events.OnBattleStarted.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnTurnStart":
                events.OnTurnStart.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnTurnEnd":
                events.OnTurnEnd.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnCardPlayed":
                events.OnCardPlayed.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnCardDrawn":
                events.OnCardDrawn.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnBalanceChanged":
                events.OnBalanceChanged.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnBalanceThreshold":
                events.OnBalanceThreshold.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnDamageDealt":
                events.OnDamageDealt.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnHeal":
                events.OnHeal.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnLifeLost":
                events.OnLifeLost.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnEnemyAction":
                events.OnEnemyAction.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            case "OnEnemyDied":
                events.OnEnemyDied.Register(phase, a => handler(a), EventPriority.Normal, owner);
                break;
            default:
                Debug.LogWarning($"[LuaBattleContextApi] RegisterCardEventHandler: 未知事件名: {eventName}");
                break;
        }
    }
}

/// <summary>
/// 暴露给 Lua 的卡牌数据只读视图。
/// </summary>
[MoonSharpUserData]
public class LuaCardDataApi
{
    private readonly CardData data;

    public LuaCardDataApi(CardData data)
    {
        this.data = data;
    }

    public string Name => data != null ? data.cardName : string.Empty;
    public int BaseCost => data != null ? data.baseCost : 0;
    public string CostType => data != null ? data.costType.ToString() : string.Empty;
}
