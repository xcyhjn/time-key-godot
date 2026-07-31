# Wave 02B Agent 03：原版手牌 UI 报告

> 状态：已完成并交回所有权
> 负责人：Wave 02B Agent 03
> 最后验证日期：2026-07-31
> 证据来源：原 Godot `CustomCard`/`Hand`、冻结 Wave 02B1 契约、Unity PlayMode 与三视口实渲染

## 交付结果

新增可由 composition root 动态创建的单卡 `CardHandView`。它使用稳定底部卡槽和内部卡面视觉节点：Idle、Hover、Selected/Targeting/Scheduling、Dragging、Disabled 状态不会引发布局重排；完整 `lighting` Sprite 始终保持纵横比。

本切片没有解析 JSON、计算范围/伤害/时间轴合法性、raycast 世界或修改 Domain。所有玩法决定继续由主 composition root 和 `CardPlaySession`/`TimelineGrid.CanPlace` 负责。

## 改动文件

- `unity/Assets/_Project/Runtime/Presentation/Cards/CardHandView.cs`
- `unity/Assets/_Project/Runtime/Presentation/Cards/CardHandView.cs.meta`
- `unity/Assets/_Project/Runtime/Presentation/Cards.meta`
- `unity/Assets/_Project/Tests/PlayMode/Cards/CardHandViewTests.cs`
- `unity/Assets/_Project/Tests/PlayMode/Cards/CardHandViewTests.cs.meta`
- `unity/Assets/_Project/Tests/PlayMode/Cards.meta`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-03-card-hand-ui.md`

两个 `Cards.meta` 是 Unity 为本 Agent 独占目录生成的文件夹元数据；未修改 Presentation 根代码、asmdef 或共享场景。

## 组件与事件

```text
CardViewModel(stableId, Sprite, isSelected, isInteractable)
CardHandView.Build(CardViewModel)
CardHandView.SetInteractionState(CardHandInteractionState)
CardHandView.RequestCancelSelectedCard() -> bool

event CardSelected(stableId)
event CardCancelRequested(stableId)
event CardDragChanged(stableId, screenPosition, CardDragPhase)
```

交互状态：`Idle`、`Selected`、`Targeting`、`Scheduling`、`Dragging`、`Disabled`。

拖动阶段：`Started`、`Moved`、`Ended`、`Cancelled`。拖动只输出屏幕坐标和阶段；不执行世界或时间轴射线检测。

## 行为说明

- 基准卡槽为原版 `125x175`，底部居中，根容器横向拉伸且自身没有 Graphic，不会把整条底边误设为输入遮罩。
- Hover 使用原框架语义的 `30 px` 抬升和 `1.10x` 放大；Selected/Targeting/Scheduling 使用原 `CustomCard` 的 `80 px` 抬升和 `1.50x` 放大，并保留金色描边。
- 左键只从 Idle 发出一次 `CardSelected`；重复点击不重复发事件。
- 右键、Escape 或 `RequestCancelSelectedCard()` 只在选中链发一次取消；随后回到 Idle。
- 拖动期间卡面临时提升到 Canvas 最上层；结束或取消时按保存的父级、sibling、anchored position、scale、rotation 恢复。
- Artwork 的 GraphicRaycaster 命中、所有 pointer 回调和滚轮都会消费事件；禁用卡仍阻止输入穿透，但不会发玩法事件。
- 重复 `Build` 更新 view model 和 Sprite，不复制卡槽、视觉层级或内部监听。

## 测试与视觉证据

- Cards 专属 PlayMode：`4/4 passed`。
- 全量 PlayMode：`12/12 passed`；现有棋盘/镜头/结算和并行 Targeting 用例均通过。
- 结构化证据：`docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/playmode-results.xml`、`playmode-full-results.xml`。
- 视觉摘要：`docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/verification-summary.md`。
- 代表截图：`1920x1080-idle.png`、`1920x1080-hover.png`、`1920x1080-selected.png`、`1280x720-selected.png`、`2560x1080-selected.png`。

人工检查确认完整卡面不裁切、不拉伸；三态清晰；小视口与超宽视口均保持底部居中并留出主要棋盘区域。

## 主智能体接线说明

1. 在共享 Canvas 下创建带 RectTransform 的 GameObject，添加 `CardHandView`，用原 `lighting` Sprite 调用 `Build(new CardViewModel("lighting", sprite, false, true))`。
2. `CardSelected` 接到现有 `SelectCard(stableId)` 并创建新的 `CardPlaySession`；成功后把 view state 设为 `Selected`/`Targeting`。
3. `CardCancelRequested` 调用 session `Cancel()`，幂等清理 Board/Timeline preview，并把 view state 设为 `Idle`。
4. `CardDragChanged` 只把 screen position/phase 交给共享 composition root；时间轴合法性必须来自 `CardPlaySession.PreviewTimeline(...).IsPlacementValid`。
5. `Selected`、`Targeting`、`Scheduling` 时由共享 Controller 设置 `BoardCamera.InputEnabled=false`；提交或取消后恢复。Idle 时空白棋盘右键继续归轨道镜头，卡面自身 pointer 仍按契约阻止泄漏。
6. Commit 成功后由主智能体隐藏/禁用或移除该手牌，并继续现有结算；本 View 不自行改弃牌或 Domain。

## 未满足差异与风险

- 没有共享场景内的实际棋盘遮挡证据；必须由主智能体接线后补拍 3D 棋盘 integrated idle/hover/selected/target/timeline/resolved 图。
- 当前只实现 `lighting` 单卡语义。02B2 多卡时需要由上层 hand coordinator 保证跨多个 view 最多一张选中，并实现完整扇形排列；本切片没有提前伪造多卡算法。
- 素材导入设置不在本 Agent 所有权内；主智能体需按 Agent 01 建议统一设置，或继续以 Texture2D 创建运行时 Sprite。
- Unity 测试运行再次随机写入 `unity/ProjectSettings/ProjectSettings.asset` 的 `ps4Passcode`；本 Agent未越权恢复。主智能体精确暂存前必须恢复为 HEAD 的空值。
- 全量 PlayMode 首次启动曾在 projectPath 切换后无 XML 退出 1；无 Unity 残留，原命令重试后 `12/12` 通过。属于启动瞬态，不是测试失败。

## Git 与所有权

- 未暂存、commit、push、切分支、merge、stash 或回退任何文件。
- 未修改 `VerticalSliceController.cs`、`BoardTileView.cs`、Targeting、场景、asmdef、Editor harness、Domain、Infrastructure、ProjectSettings 或共享迁移文档。
- 四个用户未提交 Godot 文件保持原状且未进入本 Agent 路径。
- 原始 `.log` 已删除；它们是本 Agent 可重跑生成的临时输出。XML、PNG 和验证摘要保留。
- 本报告落盘后，`Runtime/Presentation/Cards/**`、`Tests/PlayMode/Cards/**`、专属证据与报告路径全部交回主智能体。
