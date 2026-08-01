# Remaining Cards Agent B1：Built/Tower Domain

> 单一目标：在 Gate A 冻结的 occupant/result/handler 契约上实现 `built.creation=tower,value=1` 的空地重判与 Tower occupant 创建；只负责纯 Domain 与独占测试。

## 必读

- `NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- 三份 `remaining-cards-*-audit.md`
- Gate A 报告与冻结后的 Domain API
- `card_data/tower.json`、`scene/in_scene/timeline/commands/BuiltCommand.gd`、`scene/in_scene/enermy/tower.gd`

## 独占拥有路径

- 新增 `Runtime/Domain/Effects/BuiltCardEffectHandler.cs` 及 `.meta`
- 新增 `Tests/EditMode/Effects/BuiltCardEffectHandlerTests.cs` 及 `.meta`
- 独占报告 `agents/reports/remaining-cards-agent-b-built.md`

## 禁止路径

所有既有共享 Domain/Application 文件、Poison/Clear、Presentation、Composition、Infrastructure、Scene/Prefab、Editor、共享文档、Git 与 Godot 源。

## 冻结契约

- 只接受 `creation=tower,value=1`；未知 creation、0 或大于 1 在 Timeline payload 预检阶段显式失败且不占格。
- 选择与 Resolve 最终均要求真实 tile 存在且当前无 occupant；Resolve 前被占用为 no-op。
- 成功创建一个 `kind/creation=tower`、Neutral/Middle、`HP=MaxHP=100`、真实 HexCoord 的 occupant；结果 `Before=null, After=tower snapshot`。
- 本阶段不自损 50、不生成意图、不做 Prefab。

## 测试/停止/Git

覆盖空地成功、占用失败、Resolve 前占用、未知 creation/value、数量上限、HP/attitude/coord、失败纯度。不得运行并行 Unity；可做静态编译检查，正式 filter 由主智能体执行。若 Gate A 契约无法表达创建结果，先报告而不建立第二套状态。你不是唯一工作者；不得回退、stash、暂存、commit、push。
