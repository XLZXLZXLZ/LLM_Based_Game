lua_card = {
    name = "以痛为铭",
    cost_type = CostType.Calm,
    cost_value = 3,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    local buff_id = "yi_tong_wei_ming_heal_on_hit"
    ctx:RegisterBuff(buff_id, "以痛为铭", "受到动摇时，回复等量意志", 1, "ignore", "permanent", 0)
    ctx:RegisterBuffHandler(buff_id, "OnDamageDealt", "after", function(buff, bctx, args)
        if args.Target == DamageTarget.Player and args.Amount > 0 then
            bctx:HealPlayer(args.Amount)
        end
    end)
    ctx:ApplyBuff(buff_id, 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "获得永久效果：每次受到动摇时，立刻回复等量的意志。"
end
