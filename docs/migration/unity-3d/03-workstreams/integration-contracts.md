# Unity 局内战斗共享集成契约

> 状态：Wave 01/02A/02B1/02B2A 与解耦 R1/R2/R3 已冻结并验证
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：玩法等价契约、目标架构、数据迁移边界

## Domain API

首切片只要求下列语义，不冻结不必要的具体实现：

```text
HexCoord(q, r)
TimelineGrid(width=12, height=3)
TryPlace(TimelineAction) -> success/failure
Resolve(CombatSliceState) -> ResolutionSnapshot
CardDefinition(stableId, numericId, effects, range, shape)
```

`TimelineAction` 至少包含 origin、actor kind、card ID、target ID；结算按列再按行且一个 action 只执行一次。`ResolutionSnapshot` 字段与 `gameplay-parity-contract.md` 完全一致。

## Adapter API

Infrastructure 从 TextAsset/字符串解析卡牌 JSON，输出 Domain 的 `CardDefinition`。解析器必须：

- 接受源文件未映射中文字段。
- 保留 `lighting` 稳定 ID、`damage=100`、range offsets 和单格 shape。
- 对缺失稳定 ID、未知 effect 类型或无效 shape 给出显式错误。
- 不修改 fixture 内容。

## Presentation API

Presentation 创建固定 seed 731 的场景，使用 axial XZ 映射显示至少 7 个六边形地块、一个可选目标和一个敌人意图。UI 暴露选卡、放置、结算、目标 HP、阶段与 12×3 时间轴。对 Domain 的调用通过明确方法完成，不能在按钮事件中重复实现伤害规则。

`VerticalSliceController` 的公共集成面冻结为：

```text
BuildSceneGraph()                         # 幂等，可由 Awake 与 Editor harness 调用
SelectCard(string stableId) -> bool
SelectTarget(string targetId) -> bool
TryPlaceSelected(int column, int row) -> bool
ResolveTimeline() -> ResolutionSnapshot
CurrentTargetHp, EnemyIntentResolved, TimelineSlotCount
```

fixture 固定 `stableId=lighting`、`targetId=target-01`。Controller 通过 `CardJsonAdapter.Parse` 读取 TextAsset，不自建第二套 JSON DTO。`BuildSceneGraph()` 重复调用不能复制 Camera、Canvas、地块或事件监听。

## Wave 02B1 卡牌交互契约

> 状态：已实现、主智能体审查并冻结

### Domain

```text
TimelineGrid.CanPlace(TimelineAction) -> bool
CardPlaySession(card)
SelectTarget(targetId, HexCoord) -> transition result
PreviewTimeline(TimelineGrid, TimelineCell) -> valid/invalid result
Commit(TimelineGrid) -> success/failure
Cancel() -> cancelled snapshot
```

`CanPlace` 不得改变占用或 action 集合，并与 `TryPlace` 复用同一内部合法性判断。Preview、失败 Commit 和 Cancel 都不能改变 `OccupiedCellCount`。最终 Commit 使用 `TimelineAction.FromCard`，保留 `lighting`、damage 100、单格 shape 与稳定目标 ID。

### Card presentation

```text
CardHandView.Build(CardViewModel)
CardHandView.SetInteractionState(state)
event CardSelected(stableId)
event CardCancelRequested(stableId)
event CardDragChanged(stableId, pointerPosition, phase)
```

卡牌视图只表达交互意图，不 raycast 世界、不解析 JSON、不判断时间轴合法性。原卡牌基准 `125×175`，完整卡面保持纵横比。Selected/Targeting/Scheduling 状态的右键取消优先于轨道镜头；composition root 在这些状态设置 `BoardCamera.InputEnabled=false`，退出后恢复。

### Board and timeline preview

```text
BoardRangePreview.Register(HexCoord, Component)
BoardRangePreview.Show(center, relativeOffsets)
BoardRangePreview.Clear()
TimelinePlacementPreview.Show(origin, shape, isValid)
TimelinePlacementPreview.Clear()
```

`lighting` 范围固定为 `(0,0),(1,0),(2,0)`。Board preview 只投影坐标并设置表现；Timeline preview 的 `isValid` 必须来自 Domain `CanPlace`，不得复制边界/冲突规则。所有清理幂等，镜头变化不改变坐标集合。

