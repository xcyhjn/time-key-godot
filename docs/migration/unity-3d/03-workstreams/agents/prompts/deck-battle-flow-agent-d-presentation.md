# Deck & Battle Flow Agent D：Battle Flow Presentation

> 单一目标：在 Gate B API 冻结后新增只读牌堆/手牌/弃牌、Era/phase、时间币和战斗结算 Presenter、保存 Prefab 与独占 PlayMode tests。

## 独占拥有路径

- 新目录 `unity/Assets/_Project/Runtime/Presentation/BattleFlow/**` 及 `.meta`
- 新目录 `unity/Assets/_Project/Prefabs/Battle/BattleFlow/**` 及 `.meta`
- 新目录 `unity/Assets/_Project/Tests/PlayMode/BattleFlow/**` 及 `.meta`
- 独占报告 `agents/reports/deck-battle-flow-agent-d-presentation.md`

## 禁止与依赖

等待 Gate B typed view/outcome API。禁止修改现有 Card/Timeline Presenter、共享 Scene、Controller、Composition、Binding、Editor、asmdef、共享 docs/evidence/Git。Scene 与 Inspector 接线由主智能体完成。

## 冻结契约

Presentation 只消费不可变 typed result，不推导抽弃、资源、胜负或奖励。所有玩家可见文字为简体中文和 Silver；TextMesh 必须 Font/Material 成对。结算后输入控件不可交互；胜利显示一个最小“领取奖励”入口，失败不显示奖励。手牌 View 销毁后，既有 action frame 仍从保存 display payload 显示卡名、效果、source/target 和 action identity，不读取 hand View。

## 验证与协作

PlayMode 覆盖数量/资源刷新、胜负互斥、奖励只出现一次、输入锁、重复 Apply 幂等、Silver 引用和 action frame 残留。不得运行共享 harness 或执行 Git 操作；完成后写独占报告并交回。
