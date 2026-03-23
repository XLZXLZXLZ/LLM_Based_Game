lua_card = {
    name = "思绪涌现",
    cost_type = CostType.Calm,
    cost_value = 3,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DrawCards(3)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽取3张牌。"
end