实际实现另外暴露只读诊断集合 `BoardRangePreview.ActiveCoordinates`、`MissingCoordinates`，用于证明真实格与越界格不会混淆。`CardHandView.ApplyVisualStateImmediate()` 仅供 Editor harness 在无帧等待的截图阶段同步视觉状态，不参与玩法判断。

### 主智能体共享接线

现有 `VerticalSliceController` 公共方法必须保持兼容。主智能体在 Agent 交回所有权后独占修改 Controller、`BoardTileView`、共享场景和 Editor harness，把 UI 事件接到现有选卡/目标/放置/结算链。Agent 不得直接改这些共享文件。

## 场景与截图

场景路径固定：`Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`。Wave 02B1 Editor harness 输出证据到 `docs/migration/unity-3d/04-verification/evidence/unity-slice-02b1/`，文件名包含视口尺寸和交互状态。PlayMode 测试优先通过场景控制器的公共交互面驱动，并对真实右键 UI 事件链另做集成覆盖。

## 版本与提交

任何契约变更先由主智能体更新本文件和 ADR，再调整实现。代理不创建独立分支或提交；主智能体精确暂存并形成单一目的检查点。

## Wave 02B2A 七卡 Schema 与 Earthquake 契约

> 状态：已实现、主智能体集成并冻结

### Typed card effect

```text
CardEffectKind = Damage | Elevation | Recover | Built | Poison | Clear
CardEffect = Kind + NumericAmount? + CreationId? + ClearMask?
CardDefinition = StableId + NumericId + FrontImage + Effects + EffectRange + Shape
```

- Numeric effect 使用非负整数；Built 的 `CreationId` 必填，`NumericAmount` 是创建数量；Clear 的 `ClearMask` 必须为非空、归一化的只读时间轴坐标集合。
- `FrontImage` 保留源 JSON 文件名映射并拒绝目录穿越；Presentation 通过该字段加载资源，不能从 stable ID 推导 `tower_card` 或 `poison_card`。
- Clear 必须独占一张卡的 effects；不与普通 Resolve effect 混合。
- 顶层 `shape` 只属于普通 TimelineAction；Clear 从 `effects[].value` 读取 mask，`shape=0` 不能成为普通单格占用。
- 现有 Damage constructor、`lighting` stable ID、`CardDefinition.Effects/Range/Shape` 只读面保持兼容。
- Adapter 对 number/string 异构 `value` 使用结构化 typed DTO 视图；禁止 regex、substring、fixture 文本替换。新增 JSON 包只能由主智能体记录 ADR 后串行处理。

### 普通 TimelineAction 与领域棋盘

```text
TimelineAction += TargetCoord + immutable Effects + EffectRange
CombatBoardState[HexCoord] -> BoardTileState(LogicalLayerCount)
ResolutionSnapshot += EffectResults(coord, beforeLayers, afterLayers, removed)
```

- `CardPlaySession` 为普通卡保留地图目标流程，并把已保存的 `TargetCoord` 传入 action；Preview、Cancel、失败 Commit 继续无副作用。
- Resolve 时按稳定坐标重新查询当前 tile；缺失 tile/range 越界为 no-op，不创建幽灵格，不保存 Unity GameObject。
- Godot logical height 为一基；Unity `elevation=logicalLayerCount-1`，实体 block 数等于 logical layer count。
- `earthquake` 对当前存在的中心加六邻格各执行 `+2`。Unity `elevation 0 -> 2` 对应 1 层变 3 层，顶部增量严格为 `0.64`。
- 源高度有效范围为 1..6；结果 `>6` 或 `<=0` 的 Domain 结果是 tile removed。Wave 02B2A 的演示 fixture 使用安全初始高度，表现销毁不是本切片门禁。

### Presentation

```text
CardHandHost.Build(IReadOnlyList<CardViewModel>)
CardHandHost -> CardSelected/CardCancelRequested/CardDragChanged(stableId,...)
HexTileColumn.ApplyLogicalLayerCount(count)
HexTileColumn -> LayerCount/Blocks/TopBounds/OccupantAnchor/Changed
```

