# Unity 局内战斗共享集成契约

> 状态：Combat Shell Gate E 已关闭；Wave 03 仅可扩展局外地图契约
> 负责人：主智能体
> 最后验证日期：2026-08-03
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

### Clear boundary

`wind/tornado` 已在 Wave 02B2C 使用独立 `TimelineClearSession`：合法性只看 mask 边界，重叠仍合法，Commit 不创建 TimelineAction，命中任一格即按 identity 去重并移除完整 action，空清合法。Presentation 只消费三态 preview 与 removed action snapshot。

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

## Remaining Cards 总契约

> 状态：2026-08-02 Gate A Recover、Gate B Built/Poison、Gate C Clear 与 Gate D 终验全部通过

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

Gate C 已以全量 EditMode `152/152`、Scene/Presentation PlayMode `4/4`、定向 authoring 和 9 张实际渲染图验收。`ClearTimelinePreview` 使用红 `!`、蓝 `○`、绿 `HIT` 三态冗余；取消恢复原 action，Wind 完整移除敌方 action，Tornado 空清保留未命中的 action。

Gate 0 最小冒烟为 EditMode `24/24`、PlayMode `10/10`，0 失败。Gate D 已在最终集成态刷新 full EditMode `152/152`、full PlayMode `38/38`、54 张 PNG、Windows build `Succeeded`（`207171486` bytes）和 Player smoke（退出码 0、marker 存在）；结构化证据和逐图结论位于 `../04-verification/evidence/remaining-cards-gate-d/`。

## Turn Lifecycle Gate 0 冻结契约

> 状态：2026-08-02 Gate 0 完成；Gate A 可以按已审查 Prompt 实施

```text
InitialStart:
  ProcessingTurnStartStatuses -> BattleFlowNextTurnHook -> RefreshingEnemyIntents
  -> PlayerReady/InputUnlocked

EndTurn:
  EndTurnRequested/InputLocked -> ResolvingTimeline
  -> RunningBuildingBehaviors -> ClearingTimeline
  -> ProcessingTurnStartStatuses -> BattleFlowNextTurnHook
  -> RefreshingEnemyIntents -> PlayerReady/InputUnlocked
```

- `TurnLifecyclePhase` 与现有卡牌交互 `CombatSessionPhase` 分离；生命周期运行期间所有卡牌、地图、Timeline 和结束回合输入统一锁定，完成或结构化失败后恢复到确定状态。
- Timeline 结算顺序固定为 `x=0..11` 外层、`y=0..2` 内层。多格 action 以稳定 `ActionId` 去重，玩家和敌人 action 使用同一网格、同一排序与同一身份规则。
- `ActionId` 由生命周期序列号和确定性 ordinal 生成，不得复用卡牌 ID、敌人 ID、坐标、对象引用或显示文本。preview、commit、resolve、clear 与所有表现映射必须保留同一 ID。
- 不可变 action snapshot 至少包含：`ActionId`、actor、priority、source runtime ID/coord、target runtime ID/coord、card/effect ID、display payload、origin、shape、occupied cells、validity/reason 与 resolve state。
- 表现层只消费 Application snapshot/View，不解析规则、不从 Label/颜色/Prefab 实例反推 identity，也不直接读取或修改 Domain collection。
- 当前 Godot 敌方 source command 为空；Unity 必须返回明确的 `UnsupportedSourceCommand`，不得伪造伤害。意图优先级默认 `0`、中心祭坛 `999`；高优先级先执行，同优先级使用显式 seed，最多保留 5 个。
- Tower 每次建筑阶段执行同一 decay 规则：`100->50`，下一次 `50->0` 后以 `Remove` 死亡语义原子清理 occupant 与地图占用。
- Poison 回合开始阶段严格三遍：先冻结旧状态，再聚合传播，最后旧 source 按 `ceil(MaxHP*0.1*entryStacks)` 受伤并衰减 1 层；本轮新感染不伤害、不衰减。
- generic enemy 继续使用 `RemainBroken`；Tower 与 typed Radar underling 使用 `Remove`。任一阶段必须先完成全量先验校验再写状态，任意失败不得遗留半清 timeline、半更新 occupant 或陈旧 UI mapping。
- 交互优先级冻结为：`resolving/disabled > card targeting > scheduling/drag/clear > idle hover > none`。取消、清除、失败和对象移除均以 `ActionId` 原子清理卡牌、敌人、Timeline、地图之间的双向映射。

