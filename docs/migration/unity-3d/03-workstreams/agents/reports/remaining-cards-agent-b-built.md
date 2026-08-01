# Remaining Cards Agent B1 — Built / Tower Domain

> 日期：2026-08-01
> 分支：`unity_7.31`
> 所有权：仅审查/必要修正 `BuiltCardEffectHandler.cs`、`BuiltCardEffectHandlerTests.cs` 及各自 `.meta`，并新增本独占报告；未触碰共享 Domain/Application、Presentation、Composition、Infrastructure、Scene/Prefab、Godot 源或 Git。

## 1. 结论

Built / Tower 的纯 Domain handler 通过审查。实现复用 Gate A 冻结的 `CombatSliceState` occupant 索引、`ICardEffectHandler.Supports(CardEffect)` payload 预检和 `OccupantEffectResult`，没有建立第二套状态或结果通道。

冻结行为如下：

- payload 只接受 `Kind=Built`、`creation=tower`、`value=1`；未知 creation、0 和大于 1 的 value 在 `TimelineGrid.TryPlace` 占格前抛出 `UnsupportedCardEffectException`。
- Resolve 要求 action 含稳定 `TargetCoord`、effect range 非空、真实 tile 存在且目标坐标当前没有 occupant；缺坐标、缺 range、缺 tile 或已占用均为无结果、无 occupant mutation 的 no-op。
- 最终写入仍由 `CombatSliceState.TryAddOccupant` 做唯一 runtime ID / coordinate 防线；成功后重新读取 snapshot 再输出结果。
- 成功只创建一个 `kind/creation=tower`、`CombatAttitude.Neutral`、`HP=MaxHP=100`、`PoisonStacks=0`、支持生命与状态、坐标为 action `TargetCoord` 的 occupant。
- 创建结果为 `OccupantEffectResult(Built, Before=null, After=tower snapshot)`；不产生 `TileEffectResult`。
- 本阶段没有 Tower 自损 50、敌方意图、Prefab 或表现接线。

## 2. 审查与修正

运行时代码 `unity/Assets/_Project/Runtime/Domain/Effects/BuiltCardEffectHandler.cs` 无需修改。其 payload、最终占用重判、Tower 字段和结果类型均符合 reviewed prompt、Godot `BuiltCommand.gd` / `tower.gd` 与 Gate A 契约。

测试文件做了两项必要修正：

1. 增加 `using System.Collections.Generic;`，修复 `IReadOnlyList<HexCoord>` 无法解析的确定性编译错误。
2. 增加 `Resolve_MissingTargetCoordinateIsPureNoOp`，锁定缺失稳定坐标时不创建 occupant、不产生 occupant result。

两份 `.meta` 均保持原 GUID；全 `unity/Assets` 扫描中各自 GUID 只出现一次。

## 3. 测试覆盖

`BuiltCardEffectHandlerTests` 当前覆盖 10 个 NUnit case：

- 真实空地成功创建一个 Neutral Tower，并断言 kind、creation、HP/MaxHP、status capability、coordinate 与 before/after result。
- 真实 tile 缺失 no-op。
- Resolve 时已经占用 no-op，且 incumbent 的 ID、HP、MaxHP、PoisonStacks 不变。
- 放置后、Resolve 前被占用 no-op，且不会替换 late incumbent。
- effect range 缺失 no-op。
- target coordinate 缺失 no-op。
- `creation=wall`、`value=0`、`value=2` 均在占时间轴前显式失败，`OccupiedCellCount=0`。
- 即使另一个真实 tile 存在，`value=1` 也只在目标坐标创建一个 Tower。

选择阶段的 Application typed target policy、默认 handler/registry 注册、Prefab/Presentation 和正式 Unity filter 属于主智能体共享集成边界，本 Agent 未越权修改。

## 4. 静态验证

未启动 Unity。使用 Unity `6000.4.10f1` 随附 `NetCoreRuntime/dotnet.exe` 与 `DotNetSdkRoslyn/csc.dll`、`/langversion:9.0 /warnaserror+`：

- 编译全部 `Runtime/Domain/**/*.cs`（包含 Built handler）到临时程序集：通过，0 warning / 0 error。
- 以该临时 Domain 程序集编译 `BuiltCardEffectHandlerTests.cs`：通过，0 warning / 0 error。
- 在非 Unity 反射 harness 中执行该测试类的 NUnit case：`passed=10, failed=0`。
- `BuiltCardEffectHandler.cs` 与测试文件扫描 `UnityEngine|UnityEditor`：无命中。
- `card_data/tower.json` 与 Unity 内容副本 SHA-256 均为 `1BF3079D050C61EF385D28CF84A900F9A154D0201451821E1DE32827483772E3`。

正式 Unity EditMode filter 仍按 prompt 由主智能体在唯一 Unity 实例中执行；本报告不把静态编译或反射 harness 伪称为 Unity Editor 结果。

## 5. Code Review 结论与交回

- 架构：通过。Domain 无 Unity 依赖，沿 Gate A 的单一 occupant/result/handler 扩展面实现。
- 契约：通过。payload fail-fast、Resolve 最终占用重判、Tower snapshot 和失败纯度均有覆盖。
- ADR：目标文件头未引用 ADR；任务禁止 Git，因此未通过提交历史扩展 ADR 检索。
- Verdict：`APPROVED`，等待主智能体完成共享 Application/registry/Presentation 集成与正式 Unity filter。

本 Agent 未切分支、stash、暂存、commit、push、回退或清理任何他人改动。`BuiltCardEffectHandler.cs`、`BuiltCardEffectHandlerTests.cs`、各自 `.meta` 与本报告的所有权现全部交回主智能体。当前无用户决策阻塞。
