# Unity 局内战斗模块架构

> 状态：Remaining Cards Gate D 已实现并通过验证
> 最后验证日期：2026-08-02

## 依赖方向

R3 的运行时程序集形成单向、无环依赖图：

```text
TimeKey.Domain          -> []
TimeKey.Application     -> Domain
TimeKey.Infrastructure  -> Domain, Application
TimeKey.Diagnostics     -> Domain, Application
TimeKey.Presentation    -> Domain, Application
TimeKey.Composition     -> Domain, Application, Infrastructure,
                           Diagnostics, Presentation
```

对应定义位于 `unity/Assets/_Project/Runtime/*/TimeKey.*.asmdef`。`Domain`、`Application` 和 `Diagnostics` 均设置 `noEngineReferences=true`；`Presentation` 已移除 R2 的 `Infrastructure` 引用。只有最外层 `Composition` 同时看见内容实现、诊断端口实现和 Unity 表现层，因此内层模块不会反向查找 Scene、Prefab、`Resources` 或 View。

## 运行时组装与生命周期

`unity/Assets/_Project/Runtime/Composition/CombatCompositionRoot.cs` 是 Scene 的显式组合根。它通过 Inspector 持有 `VerticalSliceController`、`CombatPresentationBinding`、卡牌 `TextAsset` 列表和 `UnityCombatTraceSink`，在 `Awake()` 中执行一次 `Initialize()`：

```text
serialized card TextAssets
  -> CardContentCatalog.FromJson
  -> CardContentEntry.ArtworkResourcePath
  -> Resources.Load card artwork
  -> CardViewModel list + CombatPresentationBinding.ConfigureCards
  -> CombatSliceState + TimelineGrid + enemy intent
  -> CombatApplicationSession
  -> VerticalSliceController.Initialize
```

组合根拥有 `CombatApplicationSession` 和运行时创建的 `Sprite`。`OnDestroy()` 负责释放 session，并按 Play/Edit 模式销毁这些 Sprite。它不拥有目标规则、时间轴占格或效果结算；缺失 Inspector 引用、空 fixture 列表、空 fixture 元素或缺失卡图会在初始化时显式失败。

没有使用服务定位器或全局单例。新增依赖必须从 Scene Inspector 或构造参数进入组合根，生命周期也必须由创建它的边界关闭。

## 输入、用例与表现链路

```text
CardHandHost / TimelineCellView / resolve Button
  -> five narrow Presenters
  -> CombatPresentationBinding events
  -> VerticalSliceController compatibility facade and Unity hit testing
  -> CombatApplicationSession commands
  -> CardPlaySession 或 TimelineClearSession + TimelineGrid + ICardEffectHandler
  -> CombatCommandResult / ResolutionSnapshot / CombatSessionView
  -> CombatPresentationBinding.Refresh
  -> four Presenters update serialized Views
```

`CombatApplicationSession` 是选卡、typed target、timeline origin、commit/resolve phase 的用例状态所有者。`TimelineGrid` 是占格、结算顺序和已注册 `ICardEffectHandler` 的权威来源。`VerticalSliceController` 仍保留兼容 facade、Unity 射线检测、棋盘 mesh/collider 同步与镜头协调，但不解析 JSON、不加载卡图，也不拥有应用会话的创建或销毁。

`unity/Assets/_Project/Runtime/Presentation/Bindings/CombatPresentationBinding.cs` 统一对 Controller 暴露输入事件，并把同一个 `CombatSessionView` 分发给五个窄 Presenter：

| Presenter | Inspector 依赖 | 职责 |
| --- | --- | --- |
| `CardHandPresenter` | `CardHandHost` | 构建卡牌 ViewModel、同步选中态和交互 phase、转发选择/取消/拖拽 |
| `BoardRangePresenter` | `BoardRangePreview` | 根据 typed tile target 和卡牌 range 显示或清除范围 |
| `TimelinePresenter` | `TimelinePlacementPreview`、`ClearTimelinePreview`、序列化 `TimelineCellView` 列表 | 注册时间轴格、渲染普通 action，并显示 Clear 越界 `!`、空格 `○`、命中 `HIT` 三态 |
| `CombatHudPresenter` | 状态文本、目标文本、Resolve 按钮 | 显示会话 phase/目标状态、控制 Resolve 可用性并转发命令 |
| `CombatOccupantPresenter` | creation→Prefab 表、Tower/PoisonStatus Prefab、Scene Camera | 只按 occupant snapshot/result 在真实 `OccupantAnchor` 创建或刷新建筑、生命与状态表现 |

`Bind()`/`Unbind()` 均可重复调用，不复制监听；`CombatPresentationBinding` 不解析内容，也不重算 Domain 规则。

## 内容、原图与效果支持

`unity/Assets/_Project/Runtime/Infrastructure/Cards/CardContentCatalog.cs` 实现 Application 的 `ICardCatalog`。它接受至少一张 typed `CardDefinition`，保持输入顺序，拒绝重复 stable ID，并同时公开只读 `Cards` 与 `Entries`。`CardContentEntry` 从 JSON 的 `front_image` 文件名派生严格资源路径：

```text
front_image: "tower_card.png"
  -> ArtworkResourcePath: "Art/Battle/Cards/tower_card"
```

因此普通新增卡牌由 JSON、同名原图和 Scene 中的 `TextAsset` 引用驱动，不允许在 Controller 中按 stable ID 增加玩法或图片分支。

