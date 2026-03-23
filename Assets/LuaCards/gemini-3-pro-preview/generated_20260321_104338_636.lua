local calm_cards_played_this_turn = 0

lua_card = {
    name = "反响",
    cost_type = CostType.Anger,
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterCardEventHandler("OnTurnStart", "after", function(ctx_cb, args)
        calm_cards_played_this_turn = 0
    end)
    ctx:RegisterCardEventHandler("OnCardPlayed", "after", function(ctx_cb, args)
        if args.CostSide == CostType.Calm then
            calm_cards_played_this_turn = calm_cards_played_this_turn + 1
        end
    end)
end

function on_play(ctx, data)
    local damage = 5 * calm_cards_played_this_turn
    if damage > 0 then
        ctx:DealDamageToEnemy(damage)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    local damage = 5 * calm_cards_played_this_turn
    return string.format("本回合中，你每已打出一张费用为寂静的卡牌，则造成5点动摇。\n（当前已打出%d张，将造成%d点动摇）", calm_cards_played_this_turn, damage)
end
