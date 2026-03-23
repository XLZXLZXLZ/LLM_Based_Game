lua_card = {
    name = "游说",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("persuasion", "游说", "每回合对对手造成等同于层数的动摇，随后降低1层", 5, "stack", "turn_based", 1)
end

function on_play(ctx, data)
    ctx:ApplyBuff("persuasion", 5)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "对自身施加5层游说，游说每回合对对手造成等同于层数的动摇，随后降低1层。"
end
