lua_card = {
    name = "动摇与平衡",
    cost_type = CostType.Anger,
    cost_value = 3,
}

function can_play(ctx, data)
    return ctx.AngerPoint >= 3
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DealDamageToEnemy(4)
    ctx:AdjustBalance("anger", -1)
    ctx:AdjustBalance("calm", -1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成4动摇，随后所有狂热和寂静卡牌费用点数-1。"
end
