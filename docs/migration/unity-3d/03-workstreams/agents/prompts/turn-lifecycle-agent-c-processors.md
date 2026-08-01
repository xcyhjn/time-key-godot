# Turn Lifecycle Agent C1：Building、Poison 与 Death Processor

> 单一目标：在 lifecycle runner 上新增 Tower building handler、Poison 三 pass processor、typed death/removal result 与纯测试；不修改共享接线或表现。

## 必读

- 本阶段主 Prompt、Godot 源语义审查、Gate A/B 报告/API
- Godot `tower.gd`、`TileTurnBehaviorRunner.gd`、`hex_map.gd` status entry、`tile.gd` poison/death、StatusDB
- 当前 `CombatSliceState`、occupant/result/board API

## 独占拥有路径

- 新目录 `Runtime/Domain/Lifecycle/Buildings/**`
- 新目录 `Runtime/Domain/Lifecycle/Statuses/**`
- 新目录 `Runtime/Domain/Lifecycle/Death/**`
- 对应新 `Tests/EditMode/Lifecycle/{Buildings,Statuses,Death}/**`
- 独占报告 `agents/reports/turn-lifecycle-agent-c-processors.md`

## 禁止路径

所有既有共享文件、enemy intent、Presentation、Composition、Scene/Prefab、Controller、Editor、asmdef、共享文档/证据/Git 和 Godot 源。

## 冻结契约

- building 在 Timeline 全部 action 后拍快照，按 HexCoord、runtime ID 排序；本轮新 Tower 在内，HP `100 -> 50`；下一轮 `50 -> 0`。
- Tower=`Remove`，HP0 时 occupant/tile/status 权威状态原子清理；提前死亡或重复 phase 不再执行。
- Poison 对入口 occupant/status 做不可变排序快照；pass1 六邻聚合传播，pass2 仅旧来源按 `ceil(MaxHP*0.1*entryStacks)` 伤害，pass3 仅旧来源当前层数减1。
- 新感染本轮不传播、不受毒伤、不衰减；多源叠加 checked；死亡后不保留悬空状态。
- 普通敌人 `RemainBroken`，Tower 与 typed Radar underling `Remove`；result 含 before/after/removal/reason/phase/runtime ID。

## 非目标、测试与停止

不实现 UI/VFX、intent、Scene、02B4。测试覆盖空快照、确定性排序、Tower 两周期/提前死亡/幂等、Poison 单源/多源/边界/过滤/新感染隔离/overflow/死亡，以及 removal 原子性。共享 API 不足时报告，不复制 CombatSliceState。不得回退、stash、暂存、commit、push。
