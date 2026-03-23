-- Lua 卡牌必须在脚本中声明 lua_card（展示用：名称、费用侧、费用值），并与 get_description 一致。
lua_card = {
    name = "Lua 测试卡",
    cost_type = CostType.Anger,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_play(ctx, data)
    ctx:DealDamageToEnemy(5)
    ctx:DrawCards(1)
end

function get_description(ctx, data)
    return "造成5点动摇，抽1张牌。"
end
