using UnityEngine;

/// <summary>
/// 敌人基类（MonoBehaviour，场景中有视觉表现）。
/// 1v1 模式下只有一个实例，通过 BattleContext 访问自己的状态。
/// </summary>
public class Enemy : MonoBehaviour
{
    protected EnemyData data;

    public EnemyData Data => data;

    public virtual void Initialize(EnemyData data)
    {
        this.data = data;
    }

    /// <summary>
    /// 敌人回合：计时器倒数，归零时执行行动并重置
    /// </summary>
    public virtual void OnTurn(BattleContext ctx)
    {
        ctx.Enemy.Timer--;
        if (ctx.Enemy.Timer <= 0)
        {
            EnemyAction(ctx);
            ctx.Enemy.Timer = ctx.Enemy.MaxTimer;
        }
    }

    /// <summary>
    /// 敌人行动（默认：对玩家造成攻击力等值的伤害）。子类可重写实现特殊行为。
    /// </summary>
    public virtual void EnemyAction(BattleContext ctx)
    {
        ctx.DealDamageToPlayer(ctx.Enemy.Attack);
    }
}
