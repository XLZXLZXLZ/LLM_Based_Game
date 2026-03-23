lua_card = {
    name = "潜心",
    cost_type = CostType.Calm,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterCardEventHandler("OnCardPlayed", "after", function(ctx, args)
        if args.CostSide == CostType.Calm then
            ctx:DealDamageToEnemy(5)
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
    return "本回合中，你每已打出一张费用为寂静的卡牌，则造成5点动摇。"
end
