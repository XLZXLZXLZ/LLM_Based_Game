using System.Collections.Generic;

/// <summary>
/// Buff 全局注册表：存储 Buff 定义与行为的映射。
/// C# 预定义 Buff 和 Lua 动态 Buff 共用同一注册表。
/// </summary>
public static class BuffRegistry
{
    private static readonly Dictionary<string, (BuffDefinition def, IBuffBehavior behavior)> registry = new();

    public static void Register(string id, BuffDefinition def, IBuffBehavior behavior)
    {
        registry[id] = (def, behavior);
    }

    public static bool TryGet(string id, out BuffDefinition def, out IBuffBehavior behavior)
    {
        if (registry.TryGetValue(id, out var entry))
        {
            def = entry.def;
            behavior = entry.behavior;
            return true;
        }
        def = null;
        behavior = null;
        return false;
    }

    public static bool IsRegistered(string id) => registry.ContainsKey(id);

    public static void Clear() => registry.Clear();
}
