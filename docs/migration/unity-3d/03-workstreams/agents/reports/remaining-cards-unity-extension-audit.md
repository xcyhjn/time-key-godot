# Remaining Cards Unity Extension Audit

> 审查日期：2026-08-01
> 审查方式：只读；未运行 Unity、未修改 Runtime/Scene/Prefab/测试/共享文档
> 分支与快照：`unity_7.31`，审查起点 `da00914`
> 结论：**CONCERNS，但 Gate A 可继续**。`recover` 可以沿现有普通卡链进入且不修改 `VerticalSliceController`；Gate B 前必须补 occupant/状态模型和非地形结果通道；Gate C 必须使用独立 clear session 和按 action identity 的原子移除 API。

## 1. 审查依据与范围

已完整读取：

- `00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- `06-maintenance/add-card.md`
- `06-maintenance/add-effect.md`
- `06-maintenance/scene-and-prefab-guide.md`
- `06-maintenance/debugging-guide.md`
- `06-maintenance/module-ownership.md`
- `03-workstreams/integration-contracts.md`
- `03-workstreams/agents/reports/decoupling-r3-integration-summary.md`

并逐项核对了当前 Domain/Application/Infrastructure/Presentation/Composition C#、相关 asmdef 与现有 EditMode/Infrastructure 测试。本报告只回答 handler/session/catalog/registry/assembly 的扩展面和剩余五卡的最小修改边界；Godot 源语义和资产/UI 结论由同波次其他只读报告负责。

## 2. 当前真实扩展接口

| 层 | 真实类/API | 当前能力 | 对剩余五卡的限制 |
| --- | --- | --- | --- |
| Domain schema | `Runtime/Domain/CardDefinition.cs`：`CardEffectKind`、`CardEffect`、`CardDefinition` | 已表达 `Recover/Built/Poison/Clear`；Built 有 `CreationId`，Clear 有归一化 `ClearMask`；Clear 不能携带普通 shape | schema 无需为五卡另起平行 DTO；不得修改 JSON fixture 迎合实现 |
| Domain handler | `Runtime/Domain/Effects/ICardEffectHandler.cs`：`Kind`、`Apply(CombatSliceState, TimelineAction, CardEffect, ICollection<TileEffectResult>)` | `DamageCardEffectHandler` 与 `ElevationCardEffectHandler` 已实现 | 输出口被写死为 `TileEffectResult`；Recover 可勉强只改 HP，但 Built/Poison 无法向快照和 Presenter 交付 occupant 结果 |
| handler registry | `Runtime/Domain/TimelineGrid.cs` 构造参数 `IReadOnlyList<ICardEffectHandler>`；`EnsureEffectsSupported` | 按 `CardEffectKind` 注册；默认只注册 Damage/Elevation；Preview/TryPlace 在占格前 fail-fast | 只按 kind 判断，不能提前拒绝未知 `Built.creation` 或不支持的 Built value；默认 handler 列表与 Infrastructure UI registry 是两份清单 |
| ordinary card session | `Runtime/Domain/CardPlaySession.cs` | 地图目标 -> timeline preview -> commit/cancel；普通 action 保存 `TargetId`、`TargetCoord`、effects/range/shape | 适用于 Recover/Built/Poison；明确不适用于 Clear，且 `TimelineAction.FromCard` 已拒绝 Clear |
| timeline | `Runtime/Domain/TimelineGrid.cs`：`CanPlace`、`TryPlace`、`Resolve`、`OccupiedCellCount` | `_cells` 已把每个占格映射到同一个 `TimelineAction` 实例，`_placedActions` 保留 action identity | `Contains`、action 查询和 `Clear` 均为 private；没有 mask 预览、命中去重、完整 action 移除或部分清除结果；`Resolve` 会清空整条 timeline |
| combat state | `Runtime/Domain/CombatSliceState.cs` | 只有固定 `TargetId/TargetHp`、`Board`、seed/turn；Damage 只按 ID 扣血 | 无 Max HP、entity 坐标/存在性、attitude、occupant、PoisonStacks、supports-health/status；不能满足 Resolve 重查 |
| board | `Runtime/Domain/Terrain/CombatBoardState.cs`：`BoardTileState(LogicalLayerCount)` | 真实地块存在性与 elevation | 不知道地块 occupant，无法判断 Built 空地或从坐标重查 Recover/Poison occupant |
| snapshot | `Runtime/Domain/ResolutionSnapshot.cs` | 固定目标 HP before/after、action 顺序、`EffectResults: TileEffectResult` | 无 occupant before/after、创建结果、Poison stacks 或 clear removed-actions 结果 |
| Application use case | `Runtime/Application/CombatApplicationSession.cs` | `TryGetRequiredTargetKind` 已把 Recover/Poison 映射为 Entity、Built 映射为 Tile；Card selection/target/preview/commit/resolve/trace 均为 typed API | `IsKnownTarget` 对 Entity 只认唯一 `_state.TargetId`，对 Tile 只查 tile 存在；不检查满血/死亡/状态能力/空地；Clear 落入 `UnsupportedCardTargetPolicy`；phase 只覆盖普通 session |
| Application catalog port | `Runtime/Application/Ports/ICardCatalog.cs` | `Cards` + `TryGet(stableId)` 已足够支持任意有序七卡 catalog | **无需扩接口**；继续由配置/fixture 驱动，不在 Controller 写七卡列表 |
| Infrastructure catalog | `Runtime/Infrastructure/Cards/CardContentCatalog.cs` | 保序、stable ID 唯一、`CardDefinition.FrontImage` 映射 | **无需玩法改动**；只需保留七 fixture 与覆盖测试 |
| UI support registry | `Runtime/Infrastructure/Effects/CardEffectRegistrationCatalog.cs`、`CardEffectRegistration.cs` | data-only、只按 kind 宣告卡牌是否可交互；当前只注册 Damage/Elevation | 与 TimelineGrid handler registry 可能漂移；Clear 应在独立 clear use case 可用后才注册，不应伪造普通 handler |
| Composition | `Runtime/Composition/CombatCompositionRoot.cs` | 创建 catalog、data-only registrations、`CombatSliceState`、默认 `TimelineGrid`、Application session，并注入 Controller | registrations 与实际 handlers 并非同一来源；状态 fixture 只给 HP=10，无 Max HP/occupant；Tower/Poison 资源生命周期尚无接线 |
| Presentation facade | `Runtime/Presentation/VerticalSliceController.cs` | `SelectCard` 通用；Entity/Tile target、timeline preview/commit/resolve 通用；只消费 session/snapshot；世界 tile 与目标同步 | Entity 坐标固定为 `(1,0)`；tile API 名为 `SelectEarthquakeTarget` 但实际按 target kind 通用；timeline 事件总是走普通放置；只应用 `TileEffectResult`；不存在清除已渲染 action 的 API |

## 3. 程序集与依赖方向

当前 asmdef 图符合解耦约束：

```text
TimeKey.Domain (noEngineReferences, no references)
  <- TimeKey.Application (noEngineReferences)
  <- TimeKey.Infrastructure / TimeKey.Diagnostics

