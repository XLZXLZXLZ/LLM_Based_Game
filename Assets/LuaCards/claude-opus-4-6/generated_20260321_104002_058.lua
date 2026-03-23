lua_card = {
    name = "狂澜",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DealDamageToEnemy(5)
    if ctx.AngerPoint > 2 then
        ctx:DrawCards(1)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成5动摇。若当前狂热值>2，则抽1张牌。"
end
