lua_card = {
    name = "怒焰回响",
    cost_type = CostType.Calm,
    cost_value = 1,
}

function can_play(ctx, data)
    return ctx.AngerPoint > 0
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local damage = ctx.AngerPoint
    if damage > 0 then
        ctx:DealDamageToEnemy(damage)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    local current = ctx.AngerPoint
    return "当前每有1点狂热值，造成1动摇。\n当前造成" .. current .. "动摇。"
end
