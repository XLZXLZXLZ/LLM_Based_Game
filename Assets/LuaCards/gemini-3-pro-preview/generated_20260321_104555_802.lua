lua_card = {
    name = "深思",
    cost_type = CostType.Calm,
    cost_value = 3,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff(
        "buff_deep_thought",
        "深思",
        "每回合开始时，抽1张牌。",
        99,
        "stack",
        "turn_based",
        3
    )
    ctx:RegisterBuffHandler("buff_deep_thought", "OnTurnStart", "resolve", function(buff, ctx, args)
        ctx:DrawCards(buff.Stacks)
    end)
end

function on_play(ctx, data)
    ctx:ApplyBuff("buff_deep_thought", 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "获得持续3回合的【深思】：每回合开始时，抽1张牌。"
end
