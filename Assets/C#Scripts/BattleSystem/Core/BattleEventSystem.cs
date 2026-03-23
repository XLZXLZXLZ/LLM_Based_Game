using System;
using System.Collections.Generic;
using UnityEngine;

// ==================== 事件基础设施 ====================

public enum EventPhase
{
    Before,
    Resolve,
    After
}

public static class EventPriority
{
    public const int First = 1000;
    public const int High = 100;
    public const int Normal = 0;
    public const int Low = -100;
    public const int Last = -1000;
}

public abstract class BattleEventArgs
{
    public bool IsCancelled { get; private set; }
    public EventPhase CurrentPhase { get; internal set; }
    public object Source { get; set; }

    public void Cancel()
    {
        if (CurrentPhase == EventPhase.Before)
            IsCancelled = true;
        else
            Debug.LogWarning($"[BattleEvent] 只能在Before阶段取消事件，当前阶段: {CurrentPhase}");
    }
}

internal class EventRegistration<TArgs> where TArgs : BattleEventArgs
{
    public Action<TArgs> Handler { get; set; }
    public int Priority { get; set; }
    public EventPhase Phase { get; set; }
    public object Owner { get; set; }
    public int RegistrationOrder { get; set; }
}

/// <summary>
/// 单个事件类型的管理器，支持三阶段触发和分阶段触发
/// </summary>
public class BattleEvent<TArgs> where TArgs : BattleEventArgs
{
    private readonly List<EventRegistration<TArgs>> _registrations = new();
    private int _registrationCounter = 0;
    private bool _isDirty = false;

    public void Register(EventPhase phase, Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
    {
        _registrations.Add(new EventRegistration<TArgs>
        {
            Handler = handler,
            Priority = priority,
            Phase = phase,
            Owner = owner,
            RegistrationOrder = _registrationCounter++
        });
        _isDirty = true;
    }

    public void OnBefore(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.Before, handler, priority, owner);

    public void OnResolve(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.Resolve, handler, priority, owner);

    public void OnAfter(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.After, handler, priority, owner);

    public void Unregister(Action<TArgs> handler)
    {
        _registrations.RemoveAll(r => r.Handler == handler);
    }

    public void UnregisterByOwner(object owner)
    {
        if (owner == null) return;
        _registrations.RemoveAll(r => r.Owner == owner);
    }

    /// <summary>
    /// 完整三阶段触发（用于通知类事件）
    /// </summary>
    public void Invoke(TArgs args)
    {
        EnsureSorted();
        RunPhase(EventPhase.Before, args);
        if (args.IsCancelled) return;
        RunPhase(EventPhase.Resolve, args);
        RunPhase(EventPhase.After, args);
    }

    /// <summary>
    /// 仅触发 Before 阶段，返回 true 表示未被取消
    /// </summary>
    public bool FireBefore(TArgs args)
    {
        EnsureSorted();
        RunPhase(EventPhase.Before, args);
        return !args.IsCancelled;
    }

    /// <summary>
    /// 触发 Resolve + After 阶段（在核心逻辑执行后调用）
    /// </summary>
    public void FireResolveAndAfter(TArgs args)
    {
        RunPhase(EventPhase.Resolve, args);
        RunPhase(EventPhase.After, args);
    }

    public void Clear()
    {
        _registrations.Clear();
        _registrationCounter = 0;
    }

    private void EnsureSorted()
    {
        if (!_isDirty) return;
        _registrations.Sort((a, b) =>
        {
            int cmp = b.Priority.CompareTo(a.Priority);
            return cmp != 0 ? cmp : a.RegistrationOrder.CompareTo(b.RegistrationOrder);
        });
        _isDirty = false;
    }

    private void RunPhase(EventPhase phase, TArgs args)
    {
        args.CurrentPhase = phase;
        // 使用快照迭代，允许回调中安全地注册/注销事件（如 Buff 移除自身）
        var snapshot = _registrations.ToArray();
        foreach (var reg in snapshot)
        {
            if (reg.Phase != phase) continue;
            if (phase == EventPhase.Before && args.IsCancelled) break;
            try { reg.Handler?.Invoke(args); }
            catch (Exception e) { Debug.LogError($"[BattleEvent] 事件处理器异常: {e}"); }
        }
    }
}

// ==================== 事件管理器 ====================

public class BattleEventManager
{
    public readonly BattleEvent<BattleStartedEventArgs> OnBattleStarted = new();
    public readonly BattleEvent<TurnEventArgs> OnTurnStart = new();
    public readonly BattleEvent<TurnEventArgs> OnTurnEnd = new();

    public readonly BattleEvent<CardPlayedEventArgs> OnCardPlayed = new();
    public readonly BattleEvent<CardDrawnEventArgs> OnCardDrawn = new();

    public readonly BattleEvent<BalanceChangedEventArgs> OnBalanceChanged = new();
    public readonly BattleEvent<BalanceThresholdEventArgs> OnBalanceThreshold = new();

