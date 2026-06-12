# timeline_ui.gd 维护说明

日期：2026-06-13

## 当前职责

`scene/in_scene/timeline/timeline_ui.gd` 是时间轴 UI 的 composition root。它继续负责连接 `TimelineManager`、维护 `action_containers`、创建行动容器、持有 hover 状态、触发敌方意图 overlay、保留放置/移除动画旧入口、保留入场动画旧入口、保留旧视觉配置读取入口和清理 UI。

## 已拆模块

已拆模块位于 `scene/in_scene/timeline/ui_modules/` 与 `scene/in_scene/timeline/resources/`：

- `layout/`：展开遮罩和顶部布局。
- `grid/`：背景网格、格子交互、拖拽预览。
- `bridges/`：TimelineManager 查找。
- `config/TimelineVisualConfigReader.gd`：读取视觉配置 Resource 并提供类型兜底，不创建或修改 Resource。
- `controllers/`：行动块 hover 状态通知、时间轴入场动画编排状态。
- `animation/`：放置动画、移除残影创建、移除残影 tween。
- `presenters/`：敌方意图 overlay、行动容器几何、行动块视觉节点、行动整体形状视觉层。
- `resources/TimelineVisualConfig.gd` 与默认资源：时间轴纯表现配置。

## 不要继续硬拆

- 不要硬拆 `_on_action_placed()` 的完整生成编排，它同时牵动容器、格子、overlay、intro 动画和信号连接。
- 不要重复拆视觉配置读取，也不要让 reader 接管行动块生成或动画时机。
- 不要重复拆 `TimelineIntroPlaybackController.gd`，实际 tween 仍归 `TimelineIntroAnimator.gd`。
- 不要同批修改 TimelineManager 数据结构、敌人意图规则和行动块表现。

## 后续可做

后续只建议做小的纯表现参数补充，或在明确 UI 风险时扩展已有 presenter。若要拆 TimelineManager 规则，另开批次处理。当前不建议继续从 `_on_action_placed()` 拆大块，也不建议再围绕 intro 状态做单行包装。

## 验证入口

改动后加载 `res://scene/in_scene/in_scene.tscn`，检查时间轴入场、展开、拖拽预览、行动块 hover、敌方意图 overlay、行动移除动画。
