using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件阶段
/// </summary>
public enum EventPhase
{
    Before,     // 事件发生前，可取消、可修改参数
    Resolve,    // 事件结算中
    After       // 事件发生后，只读响应
}

/// <summary>
/// 事件优先级常量
/// </summary>
public static class EventPriority
{
    public const int First = 1000;      // 最先执行（系统级）
    public const int High = 100;        // 高优先级
    public const int Normal = 0;        // 默认
    public const int Low = -100;        // 低优先级
    public const int Last = -1000;      // 最后执行
}

/// <summary>
/// 事件参数基类
/// </summary>
public abstract class BattleEventArgs
{
    /// <summary>
    /// 事件是否被取消（仅Before阶段有效）
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// 当前事件阶段
    /// </summary>
    public EventPhase CurrentPhase { get; internal set; }

    /// <summary>
    /// 事件来源（谁触发的）
    /// </summary>
    public object Source { get; set; }

    /// <summary>
    /// 取消事件（仅Before阶段可调用）
    /// </summary>
    public void Cancel()
    {
        if (CurrentPhase == EventPhase.Before)
        {
            IsCancelled = true;
        }
        else
        {
            Debug.LogWarning($"[BattleEvent] 只能在Before阶段取消事件，当前阶段: {CurrentPhase}");
        }
    }
}

/// <summary>
/// 事件监听器注册信息
/// </summary>
internal class EventRegistration<TArgs> where TArgs : BattleEventArgs
{
    public Action<TArgs> Handler { get; set; }
    public int Priority { get; set; }
    public EventPhase Phase { get; set; }
    public object Owner { get; set; }           // 注册者，方便批量清理
    public int RegistrationOrder { get; set; }  // 注册顺序，用于同优先级排序
}

/// <summary>
/// 单个事件类型的管理器
/// </summary>
public class BattleEvent<TArgs> where TArgs : BattleEventArgs
{
    private readonly List<EventRegistration<TArgs>> _registrations = new();
    private int _registrationCounter = 0;
    private bool _isDirty = false; // 标记是否需要重新排序

