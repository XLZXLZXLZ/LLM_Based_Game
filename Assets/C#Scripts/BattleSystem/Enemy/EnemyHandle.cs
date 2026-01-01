using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHandle : MonoBehaviour
{
    private Enemy[] enemySlots = new Enemy[4];

    private void Start()
    {
        //玩家回合结束，代表敌人回合开始
        BattleEventManager.Instance.OnTurnEnd.Register(EventPhase.After, OnEnemyAction, EventPriority.Normal, this);
    }

    public void OnEnemyAction(TurnEventArgs args)
    {
        for (int i = 0; i < enemySlots.Length; i++)
        {
            if (enemySlots[i] != null)
            {
                enemySlots[i].OnEnemyTurn();
            }
        }
    }

    public void AddEnemy(Enemy enemyPrefab, int slotIndex)
    {
        enemySlots[slotIndex] = Instantiate(enemyPrefab); 
        enemySlots[slotIndex].Initialize(enemyPrefab.info, slotIndex, this);
    }

    public void RemoveEnemy(int slotIndex)
    {
        enemySlots[slotIndex] = null;
    }
}
