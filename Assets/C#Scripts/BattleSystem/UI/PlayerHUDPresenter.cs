using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家HUD绑定器：负责HP与命数图标映射。
/// </summary>
public class PlayerHUDPresenter : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private Image hpFillImage;

    [Header("Life Icons (HorizontalLayout)")]
    [SerializeField] private Transform lifeIconContainer;
    [SerializeField] private GameObject lifeIconPrefab;

    private BattleContext ctx;
    private PlayerState player;
    private bool isBound;

    public void Initialize(BattleContext context)
    {
        if (context == null) return;

        Unbind();
        ctx = context;
        player = context.Player;
        Bind();
        RefreshAll();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void Bind()
    {
        if (player == null || isBound) return;

        player.OnHpChanged += HandleHpChanged;
        player.OnMaxHpChanged += HandleMaxHpChanged;
        player.OnLifeChanged += HandleLifeChanged;
        player.OnMaxLifeChanged += HandleMaxLifeChanged;
        isBound = true;
    }

    private void Unbind()
    {
        if (!isBound || player == null) return;

        player.OnHpChanged -= HandleHpChanged;
        player.OnMaxHpChanged -= HandleMaxHpChanged;
        player.OnLifeChanged -= HandleLifeChanged;
        player.OnMaxLifeChanged -= HandleMaxLifeChanged;
        isBound = false;
    }

    private void RefreshAll()
    {
        if (player == null) return;
        EnsureLifeIconCount(player.MaxLife);
        RefreshHpFill(player.Hp, player.MaxHp);
        RefreshLifeIcons(player.Life);
    }

    private void HandleHpChanged(int hp) => RefreshHpFill(hp, player.MaxHp);
    private void HandleMaxHpChanged(int maxHp) => RefreshHpFill(player.Hp, maxHp);

    private void HandleLifeChanged(int life) => RefreshLifeIcons(life);

    private void HandleMaxLifeChanged(int maxLife)
    {
        EnsureLifeIconCount(maxLife);
        RefreshLifeIcons(player.Life);
    }

    private void RefreshHpFill(int hp, int maxHp)
    {
        if (hpFillImage == null) return;
        if (maxHp <= 0)
        {
            hpFillImage.fillAmount = 0f;
            return;
        }

        hpFillImage.fillAmount = Mathf.Clamp01((float)hp / maxHp);
    }

    private void EnsureLifeIconCount(int maxLife)
    {
        if (lifeIconContainer == null || lifeIconPrefab == null || maxLife < 0) return;

        while (lifeIconContainer.childCount < maxLife)
        {
            Instantiate(lifeIconPrefab, lifeIconContainer);
        }
    }

    private void RefreshLifeIcons(int life)
    {
        if (lifeIconContainer == null) return;

        int activeCount = Mathf.Max(0, life);
        for (int i = 0; i < lifeIconContainer.childCount; i++)
        {
            lifeIconContainer.GetChild(i).gameObject.SetActive(i < activeCount);
        }
    }
}
