using UnityEngine;

/// <summary>
/// 战斗UI总入口：统一初始化各个HUD Presenter。
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("HUD Presenter")]
    [SerializeField] private PlayerHUDPresenter playerHUDPresenter;
    [SerializeField] private EnemyHUDPresenter enemyHUDPresenter;
    [SerializeField] private BalanceHUDPresenter balanceHUDPresenter;

    public void Initialize(BattleContext ctx, EnemyData enemyData)
    {
        if (playerHUDPresenter != null)
            playerHUDPresenter.Initialize(ctx);

        if (enemyHUDPresenter != null)
            enemyHUDPresenter.Initialize(ctx, enemyData);

        if (balanceHUDPresenter != null)
            balanceHUDPresenter.Initialize(ctx);
    }
}
