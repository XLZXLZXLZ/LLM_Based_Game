lua_card = {
    name = "反弹",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("bounce", "反弹", "每次受到动摇时，恢复等量的意志。", 1, "refresh", "permanent", 0)
    ctx:RegisterBuffHandler("bounce", "OnDamageDealt", "after", function(buff, ctx, args)
        if args.Target == DamageTarget.Player and args.Amount > 0 then
            ctx:HealPlayer(args.Amount)
        end
    end)
end

function on_play(ctx, data)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "每次受到动摇时，立刻回复等量的意志。"
end
