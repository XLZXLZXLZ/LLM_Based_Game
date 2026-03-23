using System;
using System.Collections.Generic;

public class DeckState
{
    public List<Card> DrawPile { get; } = new();
    public List<Card> Hand { get; } = new();
    public List<Card> DiscardPile { get; } = new();

    public event Action<Card> OnCardAddedToHand;
    public event Action<Card> OnCardRemovedFromHand;

    public void InitializeDeck(List<Card> cards)
    {
        DrawPile.Clear();
        Hand.Clear();
        DiscardPile.Clear();
        DrawPile.AddRange(cards);
        Shuffle();
    }

    public void Shuffle()
    {
        var rng = new System.Random();
        int n = DrawPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (DrawPile[k], DrawPile[n]) = (DrawPile[n], DrawPile[k]);
        }
    }

    /// <summary>
    /// 从抽牌堆抽一张牌到手牌，堆空时自动洗回弃牌堆
    /// </summary>
    public Card Draw()
    {
        if (DrawPile.Count == 0)
        {
            if (DiscardPile.Count == 0) return null;
            ReshuffleDiscardIntoDraw();
        }
        if (DrawPile.Count == 0) return null;

        int lastIndex = DrawPile.Count - 1;
        Card card = DrawPile[lastIndex];
        DrawPile.RemoveAt(lastIndex);
        Hand.Add(card);
        OnCardAddedToHand?.Invoke(card);
        return card;
    }

    /// <summary>
    /// 直接将一张牌加入手牌（不经过抽牌堆，用于测试或特殊效果）
    /// </summary>
    public void AddToHand(Card card)
    {
        Hand.Add(card);
        OnCardAddedToHand?.Invoke(card);
    }

    public void DiscardFromHand(Card card)
    {
        if (!Hand.Remove(card)) return;
        DiscardPile.Add(card);
        OnCardRemovedFromHand?.Invoke(card);
    }

    public void DiscardAllHand()
    {
        while (Hand.Count > 0)
        {
            var card = Hand[Hand.Count - 1];
            Hand.RemoveAt(Hand.Count - 1);
            DiscardPile.Add(card);
            OnCardRemovedFromHand?.Invoke(card);
        }
    }

    private void ReshuffleDiscardIntoDraw()
    {
        DrawPile.AddRange(DiscardPile);
        DiscardPile.Clear();
        Shuffle();
    }
}