Gate 0 来源语义、Unity 缺口和视觉交互审计分别记录在 `agents/reports/turn-lifecycle-*.md`；所有权互斥 Prompt 已由主智能体审查通过，结论见 `agents/prompt-review-turn-lifecycle.md`。

### Gate A 实际冻结结果

- `TurnLifecycleRunner` 是纯 Domain 的单入口编排器；InitialStart 只跑共享尾段，EndTurn 严格跑完整冻结顺序。输入锁只由 `phase != PlayerReady` 派生。
- processor typed failure 会停在当前 phase、锁存 fault 并拒绝重放已完成 action；计划预检失败仍停在 PlayerReady。本 Gate 不实现任意异常后的全状态 transaction。
- `TimelineActionIdentity.FromSequence(sequence, ordinal)` 是正式值身份；Application session 的普通卡 preview、commit、resolve 与 clear snapshot 保留同一 ID，旧 fixture 构造仅使用兼容 transient ID。
- `TimelineGrid` 的重复检查、x 后 y 去重、clear 和 resolution snapshot 全部改用 ActionId；`CreateResolutionPlan()` 是 Runner 的只读输入，不改变 grid。
- 不可变 presentation snapshot 已冻结完整字段和 typed validity/reason/resolve state；Gate B 只能增加 catalog 投影与 View，不能改变身份或玩法规则。

验证为定向 EditMode `85/85`、全量 EditMode `183/183`、全量 PlayMode `38/38`，详见 `../04-verification/evidence/turn-lifecycle-gate-a/verification-summary.md`。Gate A 未修改 Scene/Prefab/Presentation，前置汉化视觉、build 与 Player smoke 证据按未受影响边界继承。

## Wave 02B3 最终共享契约

- phase 只能按 ADR 0008 的固定顺序前进；02B4 已只通过命名的 `BattleFlowNextTurnHook` 接入。
- `TimelineActionIdentity` 是 preview/commit/resolve/clear/presentation 的唯一 action 键；stable card ID、格子或 View 都不能替代它。
- enemy intent 候选使用显式 seed、priority 和最多五个结果；执行前重判完整 source/target/shape/effect。空 command 返回 `UnsupportedSourceCommand` no-effect。
- `CombatSliceState.TryApply` 先全量预检再提交 occupant change；runtime ID 与 coord 必须同时匹配。
- building 与 Poison 都消费稳定快照。Tower 创建周期 100→50、下一周期 50→0 Remove；Poison 新感染与本周期伤害快照隔离。
- `CardEffectFrame`/`TimelineActionFrame` 来自保存 Prefab，所有玩家可见文字与 world TextMesh 使用 Silver Font/Material。

最终验证为 full EditMode `236/236`、graphical PlayMode `53/53`、Windows build `Succeeded`（`211055434` bytes）和 actual Player smoke exit 0。

## Wave 02B4 Gate 0 冻结契约

> 状态：2026-08-02 Gate 0 完成；ADR 0009 已接受

