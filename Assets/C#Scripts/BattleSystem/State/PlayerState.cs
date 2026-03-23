using System;

public class PlayerState
{
    private int hp;
    public int Hp
    {
        get => hp;
        set
        {
            if (hp == value) return;
            hp = value;
            OnHpChanged?.Invoke(hp);
        }
    }
    public event Action<int> OnHpChanged;

    private int maxHp;
    public int MaxHp
    {
        get => maxHp;
        set
        {
            if (maxHp == value) return;
            maxHp = value;
            OnMaxHpChanged?.Invoke(maxHp);
        }
    }
    public event Action<int> OnMaxHpChanged;

    private int life;
    public int Life
    {
        get => life;
        set
        {
            if (life == value) return;
            life = value;
            OnLifeChanged?.Invoke(life);
        }
    }
    public event Action<int> OnLifeChanged;

    private int maxLife;
    public int MaxLife
    {
        get => maxLife;
        set
        {
            if (maxLife == value) return;
            maxLife = value;
            OnMaxLifeChanged?.Invoke(maxLife);
        }
    }
    public event Action<int> OnMaxLifeChanged;

    private int shield;
    public int Shield
    {
        get => shield;
        set
        {
            if (shield == value) return;
            shield = value;
            OnShieldChanged?.Invoke(shield);
        }
    }
    public event Action<int> OnShieldChanged;

    public bool IsDead => life <= 0;

    public PlayerState(int maxLife, int maxHp)
    {
        this.maxLife = maxLife;
        this.maxHp = maxHp;
        this.life = maxLife;
        this.hp = maxHp;
        this.shield = 0;
    }
}
