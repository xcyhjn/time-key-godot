# Remaining Cards Agent C1：Clear Domain/Application

> 单一目标：实现 Wind/Tornado 的独立 `TimelineClearSession`、mask 预览、基于 `TimelineAction` identity 的原子完整移除和 Application clear 交互模式；不触碰 Presentation。

## 必读

- `NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- 三份 `remaining-cards-*-audit.md`
- Gate A/B 最终冻结 API
- `TimelineClearEffect.gd`、当前 TimelineGrid/Application tests

## 独占拥有路径

- `Runtime/Domain/TimelineGrid.cs`
- 新增 `Runtime/Domain/TimelineClear*.cs` 及 `.meta`
- `Runtime/Application/CombatApplicationModels.cs`
- `Runtime/Application/CombatApplicationSession.cs`
- 新增 `Tests/EditMode/TimelineClear*.cs` 及 `.meta`
- `Tests/EditMode/Application/CombatApplicationSessionTests.cs`
- 必要时最小修改 `Tests/EditMode/TimelineGridTests.cs`
- 独占报告 `agents/reports/remaining-cards-agent-c-clear-domain.md`

## 禁止路径

Presentation、Controller、Composition、Infrastructure、Scene/Prefab、Editor、共享文档、Godot 源和 Git。

## 冻结契约

- mask 只能来自 typed `CardEffect.ClearMask`；禁止普通 Shape fallback，禁止创建普通 TimelineAction 或新增占格。
- Preview 合法性只看 12x3 边界；每格返回 OutOfBounds/Empty/Occupied 与命中 action 的只读描述。
- Commit 先完整验证，再以 `HashSet<TimelineAction>` 去重；命中任一格删除完整 action 的全部占格；玩家/敌人均可清；空清成功。
- 越界与取消无副作用；普通 CanPlace/TryPlace/Resolve 不受污染。
- Application 显式暴露 OrdinaryTimeline/TimelineClear mode；clear 不选地图目标，成功 Commit 即结束/消耗会话，不要求普通 Resolve。

## 测试/停止/Git

覆盖 2x2、12x1、边界、空清、多格 action 去重、多个 action、玩家/敌人、取消、重复 preview/commit、普通回归。若必须把 Clear 做成 TimelineAction 或暴露可变 `_cells`，停止并报告。你不是唯一工作者；不得回退、stash、暂存、commit、push。
