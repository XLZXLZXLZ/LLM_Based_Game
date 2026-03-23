using System;

public class EnemyState
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

    public int MaxHp { get; }

    private int attack;
    public int Attack
    {
        get => attack;
        set
        {
            if (attack == value) return;
            attack = value;
            OnAttackChanged?.Invoke(attack);
        }
    }
    public event Action<int> OnAttackChanged;

    private int timer;
    public int Timer
    {
        get => timer;
        set
        {
            if (timer == value) return;
            timer = value;
            OnTimerChanged?.Invoke(timer);
        }
    }
    public event Action<int> OnTimerChanged;

    public int MaxTimer { get; }

    public bool IsDead => hp <= 0;

    public EnemyState(int maxHp, int attack, int maxTimer)
    {
        MaxHp = maxHp;
        this.hp = maxHp;
        this.attack = attack;
        MaxTimer = maxTimer;
        this.timer = maxTimer;
    }
}
