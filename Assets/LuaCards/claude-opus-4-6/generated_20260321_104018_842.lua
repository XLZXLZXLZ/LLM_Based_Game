lua_card = {
    name = "振聋发聩",
    cost_type = CostType.Anger,
    cost_value = 5,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DealDamageToEnemy(30)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成30点动摇。"
end
