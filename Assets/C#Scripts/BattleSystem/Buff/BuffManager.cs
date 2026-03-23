using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 管理器：负责 Buff 的施加、叠加、移除和回合递减。
/// 通过 BattleContext.Buffs 访问。
/// </summary>
public class BuffManager
{
    private readonly Dictionary<string, BuffInstance> activeBuffs = new();
    private readonly BattleContext ctx;

    public event Action<BuffInstance> OnBuffApplied;
    public event Action<BuffInstance> OnBuffRemoved;
    public event Action<BuffInstance, int> OnBuffStacksChanged;

    public BuffManager(BattleContext ctx)
    {
        this.ctx = ctx;
    }

    public void ApplyBuff(string buffId, int stacks = 1, object source = null)
    {
        if (!BuffRegistry.TryGet(buffId, out var def, out var behavior))
        {
            Debug.LogWarning($"[BuffManager] Buff未注册: {buffId}");
            return;
        }

        if (activeBuffs.TryGetValue(buffId, out var existing))
        {
            int oldStacks = existing.Stacks;
            switch (def.StackMode)
            {
                case BuffStackMode.Stack:
                    existing.Stacks += stacks;
                    if (def.MaxStacks > 0)
                        existing.Stacks = Mathf.Min(existing.Stacks, def.MaxStacks);
                    break;
                case BuffStackMode.KeepHigher:
                    existing.Stacks = Mathf.Max(existing.Stacks, stacks);
                    break;
                case BuffStackMode.Refresh:
                    existing.RemainingTurns = def.DefaultDuration;
                    break;
                case BuffStackMode.Ignore:
                    return;
            }
            if (existing.Stacks != oldStacks)
            {
                behavior.OnStackChanged(existing, ctx, oldStacks);
                OnBuffStacksChanged?.Invoke(existing, oldStacks);
            }
        }
        else
        {
            var instance = new BuffInstance
            {
                Definition = def,
                Stacks = stacks,
                RemainingTurns = def.DefaultDuration,
                Source = source
            };
            if (def.MaxStacks > 0)
                instance.Stacks = Mathf.Min(instance.Stacks, def.MaxStacks);

            activeBuffs[buffId] = instance;
            behavior.OnApply(instance, ctx);
            OnBuffApplied?.Invoke(instance);
        }
    }

    public void RemoveBuff(string buffId)
    {
        if (!activeBuffs.TryGetValue(buffId, out var instance))
            return;

        activeBuffs.Remove(buffId);

        if (BuffRegistry.TryGet(buffId, out _, out var behavior))
            behavior.OnRemove(instance, ctx);

        OnBuffRemoved?.Invoke(instance);
    }

    public void ModifyStacks(string buffId, int delta)
    {
        if (!activeBuffs.TryGetValue(buffId, out var instance))
            return;

        int oldStacks = instance.Stacks;
        instance.Stacks += delta;

        if (instance.Definition.MaxStacks > 0)
            instance.Stacks = Mathf.Min(instance.Stacks, instance.Definition.MaxStacks);

        if (instance.Stacks <= 0)
        {
            RemoveBuff(buffId);
            return;
        }

        if (BuffRegistry.TryGet(buffId, out _, out var behavior))
            behavior.OnStackChanged(instance, ctx, oldStacks);

        OnBuffStacksChanged?.Invoke(instance, oldStacks);
    }

    public bool HasBuff(string buffId) => activeBuffs.ContainsKey(buffId);
    public int GetStacks(string buffId) => activeBuffs.TryGetValue(buffId, out var b) ? b.Stacks : 0;
    public IReadOnlyCollection<BuffInstance> GetAllBuffs() => activeBuffs.Values;

    /// <summary>
    /// 回合结束时递减所有 TurnBased Buff 的剩余回合数，到期自动移除。
    /// </summary>
    public void TickTurnEnd()
    {
        var toRemove = new List<string>();
        foreach (var kvp in activeBuffs)
        {
            if (kvp.Value.Definition.DurationType != BuffDurationType.TurnBased)
                continue;
            kvp.Value.RemainingTurns--;
            if (kvp.Value.RemainingTurns <= 0)
                toRemove.Add(kvp.Key);
        }
        foreach (var id in toRemove)
            RemoveBuff(id);
    }

    public void RemoveAll()
    {
        var ids = new List<string>(activeBuffs.Keys);
        foreach (var id in ids)
            RemoveBuff(id);
    }
}
