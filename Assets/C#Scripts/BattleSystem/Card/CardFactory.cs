using System;
using System.Collections.Generic;

/// <summary>
/// 卡牌工厂：根据 CardData.cardType 创建运行时 Card 实例。
/// 采用显式注册表，避免 switch 持续膨胀。
/// </summary>
public static class CardFactory
{
    private static readonly Dictionary<CardType, Func<CardData, Card>> Registry =
        new Dictionary<CardType, Func<CardData, Card>>
        {
            [CardType.Lua_Card] = data => new Card_0_Lua(data),
            [CardType.Speech] = data => new Card_1_Speech(data),
            [CardType.Introspection] = data => new Card_2_Introspection(data),
            [CardType.Monologue] = data => new Card_3_Monologue(data),
            [CardType.Contemplation] = data => new Card_4_Contemplation(data),
            [CardType.Questioning] = data => new Card_5_Questioning(data),
            [CardType.Simple] = data => new Card_6_Simple(data),
            [CardType.Devotion] = data => new Card_7_Devotion(data),
            [CardType.Test] = data => new Card_99_Test(data),
        };

    public static Card Create(CardData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "CardFactory.Create 失败：CardData 不能为空。");
        }

        if (!Registry.TryGetValue(data.cardType, out var creator))
        {
            throw new InvalidOperationException(
                $"CardFactory.Create 失败：未注册的 cardType={data.cardType}（index={(int)data.cardType}，卡牌名：\"{data.cardName}\"）。");
        }

        if (data.cardType == CardType.Lua_Card)
        {
            // Lua 卡允许创建；若未注册执行器，Card_0_Lua 内部会走安全降级逻辑。
            System.Diagnostics.Debug.WriteLine($"[CardFactory] 创建 Lua_Card: {data.cardName}");
        }

        return creator(data);
    }

    public static void Register(CardType cardType, Func<CardData, Card> creator)
    {
        if (creator == null)
        {
            throw new ArgumentNullException(nameof(creator), "CardFactory.Register 失败：creator 不能为空。");
        }

        Registry[cardType] = creator;
    }
}