`unity/Assets/_Project/Runtime/Infrastructure/Effects/CardEffectRegistrationCatalog.cs` 是内容可用性登记表。`CreateVerticalSlice()` 当前登记 `Damage`、`Elevation`、`Recover`、`Built`、`Poison` 和 `Clear`；其中前五种由普通 handler 在 Resolve 执行，`Clear` 只用于进入独立 `TimelineClearSession`。登记表不是效果执行器；真正执行仍由 Domain handler 或 clear session 完成，缺失实现会在改变时间轴前显式失败。

## 结构化诊断

Application 端口 `unity/Assets/_Project/Runtime/Application/Ports/ICombatTraceSink.cs` 定义 `CombatTraceEntry`。每条记录始终包含 `Command`、`PhaseBefore` 和 `PhaseAfter`，并可附带 stable ID、typed target、timeline origin、failure reason、`EffectKind`、`BeforeValue`、`AfterValue`。结算会为 damage 和 elevation 记录可比较的效果前后值。

`unity/Assets/_Project/Runtime/Composition/UnityCombatTraceSink.cs` 是可在 Inspector 关闭的 Unity 适配器。开启时输出以 `TIMEKEY_COMBAT_TRACE` 开头的 `key=value` 日志；关闭时不产生日志。Application 仍只依赖 `ICombatTraceSink`，诊断失败不得改变 seed、战斗快照或命令成败。

## 模块所有权

| 模块 | 拥有 | 不得拥有 |
| --- | --- | --- |
| Domain | 格坐标、卡牌 typed schema、时间轴合法性、效果处理器、occupant 状态、clear mask 与战斗快照 | Unity 类型、资源路径、UI、Scene 生命周期 |
| Application | 命令顺序、typed target、`CombatInteractionMode`、普通/clear 会话 phase、ports、结构化结果与 trace entry | JSON/`Resources`、Prefab、表现规则 |
| Infrastructure | JSON adapter、`CardContentCatalog`、原图资源路径映射、效果支持登记 | 玩法结果、View、Scene 生命周期 |
| Diagnostics | no-op/collecting trace sink | Unity 日志、改变命令结果 |
| Presentation | 输入映射、五个 Presenter、Binding、相机与世界/UI 同步 | JSON 解析、stable ID 玩法分支、资源定位、占格规则 |
| Composition | Inspector 引用、对象图创建、Unity trace sink、session/Sprite 生命周期 | Domain 规则、卡牌专用 Controller 分支 |

## 扩展约束

- 新普通卡牌：增加合法 JSON、`front_image` 对应原图和组合根 fixture 引用；若只使用已登记效果，不修改 Controller 或 Presenter。
- 新效果：扩展 typed schema/adapter、Domain `ICardEffectHandler` 和 Application 目标策略，再加入 `CardEffectRegistrationCatalog`；Presenter 只消费结果。
- 新表现：优先扩展窄 Presenter 或新增 Binding 输出，不让 Application 引用 Unity。
- 新基础设施：实现 Application/Domain 定义的端口，由 Composition 注入；禁止内层模块反向引用。

## Remaining Cards 领域边界

`CombatOccupantState` 以 runtime ID 与 `HexCoord` 共同标识目标，并保存 attitude、creation、HP/MaxHP、PoisonStacks 和能力字段；Unity 对象从不作为领域 identity。Recover/Built/Poison handler 复用公共 `CardEffectResultBuffer`，在 Resolve 重新查询目标并产出不可变 occupant before/after，Presenter 只消费这些结果。Tower 以 Neutral、HP100 创建，Poison 每次直接累加 2；Tower decay 与 Poison tick 刻意留给 02B3。

`CombatApplicationSession` 通过 `CombatInteractionMode.OrdinaryTimeline` 与 `TimelineClear` 区分流程。Wind/Tornado 的 Clear 只从 typed `ClearMask` 取得范围，经 `TimelineClearSession` 调用 `TimelineGrid.PreviewClear/TryClear`；它不创建普通 action，空清合法，任一格命中后按 action identity 去重并完整移除。对应决策见 `adr/0007-occupant-effects-and-independent-clear-session.md`。

保存资产总数现为十个 Prefab：TimelineCell、CardView、两种 HexBlock、HexColumn、TargetView、Tower、PoisonStatus、CardEffectFrame 和 TimelineActionFrame。Remaining Cards 历史证据位于 `../04-verification/evidence/remaining-cards-gate-d/`，当前完成判定使用 turn-lifecycle Gate D。

R3 后仍保留的刻意边界是 `VerticalSliceController` 的 Unity 世界表现 facade。后续拆分只能在保留现有 Scene/Prefab 序列化引用、typed session 行为和渲染证据的前提下进行。

## Wave 02B3 统一生命周期与行动表现

`CombatTurnLifecycleCoordinator` 现在是唯一回合编排入口：Timeline action-by-action resolve 后执行 building snapshot，再清 Timeline，然后执行 Poison 三 pass、02B4 no-op hook 和 enemy intent refresh。`CombatSliceState` 实现原子 occupant store；Tower、Poison 与死亡均输出 typed lifecycle change，Presentation 不直接修改 Domain。

玩家 action 和 enemy intent 共享 action identity/presentation snapshot。`TimelinePresenter` 只维护 identity 到 `TimelineActionFrame` 的映射；`CombatInteractionOverlayPresenter`、`CardEffectFrame` 和 `BoardRangePresenter` 消费同一 display payload/source/target/range。两个 UI Prefab 和稳定 host 已保存到 Scene，动态对象只从 Prefab 创建。完整决策见 ADR 0008。
