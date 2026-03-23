lua_card = {
    name = "寂灭回响",
    cost_type = CostType.Calm,
    cost_value = 2,
}

local calm_cards_played = 0

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterCardEventHandler("OnTurnStart", "resolve", function(c, args)
        calm_cards_played = 0
    end)
    ctx:RegisterCardEventHandler("OnCardPlayed", "after", function(c, args)
        if args.CostSide == CostType.Calm then
            calm_cards_played = calm_cards_played + 1
        end
    end)
end

function on_play(ctx, data)
    local hits = calm_cards_played - 1
    if hits < 0 then
        hits = 0
    end
    local total = hits * 5
    if total > 0 then
        ctx:DealDamageToEnemy(total)
    end
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "本回合中，你每已打出一张费用为寂静的卡牌，造成5动摇。（不含本牌自身）"
end