TimeKey.Domain + TimeKey.Application
  <- TimeKey.Presentation

Domain + Application + Infrastructure + Diagnostics + Presentation
  <- TimeKey.Composition
```

新增代码应放置如下：

- occupant、HP/MaxHP、PoisonStacks、effect result、handler、`TimelineClearSession`、timeline clear 原子操作：`TimeKey.Domain`。
- target policy、普通/clear use-case 编排、session view/command result、trace：`TimeKey.Application`。
- JSON/catalog/支持注册清单：`TimeKey.Infrastructure`；不要把玩法结果或 Prefab 放入此程序集。
- Timeline clear 预览、Tower/Poison 快照投影、动态 View：`TimeKey.Presentation`。
- handler/session 注册、Tower/Poison Prefab 与 sprite 生命周期、Inspector 引用：`TimeKey.Composition`。

不得用 `Domain -> Application`、`Application -> Infrastructure/Presentation`、`Presentation -> Infrastructure` 或任何反向 asmdef 引用解决编译问题。五卡不需要新增 package。

## 4. Recover 能否不改 Controller 加入

**可以，且 Gate A 应把“`VerticalSliceController.cs` 无改动”当作架构验收条件。**

现有链已具备：

1. `ICardCatalog.TryGet("recover")` 与通用 `SelectCard(stableId)`；
2. Application 已把 `Recover` 识别为 `CombatTargetKind.Entity`；
3. Controller 的 `SelectTarget`、timeline preview/commit/resolve 没有按 stable ID 分支；
4. `BoardRangePresenter` 直接消费 `SelectedCard.Range`，三格 range 无需新规则；
5. `CombatHudPresenter` 已能从 `ResolutionSnapshot.TargetHpAfter` 显示结算 HP。

Gate A 的最小必要修改不是 Controller，而是：

- Domain 增加可观察的 Max HP 和 occupant 存在/坐标；`Recover` handler 必须通过稳定 `TargetId + TargetCoord` 在 Resolve 时重查当前 occupant，执行 `min(MaxHp, Hp + value)`，缺失/满血为 no-op。
- `TimelineGrid` 的实际 handler 列表加入 Recover；Infrastructure data-only registry 同步加入 Recover。
- Application 的 typed target 验证增加 Recover 选择规则：存在、支持生命、存活且 `Hp < MaxHp`。这里按 effect kind 的窄策略允许存在，但不能按 `stableId == "recover"`。
- Composition 为 fixture 状态提供 `MaxHp` 与坐标；可改 Composition 和 Domain，不需改 Controller。
- trace 应由 effect result 驱动记录 Recover HP before/after，避免在 Controller 打补丁。

禁止的“假通过”包括：负伤害治疗、只在 Presenter 修改数字、只在选择时验证而 Resolve 不重查、按 stable ID 分支、为 recover 复制一套 session。

如果实现过程中证明必须修改 Controller 才能让 recover 完成逻辑选择或结算，先判定扩展面验收失败；只允许修正 Domain/Application/Composition 的通用接口，不接受 Controller 卡牌分支。

## 5. Gate A/B 需要的最小共享 Domain 接口

### 5.1 Occupant 状态

五卡的最小共同数据不是更多单卡字段，而是一个纯 Domain occupant：

```text
CombatOccupantState
  RuntimeId
  Coordinate
  CreationId / Kind
  Attitude
  Hp / MaxHp
  PoisonStacks
  SupportsHealth / SupportsStatus
  IsAlive