- starter deck 的有序输入为 `lighting x2, earthquake x2, recover x2, wind x2, tower x2, poison x2`；每张实体卡另有稳定 `CardInstanceId`。
- deck top 为数组尾端；hand limit 7、正式抽牌请求 5。deck 空时只把非空 discard 洗回一次；双空返回 typed exhausted。
- 所有洗牌只消费显式 seed/自有随机状态，固定 seed 的初始顺序和跨多轮结果必须可复现。
- EndTurnRequested 冻结 occupied cells、remaining hand instances 和 action display snapshots；现有 runner 顺序不变。
- reserved hook 固定为 `discard -> timecoin -> phase/Era -> reshuffle-if-needed -> draw 5`。InitialStart 只初始化/抽 5，不推进、不发时间币。
- 时间币按冻结的 12x3 Timeline 空格计算，不能在 clear 后读取空网格；多格 action 按实际 occupied cells 计数。
- outcome 为单一 authoritative transaction；Victory/Defeat 互斥、重复同结果幂等、相反结果 typed conflict，终局后输入锁定。
- Victory 只发一次最小 reward entry；typed return payload 明确携带 outcome/Era/phase/timecoins/deck/context。完整局外奖励与 OutScene 不迁移。
- card stable ID、card-instance identity、action identity 三者不得互代；牌离开 hand 或 View 销毁后，既有 action frame 继续消费保存的 immutable display payload。

源行号与迁移差异见 `agents/reports/deck-battle-flow-source-semantics.md`；决策见 ADR 0009。

### Wave 02B4 Gate A 实际冻结

- `DeckState.CreateStarter(seed, identityScope)` 是牌区聚合入口；`CardInstanceId` 在三堆移动中稳定，`DeckCommandId` 为直接操作去重键。
- `DeckState.Draw/DiscardFromHand/ForceDiscardHand` 返回 before/after counts、移动 instance IDs、shuffle 信息与 typed reason；所有集合防御性只读。
- `BattleRoundLedger.Apply` 是 Era/phase/timecoin 唯一写入口，同 sequence+payload 返回原结果，冲突 payload 显式失败。
- `BattleSettlementState` 是 outcome/reward/return 唯一写入口；终局 snapshot 直接提供输入锁状态。
- Gate A Unity 结果为定向 EditMode `44/44`、完整 EditMode `280/280`；证据位于 `../04-verification/evidence/deck-battle-flow-gate-a/`。

### Wave 02B4 Gate B 实际冻结

- `BattleFlowHookRequest` 是 pre-clear 冻结边界，包含 lifecycle sequence、remaining card-instance identities、occupied cell count 与 immutable action display snapshots。
- `DeckState.DiscardHand` 先验证整批 frozen identities，再一次性移动；任一缺失/重复/无效 identity 保持三堆不变。
- `CombatTurnLifecycleCoordinator` 只把 `BattleFlowNextTurnHook` 注入 ADR 0008 预留位置；Timeline、building、clear、status、intent phase 顺序没有新增或重排。
- `CombatApplicationSession` 正式手牌选择支持 card-instance identity；提交 action 后实体卡立即进入 discard，结束回合只弃 remaining hand。旧 stable-ID 入口保留为兼容路径。
- outcome、reward claim 与 return payload 只通过 battle-flow typed boundary；Victory/Defeat 后 Session 转为 `Resolved` 并拒绝后续 action。
- Gate B 验证为 Application `10/10`、集成 `60/60`、full EditMode `293/293`、full graphical PlayMode `53/53`；证据位于 `../04-verification/evidence/deck-battle-flow-gate-b/`。

### Wave 02B4 Gate C/D 最终冻结

- `BattleFlowPresenter` 只投影 deck/hand/discard、Era/phase、timecoins 与输入锁；`BattleSettlementPresenter` 只消费 authoritative settlement/reward snapshot。
- `CardHandPresenter` 以 `CardInstanceId` 创建动态实体卡 View，同 stable ID 的重复卡不会合并；提交后的 action frame 继续使用独立 `TimelineActionIdentity` 与 immutable display payload。
- Scene 在 Play 前保存 `BattleFlowPanel`、Presenter、结算按钮和终局输入锁引用；新增稳定 UI 来自保存 Prefab，全部简体中文使用 Silver。
- 自动胜利只调用纯 Domain `BattleVictoryRule`：`maximumHp > 0` 且 `currentHp * 10 <= maximumHp`。绝对 HP 常量、UI 文本或 View 不得决定 outcome。
- Gate D 验证为 full EditMode `300/300`、Direct3D12 PlayMode `61/61`、18 张人工复核 PNG、Windows build `Succeeded`（`211133001` bytes）和 actual Player exit 0；最终证据位于 `../04-verification/evidence/deck-battle-flow-gate-d/`。

