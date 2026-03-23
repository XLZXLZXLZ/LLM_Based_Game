using UnityEngine;

/// <summary>
/// 战斗特效总管：负责监听战斗事件并转发到具体表现层（相机、音效、UI等）。
/// 仅依赖 BattleContext 的事件，不直接耦合卡牌/敌人/回合流程逻辑。
/// </summary>
public class EffectManager : MonoBehaviour
{
    [Header("镜头震动")]
    [SerializeField] private bool enableCameraShakeOnDamage = true;
    [SerializeField] private float damageShakeAmplitude = 0.12f;
    [SerializeField] private float damageShakeDuration = 0.18f;

    private BattleContext ctx;
    private bool isBound;

    /// <summary>
    /// 绑定战斗上下文并开始监听事件。重复调用会先解除旧绑定。
    /// </summary>
    public void Initialize(BattleContext context)
    {
        if (ctx == context && isBound) return;

        Unbind();
        ctx = context;
        Bind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Bind()
    {
        if (ctx == null || ctx.Events == null || isBound) return;

        ctx.Events.OnDamageDealt.OnAfter(HandleDamageDealt, EventPriority.Low, this);
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || ctx == null || ctx.Events == null) return;

        ctx.Events.UnregisterAllByOwner(this);
        isBound = false;
    }

    private void HandleDamageDealt(DamageEventArgs args)
    {
        if (!enableCameraShakeOnDamage) return;
        if (args == null || args.Amount <= 0) return;

        CameraEffects.Instance.Shake(damageShakeAmplitude, damageShakeDuration);
    }
}