    /// <summary>
    /// 注册事件监听器
    /// </summary>
    /// <param name="phase">监听阶段</param>
    /// <param name="handler">回调方法</param>
    /// <param name="priority">优先级，数字越大越先执行</param>
    /// <param name="owner">注册者（可选，用于批量清理）</param>
    public void Register(EventPhase phase, Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
    {
        var registration = new EventRegistration<TArgs>
        {
            Handler = handler,
            Priority = priority,
            Phase = phase,
            Owner = owner,
            RegistrationOrder = _registrationCounter++
        };

        _registrations.Add(registration);
        _isDirty = true;
    }

    /// <summary>
    /// 快捷注册 - Before阶段
    /// </summary>
    public void OnBefore(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.Before, handler, priority, owner);

    /// <summary>
    /// 快捷注册 - Resolve阶段
    /// </summary>
    public void OnResolve(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.Resolve, handler, priority, owner);

    /// <summary>
    /// 快捷注册 - After阶段
    /// </summary>
    public void OnAfter(Action<TArgs> handler, int priority = EventPriority.Normal, object owner = null)
        => Register(EventPhase.After, handler, priority, owner);

    /// <summary>
    /// 取消注册
    /// </summary>
    public void Unregister(Action<TArgs> handler)
    {
        _registrations.RemoveAll(r => r.Handler == handler);
    }

    /// <summary>
    /// 取消某个Owner的所有注册
    /// </summary>
    public void UnregisterByOwner(object owner)
    {
        if (owner == null) return;
        _registrations.RemoveAll(r => r.Owner == owner);
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void Invoke(TArgs args)
    {
        if (_isDirty)
        {
            SortRegistrations();
            _isDirty = false;
        }

        // Before 阶段
        args.CurrentPhase = EventPhase.Before;
        foreach (var reg in GetRegistrationsForPhase(EventPhase.Before))
        {
            if (args.IsCancelled) break;
            SafeInvoke(reg.Handler, args);
        }

        // 如果被取消，不执行后续阶段
        if (args.IsCancelled)
        {
            Debug.Log($"[BattleEvent] 事件被取消: {typeof(TArgs).Name}");
            return;
        }

        // Resolve 阶段
        args.CurrentPhase = EventPhase.Resolve;
        foreach (var reg in GetRegistrationsForPhase(EventPhase.Resolve))
        {
            SafeInvoke(reg.Handler, args);
        }

        // After 阶段
        args.CurrentPhase = EventPhase.After;
        foreach (var reg in GetRegistrationsForPhase(EventPhase.After))
        {
            SafeInvoke(reg.Handler, args);
        }
    }

    /// <summary>
    /// 清空所有注册
    /// </summary>
    public void Clear()
    {
        _registrations.Clear();
        _registrationCounter = 0;
    }

    private void SortRegistrations()
    {
        // 按优先级降序（大的在前），同优先级按注册顺序升序（先注册的在前）
        _registrations.Sort((a, b) =>
        {
            int priorityCompare = b.Priority.CompareTo(a.Priority);
            if (priorityCompare != 0) return priorityCompare;
            return a.RegistrationOrder.CompareTo(b.RegistrationOrder);
        });
    }

    private IEnumerable<EventRegistration<TArgs>> GetRegistrationsForPhase(EventPhase phase)
    {
        foreach (var reg in _registrations)
        {
            if (reg.Phase == phase)
            {
                yield return reg;
            }
        }
    }

    private void SafeInvoke(Action<TArgs> handler, TArgs args)
    {
        try
        {
            handler?.Invoke(args);
        }
        catch (Exception e)
        {
            Debug.LogError($"[BattleEvent] 事件处理器异常: {e}");
        }
    }
}

/// <summary>
/// 战斗事件管理器 - 统一管理所有事件
/// </summary>
public class BattleEventManager : Singleton<BattleEventManager> 
{
    // ==================== 在这里定义所有事件 ====================

    // 回合事件
    public readonly BattleEvent<TurnEventArgs> OnTurnStart = new();
    public readonly BattleEvent<TurnEventArgs> OnTurnEnd = new();

    // 卡牌事件
    public readonly BattleEvent<CardPlayedEventArgs> OnCardPlayed = new();
    public readonly BattleEvent<CardDrawnEventArgs> OnCardDrawn = new();

    // 天平事件
    public readonly BattleEvent<BalanceChangedEventArgs> OnBalanceChanged = new();
    public readonly BattleEvent<BalanceThresholdEventArgs> OnBalanceThreshold = new();

    // 伤害事件
    public readonly BattleEvent<DamageEventArgs> OnDamage = new();

    // 玩家状态事件
    public readonly BattleEvent<PlayerDamagedEventArgs> OnPlayerDamaged = new();
    public readonly BattleEvent<PlayerHealedEventArgs> OnPlayerHealed = new();
    public readonly BattleEvent<PlayerLifeLostEventArgs> OnPlayerLifeLost = new();
    public readonly BattleEvent<PlayerLifeGainedEventArgs> OnPlayerLifeGained = new();

    // 敌人事件
    public readonly BattleEvent<EnemyActionEventArgs> OnEnemyAction = new();

    // ==================== 清理方法 ====================

    /// <summary>
    /// 清理某个Owner的所有事件注册
    /// </summary>
    public void UnregisterAllByOwner(object owner)
    {
        OnTurnStart.UnregisterByOwner(owner);
        OnTurnEnd.UnregisterByOwner(owner);
        OnCardPlayed.UnregisterByOwner(owner);
        OnCardDrawn.UnregisterByOwner(owner);
        OnBalanceChanged.UnregisterByOwner(owner);
        OnBalanceThreshold.UnregisterByOwner(owner);
        OnDamage.UnregisterByOwner(owner);
        OnPlayerDamaged.UnregisterByOwner(owner);
        OnPlayerHealed.UnregisterByOwner(owner);
        OnPlayerLifeLost.UnregisterByOwner(owner);
        OnPlayerLifeGained.UnregisterByOwner(owner);
        OnEnemyAction.UnregisterByOwner(owner);
    }

    /// <summary>
    /// 清理所有事件
    /// </summary>
    public void ClearAll()
    {
        OnTurnStart.Clear();
        OnTurnEnd.Clear();
        OnCardPlayed.Clear();
        OnCardDrawn.Clear();
        OnBalanceChanged.Clear();
        OnBalanceThreshold.Clear();
        OnDamage.Clear();
        OnPlayerDamaged.Clear();
        OnPlayerHealed.Clear();
        OnPlayerLifeLost.Clear();
        OnPlayerLifeGained.Clear();
        OnEnemyAction.Clear();
    }
}

// ==================== 具体事件参数定义 ====================

/// <summary>
/// 回合事件参数
/// </summary>
public class TurnEventArgs : BattleEventArgs
{
    public int TurnNumber { get; set; }

    public TurnEventArgs(int turnNumber, object source = null)
    {
        TurnNumber = turnNumber;
        Source = source;
    }
}

/// <summary>
/// 卡牌打出事件参数
/// </summary>
public class CardPlayedEventArgs : BattleEventArgs
{
    public Card Card { get; set; }
    public object Target { get; set; }      // 目标（可以是Enemy或其他）
    public CostType CostSide { get; set; }  // 消耗的天平侧
    public int CostValue { get; set; }      // 消耗值（Before阶段可修改）

    public CardPlayedEventArgs(Card card, CostType costSide, int costValue, object target = null, object source = null)
    {
        Card = card;
        CostSide = costSide;
        CostValue = costValue;
        Target = target;
        Source = source;
    }
}

/// <summary>
/// 抽牌事件参数
/// </summary>
public class CardDrawnEventArgs : BattleEventArgs
{
    public Card Card { get; set; }
    public int DrawCount { get; set; }      // 本次抽牌数量

    public CardDrawnEventArgs(Card card, int drawCount = 1, object source = null)
    {
        Card = card;
        DrawCount = drawCount;
        Source = source;
    }
}

/// <summary>
/// 天平变化事件参数
/// </summary>
public class BalanceChangedEventArgs : BattleEventArgs
{
    public int PreviousFrenzy { get; set; }     // 变化前狂热值
    public int PreviousSerenity { get; set; }   // 变化前寂静值
    public int CurrentFrenzy { get; set; }      // 变化后狂热值
    public int CurrentSerenity { get; set; }    // 变化后寂静值
    public int Delta { get; set; }              // 变化量
    public CostType Side { get; set; }          // 变化的侧

    public BalanceChangedEventArgs(
        int previousFrenzy, int previousSerenity,
        int currentFrenzy, int currentSerenity,
        int delta, CostType side,
        object source = null)
    {
        PreviousFrenzy = previousFrenzy;
        PreviousSerenity = previousSerenity;
        CurrentFrenzy = currentFrenzy;
        CurrentSerenity = currentSerenity;
        Delta = delta;
        Side = side;
        Source = source;
    }
}

/// <summary>
/// 天平阈值触发事件参数
/// </summary>
public class BalanceThresholdEventArgs : BattleEventArgs
{
    public CostType OverflowSide { get; set; }  // 超标的一侧
    public int Difference { get; set; }          // 差值

    public BalanceThresholdEventArgs(CostType overflowSide, int difference, object source = null)
    {
        OverflowSide = overflowSide;
        Difference = difference;
        Source = source;
    }
}

/// <summary>
/// 伤害事件参数
/// </summary>
public class DamageEventArgs : BattleEventArgs
{
    public object Target { get; set; }          // 受伤目标
    public int Damage { get; set; }             // 伤害值（Before阶段可修改）
    public object DamageSource { get; set; }    // 伤害来源（卡牌/敌人/效果）
    public string DamageType { get; set; }      // 伤害类型（可选）

    public DamageEventArgs(object target, int damage, object damageSource = null, string damageType = null)
    {
        Target = target;
        Damage = damage;
        DamageSource = damageSource;
        DamageType = damageType;
        Source = damageSource; // 伤害来源同时作为事件来源
    }
}

/// <summary>
/// 敌人行动事件参数
/// </summary>
public class EnemyActionEventArgs : BattleEventArgs
{
    public Enemy Enemy { get; set; }
    public string ActionType { get; set; }      // 行动类型
    public int Value { get; set; }              // 行动数值

    public EnemyActionEventArgs(Enemy enemy, string actionType, int value = 0)
    {
        Enemy = enemy;
        ActionType = actionType;
        Value = value;
        Source = enemy; // 敌人本身作为事件来源
    }
}

// ==================== 玩家状态事件参数 ====================

/// <summary>
/// 玩家受伤事件参数
/// </summary>
public class PlayerDamagedEventArgs : BattleEventArgs
{
    public int Damage { get; set; }             // 伤害值（Before阶段可修改）

    public PlayerDamagedEventArgs(int damage, object source = null)
    {
        Damage = damage;
        Source = source;
    }
}

/// <summary>
/// 玩家回血事件参数
/// </summary>
public class PlayerHealedEventArgs : BattleEventArgs
{
    public int HealAmount { get; set; }         // 回血量（Before阶段可修改）

    public PlayerHealedEventArgs(int healAmount, object source = null)
    {
        HealAmount = healAmount;
        Source = source;
    }
}

/// <summary>
/// 玩家丢命事件参数
/// </summary>
public class PlayerLifeLostEventArgs : BattleEventArgs
{
    public PlayerLifeLostEventArgs(object source = null)
    {
        Source = source;
    }
}

/// <summary>
/// 玩家加命事件参数
/// </summary>
public class PlayerLifeGainedEventArgs : BattleEventArgs
{
    public int Amount { get; set; }             // 加命数量（Before阶段可修改）

    public PlayerLifeGainedEventArgs(int amount = 1, object source = null)
    {
        Amount = amount;
        Source = source;
    }
}
