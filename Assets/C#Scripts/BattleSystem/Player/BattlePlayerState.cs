using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class PlayerState
{
    private int maxLife;
    public int MaxLife
    {
        get => maxLife;
        set
        {
            if (maxLife == value) return;
            OnMaxLifeChanged?.Invoke(value);
            maxLife = value;
        }
    }
    public event Action<int> OnMaxLifeChanged;

    private int maxHp;
    public int MaxHp
    {
        get => maxHp;
        set
        {
            if (maxHp == value) return;
            OnMaxHpChanged?.Invoke(value);
            maxHp = value;
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
            OnLifeChanged?.Invoke(value);
            life = value;
        }
    }
    public event Action<int> OnLifeChanged;

    private int hp;
    public int Hp
    {
        get => hp;
        set
        {
            if (hp == value) return;
            OnHpChanged?.Invoke(value);
            hp = value;
        }
    }
    public event Action<int> OnHpChanged;

    public PlayerState(int life, int hp)
    {
        this.life = life;
        this.hp = hp;
    }
}
public class BattlePlayerState : Singleton<BattlePlayerState>
{
    private PlayerState state;
    public PlayerState State
    {
        get => state;
        set => state = value;
    }

    public void Initialize(int life, int hp)
    {
        state = new PlayerState(life, hp);
    }

    public void TakeDamage(int damage, object source = null)
    {
        // 触发受伤事件
        var args = new PlayerDamagedEventArgs(damage, source);
        BattleEventManager.Instance.OnPlayerDamaged.Invoke(args);
        
        if (args.IsCancelled) return;
        
        // 使用可能被修改后的伤害值
        state.Hp -= args.Damage;
        if (state.Hp <= 0)
        {
            LoseLife();
        }
    }

    public void Heal(int amount, object source = null)
    {
        int actualHeal = Mathf.Min(amount, state.MaxHp - state.Hp);
        if (actualHeal <= 0) return;
        
        // 触发回血事件
        var args = new PlayerHealedEventArgs(actualHeal, source);
        BattleEventManager.Instance.OnPlayerHealed.Invoke(args);
        
        if (args.IsCancelled) return;
        
        state.Hp = Mathf.Min(state.Hp + args.HealAmount, state.MaxHp);
    }

    public void LoseLife(object source = null)
    {
        // 触发丢命事件
        var args = new PlayerLifeLostEventArgs(source);
        BattleEventManager.Instance.OnPlayerLifeLost.Invoke(args);
        
        if (args.IsCancelled) return;
        
        state.Life--;
        if (state.Life <= 0)
        {
            //游戏失败
            return;
        }
        state.Hp = state.MaxHp;
    }

    public void GainLife(int amount = 1, object source = null)
    {
        // 触发加命事件
        var args = new PlayerLifeGainedEventArgs(amount, source);
        BattleEventManager.Instance.OnPlayerLifeGained.Invoke(args);
        
        if (args.IsCancelled) return;
        
        state.Life = Mathf.Min(state.Life + args.Amount, state.MaxLife);
    }
}