## Combat Shell Gate 0 冻结

```text
Bootstrap (persistent, Build index 0)
  -> SceneFlowRoot + TransitionCanvas + Input/Focus gate
  -> unique EventSystem + AudioRoot + diagnostics
  -> additive content Scene with exactly one typed content entry
```

- Application 新边界只包含 Unity-free `SceneId`、typed request/payload/outcome、phase/failure/result、局外 state 与 effects port；Unity Scene 名称只在 Composition route catalog。
- 成功 phase 固定为 `Idle -> InputLocked -> Covering -> LoadingTarget -> ActivatingTarget -> BindingPayload -> WaitingForFirstRenderableFrame -> UnloadingSource -> Revealing -> InputUnlocked -> Idle`。
- `CombatLaunchPayload` 必须在 Combat 内容根启用前绑定。`CombatOutcome` 组合原 launch 的 run/room/correlation identity 与 02B4 `BattleReturnPayload`；Victory 要求 reward claimed，Defeat 不走 reward。
- 同 sequence + 同 fingerprint 幂等；同 sequence + 不同请求冲突；stale 与 Busy typed fail。异步回调以 generation 拒绝取消后的陈旧写入。
- load/activate/bind/first-frame/unload 失败均在遮罩下回滚目标、恢复来源 camera/content/focus，最后揭罩并解锁。
- Bootstrap 唯一拥有 EventSystem、TransitionCanvas 与 AudioRoot；内容 Scene 不得保留副本。该约定由 ADR 0010 明确取代 ADR 0004 中 Combat Scene 自有 EventSystem 的历史点。

### Gate A 实现态

- `SceneFlowCoordinator` 已实现 sequence/fingerprint 幂等、conflict/stale/Busy、完整成功 phase、typed failure 与 rollback/unlock；Application 继续 `noEngineReferences=true`。
- `CombatLaunchPayload` 防御性复制 deck，并覆盖 run/room/character/chapter/round/time/battle identity；`CombatOutcome` 保留原 launch identity 与 02B4 return，Victory reward guard 已测试。
- `OutOfBattleShellState` 按 outcome correlation 一次消费，并校验 run/room/launch correlation；同 payload 重放幂等，不同 payload 冲突，胜利房间只结算一次。
- state store 在 Binding 阶段预写入可回滚副本；source unload operation 成功启动后提交。提交前失败恢复旧 state，提交后失败保留 target 并只揭罩/解锁。
- Unity runtime 使用真实 additive `LoadSceneAsync`/activation/unload。取消 pending load 时先在遮罩下允许激活再卸载，避免遗留 90% operation；Bootstrap 初始 fault 通过 Task 可观察并恢复遮罩/输入。
- Combat 只接受 `CombatLaunchPayload` 进入，只接受目标一致的 `CombatOutcome` 离开；settlement、return 与 launch 的 battle tag/seed 必须一致。
- `GameOver -> MainMenu` 清理旧 run 的 shell/launch/outcome state；后续新 run 不得被旧 RunId 拒绝。
- 六 Scene build、唯一持久对象、Combat bind-before-enable、真实 bind failure 回滚与多 run 往返已由 EditMode `330/330`、Direct3D12 PlayMode `64/64`、build/Player 验证。

## Combat Shell Gate B frozen contract

