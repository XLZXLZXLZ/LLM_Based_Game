lua_card = {
    name = "执念",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("buff_zhinian_growth", "执念", "执念额外动摇", 999, "stack", "permanent", 0)
end

function on_play(ctx, data)
    local extra = 0
    if ctx:HasBuff("buff_zhinian_growth") then
        extra = ctx:GetBuffStacks("buff_zhinian_growth") * 3
    end
    local dmg = 4 + extra
    ctx:DealDamageToEnemy(dmg)
    ctx:ApplyBuff("buff_zhinian_growth", 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    local extra = 0
    if ctx:HasBuff("buff_zhinian_growth") then
        extra = ctx:GetBuffStacks("buff_zhinian_growth") * 3
    end
    local dmg = 4 + extra
    return string.format("造成%d动摇。每次打出此牌，其动摇伤害增加3点。", dmg)
end
