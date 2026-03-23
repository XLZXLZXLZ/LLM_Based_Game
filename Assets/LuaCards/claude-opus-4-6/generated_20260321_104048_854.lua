lua_card = {
    name = "闭目沉思",
    cost_type = CostType.Calm,
    cost_value = 2,
}

local draw_blocked = false

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterCardEventHandler("OnTurnStart", "before", function(ctx, args)
        draw_blocked = false
    end)

    ctx:RegisterCardEventHandler("OnCardDrawn", "before", function(ctx, args)
        if draw_blocked then
            args:Cancel()
        end
    end)
end

function on_play(ctx, data)
    draw_blocked = false
    ctx:DrawCards(5)
    draw_blocked = true
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "抽5张牌。本回合内不能再抽取卡牌。"
end
