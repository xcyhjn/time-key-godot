# Deck & Battle Flow Agent C：Application Reserved Hook

> 单一目标：在 Agent A/B API 冻结后，只新增 02B4 reserved-hook Application transaction 和 tests；不得修改现有 lifecycle runner/coordinator。

## 独占拥有路径

- 新目录 `unity/Assets/_Project/Runtime/Application/BattleFlow/**` 及 `.meta`
- 新目录 `unity/Assets/_Project/Tests/EditMode/Application/BattleFlow/**` 及 `.meta`
- 独占报告 `agents/reports/deck-battle-flow-agent-c-application-hook.md`

## 禁止与依赖

等待 Agent A/B 交回。禁止修改任何既有文件、Deck/BattleFlow Domain、Presentation、Composition、Scene/Prefab、Editor、asmdef、共享 docs/evidence/Git。主智能体负责把新 hook 接入现有 `CombatTurnLifecycleCoordinator`。

## 冻结契约

一个 hook transaction 消费 lifecycle sequence、预先冻结的 Timeline 占格数和当前战斗状态；按固定顺序完成剩余手牌弃置、时间币结算、Era/phase 推进、必要洗回和抽 5。InitialStart 只初始化/抽牌，不推进 Era、不发回合时间币。重复 sequence 返回同一 typed result，不重复移动卡牌或加币。终局状态拒绝 hook。输出包含 deck/hand/discard、round/resources、outcome 和 presentation-safe snapshot；不得包含 CardView/GameObject。

## 验证与协作

测试覆盖 InitialStart、正式 EndTurn、多轮洗回、双空堆、重复 sequence、步骤失败纯度、终局拒绝、action display snapshot 在弃牌后仍可读。纯 C# 与定向 EditMode 通过后写报告。不得运行 Unity 图形实例或执行 Git 操作。
