# DragShapeController.gd 维护说明

日期：2026-06-12

## 当前职责

`scene/in_scene/DragShapeController.gd` 是拖拽放置流程的 composition root。它继续负责编排拖拽开始、hover、放置校验、拒绝反馈、放置动画、时间轴提交、卡牌效果触发、成功/失败收尾和 UI 状态恢复。

## 已拆模块

已拆模块位于 `scene/in_scene/drag_modules/`：

- `bridges/DragShapeNodeBridge.gd`：查找 MainBoard、CardManager、弃牌堆、手牌和 HexMap。
- `ui/`：拒绝提示、场景交互锁、时间轴格子鼠标过滤、时间轴展开/收起状态。
- `coordinates/`：网格坐标与放置动画目标坐标解析。
- `presenters/`：自由拖拽、预览转发、目标效果预览、放置前视觉状态、成功卡牌视觉复原。
- `rules/`：卡牌形状、效果预览文案、放置查询、玩家 TimelineAction 创建。
- `animation/`：放置动画和拒绝动画。
- `timeline/DragTimelineActionSubmitter.gd`：提交玩家行动到 TimelineManager。
- `cards/DragSuccessDiscardMover.gd`：成功后移入弃牌区。

## 不要继续硬拆

- `end_dragging_success()` 中的 `collapse(timeline_ui)` 已经是既有模块单行调用，不要再包一层。
- `_return_card_to_hand()` 同时牵动手牌、tooltip、时间轴和主状态，暂不拆。
- 不要同批改 clear 卡即时效果、普通拖拽放置和弃牌收尾。

## 后续可做

除非未来要统一多处回手牌 fallback，否则当前应停止围绕成功收尾继续拆。若新增拖拽表现，优先放入既有 presenter 或 animation 模块。

## 验证入口

改动后至少加载局内场景，并手动回归普通拖拽、非法放置、成功放置、弃牌移动和时间轴收起。