```

`CombatSliceState` 应提供按 `RuntimeId + HexCoord` 和按 `HexCoord` 的只读查询，以及受控的 add/remove/mutate。现有 `TargetId/TargetHp` 可保留为 fixture 兼容 getter，避免一次性打碎 lighting 与既有测试。不要让 Presenter 保存权威 HP、PoisonStacks 或占用关系。

为避免重复所有权，建议 `CombatSliceState` 拥有 occupant 索引，`CombatBoardState` 继续拥有 tile/layer；所有添加 occupant 都必须先确认对应 tile 存在且该坐标无 occupant。这样 Recover/Poison 可按 action 保存的 ID+coord 重查，Built 可用同一坐标索引做空地最终防线。

### 5.2 Handler 结果通道

当前 `ICardEffectHandler.Apply(..., ICollection<TileEffectResult>)` 是 Gate B 的实际阻塞。最小修正是把最后一个参数替换为一个纯 Domain 结果收集器，保留现有地形结果并新增 occupant before/after：

```text
CardEffectResultBuffer
  AddTile(TileEffectResult)
  AddOccupant(OccupantEffectResult)

OccupantEffectResult
  EffectKind
  Before: OccupantSnapshot?   # Built 前可为空
  After: OccupantSnapshot?    # 移除后可为空
