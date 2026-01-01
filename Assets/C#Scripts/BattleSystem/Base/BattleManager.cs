using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : Singleton<BattleManager>
{
    private int turn = 0;
    public int Turn
    {
        get => turn;
        set => turn = value;
    }

    public void StartBattle()
    {
        OnTurnStart();
    }

    public void OnTurnStart()
    {
        Turn++;
        BattleEventManager.Instance.OnTurnStart.Invoke(new TurnEventArgs(Turn));
    }

    public void OnTurnEnd()
    {
        BattleEventManager.Instance.OnTurnEnd.Invoke(new TurnEventArgs(Turn));
    }


}
