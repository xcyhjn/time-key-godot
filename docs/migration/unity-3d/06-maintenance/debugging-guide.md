# 局内战斗调试指南

> 状态：适用于 Combat Shell Gate E

## 调用链

```text
CardHandHost / TimelineCell / world raycast
  -> CardHandPresenter / TimelinePresenter / BoardRangePresenter
  -> CombatPresentationBinding
  -> VerticalSliceController compatibility facade
  -> CombatApplicationSession
  -> CardPlaySession 或 TimelineClearSession + TimelineGrid + registered Domain handlers
  -> CombatCommandResult / CombatSessionView / ResolutionSnapshot / TimelineClearResult
  -> CombatTurnLifecycleCoordinator -> BattleFlowNextTurnHook
  -> presenters refresh hand, BattleFlow/Settlement HUD, range, timeline, world and occupant/status views
  -> optional ICombatTraceSink
```

Composition 入口是 `Runtime/Composition/CombatCompositionRoot.cs`。它创建 catalog、战斗状态、时间轴和 Application session，并把它们注入 Controller；Binding 负责订阅和解除订阅，Controller 不解析 JSON、不加载卡图，也不按 stable ID 决定表现。

## Trace 与断点

场景根的 `UnityCombatTraceSink.loggingEnabled` 可在 Inspector 关闭。结构化条目包含 command、phase、stable card ID、typed target、时间轴格、failure，以及结算时的 effect kind、before/after。sink 抛错必须被隔离，不能改变命令结果或 seed。

常用断点：

- 输入未到达用例：`CombatPresentationBinding` 的绑定/事件处理，以及各 Presenter 的事件转发。
- 状态机或失败原因：`CombatApplicationSession.SelectCard/SelectTarget/PreviewTimeline/CommitTimeline/ResolveTimeline`。
- 时间轴合法性：`TimelineGrid.CanPlace/TryPlace/Resolve`，不要在 Presenter 复制规则。
- 地震结果：Domain elevation handler 的 `EffectResults`，随后检查 Controller 对 `HexTileColumn.ApplyLogicalLayerCount` 的同步。
- Tower/Poison：先看 `OccupantEffectResults` 的 runtime ID、coordinate、creation/HP/stacks before/after，再看 `CombatOccupantPresenter` 的 anchor、Prefab registration 和 status View；不要从 GameObject 反推规则状态。
- Wind/Tornado：先看 `InteractionMode`、`ClearPreview.Cells/HitActions` 与 `ClearResult.RemovedActions`，再看 `ClearTimelinePreview` marker 和 `TimelinePresenter.ApplyClearResult()`；不得从 label 反推占用。
- 资源错误：`CardJsonAdapter.Parse`、`CardContentCatalog`、`CardContentEntry.ArtworkResourcePath` 和 Composition 的 `Resources.Load<Sprite>`。
- 牌区/回合错误：先看 EndTurn 冻结的 occupancy/hand/action snapshots，再看 `LastBattleFlowResult` 的 discard/timecoin/advance/shuffle/draw；不要在 clear 后重算占格。
- 终局错误：看 `BattleSettlementResult` 的 sequence、outcome 和 failure。相反结果应为 `OutcomeConflict`；奖励与 return 只通过 typed command，不直接改 Presenter。

## 常见故障

- 卡面可见但不可选：效果未在 `CardEffectRegistrationCatalog` 注册，或 Application 返回 `UnsupportedEffect`；这是 fail-fast，不是 UI 故障。
- 图片为空：JSON `front_image` 必须是文件名，定位结果应为 `Art/Battle/Cards/<stem>`；不要从 stable ID 猜图名。
- 重复点击产生双事件：检查 Binding 的对称 bind/unbind 和重复初始化测试。
- 范围/Timeline 颜色错误：确认 Presenter 消费 `CombatSessionView` 与 `CanPlace` 结果，没有重新计算领域合法性。
- 地震视觉层数不对：先比较 trace 的 before/after，再检查每列 blocks、`TopBounds`、anchor 和 collider；逻辑正确而截图不对属于 Presentation 同步问题。
- 四向点选失败：检查 EventSystem 的 UI 输入门禁、camera input 状态和抬高后顶层 collider。
- Tower 不显示或 Poison 图标为 Missing：检查 Scene 的 `CombatOccupantPresenter.creationViews` / `poisonStatusPrefab` 是否为非零 Prefab GUID，以及原 PNG import 是否 Point、Clamp、无 mipmap/压缩。
- Clear 颜色/marker 残留：确认 `ClearTimelinePreview.Clear()` 在 cancel/commit/普通预览切换时运行，并检查 `TimelineCellView` 的 `hasDefaultAppearance/defaultText/defaultColor` 已由 Gate C authoring 保存。整 action 只清一格时应回到 Domain removed snapshot，不能在 View 层补 identity 规则。

运行命令与证据规则见 `testing-and-evidence.md`。调试修复后先跑对应 filter，再跑全量 EditMode/PlayMode；渲染或场景接线变化还必须重跑 harness、build、Player 并人工开图。

