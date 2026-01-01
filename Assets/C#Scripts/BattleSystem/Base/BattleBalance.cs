using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleBalance : Singleton<BattleBalance>
{
    //最大差值
    [SerializeField]
    private int maxDifference = 5;
    public int MaxDifference => maxDifference;
    private int angerPoint;
    public int AngerPoint
    {
        get => angerPoint;
        set
        {
            angerPoint = value;
            CheckBalance();
        }
    }

    private int calmPoint;
    public int CalmPoint
    {
        get => calmPoint;
        set 
        {
            calmPoint = value;
            CheckBalance();
        }
    }

    public void AdjustBalance(CostType costType, int value)
    {
        if (costType == CostType.Anger)
        {
            AngerPoint += value;
        }
        else if (costType == CostType.Calm)
        {
            CalmPoint += value;
        }
    }

    public void CheckBalance()
    {
        if (Mathf.Abs(AngerPoint - CalmPoint) > maxDifference)
        {
            //受到致死伤害扣掉一血
            BattlePlayerState.Instance.TakeDamage(99999);
        }
    }
}
