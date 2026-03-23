using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

/// <summary>
/// 通用 Lua Buff 行为：将 Lua 回调函数桥接到 BattleEvent 系统。
/// 由 LuaBattleContextApi.RegisterBuffHandler 填充回调，OnApply 时注册到事件系统。
/// </summary>
public class LuaBuffBehavior : IBuffBehavior
{
    private readonly List<(string eventName, EventPhase phase, DynValue callback)> handlers = new();

    public void AddHandler(string eventName, string phaseStr, DynValue callback)
    {
        var phase = ParsePhase(phaseStr);
        handlers.Add((eventName, phase, callback));
    }

    public void OnApply(BuffInstance buff, BattleContext ctx)
    {
        foreach (var (eventName, phase, callback) in handlers)
        {
            BindToEvent(ctx.Events, eventName, phase, args =>
            {
                try
                {
                    callback.Function.Call(
                        UserData.Create(buff),
                        UserData.Create(new LuaBattleContextApi(ctx)),
                        UserData.Create(args));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LuaBuffBehavior] Buff回调执行异常: {e.Message}");
                }
            }, buff);
        }
    }

    public void OnRemove(BuffInstance buff, BattleContext ctx)
    {
        ctx.Events.UnregisterAllByOwner(buff);
    }

    public void OnStackChanged(BuffInstance buff, BattleContext ctx, int oldStacks) { }

    private static EventPhase ParsePhase(string phaseStr)
    {
        return (phaseStr ?? "").ToLower() switch
        {
            "before" => EventPhase.Before,
            "resolve" => EventPhase.Resolve,
            "after" => EventPhase.After,
            _ => EventPhase.After
        };
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
                Debug.LogWarning($"[LuaBuffBehavior] 未知事件名: {eventName}");
                break;
        }
    }
}
