lua_card = {
    name = "调和",
    cost_type = CostType.Calm,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DrawCards(1)
    local anger = ctx.AngerPoint
    local calm = ctx.CalmPoint
    if anger > calm then
        ctx:AdjustBalance("calm", anger - calm)
    elseif calm > anger then
        ctx:AdjustBalance("anger", calm - anger)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽1张牌。增加较低一侧的天平点数，使其与较高一侧相等。"
end
