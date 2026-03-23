lua_card = {
    name = "偏执",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local amount = ctx.AngerPoint
    if amount > 0 then
        ctx:DealDamageToEnemy(amount)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成与当前狂热点数等同的动摇。\n(当前：" .. ctx.AngerPoint .. "动摇)"
end
