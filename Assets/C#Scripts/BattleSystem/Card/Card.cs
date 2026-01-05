using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CostType
{
    Anger,
    Calm
}

public enum CardQuality
{
    Common,
    Rare,
    Epic,
    Legendary
}

public class CardInfo
{
    public string cardName;
    public string cardDescription;
    public CardQuality cardQuality;
    public CostType costType;
    public int baseCost;
}

public enum CardTargetType //卡牌目标类型，需要选择目标或直接使用
{
    ChooseTarget,
    DirectUse
}

public class CardTarget //卡牌目标，可以是一个敌人，也可以是多个敌人，也可以是自身或无目标。可以是多者。
{
    public Enemy SingleEnemy { get; set; } //这张卡牌选定了一个敌人
    public List<Enemy> MultipleEnemies { get; set; } //这张卡牌选定了多个敌人
    public bool IsSelf { get; set; } //这张卡牌选定了自身
    public bool IsNone => SingleEnemy == null && MultipleEnemies == null && !IsSelf; //这张卡牌没有选定目标
    
    public static CardTarget None => new CardTarget();
    public static CardTarget Self => new CardTarget { IsSelf = true };
    public static CardTarget FromEnemy(Enemy enemy) => new CardTarget { SingleEnemy = enemy };
    public static CardTarget FromEnemies(List<Enemy> enemies) => new CardTarget { MultipleEnemies = enemies };
}

public class Card : MonoBehaviour
{
    public CardInfo info;

    public CardTargetType targetType;

    public virtual bool CanPlay() //检查是否可以打出该牌
    {
        return true; //由子类重写
    }

    public virtual void Init() //战斗开始时的初始化
    {
        
    }

    /// <summary>
    /// 检查使用该牌后，天平差值是否会超过阈值，用于UI提示
    /// </summary>
    /// <returns>true表示安全可打出，false表示会导致天平溢出</returns>
    public virtual bool IsSafe()
    {
        var balance = BattleBalance.Instance;
        int newAnger = balance.AngerPoint;
        int newCalm = balance.CalmPoint;

        // 计算打出卡牌后的新点数
        if (info.costType == CostType.Anger)
        {
            newAnger += info.baseCost;
        }
        else if (info.costType == CostType.Calm)
        {
            newCalm += info.baseCost;
        }

        // 检查新差值是否会超过阈值（5）
        int newDifference = Mathf.Abs(newAnger - newCalm);
        return newDifference <= balance.MaxDifference;
    }

    public virtual void CardEffect(CardTarget target)
    {
        Cost(); 
        //具体效果由子类重写
    }

    /// <summary>
    /// 验证单个目标是否有效，子类可重写添加筛选条件
    /// </summary>
    public virtual bool IsValidTarget(Enemy target)
    {
        return target != null;
    }

    /// <summary>
    /// 获取所有有效目标列表，供UI高亮使用
    /// </summary>
    public virtual List<Enemy> GetValidTargets()
    {
        // TODO: 从 EnemyHandle 获取所有存活敌人
        return new List<Enemy>();
    }

    public virtual void Cost() //消耗资源
    {
        BattleBalance.Instance.AdjustBalance(info.costType, info.baseCost);
    }

    public virtual string GetCardDescription() //卡牌描述，部分内容可能在战斗中变化，因此需要子类重写
    {
        return info.cardDescription;
        // 卡牌描述改变时，调用RefreshDescription
    }
}