    public readonly BattleEvent<DamageEventArgs> OnDamageDealt = new();
    public readonly BattleEvent<HealEventArgs> OnHeal = new();
    public readonly BattleEvent<LifeLostEventArgs> OnLifeLost = new();

    public readonly BattleEvent<EnemyActionEventArgs> OnEnemyAction = new();
    public readonly BattleEvent<EnemyDiedEventArgs> OnEnemyDied = new();

    public void UnregisterAllByOwner(object owner)
    {
        OnBattleStarted.UnregisterByOwner(owner);
        OnTurnStart.UnregisterByOwner(owner);
        OnTurnEnd.UnregisterByOwner(owner);
        OnCardPlayed.UnregisterByOwner(owner);
        OnCardDrawn.UnregisterByOwner(owner);
        OnBalanceChanged.UnregisterByOwner(owner);
        OnBalanceThreshold.UnregisterByOwner(owner);
        OnDamageDealt.UnregisterByOwner(owner);
        OnHeal.UnregisterByOwner(owner);
        OnLifeLost.UnregisterByOwner(owner);
        OnEnemyAction.UnregisterByOwner(owner);
        OnEnemyDied.UnregisterByOwner(owner);
    }

    public void ClearAll()
    {
        OnBattleStarted.Clear();
        OnTurnStart.Clear();
        OnTurnEnd.Clear();
        OnCardPlayed.Clear();
        OnCardDrawn.Clear();
        OnBalanceChanged.Clear();
        OnBalanceThreshold.Clear();
        OnDamageDealt.Clear();
        OnHeal.Clear();
        OnLifeLost.Clear();
        OnEnemyAction.Clear();
        OnEnemyDied.Clear();
    }
}

// ==================== 事件参数定义 ====================

public enum DamageTarget
{
    Player,
    Enemy
}

public class BattleStartedEventArgs : BattleEventArgs
{
    public BattleStartedEventArgs(object source = null)
    {
        Source = source;
    }
}

public class TurnEventArgs : BattleEventArgs
{
    public int TurnNumber { get; set; }

    public TurnEventArgs(int turnNumber, object source = null)
    {
        TurnNumber = turnNumber;
        Source = source;
    }
}

public class CardPlayedEventArgs : BattleEventArgs
{
    public Card Card { get; set; }
    public CostType CostSide { get; set; }
    public int CostValue { get; set; }

    public CardPlayedEventArgs(Card card, object source = null)
    {
        Card = card;
        CostSide = card.CostType;
        CostValue = card.CostValue;
        Source = source;
    }
}

public class CardDrawnEventArgs : BattleEventArgs
{
    public Card Card { get; set; }

    public CardDrawnEventArgs(Card card, object source = null)
    {
        Card = card;
        Source = source;
    }
}

public class BalanceChangedEventArgs : BattleEventArgs
{
    public int PreviousAnger { get; set; }
    public int PreviousCalm { get; set; }
    public int CurrentAnger { get; set; }
    public int CurrentCalm { get; set; }
    public int Delta { get; set; }
    public CostType Side { get; set; }

    public BalanceChangedEventArgs(
        int prevAnger, int prevCalm,
        int curAnger, int curCalm,
        int delta, CostType side,
        object source = null)
    {
        PreviousAnger = prevAnger;
        PreviousCalm = prevCalm;
        CurrentAnger = curAnger;
        CurrentCalm = curCalm;
        Delta = delta;
        Side = side;
        Source = source;
    }
}

public class BalanceThresholdEventArgs : BattleEventArgs
{
    public CostType OverflowSide { get; set; }
    public int Difference { get; set; }

    public BalanceThresholdEventArgs(CostType overflowSide, int difference, object source = null)
    {
        OverflowSide = overflowSide;
        Difference = difference;
        Source = source;
    }
}

public class DamageEventArgs : BattleEventArgs
{
    public DamageTarget Target { get; set; }
    public int Amount { get; set; }

    public DamageEventArgs(DamageTarget target, int amount, object source = null)
    {
        Target = target;
        Amount = amount;
        Source = source;
    }
}

public class HealEventArgs : BattleEventArgs
{
    public int Amount { get; set; }

    public HealEventArgs(int amount, object source = null)
    {
        Amount = amount;
        Source = source;
    }
}

public class LifeLostEventArgs : BattleEventArgs
{
    public LifeLostEventArgs(object source = null)
    {
        Source = source;
    }
}

public class EnemyActionEventArgs : BattleEventArgs
{
    public string ActionType { get; set; }
    public int Value { get; set; }

    public EnemyActionEventArgs(string actionType, int value = 0, object source = null)
    {
        ActionType = actionType;
        Value = value;
        Source = source;
    }
}

public class EnemyDiedEventArgs : BattleEventArgs
{
    public EnemyDiedEventArgs(object source = null)
    {
        Source = source;
    }
}
