lua_card = {
    name = "代价",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DrawCards(2)
    ctx:DealDamageToPlayer(3)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽2张牌，随后自身受到3动摇。"
end
