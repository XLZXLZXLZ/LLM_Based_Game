-- 均衡之击
lua_card = {
    name = "均衡之击",
    cost_type = CostType.Calm,
    cost_value = 2,
}

-- 每当对敌人造成伤害时：
--   寂静 > 狂热 → 狂热+1，伤害+3
--   狂热 > 寂静 → 寂静+1，获得3信念
-- 触发3次后消失

function on_play(ctx, card)
    ctx:RegisterBuff(
        "balance_strike",   -- id
        "均衡之击",          -- 名称
        "对敌造成动摇时根据天平状态触发效果（剩余{stacks}次）", -- 描述
        3,                  -- maxStacks
        "stack",            -- stackMode
        "permanent",        -- durationType
        0                   -- duration
    )

    ctx:RegisterBuffHandler("balance_strike", "OnDamageDealt", "before",
        function(buff, ctx, args)
            if args.Target ~= DamageTarget.Enemy then return end

            if ctx.CalmPoint > ctx.AngerPoint then
                ctx:AdjustBalance("anger", 1)
                args.Amount = args.Amount + 3
            elseif ctx.AngerPoint > ctx.CalmPoint then
                ctx:AdjustBalance("calm", 1)
                ctx:GainShield(3)
            end

            ctx:ModifyBuffStacks("balance_strike", -1)
        end
    )

    ctx:ApplyBuff("balance_strike", 3)
end

function can_play(ctx, card)
    return true
end

function get_description(ctx, card)
    local stacks = ctx:GetBuffStacks("balance_strike")
    if stacks > 0 then
        return "均衡之击（剩余" .. stacks .. "次）\n对敌造成动摇时：寂静>狂热则狂热+1且伤害+3；狂热>寂静则寂静+1且获得3信念"
    end
    return "均衡之击\n对敌造成动摇时：寂静>狂热则狂热+1且伤害+3；狂热>寂静则寂静+1且获得3信念。触发3次后消失"
end
