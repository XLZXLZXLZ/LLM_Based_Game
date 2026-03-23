using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    NotStarted,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

/// <summary>
/// 战斗流程编排器。只负责回合流转调度，不含任何战斗逻辑。
/// 所有实际操作通过 BattleContext 完成。
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("场景引用")]
    [SerializeField] private HandUI handUI;
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private EffectManager effectManager;
    [SerializeField] private BattleUIManager battleUIManager;

    [Header("战斗参数")]
    [SerializeField] private int drawPerTurn = 5;
    [SerializeField] private int playerMaxLife = 3;
    [SerializeField] private int playerMaxHp = 30;
    [SerializeField] private int balanceMaxDifference = 5;

    private BattleContext ctx;
    private Enemy currentEnemy;
    private bool waitingForPlayerInput;

    public BattleContext Context => ctx;
    public BattleState CurrentState { get; private set; } = BattleState.NotStarted;

    public event Action<BattleState> OnBattleStateChanged;

    // ==================== 启动 ====================

    public void StartBattle(EnemyData enemyData, List<Card> cards)
    {
        var playerState = new PlayerState(playerMaxLife, playerMaxHp);
        var enemyState = new EnemyState(enemyData.maxHp, enemyData.baseAttack, enemyData.maxTimer);
        var balanceState = new BalanceState(balanceMaxDifference);
        var deckState = new DeckState();

        ctx = new BattleContext(playerState, enemyState, balanceState, deckState);
        deckState.InitializeDeck(cards);
        InitializeAllDeckCards();
        ctx.Events.OnBattleStarted.Invoke(new BattleStartedEventArgs(this));

        if (enemyPrefab != null && enemySpawnPoint != null)
        {
            currentEnemy = Instantiate(enemyPrefab, enemySpawnPoint);
            currentEnemy.Initialize(enemyData);
        }

        if (handUI != null)
        {
            handUI.Initialize(ctx);
        }

        if (effectManager == null)
            effectManager = GetComponentInChildren<EffectManager>();

        if (effectManager != null)
            effectManager.Initialize(ctx);

        if (battleUIManager == null)
            battleUIManager = GetComponentInChildren<BattleUIManager>();

        if (battleUIManager != null)
            battleUIManager.Initialize(ctx, enemyData);

        StartCoroutine(BattleLoop());
    }

    public void StartBattle(EnemyData enemyData, List<CardData> cardDatas)
    {
        var cards = new List<Card>();
        foreach (var data in cardDatas)
            cards.Add(CardFactory.Create(data));
        StartBattle(enemyData, cards);
    }

    // ==================== 战斗主循环 ====================

    private IEnumerator BattleLoop()
    {
        while (!ctx.Enemy.IsDead && !ctx.Player.IsDead)
        {
            yield return StartCoroutine(PlayerTurn());
            if (ctx.Enemy.IsDead || ctx.Player.IsDead) break;

            yield return StartCoroutine(EnemyTurn());
        }

        if (ctx.Enemy.IsDead)
        {
            SetState(BattleState.Victory);
            Debug.Log("[BattleManager] 战斗胜利！");
        }
        else
        {
            SetState(BattleState.Defeat);
            Debug.Log("[BattleManager] 战斗失败！");
        }
    }

    private IEnumerator PlayerTurn()
    {
        SetState(BattleState.PlayerTurn);

        ctx.CurrentTurn++;
        ctx.ClearShield();
        ctx.Events.OnTurnStart.Invoke(new TurnEventArgs(ctx.CurrentTurn));

        ctx.DrawCards(drawPerTurn);

        if (handUI != null)
        {
            handUI.IsInteractable = true;
            handUI.RefreshPlayableStates();
        }

        waitingForPlayerInput = true;
        while (waitingForPlayerInput && !ctx.Enemy.IsDead && !ctx.Player.IsDead)
        {
            yield return null;
        }

        if (handUI != null)
            handUI.IsInteractable = false;

        ctx.Deck.DiscardAllHand();
        ctx.ClearBalance("turn_end");
    }

    private IEnumerator EnemyTurn()
    {
        SetState(BattleState.EnemyTurn);

        if (currentEnemy != null)
        {
            var args = new EnemyActionEventArgs("turn", ctx.Enemy.Attack);
            ctx.Events.OnEnemyAction.Invoke(args);

            if (!args.IsCancelled)
                currentEnemy.OnTurn(ctx);
        }

        ctx.Events.OnTurnEnd.Invoke(new TurnEventArgs(ctx.CurrentTurn));
        ctx.Buffs.TickTurnEnd();

        yield return new WaitForSeconds(0.5f);
    }

    // ==================== 外部接口 ====================

    public void EndPlayerTurn()
    {
        if (CurrentState != BattleState.PlayerTurn) return;
        waitingForPlayerInput = false;
    }

    // ==================== 内部 ====================

    private void SetState(BattleState newState)
    {
        CurrentState = newState;
        OnBattleStateChanged?.Invoke(newState);
    }

    private void InitializeAllDeckCards()
    {
        var uniqueCards = new HashSet<Card>();

        foreach (var card in ctx.Deck.DrawPile)
            uniqueCards.Add(card);
        foreach (var card in ctx.Deck.Hand)
            uniqueCards.Add(card);
        foreach (var card in ctx.Deck.DiscardPile)
            uniqueCards.Add(card);

        foreach (var card in uniqueCards)
            card?.OnInitialize(ctx);
    }
}
