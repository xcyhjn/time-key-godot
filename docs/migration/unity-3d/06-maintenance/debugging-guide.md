# 局内战斗调试指南

> 状态：适用于 Remaining Cards Gate D

## 调用链

```text
CardHandHost / TimelineCell / world raycast
  -> CardHandPresenter / TimelinePresenter / BoardRangePresenter
  -> CombatPresentationBinding
  -> VerticalSliceController compatibility facade
  -> CombatApplicationSession
  -> CardPlaySession 或 TimelineClearSession + TimelineGrid + registered Domain handlers
  -> CombatCommandResult / CombatSessionView / ResolutionSnapshot / TimelineClearResult
  -> presenters refresh HUD, range, timeline, world columns and occupant/status views
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