- `CombatSessionView` is the only Top HUD state source. Presentation receives Era, phase, timecoins, draw/hand/discard counts, target current/max HP, player identity and input lock as immutable values.
- `CombatTopHudPresenter` and `CombatBattleBackground` are saved presentation objects. They cannot mutate deck, turn, outcome, target or action-identity state.
- Modal input uses a scoped `SceneInputLockState.Acquire()` lease. Closing a modal releases only that lease and cannot clear an independent transition lock.
- Stable combat UI event callbacks reject input while `SceneInputLockState.IsLocked`; programmatic Application commands and Domain contracts are unchanged.
- `ISceneRevealPresentation` is Unity-free. Composition starts the active content reveal after uncover and waits for completion before input unlock; disable/missing/zero-duration presentations complete deterministically.
- The legacy ground collider remains active for board interaction while only its near-black renderer is disabled. Background materials, textures and six renderers live in the saved Prefab/Scene.
- Gate B is frozen by full EditMode `334/334`, CombatShell PlayMode `5/5`, full D3D12 PlayMode `69/69`, post-build asset verification `3/3`, Windows build and actual Bootstrap Player smoke. Evidence is under `../04-verification/evidence/combat-shell-gate-b/`.

## Combat Shell Gate C frozen contract

- GameStart reveal is approximately three seconds and non-skippable in the authored production Prefab. The source key, independent three-character motion and black/gold/black sequence report explicit completion; immediate completion stops active routines.
- The six MainMenu commands are New Game, Seed Game, Continue, Settings, Database and Exit. Continue remains disabled; Database is unavailable; saved settings connect master volume/fullscreen and explicitly disable absent music/SFX channels.
- Presentation emits `MainMenuCommandRequest` only. Composition creates `RunStartPayload` with explicit run ID/seed/state, owns increasing sequence identity and owns application quit.
- Settings, seed and modal overlays own scoped input-lock leases, restore prior focus and give Escape priority to the top open overlay. Idle Escape opens Settings; Return/KeypadEnter confirms an open seed input once. Repeated navigation commands are suppressed until failure or source unload.
- Presentation emits a typed settings snapshot only. Composition owns PlayerPrefs, master AudioListener volume and Screen fullscreen application; unavailable Music/SFX channels stay disabled.
- MainMenu background, title, clock and source-texture button layers implement `ISceneRevealPresentation`; SceneFlow cannot unlock input before the layered entrance is complete.
- `TransitionVisualPresenter` owns only cover/reveal visuals and completion. SceneFlow awaits cover before disabling the source camera and awaits both transition and content reveal before input unlock.
- Bootstrap remains the unique EventSystem/TransitionCanvas/Audio owner. GameStart and MainMenu only contain their typed content entry, content camera, responsive canvas, Presenter and scene adapter.
- Source Scene lifetime cancellation is checked before Bootstrap accepts a request. Once accepted, normal source unload cannot cancel post-commit reveal or input unlock.
- Gate C is frozen by full EditMode `340/340`, full D3D12 PlayMode `85/85` and 51 manually inspected PNGs. Evidence is under `../04-verification/evidence/combat-shell-gate-c/`.

## Combat Shell Gate D frozen contract

- `RunStartPayload.CharacterId` is explicit and participates in the request fingerprint. The compatibility constructor keeps `silver-character`; new production navigation passes it explicitly.
- `OutOfBattleShellState.CreateCombatLaunch(...)` is the only production launch factory. It preserves run, character, chapter, Era, phase, timecoins and deck snapshots while assigning stable room, launch-correlation and battle identities.
- `CombatCompositionRoot` consumes the active typed launch before presentation activation. Direct scene loads retain test-fixture fallback, but formal SceneFlow never substitutes fixture seed, deck or round state.
- The first accepted `CombatOutcome` closes `ActiveLaunch`. Exact same-correlation replay is idempotent; a different or opposite outcome after consumption is rejected. Pre-commit rollback can still restore the original launch snapshot.
- Victory navigation waits for the authoritative reward claim, then returns to the same shell state exactly once. Defeat bypasses reward, binds the typed outcome to GameOver, and returning to MainMenu clears the run.
- Out-of-battle and GameOver Presentation emit only typed room/command callbacks. Composition alone owns payload construction, retry identity, SceneFlow request sequencing and application state.
- Saved OutOfBattleShell/GameOver Prefabs use an ordinary content root, one child `GateDCanvas`, one sibling content camera and Silver on every visible `Text`; scenes contain one typed `SceneContentEntry` and prefab-backed content root.
- Gate D is frozen by full EditMode `343/343`, full graphical D3D12 PlayMode `92/92`, targeted production Victory/Defeat round trips `2/2`, asset constraints `3/3`, and 18 manually inspected PNGs across three viewports. Build, actual Player smoke and repeated three-cycle stability remain Gate E delivery checks.