- `CardHandView` 继续只负责单卡。Host/coordinator 管理两张真实卡、稳定顺序和单选互斥，不解析 CardDefinition。
- `HexTileColumn` 每层创建独立 FBX visual、renderer 和 collider，local Y 为 `index*0.32`；重复应用幂等。
- `TopBounds`、occupant anchor、选中 collider 和范围高亮从真实 block collection/bounds 更新，不使用固定顶部偏移。
- 主智能体独占 Controller/BoardTileView/scene 接线，把 `lighting` 和 `earthquake` 分别路由到既有 damage 与新增 elevation Domain；UI 不复制规则。
- Timeline invalid 不能只使用与敌人意图接近的红色；必须同时有边框、图标或形状标记，并接受 PlayMode/截图检查。

### Future clear boundary

`wind/tornado` 留到 Wave 02B2C，届时必须使用独立 `TimelineClearSession` 或等价窄 API：合法性只看 mask 边界，重叠仍合法，Commit 不创建 TimelineAction，命中任一格即移除完整 action，空清合法。02B2A 不实现该运行流程，但 schema 必须无损保留 clear mask。

### Wave 02B2A 实际冻结结果

- 七张 fixture 均由同一 `CardJsonAdapter` 解析为 typed effects；`FrontImage` 直接驱动资源加载，`tower_card` 与 `poison_card` 不再依赖 stable ID 推导。
- `CardHandHost` 复用单卡 `CardHandView`，提供稳定的两卡顺序、单选互斥、取消和 drag 转发。
- `earthquake` 的 action 保存稳定 `TargetCoord`、effects、range 与两格 shape；Resolve 对当前存在的中心加六邻格逐格返回 `EffectResults`。
- `HexTileColumn` 每个逻辑层创建独立 FBX visual、renderer 与 collider，层间 local Y 严格为 `0.32`；`TopBounds` 和 `OccupantAnchor` 从真实顶层 bounds 重算。
- 集成控制器只消费 Domain 结果更新表现；一层变三层时七个有效柱各新增两块，顶面与占位锚点均上移 `0.64`，缺失坐标不创建幽灵格。
- 冻结证据为 EditMode `67/67`、PlayMode `25/25`、Windows build `Succeeded`、Player marker `TIMEKEY_PLAYER_SMOKE_PASS`，详见 `04-verification/evidence/unity-slice-02b2a/verification-summary.md`。

## 解耦 R2 Application 契约

```text
CombatApplicationSession(ICardCatalog, CombatSliceState, TimelineGrid, initialActions?, ICombatTraceSink?)
SelectCard(stableId) -> CombatCommandResult
SelectTarget(CombatTarget) -> CombatCommandResult
PreviewTimeline(TimelineCell) -> CombatCommandResult
CommitTimeline() -> CombatCommandResult
CancelCard() -> CombatCommandResult
ResolveTimeline() -> CombatCommandResult + ResolutionSnapshot
Current -> CombatSessionView
```

- `CombatTarget` 只有 Entity 与 Tile；Controller 的旧 `SelectTarget(string)`/`SelectEarthquakeTarget(HexCoord)` 是兼容映射。
- Application 拥有 selected card、target、timeline origin、phase 与 commit/resolve 顺序；Presentation 不得直接命令 `CardPlaySession`。
- 初始 actions 全量预检后只放置一次。当前固定 enemy intent 仍保留 MIG-002 无效果行为。
- Recover/Built/Poison/Clear 在 handler/session 未支持时返回结构化失败，不静默 no-op。
- `ICombatTraceSink` 异常不能改变快照；R3 在不改 port 消费方向的前提下扩展 effect before/after 字段。
- R2 冻结证据为 Application/Diagnostics `14/14`、全量 EditMode `86/86`、PlayMode `26/26`、Windows build/Player smoke/11 张截图全通过。

## 解耦 R3 Composition、Catalog 与 Presenter 契约

```text
CombatCompositionRoot
  -> CardContentCatalog + CardEffectRegistrationCatalog
  -> CombatApplicationSession
  -> VerticalSliceController.Initialize(...)
  -> CombatPresentationBinding.Bind(...)
```

