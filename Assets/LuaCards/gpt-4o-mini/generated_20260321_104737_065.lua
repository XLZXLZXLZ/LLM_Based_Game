lua_card = {
    name = "成长",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("growth_buff", "成长", "每回合开始时，获得1点动摇。", 5, "stack", "turn_based", 1)
end

function on_play(ctx, data)
    ctx:ApplyBuff("growth_buff", 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "获得1点动摇，最多叠加5层，每回合开始时触发。"
end
