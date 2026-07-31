# Wave 02B2A Agent 03：Earthquake Domain

> 状态：已审查；仅在 Gate A 后启动
> 负责人：Wave 02B2A Agent 03
> 最后验证日期：2026-08-01

## 角色与单一目标

在纯 C# Domain 中让普通 TimelineAction 保留目标坐标/effects/range，并把 `earthquake` 结算为真实的逻辑层数变化；不写 Unity 表现。你不是仓库唯一工作者，不得回退他人改动。

## 启动前置、必读与能力

主智能体必须先确认 Agent 01 的七卡 schema/adapter 已通过并冻结实际 API。

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- Agent 01 报告
- `unity/Assets/_Project/Runtime/Domain/TimelineAction.cs`
- `TimelineGrid.cs`、`CombatSliceState.cs`、`ResolutionSnapshot.cs`、`CardPlaySession.cs`
- `scene/in_scene/effect/effect_processor.gd`
- `scene/in_scene/timeline/commands/ElevationCommand.gd`
- `scene/in_scene/hex_map_modules/elevation/TileElevationService.gd`

使用 `codebase-migrate` 保持 Godot 行为等价；使用 `Verification & Quality Assurance` 先写失败路径和确定性测试。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Domain/TimelineAction.cs`
- `unity/Assets/_Project/Runtime/Domain/TimelineGrid.cs`
- `unity/Assets/_Project/Runtime/Domain/CombatSliceState.cs`
- `unity/Assets/_Project/Runtime/Domain/ResolutionSnapshot.cs`
- `unity/Assets/_Project/Runtime/Domain/CardPlaySession.cs`
- 可新增 `unity/Assets/_Project/Runtime/Domain/Terrain/**`
- 可新增 `unity/Assets/_Project/Tests/EditMode/Terrain/**`
- 必要时最小更新既有 `unity/Assets/_Project/Tests/EditMode/TimelineGridTests.cs` 与 `CardPlaySessionTests.cs`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b2a-agent-03-earthquake-domain.md`

禁止修改 `CardDefinition.cs`/`Domain/Cards/**`、Infrastructure、Presentation、Scenes、Editor、asmdef、Packages、共享文档、Godot 源和其他 Agent 路径。

## 冻结契约

- 普通 TimelineAction 不可变地携带 target `HexCoord`、effects 和 effect range；旧 constructor/`Damage`/target ID 行为保持兼容。
- 纯领域棋盘按 `HexCoord` 存储 tile；logical layer count 是一基，Unity elevation 0 对应 1 层。
- Resolve 时重新按坐标查询 tile；缺失范围格跳过，不创建 tile。
- `earthquake` 对 7 个当前存在范围格各执行 `+2`，不是设为 2；目标 occupant 不影响合法性。
- 表现层换算为 `blockCount = logicalLayerCount`、`unityElevation = logicalLayerCount - 1`、top delta=`2*0.32`。
- logical height 超过 6 或不高于 0 的源行为是销毁 tile。演示 fixture 不触发销毁，但 pure Domain 测试冻结该规则。
- ResolutionSnapshot 只做加法扩展，能观察每个受影响坐标的 before/after/removed；旧字段不删除。
- Preview/失败 Commit/Cancel 仍不得改变 TimelineGrid 或棋盘状态。

## 实现步骤与测试

1. 先写 tests：中心 7 格、边缘越界、缺失 tile、1->3、5->removed、两格 shape、预览不变异、固定 seed 731 重复一致。
2. 建立最小 tile/elevation state，不为 built/poison 提前加入推测字段。
3. 最小扩展 TimelineAction/Resolve 以 dispatch Elevation；Damage 路径结果保持一致。
4. CardPlaySession 创建 action 时传递既有 TargetCoord；非法流程仍返回现有显式失败。
5. 旧 31 项 EditMode 与新增测试全部通过；写独占报告。

使用命令目录的单实例 EditMode 命令；执行 `git diff --check -- unity/Assets/_Project/Runtime/Domain unity/Assets/_Project/Tests/EditMode`。不运行 PlayMode、截图或 build。

## 非目标和停止条件

不实现 clear、recover、poison tick、built、塔自损、Unity GameObject、动画或 UI。若 Agent 01 API 无法承载 immutable effects/range，停止修改共享 schema，在报告中提出最小契约差异并通知主智能体。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash。回报：实际 API、规则表、测试计数、旧回归、失败项、性能复杂度和 Presentation 所需只读结果。
