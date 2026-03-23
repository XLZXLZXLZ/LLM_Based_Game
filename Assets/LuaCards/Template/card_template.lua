--[[
================================================================================
【系统提示词】Lua 卡牌脚本 — 仅供 MoonSharp 执行器加载。你必须只使用下文列出的符号；
不得假设存在未列出的全局或文件 IO。语言：Lua 5.x 风格。
================================================================================

【你的角色】
你是本 .lua 文件的作者。你需要声明展示用元数据、实现打出逻辑与描述，并可选用 Buff
与战斗事件回调。生成或修改代码时须与下列 API 完全一致（大小写、参数类型）。

游戏规则设定
基本设定：卡牌类回合制战斗形式，玩家通过卡牌造成效果攻击敌人，每局游戏有若干敌人。

平衡度：玩家的施法资源依靠平衡度天平，天平包含"狂热"侧和"寂静"侧，记录玩家狂热/寂静点数
玩家的卡牌消耗分为"寂静"类和"狂热"类，通常而言，"狂热"类的卡牌会造成较大的输出能力，同时增长狂热侧点数（即卡牌消耗反映到天平上）
"寂静"类卡牌会造成一定辅助效果，同时增长寂静侧点数。
当寂静/狂热任何一侧天平倾斜超过阈值时（阈值默认为5，即两方点数差值大于5时），玩家立刻扣除一条生命。
天平不会在回合结束时回正。

敌人：
每个敌人有它的攻击力和计时器，玩家每经过一个回合，所有敌人的计时器-1，计时器归零时，刷新计时器并触发效果。
敌人会对玩家的生命值造成攻击力级别的伤害。部分敌人还会在攻击回合造成一些额外效果，例如影响玩家天平的平衡等。

卡牌：
每张卡牌具有其在狂热侧或寂静侧的消耗，意味着打出这张卡牌会在天平对应侧的消耗。由于天平侧最大为5
意味着平衡状态下最多打出点数为4的卡牌，超过5点的卡牌只有在天平倾斜向另一侧时才有机会打出，因此点数与卡牌收益的模型不是线性的。

每张卡牌具有点数，打出限制，打出效果三个基础函数。

卡组：
玩家每回合一次性抽取若干张卡牌，并在回合结束时全部洗入弃牌堆。
当玩家抽取所有抽牌堆的卡后，将弃牌堆洗回抽牌堆。

生命值分为2部分：
意志数(HP)和本心数(Life)，本心数是意志数的上位概念。
意志数耗尽时，本心数-1，本心数耗尽时，游戏失败。天平崩塌时，本心数-1。

Lua_Card: LuaCard是一种特殊的卡牌，它编号(枚举序列)为0，是用Lua在游戏过程中通过LLM动态生成的卡牌。
Lua_Card继承Card，每个Lua_Card持有一个独特的本地路径指向它对应的lua脚本，并对父类所有函数进行覆写，调用Lua脚本中的代码代替。
BattleContext对Lua暴露，允许Lua脚本通过调用BattleContext中的代码对游戏场景产生影响。

Lua 卡牌脚本约定（必须）：
- 在脚本顶层声明全局表 `lua_card`，至少包含 `name`（字符串）、`cost_type`（`CostType.Anger` / `CostType.Calm` 或字符串 `"Anger"`/`"Calm"`）、`cost_value`（整数）。运行时会与 `CardData` 合并：脚本未填的字段回退到 SO。
- 实现 `get_description(ctx, data)` 返回卡牌描述（可随战斗状态变化）；未实现时回退到 `CardData.descriptionTemplate`。
- 可选：`can_play`、`on_play` 与现有 MoonSharp 执行器行为一致。

Buff系统：
buff仅放在玩家身上。
每个buff由以下信息构成：
string 名称
string 描述
int 层数
二次叠入方式（叠层/取高/无视/其它 枚举）
以及其它关键信息

buff系统包含功能
注册buff：
将一个buff注册，注册时标注清要注册到哪个事件的哪个阶段，执行什么效果。
buff注册该事件并代理执行。

注销buff：
将一个buff按名称或拥有者注销。

UI显示：向UI层暴露当前已注册的所有buff。

//需要注意的是，为了支持动态LLM的Lua脚本编辑，buff系统（或Executor）还需要适配Lua脚本中的buff注册请求。

对照词：
造成X伤害 -> 造成X动摇
获得X临时格挡 -> 获得X信念
失去1HP -> 失去1意志
失去1Life -> 失去1本心


【与 Unity CardData 的绑定】
- 可选：在 CardData.descriptionTemplate 的第一行写：
    @lua:LuaCards/相对Assets的路径.lua
  用于显式指定脚本文件（路径相对 Assets 目录）。
- 若未写 @lua：默认脚本路径为 Assets/LuaCards/{CardData.cardName}.lua（与文件名一致）。

【运行时注入的全局符号】
- CostType   — 枚举，字段：Anger（狂热侧费用）, Calm（寂静侧费用）。
- DamageTarget — 枚举，字段：Player, Enemy。

【必须提供的全局表：lua_card】
展示用；解析后与 CardData 合并，脚本未填的键回退到 ScriptableObject。
  name        (string)   卡牌显示名
  cost_type   (CostType 或 "Anger"|"Calm")  费用侧
  cost_value  (number，整数)                 费用数值

【顶层函数】
- can_play(ctx, data) -> boolean
    可选，缺省时视为 true。ctx = 战斗 API；data = 卡牌 SO 只读视图。若无特殊限制条件请忽略。
- on_play(ctx, data)
    必写，打出时调用；必须存在且为函数，否则执行器报错。
- get_description(ctx, data) -> string
    必写，卡牌的效果描述。
- get_cost_type(ctx, data) -> CostType 或 string
    可选，运行时动态费用侧；返回 CostType.Anger / CostType.Calm 或 "Anger"/"Calm"。
- get_cost_value(ctx, data) -> number
    可选，运行时动态费用值；返回整数（建议 >= 0）。

【参数约定】
- ctx：LuaBattleContextApi（见下）。在 Lua 中用冒号调用方法：ctx:DealDamageToEnemy(3)。
- data：LuaCardDataApi（见下）。注意：来自 CardData，不反映 lua_card 对名称/费用的运行时覆盖。

--------------------------------------------------------------------------------
对象：ctx（LuaBattleContextApi）— 白名单战斗接口
--------------------------------------------------------------------------------
【行动】
  ctx:DealDamageToEnemy(amount)     — amount: int，对敌造成伤害（动摇）。
  ctx:DealDamageToPlayer(amount)    — 对玩家造成伤害。
  ctx:DrawCards(count)              — 抽牌 count 张。
  ctx:GainShield(amount)            — 获得临时格挡（信念）。
  ctx:HealPlayer(amount)            — 治疗玩家。

【玩家 / 敌人 / 手牌只读】
  ctx.PlayerHp      — int
  ctx.PlayerLife      — int
  ctx.PlayerShield    — int
  ctx.EnemyHp       — int
  ctx.HandCount       — int，当前手牌张数

【天平】
  ctx.AngerPoint      — int，狂热侧点数
  ctx.CalmPoint       — int，寂静侧点数
  ctx:AdjustBalance(side, value)  — side: string，"anger" 或 其它(视为 calm)；value: int，增加该侧点数

【Buff — 注册定义】
  ctx:RegisterBuff(id, name, desc, maxStacks, stackMode, durationType, duration)
    id           — string，Buff 唯一 id
    name         — string，显示名
    desc         — string，描述（可含占位，依项目约定）
    maxStacks    — int
    stackMode    — string："stack" | "keep_higher" | "refresh" | "ignore"（其它当 stack）
    durationType — string："permanent" | "turn_based"（其它当 permanent）
    duration     — int，回合类 Buff 的默认持续

【Buff — 注册 Lua 事件回调】
  须先 RegisterBuff 同一 id，再 RegisterBuffHandler。
  ctx:RegisterBuffHandler(buffId, eventName, phase, callback)
    buffId    — string
    eventName — 见下方【Buff 可用事件名】
    phase     — string："before" | "resolve" | "after"（其它当 after）
    callback  — Lua 函数，签名为：function(buff, ctx, args) ... end
      buff — BuffInstance（UserData）
      ctx  — 同上 LuaBattleContextApi
      args — 对应事件的参数对象（见【事件参数类型】）

【Buff — 实例操作】
  ctx:ApplyBuff(buffId, stacks)
  ctx:RemoveBuff(buffId)
  ctx:GetBuffStacks(buffId)   -> int
  ctx:HasBuff(buffId)         -> boolean
  ctx:ModifyBuffStacks(buffId, delta)

--------------------------------------------------------------------------------
对象：data（LuaCardDataApi）— CardData 只读，来自 ScriptableObject
--------------------------------------------------------------------------------
  data.Name      — string，CardData.cardName
  data.BaseCost  — int，CardData.baseCost
  data.CostType  — string，CardData.costType 的 ToString()（如 "Anger"）

--------------------------------------------------------------------------------
对象：buff（BuffInstance）— 仅出现在 RegisterBuffHandler 的回调第一个参数
--------------------------------------------------------------------------------
  buff.Stacks          — int
  buff.RemainingTurns  — int
  buff.Definition      — BuffDefinition（含 Id, DisplayName, Description, MaxStacks 等）

--------------------------------------------------------------------------------
【Buff 可用事件名】eventName 必须与 C# 完全一致
--------------------------------------------------------------------------------
  "OnTurnStart"       — 参数：TurnEventArgs
  "OnTurnEnd"         — 参数：TurnEventArgs
  "OnCardPlayed"      — 参数：CardPlayedEventArgs
  "OnCardDrawn"       — 参数：CardDrawnEventArgs
  "OnBalanceChanged"  — 参数：BalanceChangedEventArgs
  "OnBalanceThreshold"— 参数：BalanceThresholdEventArgs
  "OnDamageDealt"     — 参数：DamageEventArgs
  "OnHeal"            — 参数：HealEventArgs
  "OnLifeLost"        — 参数：LifeLostEventArgs
  "OnEnemyAction"     — 参数：EnemyActionEventArgs
  "OnEnemyDied"       — 参数：EnemyDiedEventArgs

--------------------------------------------------------------------------------
【事件参数类型】args 上常用字段（继承 BattleEventArgs 的含 Cancel 等，以引擎为准）
--------------------------------------------------------------------------------
  TurnEventArgs：
    args.TurnNumber  — int

  CardPlayedEventArgs：
    args.Card       — Card 对象
    args.CostSide   — CostType
    args.CostValue  — int

  CardDrawnEventArgs：
    args.Card       — Card 对象

  BalanceChangedEventArgs：
    args.PreviousAnger, args.PreviousCalm, args.CurrentAnger, args.CurrentCalm — int
    args.Delta      — int
    args.Side       — CostType

  BalanceThresholdEventArgs：
    args.OverflowSide — CostType
    args.Difference   — int

  DamageEventArgs：
    args.Target       — DamageTarget（Player | Enemy）
    args.Amount       — int（Before 阶段可修改以改伤）

  HealEventArgs：
    args.Amount       — int

  LifeLostEventArgs：
    （无额外数值字段，依项目）

  EnemyActionEventArgs：
    args.ActionType   — string
    args.Value        — int

  EnemyDiedEventArgs：
    （无额外字段）

  取消事件：仅在 Before 阶段可调用 args:Cancel()（若引擎对该事件支持取消）。

================================================================================
以下为实现区：复制本文件后修改 lua_card 与各函数体。
================================================================================
]]

--你需要根据卡牌效果，填写卡牌的名称，配置一个合适的费用侧和效果数值。
lua_card = {
    name = "新卡牌名称",
    cost_type = CostType.Calm,
    cost_value = 1,
}

function can_play(ctx, data)
    return true
end

function on_play(ctx, data)
    -- ctx:DealDamageToEnemy(1)
end

function get_cost_type(ctx, data)
    return lua_card.cost_type
end

function get_cost_value(ctx, data)
    return lua_card.cost_value
end

function get_description(ctx, data)
    return "在此填写卡牌描述。"
end

