using UnityEngine;

/// <summary>
/// 测试用卡牌 - 不需要选择目标，打出只输出Debug
/// </summary>
public class TestCard : Card
{
    public override void Init()
    {
        base.Init();
        Debug.Log($"[TestCard] 初始化卡牌: {info?.cardName}");
    }

    public override bool CanPlay()
    {
        // 测试卡牌始终可以打出
        return true;
    }

    public override void CardEffect(CardTarget target)
    {
        Debug.Log($"========================================");
        Debug.Log($"[TestCard] 打出卡牌: {info.cardName}");
        Debug.Log($"[TestCard] 费用类型: {info.costType}");
        Debug.Log($"[TestCard] 费用数值: {info.baseCost}");
        Debug.Log($"[TestCard] 卡牌品质: {info.cardQuality}");
        Debug.Log($"========================================");

        // 调用基类的Cost()消耗天平
        base.CardEffect(target);
    }

    public override string GetCardDescription()
    {
        // 可以返回动态描述，这里直接返回基础描述
        return info?.cardDescription ?? "无描述";
    }
}

