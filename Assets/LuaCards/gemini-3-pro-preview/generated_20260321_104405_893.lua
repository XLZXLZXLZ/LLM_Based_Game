local total_calm_generated = 0
local initialized = false

lua_card = {
    name = "积淀",
    cost_type = CostType.Anger,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    if not initialized then
        ctx:RegisterCardEventHandler("OnBalanceChanged", "after", function(c, args)
            local diff = args.CurrentCalm - args.PreviousCalm
            if diff > 0 then
                total_calm_generated = total_calm_generated + diff
            end
        end)
        initialized = true
    end
end

function on_play(ctx, data)
    if total_calm_generated > 0 then
        ctx:DealDamageToEnemy(total_calm_generated)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return string.format("造成等同于本局对战中累计生成的寂静点数的动摇（当前：%d）。", total_calm_generated)
end
