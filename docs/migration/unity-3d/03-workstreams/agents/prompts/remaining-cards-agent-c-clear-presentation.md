# Remaining Cards Agent C2：Clear Presentation

> 单一目标：在 C1 冻结结果上实现 clear 三态时间轴预览与完整 action UI 清除；不计算合法性或 action identity。

## 必读

- `NEXT_STAGE_REMAINING_CARDS_PROMPT.md`
- `remaining-cards-asset-ui-audit.md`
- C1 报告/API
- `TimelinePresenter.cs`、`TimelinePlacementPreview.cs`、`TimelineCellView.cs` 与对应 PlayMode tests

## 独占拥有路径

- `Runtime/Presentation/Targeting/TimelinePlacementPreview.cs` 或新增独立 `ClearTimelinePreview.cs` 及 `.meta`
- `Runtime/Presentation/Presenters/TimelinePresenter.cs`
- `Runtime/Presentation/TimelineCellView.cs`
- 对应 `Tests/PlayMode/Targeting/**`、`Tests/PlayMode/Presenters/**` 新/改文件
- 独占报告 `agents/reports/remaining-cards-agent-c-clear-presentation.md`

## 禁止路径

Controller、Binding、Domain/Application/Infrastructure/Composition、Scene/Prefab/Editor、共享文档、Godot 源和 Git。

## 冻结契约

- 只消费 Application clear preview/result；不扫描 label 推断占用、不复制边界/去重规则。
- 空格：天蓝+空心标记；命中：绿色+粗边框/HIT 或叉号；越界：红色+高对比边框/`!` 与可见状态文字。
- Clear/Cancel 后同帧恢复原 action label/color/outline；RemovedActions 的完整 shape 全部清空。
- 普通 valid/invalid preview 回归，重复 Bind/Show/Clear 不复制组件或残留。

## 测试/停止/Git

覆盖三态、2x2/12x1、重复刷新、取消、完整 UI 清除和普通预览回归。若 View 必须自行访问 TimelineGrid 或 card ID 分支，停止并报告。你不是唯一工作者；不得回退、stash、暂存、commit、push。
