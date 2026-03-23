lua_card = {
    name = "回响",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local hp_before = ctx.EnemyHp
    ctx:DealDamageToEnemy(3)
    local hp_after = ctx.EnemyHp
    local actual_damage = hp_before - hp_after
    
    if actual_damage > 0 then
        ctx:GainShield(actual_damage)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成3动摇，获得等同于实际造成动摇的信念"
end
