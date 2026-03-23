lua_card = {
    name = "静谧之力",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return ctx.CalmPoint > 0
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local damage = ctx.CalmPoint
    ctx:DealDamageToEnemy(damage)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成等同于本局对战中已生成寂静点数的动摇值。"
end
