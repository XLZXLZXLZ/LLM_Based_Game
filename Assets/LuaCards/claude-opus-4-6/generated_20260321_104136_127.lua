lua_card = {
    name = "渐悟",
    cost_type = CostType.Calm,
    cost_value = 1,
}

function on_initialize(ctx, data)
    ctx:RegisterBuff("jianwu_growth", "渐悟", "每次打出卡牌时，渐悟的伤害永久+1", 99, "stack", "permanent", 0)

    ctx:RegisterBuffHandler("jianwu_growth", "OnDamageDealt", "after", function(buff, bctx, args)
        if args.Target == DamageTarget.Enemy then
        end
    end)

    ctx:RegisterCardEventHandler("OnCardPlayed", "after", function(bctx, args)
        if bctx:HasBuff("jianwu_growth") then
            bctx:ModifyBuffStacks("jianwu_growth", 1)
        end
    end)
end

function on_play(ctx, data)
    if not ctx:HasBuff("jianwu_growth") then
        ctx:ApplyBuff("jianwu_growth", 0)
    end
    local stacks = ctx:GetBuffStacks("jianwu_growth")
    local baseDamage = 3
    ctx:DealDamageToEnemy(baseDamage + stacks)
end

function can_play(ctx, data)
    return true
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    local stacks = 0
    if ctx:HasBuff("jianwu_growth") then
        stacks = ctx:GetBuffStacks("jianwu_growth")
    end
    local total = 3 + stacks
    return "造成" .. total .. "点动摇。\n每打出一张卡牌，本牌伤害永久+1。"
end