```

`ResolutionSnapshot` 继续保留 `EffectResults` 兼容 elevation 测试，同时新增只读 `OccupantEffectResults`。Recover/Poison 的 before/after 分别自然表达 HP 和 PoisonStacks；Built 用 `Before=null, After=tower snapshot` 表达创建。Presentation 只投影 snapshot，不回读 handler 或复制规则。

### 5.3 Effect payload 预检

`ICardEffectHandler.Kind` 只证明支持 kind，不足以证明支持 Built payload。应增加一个窄的 payload 预检（例如 `Supports(CardEffect)` 或 `Validate(CardEffect)`），并让 `TimelineGrid.CanPlace/TryPlace` 在占格前调用。Built handler 本阶段只接受：

- `Kind=Built`
- `CreationId="tower"`
- `Value=1`

未知 creation、0 或大于 1 的 value 必须显式失败且 timeline/state 无副作用；不能拖到 Resolve 中途才抛错。Infrastructure registry 可继续 data-only，但至少需要测试证明 UI 支持清单与 Domain 实际能力一致。

## 6. Built/Tower 最小修改面

### Domain/Application

- 新增 Built handler，选择与 Resolve 都检查 tile 存在且 occupant 为空；Resolve 是最终防线。
- 成功时只创建一个 `CreationId=tower`、`Attitude=Neutral`、`Hp=MaxHp=100`、坐标为 action `TargetCoord` 的 occupant snapshot。
- Application 仍使用 `CombatTargetKind.Tile`，但 `IsKnownTarget` 必须调用 typed target policy，不能再只判断 tile 存在。
- 失败/no-op 不得产生 occupant result，不得改变 tile elevation 或 timeline action identity。

### Presentation/Composition

- Composition 持有保存的 Tower Prefab 引用；Presentation 根据 `OccupantEffectResult.After` 在对应 `HexTileColumn.OccupantAnchor` 实例化/刷新。
- 这需要通用 occupant result 投影入口；可以修改 Controller 的通用 snapshot 应用方法或把它放进独立 Presenter/Binding，但禁止 `stableId == "tower"` 或 JSON 解析分支。
- 当前 `SelectTile -> SelectEarthquakeTarget` 的方法名是历史债务，行为本身按 `RequiredTargetKind` 通用。Gate B 可先复用，除非重命名能保持公共兼容且不扩大 diff；不要在 Gate A 顺手重构。

## 7. Poison 最小修改面

### Domain/Application

- 新增 Poison handler；选择时要求 occupant 仍存在、存活、支持状态；Resolve 以 action 的稳定 ID+coord 重查同一 occupant。
- `PoisonStacks = checked(old + effect.Value)` 或先定义明确整数溢出行为；正常规则不设游戏上限，重复施加为直接加 2。
- `OccupantEffectResult` 必须提供 old/new snapshot，测试直接断言 0->2、2->4；本阶段不在 Resolve 之外自动 tick。
- Application 仍使用 Entity target；不要在 Presenter 判断死亡、空格或 supports-status。

### Presentation/Composition

- Poison 图标/层数来自 `After.PoisonStacks`；动态状态 View 由保存 Prefab 或明确 factory 创建，并绑定 occupant anchor。
- 逻辑层数只来自 Domain snapshot；图标缺失不能反向改变 Poison handler 结果。

## 8. Wind/Tornado Clear 所需最小接口

Clear 不能注册成普通 `ICardEffectHandler` 后塞入 `TimelineAction`。`TimelineAction.FromCard` 当前明确拒绝 Clear，这个保护必须保留。

### 8.1 TimelineGrid 原子 API

在不暴露 `_cells`/`_placedActions` 可变集合的前提下，增加：

```text
PreviewClear(origin, clearMask) -> TimelineClearPreview
TryClear(origin, clearMask) -> TimelineClearResult

TimelineClearPreview
  IsInBounds
  Cells[]: coordinate + Empty/Occupied/OutOfBounds
  HitActions[]: 每个 action identity 只出现一次

TimelineClearResult
  Succeeded
  RemovedActions[]: origin/cardId/actorKind/完整 shape
  RemovedCellCount
