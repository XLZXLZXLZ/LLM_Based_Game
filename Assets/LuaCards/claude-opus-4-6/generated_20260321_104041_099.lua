lua_card = {
    name = "寂照",
    cost_type = CostType.Calm,
    cost_value = 2,
}

local total_calm_generated = 0

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterCardEventHandler("OnBalanceChanged", "after", function(c, args)
        if args.Side == CostType.Calm and args.Delta > 0 then
            total_calm_generated = total_calm_generated + args.Delta
        end
    end)
end

function on_play(ctx, data)
    if total_calm_generated > 0 then
        ctx:DealDamageToEnemy(total_calm_generated)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "造成等同于本局已累计生成寂静点数的动摇。\n当前累计寂静：" .. total_calm_generated .. "。"
end
