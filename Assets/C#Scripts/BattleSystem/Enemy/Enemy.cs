using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class EnemyInfo
{
    public string enemyName;
    public int enemyMaxHp;
    public int enemyAttack;

    public int enemyMaxTimer;
}

public class EnemyState
{
    private int hp;
    public int Hp
    {
        get { return hp; }
        set 
        { 
            if(hp == value) return;
            OnHpChanged?.Invoke(value);
            hp = value;  
        }
    }
    public event Action<int> OnHpChanged;
    private int attack;
    public int Attack
    {
        get { return attack; }
        set 
        {
            if(attack == value) return;
            OnAttackChanged?.Invoke(value);
            attack = value;
        }
    }
    public event Action<int> OnAttackChanged;

    private int timer;
    public int Timer
    {
        get { return timer; }
        set 
        {
            if(timer == value) return;
            OnTimerChanged?.Invoke(value);
            timer = value;
        }
    }
    public event Action<int> OnTimerChanged;

    public EnemyState(int hp, int attack, int timer)
    {
        this.hp = hp;
        this.attack = attack;
        this.timer = timer;
    }

}
public class Enemy : MonoBehaviour
{
    public EnemyInfo info;

    public EnemyState state;

    public int slotIndex;

    private EnemyHandle enemyHandle;

    public virtual void Initialize(EnemyInfo enemyInfo, int slotIndex, EnemyHandle enemyHandle)
    {
        this.info = enemyInfo;
        this.state = new EnemyState(enemyInfo.enemyMaxHp, enemyInfo.enemyAttack, enemyInfo.enemyMaxTimer);
        this.slotIndex = slotIndex;
        this.enemyHandle = enemyHandle;
    }

    public virtual void OnEnemyTurn()
    {
        state.Timer--;
        if (state.Timer <= 0)
        {
            EnemyAction();
            state.Timer = info.enemyMaxTimer;
        }
    }

    public virtual void EnemyAction()
    {
        //默认回合效果：攻击玩家
        Attack(state.Attack);
    }

    public virtual void Attack(int attack)
    {
        //默认攻击效果
    }

    public virtual void TakeDamage(int damage)
    {
        //默认受伤效果
        state.Hp -= damage;
        if (state.Hp <= 0)
        {
            //死亡
            Die();
        }
    }

    public virtual void Die()
    {
        //默认死亡效果
        enemyHandle.RemoveEnemy(slotIndex);
    }
}
