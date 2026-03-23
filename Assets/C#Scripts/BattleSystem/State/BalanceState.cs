using System;
using UnityEngine;

public class BalanceState
{
    private int angerPoint;
    public int AngerPoint
    {
        get => angerPoint;
        set
        {
            if (angerPoint == value) return;
            angerPoint = value;
            OnAngerChanged?.Invoke(angerPoint);
        }
    }
    public event Action<int> OnAngerChanged;

    private int calmPoint;
    public int CalmPoint
    {
        get => calmPoint;
        set
        {
            if (calmPoint == value) return;
            calmPoint = value;
            OnCalmChanged?.Invoke(calmPoint);
        }
    }
    public event Action<int> OnCalmChanged;

    public int MaxDifference { get; }

    public int Difference => Mathf.Abs(angerPoint - calmPoint);

    public bool IsOverflow => Difference > MaxDifference;

    public BalanceState(int maxDifference = 5)
    {
        MaxDifference = maxDifference;
        angerPoint = 0;
        calmPoint = 0;
    }
}