Gate D 的一次全量 PlayMode 暴露了可复用的 Scene 夹具问题：当 Binding 增加必需 Presenter 时，保存 Scene 和所有 PlayMode test rig 都必须同步补序列化引用。当前该夹具已修正并以 full PlayMode `38/38` 验证；完整证据见 `04-verification/evidence/remaining-cards-gate-d/`。

回滚时按 Application、Infrastructure、Presentation/Composition 的职责边界撤销单一目的改动；不要用重建 Scene 掩盖丢失引用，也不要回退用户未提交文件。

## Lifecycle 与 identity 排错

先看 `CombatTurnLifecycleCoordinator.LastResult` 的 phase history、sequence 和 current action identity。重复 Tower/Poison 通常表示同一 sequence 被再次推进；残留 frame 通常表示 removal/clearing 没按 identity 或 source runtime ID 送达 Binding。

enemy intent 先检查 source catalog snapshot，再检查 scheduler seed/priority/shape，最后看 resolver 的 invalid reason 或 `UnsupportedSourceCommand`。地图与 Timeline 结果不同表示 Presentation 没消费同一 snapshot，不能在某一侧补算目标。

Tower/Poison 数值正确但 View 错误时检查 `LifecycleOccupantChangeResult`、Presenter 的 sequence/phase 幂等键、Prefab runtime ID 和 status anchor。world TextMesh 不可见时同时检查 Silver Font 与 MeshRenderer material。最终复现命令和截图入口见 `testing-and-evidence.md`。

## Deck/BattleFlow 排错

同 stable ID 卡牌消失或合并时，检查 `CardInstanceId -> CardViewModel.ViewId`，不要把内容 ID 当 View key。重复 sequence 必须返回原结果或 typed conflict，不能再次弃手、发币或推进。抽牌不守恒时逐步核对 before/after counts 与 moved instance IDs；只有 deck 空且 discard 非空时允许一次回洗。

胜利阈值只看 `BattleVictoryRule(currentHp, maximumHp)`；不要回退到绝对 HP 或 UI 文本。终局后仍能选卡时检查 `BattleFlow.IsInputLocked`、Presenter 的交互控件列表和 Session `Resolved` phase。最终 Player smoke 覆盖这条链，原始值见 `player-smoke-summary.json`。

## SceneFlow 排错

先看 request sequence/correlation/fingerprint、`CurrentPhase` 与 result 的 failed phase/history。相同 sequence 同 fingerprint 应返回原结果；不同 fingerprint 必须 `SequenceConflict`，旧 sequence 为 `Stale`，Busy 不排队。

卡在 90% 通常表示 pending additive load 未允许 activation；回滚必须在遮罩下激活后卸载。黑屏但任务完成时检查目标 entry 的 camera 已启用、至少跨过一个 `Time.frameCount`、source camera 在 cover 后禁用。重复 EventSystem/Audio/Transition 直接检查 Scene asset 门禁，不能在运行时发现后销毁。

Player smoke 必须可见运行；隐藏窗口会因 `runInBackground=false` 暂停。marker 后非零退出时检查是否在 Task continuation 内立即 Quit；当前 smoke 由后续 LateUpdate 延迟两帧退出。

Binding 或首帧等待失败后先检查 `SceneFlowStateStore` 是否仍有 pending record；`RollingBackTarget` 必须恢复旧 state，source unload operation 启动后才允许 commit。source 已提交后的 reveal/unlock 失败不得再卸载 target。Bootstrap 卡在未 ready 时等待 `InitializationTask` 并查看 `InitializationException`，不要无限轮询 `IsReady`。

## Gate E layered reveal 排错

进入内容 Scene 后长期锁输入时，先检查当前 `SceneContentEntry.RevealPresentation` 是否指向正确 Presenter，再检查各 `CanvasGroup` 是否已序列化。不要在 SceneFlow 增加 fixed timeout 来掩盖漏引用；missing/zero/disable/destroy 应由 Presenter 自身完成 terminal completion。

中间帧没有层次时，按 Scene 检查顺序：OutOfBattle 是 background/context/room，Combat 是 status/timeline/hand，GameOver 是 background/panel。`animation-timeline.json` 中 middle 帧应至少出现第一层 alpha 大于末层，complete 帧所有 alpha 都为 1。若初始帧已为 1，通常是测试/入口先自动播放后又直接采样，应通过正式 `PlayReveal()` 生命周期重置，而不是修改截图像素。

三轮 smoke 卡住或拓扑计数增加时，依次检查 Bootstrap 数量、content entry 数量、旧 content Scene 是否卸载、active launch 是否在 outcome 后关闭，以及 transition completion 是否释放 `SceneInputLockState`。每轮 room、launch correlation 与 outcome correlation 都应唯一且互相对应。

Player 内存样本小幅递增不等于已证明泄漏；先看稳定性测试的 post-GC 增量预算，再用 Profiler 做更长采样。D3D12 Player 若无法取得有效的 `Draw Calls Count`/`Batches Count`，smoke 会回退到 `SetPass Calls Count` 并在 JSON 记录实际名称；不要把它误报为 draw-call 计数。最终 Player 必须以 exit 0、PASS 一次、PERF 三次、FAIL 零次和 `finalInputLocked=false` 联合判定。
