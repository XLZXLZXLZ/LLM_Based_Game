lua_card = {
    name = "余音",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return not ctx:HasBuff("afterecho")
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("afterecho", "余音", "每回合开始时，造成3动摇", 5, "stack", "permanent", 0)
    ctx:RegisterBuffHandler("afterecho", "OnTurnStart", "resolve", function(buff, ctx, args)
        ctx:DealDamageToEnemy(3 * buff.Stacks)
    end)
end

function on_play(ctx, data)
    ctx:ApplyBuff("afterecho", 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    local stacks = ctx:GetBuffStacks("afterecho")
    if stacks > 0 then
        return "获得1层「余音」。（当前" .. stacks .. "层）\n余音：每回合开始时，每层造成3动摇。\n不可重复打出。"
    end
    return "获得1层「余音」。\n余音：每回合开始时，每层造成3动摇。\n不可重复打出。"
end
