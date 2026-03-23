lua_card = {
    name = "狂热之力",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return ctx.AngerPoint > 0
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DealDamageToEnemy(ctx.AngerPoint)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "当前每有1狂热值，造成1动摇。"
end