```

实现必须先验证整个 mask 都在 12x3 边界内，再收集命中的 `HashSet<TimelineAction>`，最后按每个 action 的完整 shape 从 `_cells` 删除并从 `_placedActions` 删除。空 `HitActions` 仍成功；越界失败且零修改；同一 action 被多格命中只返回/移除一次。不要按 actor kind、card ID 或未来建筑类型过滤，除非 Godot 源审查报告给出可观察过滤规则。

### 8.2 独立 Domain/Application session

新增 `TimelineClearSession`（或同等窄类型），最少状态为 Selected/Preview/Committed/Cancelled，并只持有 card、mask、origin、preview/result。它不接受地图目标、不调用 `CardPlaySession`、不占新格。

Application 需要显式交互模式和 clear 命令，例如：

```text
CombatInteractionMode = OrdinaryTimeline | TimelineClear
PreviewClear(TimelineCell)
CommitClear()
```

`CombatSessionView` 暴露 clear preview/result；Selecting Clear 时 `RequiredTargetKind=null`，直接进入 clear timeline 交互。不要把 Clear 硬塞进 `TryGetRequiredTargetKind` 的 Entity/Tile 返回值，也不要让普通 `PreviewTimeline/CommitTimeline` 承担两套不同副作用。

### 8.3 Registry 与 Presentation

- Infrastructure registry 只有在 `TimelineClearSession` 和 Application clear use case 已接线时才宣告 `CardEffectKind.Clear` 可交互。
- `TimelinePresenter.Refresh` 当前只能显示普通 valid/invalid shape，且 `TimelineCellView` 只有 `SetContent` 没有清空 API。Gate C 至少要增加 clear 三态预览和按 `RemovedActions[].完整 shape` 清除标签/底色的 API。
- Controller 当前 timeline hover/click 总是调用普通 preview/place。Gate C 允许一次**按 `CombatInteractionMode` 的通用路由**，或把该路由收进 Binding/Application；禁止 wind/tornado stable-ID 分支或 effect-kind 大 switch。

## 9. Catalog/Registry/Composition 的一致性风险

当前存在两个独立“支持清单”：

1. `TimelineGrid.CreateDefaultEffectHandlers()` 决定普通 action 真正可 Resolve 的 kind；
2. `CardEffectRegistrationCatalog.CreateVerticalSlice()` 决定 CardView 是否可交互。

这不是程序集环，但会出现“UI 可点、Domain 不支持”或“Domain 支持、UI 禁用”。最小控制方式：

- Composition 在同一位置构造本阶段能力，并增加 parity test，逐卡断言 ordinary handler/clear session 与 data-only registry 同步；
- 不必为本阶段引入 DI 容器或新 package；
- Clear 不能为了 parity 被加入普通 TimelineGrid handler 列表；它应与 clear-session capability 对齐。

`ICardCatalog` 与 `CardContentCatalog` 已足够，不应扩展成 service locator、资源加载器或玩法 registry。七卡顺序、stable ID、FrontImage 继续由 fixture/catalog 驱动。

## 10. 建议测试边界

| 边界 | 必测内容 | 不应测试/实现的位置 |
| --- | --- | --- |
| Domain Recover | 缺失/坐标或 ID 不匹配 no-op；满血 no-op；受伤目标 +100；钳制 MaxHP；snapshot before/after；三格 shape 边界仍由 TimelineGrid 负责 | Presenter 不测治疗公式；不得用负 Damage |
| Application Recover | 满血选择非法；选择后目标消失/变满，Resolve no-op；无 Controller stable-ID 路由；trace before/after | 不在 Controller 复制 target validity |
| Domain Built | 空地成功；占用失败；commit 后 Resolve 前被占用；未知 creation/value fail-fast 且纯；Tower HP/attitude/coord snapshot | 不在 Prefab 脚本创建权威 occupant |
| Domain Poison | 活体 0->2、2->4；空格/死亡/不支持状态 no-op；Resolve 重查；本阶段无自动 tick | 不在状态图标上保存 stacks |
| Domain Clear | Wind 2x2、Tornado 12x1；in/out bounds；空清；多格命中同 action 去重；完整 action 移除；取消纯度；普通 CanPlace/TryPlace 回归 | 不创建 TimelineAction；不从 card shape 回退 |
| Application Clear | clear card 无地图 target；interaction mode/phase；preview 无副作用；commit 不增加占格；取消；结果包含完整 removed actions | 不通过 `TryGetRequiredTargetKind` 伪造 Tile target |
| Infrastructure | 七 ID/顺序/FrontImage；registry 与普通 handlers/clear capability 对齐；未知 Built payload 显式不可执行 | 不改 JSON fixture |
| Presentation/PlayMode | 三态 clear preview；完整 action UI 清空；Tower/Poison snapshot 投影；Binding 重复 bind/unbind；recover 通用链 | 不重新计算 mask 边界、占用、HP 或 stacks |
| Assembly/static | Domain/Application `noEngineReferences=true`；Presentation 无 Infrastructure 引用；Recover Gate A diff 不含 `VerticalSliceController.cs` | 不通过反向 asmdef 引用解错 |

现有测试中需要明确更新的旧断言：

- `Tests/EditMode/TimelineGridTests.cs` 与 `Tests/EditMode/Application/CombatApplicationSessionTests.cs` 当前把 Recover 断言为 `UnsupportedEffect`；Gate A 通过后必须改成成功/规则测试，保留一个真正未支持或无效 payload 的 fail-fast 用例。
- `Tests/Infrastructure/Effects/CardEffectRegistrationCatalogTests.cs` 当前断言仅 Damage/Elevation 支持；每个 Gate 只在真实 use case 就绪后推进注册断言。
- `Tests/EditMode/Terrain/EarthquakeResolutionTests.cs`、lighting/earthquake Application 测试必须作为 handler 结果通道修改后的回归边界。

## 11. 冲突、验收失败与停止条件

以下不是普通实现选择，而是必须修正或停止集成的条件：

1. **Recover 需要 Controller stable-ID/effect 分支**：判定解耦扩展面验收失败；先修 Domain/Application/Composition 通用接口。
2. **Built/Poison 只能靠 Presenter 回读并推断可变 Domain 状态**：说明 handler/snapshot 结果口仍不足；先补 occupant before/after 结果。
3. **未知 Built creation 只能在 Resolve 中途抛错**：会留下 committed action/不明确 phase；必须在 Preview/TryPlace 前 payload fail-fast。
4. **Clear 被包装成普通 TimelineAction、占新格或从 `CardDefinition.Shape` 取 mask**：直接违反冻结契约，停止 Gate C。
5. **Clear 逐格删除导致半个 action 残留或重复返回**：必须回到 action identity 原子 API，不能在 Presenter 修补。
6. **为接线引入 asmdef 反向依赖、Service Locator、`GameObject.Find` 或 Presenter -> Infrastructure**：停止并调整 Composition 边界。
7. **注册清单与实际能力不一致**：不能把对应卡标为可交互，也不能进入视觉门禁。
8. **Domain snapshot 无法区分 Recover HP、Poison stacks 与 Built occupant 创建**：不能把它们继续塞进 `TileEffectResult.BeforeLayers/AfterLayers`；先修类型边界。

## 12. 建议实施顺序与最小文件面

### Gate A：Recover

1. Domain occupant/MaxHP 最小模型 + Recover handler + handler payload/result 基础。
2. `TimelineGrid` 实际 handler 注册。
3. Application typed target validator 与通用 trace。
4. Infrastructure Recover registration + parity tests。
5. Composition fixture 注入 MaxHP/坐标。
6. Domain/Application/Infrastructure 测试；确认 `git diff -- VerticalSliceController.cs` 为空。

### Gate B：Built/Poison

1. 在 Gate A 的同一 occupant 模型上增加空地创建和 PoisonStacks，不新建第二套 entity/status 数据。
2. Built/Poison handlers + resolve 重判 + `OccupantEffectResult`。
3. Application target policy 与 registry。
4. Presentation 通用 occupant snapshot 投影；Composition 只持有 Tower/状态 Prefab 与资源引用。

### Gate C：Clear

1. TimelineGrid mask preview/atomic clear result。
2. 独立 `TimelineClearSession`。
3. Application interaction mode 与 clear commands/view。
4. Registry 宣告 Clear 可用。
5. TimelinePresenter 三态预览、完整 action 清空、Binding/Controller 通用 mode 路由。

这个顺序保持 Recover 作为“新增普通效果不改 Controller”的验收卡，同时避免为 Built/Poison 各自发明状态容器，也避免 Clear 污染普通放置/Resolve。

## 13. 最终审查结论

- **Catalog：PASS。** `ICardCatalog`/`CardContentCatalog` 已满足七卡数据驱动，不需扩接口。
- **Recover 普通链：PASS WITH REQUIRED DOMAIN WORK。** 不需修改 Controller；所缺是 MaxHP/occupant re-query、handler 与 target validator。
- **Handler/result：CONCERN。** 现有 `ICollection<TileEffectResult>` 是 Built/Poison 的真实结构缺口，必须在 Gate B 前最小修正。
- **Registry：CONCERN。** data-only registry 与 TimelineGrid handler registry 分离，需要 Composition 同源组装或 parity test，不能仅手工同步无证明。
- **Built/Poison：BLOCKED ON SHARED OCCUPANT MODEL/RESULT ONLY。** 不是 Controller 或 asmdef 阻塞。
- **Clear：BLOCKED ON INTENDED NEW SESSION/API。** 当前代码正确地拒绝把 Clear 当普通 action；补独立 session、mask 原子 API 和 Presenter 清除能力后可继续。
- **程序集：PASS。** 现有依赖方向足够容纳全部修改，无需新包或反向引用。

当前无需要用户决策的架构分歧；主智能体可以按 Gate A 立即实现，并把上述停止条件作为每个 Gate 的验收线。
