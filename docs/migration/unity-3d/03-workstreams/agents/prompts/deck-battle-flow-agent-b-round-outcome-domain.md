# Deck & Battle Flow Agent B：Round、Timecoin 与 Outcome Domain

> 单一目标：只新增无 Unity 依赖的 Era/phase、时间币、互斥战斗终局、奖励入口和 typed return payload Domain，以及独占 EditMode tests。

## 必读

- 本阶段主 Prompt
- `agents/reports/deck-battle-flow-source-semantics.md`
- ADR 0008 与 02B3 Gate D 最终报告
- 当前 lifecycle models、timeline plan/snapshot 与 combat state API

## 独占拥有路径

- 新目录 `unity/Assets/_Project/Runtime/Domain/BattleFlow/**` 及 `.meta`
- 新目录 `unity/Assets/_Project/Tests/EditMode/BattleFlow/**` 及 `.meta`
- 独占报告 `agents/reports/deck-battle-flow-agent-b-round-outcome-domain.md`

## 禁止路径

所有既有文件、Deck、Application、Presentation、Composition、Scene、Prefab、Editor、asmdef、共享文档/证据/Git 和 Godot 源。

## 冻结契约

- `Era >= 1`，phase 为 `1..8`；每次正式 EndTurn 恰好推进一次，8 后变为下一 Era 的 1。InitialStart 不推进。
- Timeline 固定 12x3；时间币奖励为命令开始时 `36 - occupiedCellCount`，每空格 1，不能为负。多格 action 按 occupied cell 计数，不按 action 数。
- 余额与 round state 只能由 typed transaction 修改；重复 sequence 幂等，失败全无副作用。
- 战斗状态至少区分 Active、VictorySettlement、Defeat；Victory 与 Defeat 同一事务互斥，终局后输入锁定且重复结算不重复发奖励。
- 胜利只产生最小局内 reward entry；局外商店/锻造/删牌仍为 Godot 非目标。
- typed outcome/return boundary 必须携带 outcome、Era/phase、timecoins、deck stable-ID snapshot、battle tag/seed；失败不得伪装 completed。
- 所有集合防御性复制，所有无效转换返回 typed reason。

## 测试与验证

覆盖 1:1 起步、phase 8 rollover、多轮推进、0/36/部分占格奖励、重复 sequence、余额溢出保护、胜负竞争、结算后命令拒绝、奖励只发一次、victory/defeat typed payload 和失败无副作用。运行纯 C# 编译与定向 EditMode，解析 XML，静态确认无 Unity API。不得运行 graphical Unity 或执行 Git 操作。

## 停止条件

只有必须修改禁止路径或权威胜负条件无法由 typed 输入表达时停止并报告。你不是仓库唯一工作者，只改拥有路径，不回退他人修改。