- `CardContentCatalog` 实现 `ICardCatalog`，接受任意非空且 stable ID 唯一的卡牌列表并保持顺序；新增第八张普通卡不得修改 Controller 路由。
- `CardContentEntry` 只从 `CardDefinition.FrontImage` 生成 `Art/Battle/Cards/<stem>`，不得从 stable ID 猜素材。
- `CardEffectRegistrationCatalog` 只声明当前真正支持的 effect kind；未注册效果保持 visible + explicit failure，不静默 no-op。
- `CombatCompositionRoot` 是运行时状态、session、trace、sprite 生命周期和 Scene Inspector 引用的唯一组装入口；不得演化为全局 Service Locator。
- `CombatPresentationBinding` 独占输入订阅/解除订阅；`CardHandPresenter`、`BoardRangePresenter`、`TimelinePresenter`、`CombatHudPresenter` 各自只刷新一个界面区域。
- `VerticalSliceController` 保留既有公共 facade 和真实 3D 棋盘/射线同步，不再解析 JSON、加载 Resources、构造 catalog/session、持有 36 格列表或按 stable ID 选择卡图/Timeline 标签。
- `CombatTraceEntry` 的结算记录包含 effect kind 与 before/after；`UnityCombatTraceSink` 可在 Inspector 关闭，sink 故障不得改变结果。
- 最终依赖中 `Presentation` 不引用 `Infrastructure`，`Domain` 与 `Application` 均为 `noEngineReferences=true`，asmdef 图无环。

R3 冻结证据为 full EditMode `92/92`、full PlayMode `31/31`、Windows build `Succeeded`、Player marker `TIMEKEY_PLAYER_SMOKE_PASS` 和 14 张人工检查截图，详见 `04-verification/evidence/unity-decoupling-r3/verification-summary.md`。

## Remaining Cards Gate 0 冻结契约

> 状态：2026-08-01 Gate A Recover 与 Gate B Built/Poison 通过；实现按 Gate C 推进

```text
ordinary: CardDefinition -> CombatApplicationSession -> CardPlaySession
          -> TimelineGrid -> ICardEffectHandler -> typed ResolutionSnapshot

clear:    CardDefinition.ClearMask -> TimelineClearSession
          -> TimelineGrid PreviewClear/TryClear -> removed action snapshots
```

- Recover/Built/Poison 共用纯 Domain occupant：稳定 runtime ID、HexCoord、kind/creation、attitude、HP/MaxHP、PoisonStacks 与能力标志。表现对象不是 identity。
- 普通 handler 使用统一 result buffer；`ResolutionSnapshot.EffectResults` 保持 earthquake 兼容，并新增 occupant before/after 结果。Recover/Built/Poison 的表现只能消费该结果。
- 目标选择与 Resolve 都调用 typed 规则：Recover 为存在且 `HP<MaxHP` 的生命 occupant；Built 为存在且空的 tile；Poison 为存在、存活且支持状态的 occupant。Resolve 按稳定坐标/ID重判。
- Built 本阶段只支持 `creation=tower,value=1`；未知 creation/value 在时间轴占格前显式失败。Tower 为 Neutral/Middle、HP100，不自损。
- Poison 每次直接累加 2 且无游戏上限；本阶段不 tick。
- Clear 只使用 typed `ClearMask`，不创建 TimelineAction、不占格。合法性只看 12x3 边界；空清成功；按 `TimelineAction` identity 去重，任一格命中移除完整 action，玩家/敌人不做过滤。
- Application 显式区分 OrdinaryTimeline 与 TimelineClear；Controller 只允许按交互模式做通用路由，禁止 stable-ID/effect 大 switch。
- Tower 使用原 `tower.png` billboard Prefab；Poison 使用原 `poison_icon.png` + 层数 Prefab；二者挂真实 `OccupantAnchor`，逻辑不存于 Prefab。
- Gate A 架构验收：`recover` 完整链不得修改 `VerticalSliceController.cs`。

Gate A 已以 Controller 零 diff、全量 EditMode `107/107`、Recover 公共 Scene PlayMode `1/1` 和 5 张实际渲染图验收。共享 occupant/result 与 `ICardEffectHandler.Supports(CardEffect)` 契约现为 Gate B 的输入，不得由 Built/Poison 各自复制第二套状态模型。

Gate B 已以全量 EditMode `130/130`、Scene PlayMode `3/3`、定向 authoring、两个保存 Prefab 和 8 张实际渲染图验收。`CombatOccupantPresenter` 只以 creation→Prefab 序列化表和 snapshot 状态创建/刷新 View，不持有 Domain identity；Tower decay 与 Poison tick 继续留给 02B3。

Gate 0 最小冒烟为 EditMode `24/24`、PlayMode `10/10`，0 失败。旧 R3 build/Player/未受影响视觉先继承；修改对应运行程序集/Scene 后在 Gate D 全量刷新。
