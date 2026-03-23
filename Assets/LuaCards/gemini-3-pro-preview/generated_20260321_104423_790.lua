lua_card = {
    name = "冥想",
    cost_type = CostType.Calm,
    cost_value = 5,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("buff_no_draw", "冥想", "本回合内无法再抽取卡牌", 1, "refresh", "turn_based", 1)
    ctx:RegisterBuffHandler("buff_no_draw", "OnCardDrawn", "before", function(buff, ctx, args)
        if args.Cancel then
            args:Cancel()
        end
    end)
end

function on_play(ctx, data)
    ctx:DrawCards(5)
    ctx:ApplyBuff("buff_no_draw", 1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽5张牌。本回合内无法再抽取卡牌。"
end