## Combat Shell Gate E delivery contract

- `LayeredSceneRevealPresenter` remains Presentation-only and implements the existing Unity-free `ISceneRevealPresentation` boundary. SceneFlow waits for completion; no reveal uses `Task.Delay` as a substitute for rendered progression.
- OutOfBattle reveal order is background, context, room. Combat reveal order is status, timeline, detail/effect, hand. GameOver reveal order is background, panel. All layers finish at alpha 1 with interaction and raycasts restored.
- Missing layers, zero duration, disable and destruction complete deterministically so SceneFlow cannot retain a stale input lock.
- One persistent Bootstrap must survive three consecutive OutOfBattle -> Combat -> Victory -> OutOfBattle cycles. Each cycle preserves its own room and launch/outcome correlation identities, settles exactly one room and unloads the prior content Scene.
- Build Settings remain exactly six enabled Scenes in the frozen order. The Windows Development build must include `Silver-ATTRIBUTION.txt`; the current build artifact satisfies this contract.
- The actual Player smoke uses `-timekeyCombatShellGateESmoke`, performs three typed cycles, verifies one Bootstrap and one content entry, resizes to 2560x1080 and exits with input unlocked.
- The out-of-battle shell reuses the byte-identical Godot ocean tile through `OutOfBattleOceanBackground`; its UV scale preserves square pixels across 1280x720, 1920x1080 and 2560x1080.
- Gate E is frozen by full EditMode `343/343`, full graphical Direct3D12 PlayMode `100/100`, the six-Scene Windows build, three-cycle actual Player smoke and 17 manually reviewed PNGs. Post-review coverage proves running reveal completion stays terminal, evidence outputs are fresh, render-counter semantics are explicit and positive post-GC growth is not stable or increasing. Gate B four-yaw evidence remains inherited because the combat camera/background boundary was unchanged.

## Wave 02B3R effect-frame stability contract

- `TimelineActionIdentity` is the scheduled-action primary key. `CardInstanceId`, card stable ID and map runtime ID are reverse lookup keys only; duplicate stable IDs never choose a frame unless exactly one match exists.
- `TimelineActionFrame` visuals are the union of actual occupied cells and perimeter edges. The root bounding rect is layout-only and must not render missing Tower/Poison cells or receive raycasts.
- Binding owns one overlay priority coordinator: `Disabled > Resolving > CardTargeting > Clear > Drag > Scheduling > IdleActionHover > None`. Refresh reconstructs the base owner from the Application view.
- `IdleTileInspect` is allowed only with no selected card in `Idle` or `Cancelled`. Same-point same-tile click toggles off; another tile replaces; blank, Escape and short right-click clear; right drag does not clear. Committed, Resolved and global Scene input lock reject inspection.
- Card selection, cancel, commit, resolve, occupant death, Scene rebind and unbind clear stale inspection/highlight/index state. Timeline pointer exit cannot leave Application in `TimelinePreview` while only Presentation is cleared.
- Closure evidence is full `351/351 + 107/107`, 9 fresh three-viewport/resize PNGs, Windows build and actual Player smoke.
