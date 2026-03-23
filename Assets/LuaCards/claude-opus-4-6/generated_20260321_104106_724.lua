lua_card = {
    name = "中庸",
    cost_type = CostType.Calm,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
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
    return "将天平两侧点数调整至相等，恢复平衡。"
end
