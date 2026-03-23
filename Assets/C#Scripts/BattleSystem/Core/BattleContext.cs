using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗上下文 —— 所有战斗操作的唯一入口。
/// Card / Enemy / Lua 对战斗状态的一切读写都通过此类完成。
/// 每个操作方法内部自动触发对应的战斗事件。
/// </summary>
public class BattleContext
{
    public PlayerState Player { get; }
    public EnemyState Enemy { get; }
    public BalanceState Balance { get; }
    public DeckState Deck { get; }
    public BattleEventManager Events { get; }
    public BuffManager Buffs { get; }
    public int CurrentTurn { get; set; }

    public BattleContext(PlayerState player, EnemyState enemy, BalanceState balance, DeckState deck)
    {
        Player = player;
        Enemy = enemy;
        Balance = balance;
        Deck = deck;
        Events = new BattleEventManager();
        Buffs = new BuffManager(this);
    }

    // ==================== 出牌流水线 ====================

    /// <summary>
    /// 获取卡牌当前的有效费用（与出牌时扣费一致，会经过 OnCardPlayed.Before 的修饰）。
    /// 用于 UI 提前显示减费等效果。无 ctx 时可用 Card.CostValue 作为显示用基础值。
    /// </summary>
    public (CostType costType, int value) GetEffectiveCost(Card card)
    {
        var args = new CardPlayedEventArgs(card);
        args.CostSide = card.GetCostType(this);
        args.CostValue = card.GetCostValue(this);
        Events.OnCardPlayed.FireBefore(args);
        return (args.CostSide, args.CostValue);
    }

    /// <summary>
    /// 打出一张卡牌：验证 → Before事件 → 扣费 → 效果 → 弃牌 → After事件
    /// </summary>
    public bool PlayCard(Card card)
    {
        if (!card.CanPlay(this)) return false;

        var args = new CardPlayedEventArgs(card);
        args.CostSide = card.GetCostType(this);
        args.CostValue = card.GetCostValue(this);
        if (!Events.OnCardPlayed.FireBefore(args)) return false;

        AdjustBalance(args.CostSide, args.CostValue);
        card.OnPlay(this);
        Deck.DiscardFromHand(card);

        Events.OnCardPlayed.FireResolveAndAfter(args);
        return true;
    }

    // ==================== 伤害 ====================

    public void DealDamageToEnemy(int amount, object source = null)
    {
        var args = new DamageEventArgs(DamageTarget.Enemy, amount, source);
        if (!Events.OnDamageDealt.FireBefore(args)) return;

        Enemy.Hp -= args.Amount;

        Events.OnDamageDealt.FireResolveAndAfter(args);

        if (Enemy.IsDead)
        {
            Events.OnEnemyDied.Invoke(new EnemyDiedEventArgs(source));
        }
    }

    public void DealDamageToPlayer(int amount, object source = null)
    {
        var args = new DamageEventArgs(DamageTarget.Player, amount, source);
        if (!Events.OnDamageDealt.FireBefore(args)) return;

        int damage = args.Amount;
        if (Player.Shield > 0)
        {
            int absorbed = Mathf.Min(Player.Shield, damage);
            Player.Shield -= absorbed;
            damage -= absorbed;
        }

        if (damage > 0)
            Player.Hp -= damage;

        Events.OnDamageDealt.FireResolveAndAfter(args);

        if (Player.Hp <= 0)
        {
            LoseLife(source);
        }
    }

    // ==================== 护盾 ====================

    public void GainShield(int amount)
    {
        Player.Shield += amount;
    }

    public void ClearShield()
    {
        Player.Shield = 0;
    }

    // ==================== 治疗 ====================

    public void HealPlayer(int amount, object source = null)
    {
        int actual = Mathf.Min(amount, Player.MaxHp - Player.Hp);
        if (actual <= 0) return;

        var args = new HealEventArgs(actual, source);
        if (!Events.OnHeal.FireBefore(args)) return;

        Player.Hp = Mathf.Min(Player.Hp + args.Amount, Player.MaxHp);

        Events.OnHeal.FireResolveAndAfter(args);
    }

    // ==================== 天平 ====================

    public void AdjustBalance(CostType side, int value)
    {
        int prevAnger = Balance.AngerPoint;
        int prevCalm = Balance.CalmPoint;

        if (side == CostType.Anger)
            Balance.AngerPoint += value;
        else
            Balance.CalmPoint += value;

        var changedArgs = new BalanceChangedEventArgs(
            prevAnger, prevCalm,
            Balance.AngerPoint, Balance.CalmPoint,
            value, side);
        Events.OnBalanceChanged.Invoke(changedArgs);

        if (Balance.IsOverflow)
        {
            CostType overflowSide = Balance.AngerPoint > Balance.CalmPoint
                ? CostType.Anger : CostType.Calm;

            var thresholdArgs = new BalanceThresholdEventArgs(overflowSide, Balance.Difference);
            if (Events.OnBalanceThreshold.FireBefore(thresholdArgs))
            {
                LoseLife("balance_overflow");
                Events.OnBalanceThreshold.FireResolveAndAfter(thresholdArgs);
            }
        }
    }

    // ==================== 生命 ====================

    private void LoseLife(object source = null)
    {
        var args = new LifeLostEventArgs(source);
        if (!Events.OnLifeLost.FireBefore(args)) return;

        Player.Life--;
        ClearBalance(source);

        if (Player.Life > 0)
        {
            Player.Hp = Player.MaxHp;
        }

        Events.OnLifeLost.FireResolveAndAfter(args);
    }

    public void ClearBalance(object source = null)
    {
        int prevAnger = Balance.AngerPoint;
        int prevCalm = Balance.CalmPoint;

        if (prevAnger == 0 && prevCalm == 0) return;

        Balance.AngerPoint = 0;
        Balance.CalmPoint = 0;

        if (prevAnger != 0)
        {
            Events.OnBalanceChanged.Invoke(new BalanceChangedEventArgs(
                prevAnger, prevCalm,
                Balance.AngerPoint, Balance.CalmPoint,
                -prevAnger, CostType.Anger,
                source));
        }

        if (prevCalm != 0)
        {
            Events.OnBalanceChanged.Invoke(new BalanceChangedEventArgs(
                prevAnger, prevCalm,
                Balance.AngerPoint, Balance.CalmPoint,
                -prevCalm, CostType.Calm,
                source));
        }
    }

    // ==================== 抽牌 ====================

    public List<Card> DrawCards(int count)
    {
        var drawn = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card card = Deck.Draw();
            if (card == null) break;

            var args = new CardDrawnEventArgs(card);
            Events.OnCardDrawn.Invoke(args);
            drawn.Add(card);
        }
        return drawn;
    }

    // ==================== 敌人计时器 ====================

    public void ModifyEnemyTimer(int delta)
    {
        Enemy.Timer += delta;
    }
}
