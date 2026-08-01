# Remaining Cards Agent B2：Poison Domain

> 单一目标：在 Gate A 冻结的 occupant/result 契约上实现 `poison.value=2` 的活体状态重判、无上限整数累加和 before/after 快照；只负责纯 Domain 与独占测试。

## 必读

- `NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- 三份 `remaining-cards-*-audit.md`
- Gate A 报告与冻结后的 Domain API
- `card_data/poison.json`、`PoisonCommand.gd`、`status_component.gd`

## 独占拥有路径

- 新增 `Runtime/Domain/Effects/PoisonCardEffectHandler.cs` 及 `.meta`
- 新增 `Tests/EditMode/Effects/PoisonCardEffectHandlerTests.cs` 及 `.meta`
- 独占报告 `agents/reports/remaining-cards-agent-b-poison.md`

## 禁止路径

所有既有共享 Domain/Application 文件、Built/Clear、Presentation、Composition、Infrastructure、Scene/Prefab、Editor、共享文档、Git 与 Godot 源。

## 冻结契约

- 只对 Resolve 时仍存在、ID+coord 匹配、`HP>0`、支持状态的 occupant 生效；空格、死亡、消失为 no-op。
- 每次直接 `old+2`，不覆盖旧值、不设游戏上限；整数溢出必须显式失败而不是回绕。
- snapshot 可观察 `0->2`、`2->4`；失败无结果/无副作用。
- 本阶段绝不传播、伤害、衰减或自动 tick。

## 测试/停止/Git

覆盖活体、空格、死亡、无状态能力、重判消失/替换、累加、快照副本和无自动 tick。不得运行并行 Unity；正式 filter 由主智能体执行。若共享契约不足，报告而不复制 occupant/status 容器。你不是唯一工作者；不得回退、stash、暂存、commit、push。
