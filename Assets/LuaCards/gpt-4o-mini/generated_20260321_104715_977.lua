lua_card = {
    name = "封闭的思绪",
    cost_type = CostType.Calm,
    cost_value = 3,
}

function can_play(ctx, data)
    return ctx.HandCount < 5
end

function on_initialize(ctx, data)
end

function on_play(ctx, data)
    ctx:DrawCards(5)
    ctx:RegisterCardEventHandler("OnCardDrawn", "before", function(ctx, args)
        args:Cancel()
    end)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽5张牌，本回合内不能再抽取卡牌。"
end
