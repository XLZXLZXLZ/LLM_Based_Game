lua_card = {
    name = "游说",
    cost_type = "Anger",
    cost_value = 2,
}

function can_play(ctx, data)
    return true
end

function on_initialize(ctx, data)
    ctx:RegisterBuff("buff_persuasion", "游说", "每回合结束时，对敌人造成等同于层数的动摇，随后降低1层。", 99, "stack", "permanent", 0)
    ctx:RegisterBuffHandler("buff_persuasion", "OnTurnEnd", "resolve", function(buff, ctx, args)
        local stacks = buff.Stacks
        if stacks > 0 then
            ctx:DealDamageToEnemy(stacks)
            ctx:ModifyBuffStacks("buff_persuasion", -1)
        end
    end)
end

function on_play(ctx, data)
    ctx:ApplyBuff("buff_persuasion", 5)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "获得5层游说。\n（游说：每回合结束时，对敌人造成等同于层数的动摇，随后降低1层）"
end
