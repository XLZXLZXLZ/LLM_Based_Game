lua_card = {
    name = "信念之光",
    cost_type = CostType.Anger,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local damage = 3
    ctx:DealDamageToEnemy(damage)
    ctx:GainShield(damage)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成3动摇，获得等同于最终造成的信念。"
end
