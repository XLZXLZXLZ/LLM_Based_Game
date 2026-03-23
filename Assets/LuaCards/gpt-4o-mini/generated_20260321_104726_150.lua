lua_card = {
    name = "平衡",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return ctx.AngerPoint - ctx.CalmPoint < 5 and ctx.CalmPoint - ctx.AngerPoint < 5
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local difference = ctx.AngerPoint - ctx.CalmPoint
    if difference > 0 then
        ctx:AdjustBalance("calm", difference)
    elseif difference < 0 then
        ctx:AdjustBalance("anger", -difference)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "调整天平，使狂热与寂静的差值归零。"
end
