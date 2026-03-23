using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 敌人HUD绑定器：负责敌人血条 fillAmount 与 hp 文本。
/// </summary>
public class EnemyHUDPresenter : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image hpFillImage;
    [SerializeField] private Text hpText;

    private EnemyState enemy;
    private int maxHp;
    private bool isBound;

    public void Initialize(BattleContext ctx, EnemyData enemyData)
    {
        if (ctx == null || ctx.Enemy == null) return;

        Unbind();
        enemy = ctx.Enemy;
        maxHp = enemyData != null ? enemyData.maxHp : enemy.MaxHp;

        Bind();
        RefreshHp(enemy.Hp);
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Bind()
    {
        if (enemy == null || isBound) return;
        enemy.OnHpChanged += HandleHpChanged;
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || enemy == null) return;
        enemy.OnHpChanged -= HandleHpChanged;
        isBound = false;
    }

    private void HandleHpChanged(int hp)
    {
        RefreshHp(hp);
    }

    private void RefreshHp(int hp)
    {
        if (hpFillImage != null)
        {
            if (maxHp <= 0)
                hpFillImage.fillAmount = 0f;
            else
                hpFillImage.fillAmount = Mathf.Clamp01((float)hp / maxHp);
        }

        if (hpText != null)
            hpText.text = $"{Mathf.Max(0, hp)}/{Mathf.Max(0, maxHp)}";
    }
}
