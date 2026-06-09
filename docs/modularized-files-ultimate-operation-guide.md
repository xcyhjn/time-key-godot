# 已拆模块终极使用、操作、改进与维护说明

日期：2026-06-07

## 先读这一段

这份文档覆盖当前已经从主脚本里拆出来的模块文件。它的目的不是复述每一行代码，而是让后续维护者知道：

- 这个文件什么时候该用。
- 它应该负责什么。
- 它不能顺手接管什么。
- 继续优化时应该从哪里下手。

如果要改地图系统，先读 `docs/hex-map-ultimate-operation-guide.md`，再回到本文查具体模块。本文会列出 `hex_map_modules/` 下的每个拆分文件，但地图数据契约、`map_data`、`stack_nodes` 和 metadata 细节仍以 HexMap 手册为准。

## 当前模块目录

当前已归档的拆分目录：

```text
scene/in_scene/hex_map_modules/
scene/in_scene/in_scene_modules/
scene/in_scene/drag_modules/
scene/in_scene/timeline/ui_modules/animation/
scene/in_scene/timeline/ui_modules/bridges/
scene/in_scene/timeline/ui_modules/controllers/
scene/in_scene/timeline/ui_modules/grid/
scene/in_scene/timeline/ui_modules/layout/
scene/in_scene/timeline/ui_modules/presenters/
scene/in_scene/timeline/resources/
scene/in_scene/rewards/animation/
scene/in_scene/rewards/bridges/
scene/in_scene/rewards/diagnostics/
scene/in_scene/rewards/factory/
scene/in_scene/rewards/presenters/
scene/in_scene/rewards/rules/
scene/in_scene/rewards/resources/
scene/out_scene/out_scene_modules/
```

这些目录下的模块都应该保持“小职责、旧入口转发、可单批验证”的风格。不要为了减少主文件行数，把多个跨系统步骤塞进一个新模块。

## 维护总原则

- 规则模块只回答规则问题，不改节点树。
- presenter 只处理显示、布局、显隐、动画前状态，不写规则数据。
- bridge 只查找或同步跨系统节点，不持有玩法状态。
- factory 只创建或清理明确对象，不决定流程。
- controller 可以编排一个小流程，但不要吞掉主场景的整体生命周期。
- 每次改模块时，同时看它的主脚本旧入口，确认调用顺序没有被改变。
- 新增模块必须写中文职责注释，说明“负责什么”和“不负责什么”。

## OutScene 拆分模块

这些文件服务于 `scene/out_scene/out_scene_map_exp.gd`。局外地图当前仍是地图初始化、房间结算、章节推进、玩家移动和切场景的 composition root；新增模块只接管明确的小流程，不要把镜头动画、移动动画或场景切换一次性搬出去。

#### `scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd`

- 用途：执行局外 boss 后新章节地块的揭示动画，包括初始位置/透明度设置、升起 tween 和收尾 tween。
- 维护：不要在这里推进 `current_tier`，不要移动镜头，不保存 `MapState`，也不要决定哪些地块应该揭示。
- 改进：如果未来多个局外动画共用随机延迟或落点参数，可以抽成更小的动画配置 Resource，但运行时 tile 节点不要资源化。

#### `scene/out_scene/out_scene_modules/OutScenePayloadBridge.gd`

- 用途：在局外切入新场景且新场景入树前，把 payload 写给 HexMap、MainBoard、根节点或旧 `received_text` 兜底节点。
- 维护：不要在这里加载 PackedScene，不挂树，不替换 `current_scene`，不释放旧场景，也不解析 payload 内容。
- 改进：如果后续教程场景和正式局内场景的 payload 协议完全统一，可以把旧 `received_text` 兜底逐步删掉，但要先验证教程入口。

#### `scene/out_scene/out_scene_modules/RoomResolutionController.gd`

- 用途：统一处理局外房间结算 payload 的读取、坐标解析、boss 房判定和 tier 推进计划。
- 维护：不要在这里播放章节揭示动画，不移动玩家，不切换场景，也不直接写入 `current_tier` 或 `MapState.current_tier`。
- 改进：如果后续要标记房间已清空或已领取奖励，可优先在这里补纯规则判断，再由 `out_scene_map_exp.gd` 执行真实地图状态修改。

## 使用本文

每个条目按这个方式读：

```text
文件
-> 什么时候用
-> 维护时不要做什么
-> 后续改进方向
```

如果一个模块只暴露一个入口，优先从该入口理解。若一个模块有多个入口，先看公开 `func`，再看主脚本中对应的 `_get_xxx()` 或旧 wrapper。

## DragShapeController 拆分模块

这些文件服务于 `scene/in_scene/DragShapeController.gd`。它们处理拖拽、时间轴预览、放置校验、拒绝提示、放置动画、玩家行动创建、时间轴提交、成功后的卡牌视觉复原和弃牌移动。

### animation

#### `scene/in_scene/drag_modules/animation/DragPlacementAnimationRunner.gd`

- 用途：播放卡牌飞向时间轴格子的放置动画。
- 入口：`play_placement_animation(...)`。
- 维护：不要在这里判断放置是否合法，也不要创建 `TimelineAction` 或移动卡牌到弃牌区。
- 改进：如果放置动画曲线、透明度或缩放需要配置，优先从参数注入，不要读取主脚本成员。

#### `scene/in_scene/drag_modules/animation/DragRejectAnimationRunner.gd`

- 用途：播放拖拽放置失败时的卡牌抖动，并安排拒绝提示隐藏。
- 入口：`play_reject_animation(...)`。
- 维护：不要在这里结束拖拽，也不要写时间轴、手牌或弃牌区状态。
- 改进：可以把抖动幅度和持续时间继续参数化，但不要让它知道失败原因的规则来源。

### bridges

#### `scene/in_scene/drag_modules/bridges/DragShapeNodeBridge.gd`

- 用途：集中查找拖拽控制器需要的 `MainBoard`、`CardManager`、弃牌堆、手牌和 `HexMap`。
- 入口：`get_main_board()`、`get_card_manager()`、`find_discard_pile()`、`find_player_hand()`、`get_hex_map()`。
- 维护：只改查找路径和兜底顺序，不要缓存拖拽状态。
- 改进：如果场景树继续变化，优先改这个文件，避免把新路径散回 `DragShapeController.gd`。

### cards

#### `scene/in_scene/drag_modules/cards/DragSuccessDiscardMover.gd`

- 用途：成功放置后把卡牌移入弃牌区，并触发 `MainBoard` 旧弃牌效果。
- 入口：`move_to_discard(...)`。
- 维护：不要查找弃牌区或 MainBoard，不处理回手牌 fallback，也不要收起时间轴。
- 改进：如果后续弃牌效果从 MainBoard 迁出，可在这里替换触发入口，但仍保持失败时只返回 `false`。

### coordinates

#### `scene/in_scene/drag_modules/coordinates/DragPlacementTargetResolver.gd`

- 用途：计算放置动画目标左上角位置。
- 入口：`resolve_card_top_left(...)`。
- 维护：不要创建 tween，不要锁 UI，不要执行实际放置。
- 改进：若时间轴格子尺寸配置变化，优先补足输入参数，而不是读取时间轴内部状态。

#### `scene/in_scene/drag_modules/coordinates/DragTimelineGridCoordinateResolver.gd`

- 用途：把时间轴本地鼠标位置换算成网格坐标和边界状态。
- 入口：`resolve_hover(...)`、`resolve_fixed_bounds(...)`。
- 维护：不要判断卡牌形状是否合法，也不要更新预览格。
- 改进：后续可把 fallback slot/spacing 统一成配置对象，减少参数散落。

### presenters

#### `scene/in_scene/drag_modules/presenters/DragEffectPreviewPresenter.gd`

- 用途：显示和清理拖拽目标效果预览。
- 入口：`trigger_preview(...)`、`clear_preview(...)`。
- 维护：不要生成文案，不执行卡牌效果，也不要修改拖拽状态。
- 改进：敌人与血条查找仍可继续桥接化，降低 presenter 的场景依赖。

#### `scene/in_scene/drag_modules/presenters/DragFreeDragPresenter.gd`

- 用途：普通拖拽时让卡牌跟随鼠标，并恢复拖拽视觉状态。
- 入口：`update_free_drag(...)`。
- 维护：不要处理 clear 模式预览，不判断放置是否合法。
- 改进：如果拖拽视觉 shader 参数增多，可单独拆一个视觉状态 adapter。

#### `scene/in_scene/drag_modules/presenters/DragPlacementVisualStatePreparer.gd`

- 用途：卡牌进入放置动画前禁用输入并清理无效放置视觉状态。
- 入口：`prepare_for_placement(...)`。
- 维护：不要计算目标格位置，不播放动画，不执行时间轴放置。
- 改进：保持它只处理卡牌自身状态，不要接触 `TimelineUI`。

#### `scene/in_scene/drag_modules/presenters/DragSuccessCardVisualRestorer.gd`

- 用途：成功放置后恢复卡牌自身视觉状态，包括拖拽 shader、透明度、材质、缩放和鼠标过滤。
- 入口：`restore(...)`。
- 维护：不要发射 `drag_ended`，不要移动卡牌到弃牌区，也不要收起时间轴。
- 改进：如果后续卡牌视觉复原字段继续增多，优先在这里集中处理，保持 `end_dragging_success()` 只编排流程。

#### `scene/in_scene/drag_modules/presenters/DragTimelineGridPreviewPresenter.gd`

- 用途：把拖拽形状预览转发给时间轴 UI。
- 入口：`update_preview(...)`、`clear_preview(...)`。
- 维护：不要计算鼠标坐标，不判断最终放置结果。
- 改进：时间轴 UI 的预览方法若改名，只在这里调整转发。

### rules

#### `scene/in_scene/drag_modules/rules/DragCardEffectPreviewTextResolver.gd`

- 用途：从卡牌描述生成拖拽效果预览文案和伤害数值。
- 入口：`get_preview_text(...)`、`get_damage_amount(...)`。
- 维护：不要显示 tooltip，不查找敌人或血条。
- 改进：描述解析可以改成读取结构化卡牌数据，减少文本正则依赖。

#### `scene/in_scene/drag_modules/rules/DragCardShapeResolver.gd`

- 用途：从卡牌数据读取时间轴形状坐标。
- 入口：`resolve_shape_coords(...)`、`convert_to_vector2i_array(...)`。
- 维护：不要启动拖拽，不写节点，不判断能否放置。
- 改进：卡牌形状字段若标准化，优先在这里统一兼容旧字段。

#### `scene/in_scene/drag_modules/rules/DragPlacementQueryService.gd`

- 用途：回答当前拖拽形状在指定时间轴格子是否可用。
- 入口：`is_placement_valid(...)`。
- 维护：不要创建 `TimelineAction`，不要改时间轴或卡牌状态。
- 改进：清除类卡牌与普通卡牌的校验分支可以继续拆成更小规则对象。

#### `scene/in_scene/drag_modules/rules/DragPlayerActionFactory.gd`

- 用途：从当前拖拽上下文创建玩家 `TimelineAction`。
- 入口：`create_action(...)`。
- 维护：不要调用 `TimelineManager.place_action()`，不要发射放置信号，也不要移动卡牌到弃牌区。
- 改进：如果玩家行动颜色、action_data 复制或卡牌元数据以后需要统一策略，优先在这里集中处理。

### timeline

#### `scene/in_scene/drag_modules/timeline/DragTimelineActionSubmitter.gd`

- 用途：把已创建的玩家 `TimelineAction` 提交给 `TimelineManager`，并在成功时回调主脚本发射旧信号。
- 入口：`submit_action(...)`。
- 维护：不要创建 `TimelineAction`，不要播放失败动画，不清理预览，也不要移动卡牌到弃牌区。
- 改进：如果后续提交结果需要更详细的失败原因，可返回结果字典，但仍让 `DragShapeController.gd` 处理失败动画和成功收尾。

### ui

#### `scene/in_scene/drag_modules/ui/DragRejectTooltipController.gd`

- 用途：维护拖拽拒绝提示的文本、位置和显隐。
- 入口：`show_tooltip(...)`、`update_position(...)`、`hide_tooltip(...)`。
- 维护：不要判断拒绝原因，不播放卡牌抖动。
- 改进：如果拒绝提示样式变复杂，可以单独接入共享 tooltip 样式资源。

#### `scene/in_scene/drag_modules/ui/DragSceneInteractionLockController.gd`

- 用途：拖拽和放置动画期间锁定或恢复局内场景交互。
- 入口：`lock_for_drag(...)`、`restore_after_drag(...)`、`lock_for_placement_animation(...)`。
- 维护：不要处理卡牌状态，不更新预览，不判断放置。
- 改进：后续可以记录旧 mouse_filter，避免假设恢复值永远固定。

#### `scene/in_scene/drag_modules/ui/DragTimelineGridMouseFilterController.gd`

- 用途：集中修改时间轴格子的鼠标过滤状态。
- 入口：`disable_grid_cells(...)`、`restore_grid_cells(...)`。
- 维护：不要展开或收起时间轴，不处理 hover。
- 改进：如果时间轴格子节点结构变化，只改这个文件的遍历方式。

#### `scene/in_scene/drag_modules/ui/DragTimelineUiStateController.gd`

- 用途：拖拽期间控制时间轴 UI 展开、收起和点击展开开关。
- 入口：`enter_drag_mode(...)`、`exit_drag_mode(...)`、`collapse(...)`。
- 维护：不要处理预览格子，不移动卡牌。
- 改进：可以把拖拽期间时间轴状态抽成显式状态对象，减少 UI 侧隐式标记。

## TimelineUI 拆分模块

这些文件服务于 `scene/in_scene/timeline/timeline_ui.gd`。它们处理时间轴 UI 的布局、背景网格、拖拽预览、敌方意图 overlay、行动方格动画、行动容器几何计算、行动方块视觉节点创建、行动整体形状视觉层、清理动画残影创建、原容器运行时视觉清理、残影 tween 播放和行动块 hover 状态通知。`timeline_ui.gd` 仍负责连接 `TimelineManager`、维护行动容器字典、持有当前 hover 引用和决定清理动画触发时机。

### animation

#### `scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd`

- 用途：从即将移除的时间轴行动容器生成无 Shader、无 Overlay、无鼠标交互的清理动画残影，并卸载原容器上的运行时视觉效果。
- 入口：`create_ghost(...)`、`strip_runtime_effects(...)`。
- 维护：不要启动 Tween，不修改 `TimelineManager` 数据，不维护 `action_containers`，也不要释放原行动容器。
- 改进：如果清理动画需要多种残影样式，可以在这里扩展复制策略，仍让 `timeline_ui.gd` 决定何时触发播放和释放原容器。

#### `scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalAnimator.gd`

- 用途：播放时间轴行动残影的淡出、下落、缩放，并在结束后释放残影。
- 入口：`animate_removal(...)`。
- 维护：不要创建残影，不修改 `TimelineManager` 数据，不维护 `action_containers`，也不要处理原行动容器释放。
- 改进：如果不同移除原因需要不同曲线，可从参数或配置注入，不要读取主脚本状态。

#### `scene/in_scene/timeline/ui_modules/animation/TimelineBlockPlacementAnimator.gd`

- 用途：播放单个时间轴行动方格的放置入场动画。
- 入口：`animate_block_placement(...)`。
- 维护：不要创建行动容器，不修改 `TimelineManager` 数据，也不要处理敌方意图入场动画。
- 改进：动画曲线和时长后续可改为配置参数，仍通过主脚本传入 tween factory。

### bridges

#### `scene/in_scene/timeline/ui_modules/bridges/TimelineManagerLocator.gd`

- 用途：为 `timeline_ui.gd` 查找 `TimelineManager`。
- 入口：`find_timeline_manager(...)`。
- 维护：只负责查找路径和兜底顺序，不连接信号，不读取时间轴数据。
- 改进：后续可改为由 `in_scene.gd` 注入 manager，减少运行时查找。

### controllers

#### `scene/in_scene/timeline/ui_modules/controllers/TimelineActionHoverStateController.gd`

- 用途：处理时间轴行动方块 hover 进入、退出和行动被移除时的本地状态切换，并沿用 `TimelineManager.action_hovered_changed` 通知外部。
- 入口：`enter_hover(...)`、`exit_hover(...)`、`clear_removed_action_hover(...)`。
- 维护：不要创建行动方块，不展示 tooltip，不处理敌方意图预览，也不要修改 `TimelineManager` 的网格数据。
- 改进：如果未来需要区分重复 hover 或跨行动切换时的旧 action 清理，可在这里扩展状态转换结果，但不要直接读取场景树。

### grid

#### `scene/in_scene/timeline/ui_modules/grid/TimelineGridBuilder.gd`

- 用途：构建时间轴背景格子，设置间距、尺寸和鼠标信号。
- 入口：`build_grid(...)`。
- 维护：不要创建行动块，不处理拖拽预览，也不要读写 `TimelineManager`。
- 改进：格子样式如果资源化，可从参数中接收样式配置。

#### `scene/in_scene/timeline/ui_modules/grid/TimelineGridCellInteractionPresenter.gd`

- 用途：把格子索引换算成网格坐标，并应用 hover 默认色。
- 入口：`get_grid_pos(...)`、`apply_cell_color(...)`。
- 维护：不要发射点击或 hover 信号，不判断放置是否合法。
- 改进：如果格子结构变化，只改这里的坐标换算。

#### `scene/in_scene/timeline/ui_modules/grid/TimelineGridPreviewPresenter.gd`

- 用途：显示和清理拖拽形状在时间轴背景格子上的预览样式。
- 入口：`update_grid_preview(...)`、`clear_grid_preview(...)`。
- 维护：不要创建行动块，不修改 `TimelineManager` 数据，不处理最终放置规则。
- 改进：预览颜色和边框可以继续资源化。

### layout

#### `scene/in_scene/timeline/ui_modules/layout/TimelineExpandVisualController.gd`

- 用途：处理时间轴展开/收起时的遮罩表现、时间轴缩放表现和地图交互过滤。
- 入口：`create_background_mask(...)`、`animate_background_mask(...)`、`animate_timeline_scale(...)`、`set_map_interaction_for_expand(...)`。
- 维护：不要创建行动块，不改时间轴数据，也不要处理拖拽预览。
- 改进：遮罩颜色和层级可以进入视觉配置 Resource。

#### `scene/in_scene/timeline/ui_modules/layout/TimelineLayoutController.gd`

- 用途：根据网格尺寸、格子大小、间距和顶部预留空间应用时间轴锚点布局。
- 入口：`apply_anchor_layout(...)`。
- 维护：不要处理展开动画，不连接信号，不创建格子或行动块。
- 改进：布局参数可与 HUD 顶部空间配置统一。

### presenters

#### `scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd`

- 用途：创建和配置时间轴敌方意图 overlay 材质，批量切换 overlay 显隐，并应用/清理敌方意图预览的容器视觉状态。
- 入口：`build_config(...)`、`create_overlay_material_from_config(...)`、`set_overlay_visible_from_config(...)`、`apply_preview_state(...)`、`clear_preview_state(...)`、`configure_material_from_config(...)`；旧的逐参数入口仍保留作兼容包装。
- 维护：不要决定何时进入预览，不创建行动块，不修改 `TimelineManager` 数据，也不处理移除动画。
- 改进：敌方意图 overlay 的 shader 参数已由 `timeline_ui.gd` 从 `TimelineVisualConfig` 读取；后续只补纯表现字段，不要把预览时机迁入资源。

#### `scene/in_scene/timeline/ui_modules/presenters/TimelineActionBlockPresenter.gd`

- 用途：创建单个时间轴行动方块 Panel、基础样式和 `EnemyIntentOverlay` 子节点。
- 入口：`create_action_block_style(...)`、`build_config(...)`、`create_action_block_from_config(...)`；旧的逐参数入口仍保留作兼容包装。
- 维护：不要连接 hover，不播放动画，不修改 `TimelineManager` 数据，也不要决定行动块何时被创建或移除。
- 改进：如果行动方块样式继续膨胀，可让这里接收视觉配置资源，但不要接管整组行动容器生成。

#### `scene/in_scene/timeline/ui_modules/presenters/TimelineActionGeometryPresenter.gd`

- 用途：计算时间轴行动形状边界、行动容器尺寸位置和单个方块在容器内的局部位置。
- 入口：`get_shape_bounds(...)`、`apply_shape_container_geometry(...)`、`get_block_local_position(...)`。
- 维护：不要创建行动方块，不连接 hover，不播放动画，也不要修改 `TimelineManager` 数据。
- 改进：如果后续需要更强类型的几何结果，可把当前字典返回值替换为轻量数据对象。

#### `scene/in_scene/timeline/ui_modules/presenters/TimelineActionShapeVisualPresenter.gd`

- 用途：创建时间轴行动整体形状视觉层，包括局部坐标归一化、BACKPLATE 和 OUTLINE 绘制节点挂载。
- 入口：`build_config(...)`、`get_local_shape_coords(...)`、`add_action_shape_visual_from_config(...)`；旧的逐参数入口仍保留作兼容包装。
- 维护：不要创建行动方块，不连接 hover，不创建敌方意图 overlay，也不要修改 `TimelineManager` 数据。
- 改进：行动整体轮廓参数已由 `timeline_ui.gd` 从 `TimelineVisualConfig` 读取并传入；这里仍不要接管 `_on_action_placed()` 的完整生成流程。

### resources

#### `scene/in_scene/timeline/resources/TimelineVisualConfig.gd`

- 用途：保存时间轴 UI 的纯视觉静态参数，包括网格颜色、展开动画、敌方意图 overlay、移除动画、行动整体轮廓和遮罩表现。
- 维护：不要在这里创建行动块，不读取或修改 `TimelineManager`，不决定拖拽放置、敌人意图规则或 hover 通知。
- 改进：如果未来新增时间轴表现参数，优先补这里；`slot_size`、`spacing`、`grid_width` 和 `grid_height` 仍涉及布局契约与外部调用，暂时保留在 `timeline_ui.gd`。

#### `scene/in_scene/timeline/resources/default_timeline_visual_config.tres`

- 用途：默认时间轴视觉配置数据资产，当前覆盖网格默认/悬停色、展开缩放、敌方意图 shader 参数、移除动画、整体轮廓和遮罩表现。
- 维护：这是数据资产，不是运行态状态；调参时只改静态表现值，不写 action、Tween、hover 状态或场景节点。
- 改进：如果不同 UI 主题需要不同时间轴风格，可以新增多份 `.tres`，由场景或上层流程选择资源。

## InScene 拆分模块

这些文件服务于 `scene/in_scene/in_scene.gd`。`in_scene.gd` 仍是局内场景 composition root，模块不要反向接管整个局内生命周期。

### bridges

#### `scene/in_scene/in_scene_modules/bridges/InSceneGlobalClockBridge.gd`

- 用途：集中连接和同步 `GlobalClock`。
- 维护：只处理全局时钟桥接，不推进回合，不打开结算。
- 改进：后续可以把时代、阶段、时间币等全局状态桥接拆成更细资源。

#### `scene/in_scene/in_scene_modules/bridges/InSceneNodeBridge.gd`

- 用途：集中维护 `in_scene.gd` 依赖的场景节点路径。
- 维护：只查找节点，不缓存玩法状态，不修改场景树。
- 改进：场景树调整时优先改这里，再检查主脚本旧入口。

### cards

#### `scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd`

- 用途：编排抽牌流程。
- 维护：不要处理胜负结算，不切场景。
- 改进：抽牌动画、无动画抽牌和 UI 刷新可以继续按入口分层。

#### `scene/in_scene/in_scene_modules/cards/CardPileUiController.gd`

- 用途：刷新抽牌堆、弃牌堆和相关 UI 显示。
- 维护：不要移动实际卡牌，不修改 deck 数据。
- 改进：牌堆查看器和计数标签可以继续拆成更小 presenter。

#### `scene/in_scene/in_scene_modules/cards/CardSystemBootstrap.gd`

- 用途：创建或连接局内 CardManager、Hand、Deck、Discard 和 CardFactory。
- 维护：不要处理回合推进，不处理结算奖励。
- 改进：Card 系统依赖可以继续变成明确配置字典，减少主脚本赋值顺序风险。

#### `scene/in_scene/in_scene_modules/cards/HandDiscardFlowController.gd`

- 用途：处理手牌进入弃牌堆的流程。
- 维护：不要推进战斗阶段，不打开奖励页。
- 改进：如果动画和数据移动继续耦合，优先拆动画 runner。

### scene_flow

#### `scene/in_scene/in_scene_modules/scene_flow/InSceneExternalPayloadParser.gd`

- 用途：解析外部场景传回局内的 payload。
- 维护：不要直接恢复 UI，也不要切换场景。
- 改进：payload 字段可以整理成类型化资源或常量表。

#### `scene/in_scene/in_scene_modules/scene_flow/InScenePayloadBridge.gd`

- 用途：在局内场景和全局/局外状态之间传递 payload。
- 维护：只做桥接，不改战斗规则。
- 改进：继续减少对 root meta 的散落依赖。

#### `scene/in_scene/in_scene_modules/scene_flow/InSceneReturnFlowController.gd`

- 用途：编排从局内返回局外的流程。
- 维护：不要构造所有 payload 细节，也不要直接处理奖励消费。
- 改进：返回流程失败恢复可以单独拆成错误恢复模块。

#### `scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd`

- 用途：构造返回局外所需 payload。
- 维护：不要执行场景切换，不播放 UI 动画。
- 改进：payload 字段新增时，在这里写明来源和默认值。

#### `scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchExecutor.gd`

- 用途：执行场景切换请求。
- 维护：不要构造 payload，不负责 UI 隐藏。
- 改进：场景切换失败日志和恢复动作可以更明确地返回结果字典。

#### `scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchLoader.gd`

- 用途：加载目标场景资源。
- 维护：不要执行切换，不修改当前场景状态。
- 改进：可以集中缓存常用 PackedScene，减少重复 load。

### settlement

#### `scene/in_scene/in_scene_modules/settlement/CombatDefeatFlowController.gd`

- 用途：处理战斗失败流程。
- 维护：不要处理最终通关胜利，也不要打开奖励页。
- 改进：失败 UI、音效和 payload 可以继续拆分。

#### `scene/in_scene/in_scene_modules/settlement/CombatVictorySettlementController.gd`

- 用途：处理战斗胜利进入结算阶段。
- 维护：不要直接切回局外，不负责最终胜利页。
- 改进：结算前 UI 锁定和牌组快照可以继续薄化。

#### `scene/in_scene/in_scene_modules/settlement/GameWinFlowController.gd`

- 用途：触发最终胜利页、音效和 BGM 停止。
- 维护：不要处理普通战斗胜利结算。
- 改进：音频控制可以继续抽到音频桥接模块。

#### `scene/in_scene/in_scene_modules/settlement/SettlementDeckReclaimService.gd`

- 用途：结算期把运行时卡牌无动画回收到抽牌堆。
- 维护：不要判断胜负，不打开奖励页，不刷新奖励按钮。
- 改进：卡牌静默移动和抽牌堆同步可以再拆成两个服务。

#### `scene/in_scene/in_scene_modules/settlement/SettlementDeckSnapshotService.gd`

- 用途：进入结算整理前保存当前牌组快照。
- 维护：不要移动卡牌，不刷新 UI。
- 改进：快照字段变化时，在这里集中兼容旧版本。

#### `scene/in_scene/in_scene_modules/settlement/SettlementRewardConsumer.gd`

- 用途：把奖励建筑已使用状态写回 `HexMap`。
- 维护：不要打开奖励页，不恢复 UI。
- 改进：奖励消费结果可以返回更明确的原因码。

#### `scene/in_scene/in_scene_modules/settlement/SettlementRewardExitController.gd`

- 用途：读取奖励页退出状态并关闭奖励页实例。
- 维护：不要写 `HexMap` 奖励状态，不改变战斗阶段。
- 改进：奖励页退出字段可以标准化。

#### `scene/in_scene/in_scene_modules/settlement/SettlementRewardExitFlowController.gd`

- 用途：编排奖励页退出后的主场景收尾。
- 维护：不要打开奖励页，不改变战斗阶段。
- 改进：UI 恢复与奖励消费可以保持两个清晰子步骤。

#### `scene/in_scene/in_scene_modules/settlement/SettlementRewardSceneController.gd`

- 用途：打开结算奖励页并注入上下文。
- 维护：不要消费奖励，不处理奖励页退出。
- 改进：奖励场景路径和缓存可以集中成配置表。

#### `scene/in_scene/in_scene_modules/settlement/SettlementRuntimeCardCollector.gd`

- 用途：收集结算期需要回收到抽牌堆的运行时卡牌。
- 维护：不要移动节点，不洗牌，不改卡牌视觉。
- 改进：运行时卡牌来源可以进一步显式化，减少 loose card 扫描。

### turn

#### `scene/in_scene/in_scene_modules/turn/EnemyIntentTimelineRefresher.gd`

- 用途：把当前敌人组刷新到时间轴。
- 维护：不要推进回合，不处理敌人意图 hover。
- 改进：敌人来源和时间轴写入可以继续拆为 reader 和 writer。

#### `scene/in_scene/in_scene_modules/turn/FirstTurnIntroRunner.gd`

- 用途：处理首回合启动前的等待与入场 UI 动画。
- 维护：不要推进回合规则，不生成敌人意图。
- 改进：等待条件可以形成状态机，避免主脚本多处判断。

### ui

#### `scene/in_scene/in_scene_modules/ui/CardTooltipUiAdapter.gd`

- 用途：连接 InScene 与共享 `CardTooltipPresenter`。
- 维护：不要绘制 tooltip 内容，不改变选中卡牌规则。
- 改进：CardManager 查找可复用现有 locator。

#### `scene/in_scene/in_scene_modules/ui/CombatCartoonUiController.gd`

- 用途：维护顶部 CartoonUI 的布局和进度文字。
- 维护：不要推进 `GlobalClock`，不决定战斗阶段。
- 改进：布局参数可继续资源化。

#### `scene/in_scene/in_scene_modules/ui/CursorTooltipController.gd`

- 用途：包装、定位和同步光标提示框尺寸。
- 维护：不要决定提示文本内容，不参与目标判定。
- 改进：样式资源可以与奖励页 tooltip 继续统一。

#### `scene/in_scene/in_scene_modules/ui/InSceneInputEventController.gd`

- 用途：把 MainBoard 原始输入拆成明确动作。
- 维护：需要 await 的弃牌移动仍交回主脚本，不要在这里吞掉异步流程。
- 改进：输入动作结果可以改成更明确的枚举。

#### `scene/in_scene/in_scene_modules/ui/InSceneInputLockController.gd`

- 用途：执行局内玩家输入锁定和解锁。
- 维护：不要决定什么时候锁输入，只执行命令。
- 改进：记录旧按钮状态，避免恢复时覆盖其他系统锁。

#### `scene/in_scene/in_scene_modules/ui/InSceneUiVisibilityController.gd`

- 用途：执行局外场景和结算阶段的 UI 显隐。
- 维护：不要切换场景，不消费奖励。
- 改进：UI 组可以配置化，减少手动节点列表。

#### `scene/in_scene/in_scene_modules/ui/TargetSelectionHoverUiController.gd`

- 用途：处理目标选择 hover 的光标提示表现。
- 维护：目标是否合法仍由 MainBoard/HexMap 判断。
- 改进：合法/非法提示文本可抽成配置。

#### `scene/in_scene/in_scene_modules/ui/TimelineActionHoverUiController.gd`

- 用途：处理时间轴行动 hover 的旧 UI 表现。
- 维护：敌方意图 hover 已由地图意图 presenter 处理，这里保持 ENEMY 早退。
- 改进：玩家行动 hover 和敌方行动 hover 可以彻底分离。

## Rewards 拆分模块

这些文件服务于奖励页、商店页和合成页。奖励页模块已经按 `animation`、`bridges`、`diagnostics`、`factory`、`presenters`、`rules` 归档。

### animation

#### `scene/in_scene/rewards/animation/RewardCardFlyToDeckAnimator.gd`

- 用途：播放奖励页卡牌飞入牌库的视觉动画。
- 入口：`play(...)`。
- 维护：不要写入牌组数据，不同步抽牌堆，不决定奖励页关闭。
- 改进：飞行动画曲线和拖影参数可以继续配置化。

### bridges

#### `scene/in_scene/rewards/bridges/RewardCardManagerLocator.gd`

- 用途：奖励页面自动查找 `CardManager`。
- 维护：不要生成奖励卡牌，不改牌组。
- 改进：和地图、局内的 CardManager locator 可以继续统一。

#### `scene/in_scene/rewards/bridges/RewardDeckSyncBridge.gd`

- 用途：奖励页新增卡牌后写入 `deck_manager` 并同步局内抽牌堆。
- 维护：不要关闭奖励页，不修改商店列表。
- 改进：后续可把“新增卡牌”和“同步运行时抽牌堆”拆成两个入口。

#### `scene/in_scene/rewards/bridges/ShopCardPoolBridge.gd`

- 用途：按时代从 `CardDataPool` 读取商店候选卡牌，并保留旧 fallback 卡牌池。
- 维护：不要选择时代权重，不创建卡牌，不处理商品 UI。
- 改进：fallback 卡牌池后续可以配置化，避免硬编码在桥接模块里。

#### `scene/in_scene/rewards/bridges/ShopGlobalNodeFinder.gd`

- 用途：查找商店依赖的 `GlobalClock` 和 `global_timecoin`。
- 维护：不要读取时代值，不消费时间币。
- 改进：如果 autoload 名称固定，可以减少递归脚本名兜底。

### diagnostics

#### `scene/in_scene/rewards/diagnostics/ShopDebugLogger.gd`

- 用途：集中商店初始化和商品生成阶段的调试输出。
- 维护：不要计算价格，不生成商品，不改变流程。
- 改进：可以增加 `enabled` 注入或按调试级别控制输出。

### factory

#### `scene/in_scene/rewards/factory/RewardDraftCardFactory.gd`

- 用途：创建奖励页使用的轻量 DraftCard 并写入基础展示尺寸。
- 维护：不要读取真实卡牌数据，不连接点击信号。
- 改进：DraftCard 初始显示字段可继续统一到 data applier。

#### `scene/in_scene/rewards/factory/RewardRealCardCleaner.gd`

- 用途：从临时幽灵牌堆移除真实卡牌并释放节点。
- 维护：不要读取卡牌数据，不改 DraftCard。
- 改进：清理失败时可以返回布尔值，方便调用方记录。

#### `scene/in_scene/rewards/factory/RewardRealCardSpawner.gd`

- 用途：把 card_factory 生成的真实卡牌临时放入奖励页幽灵牌堆。
- 维护：不要复制卡牌数据，不清理临时真实卡牌。
- 改进：真实卡牌生成失败原因可以结构化返回。

#### `scene/in_scene/rewards/factory/RewardTempPileFactory.gd`

- 用途：为奖励页面创建临时幽灵牌堆；需要完全隐藏时用 `create_hidden_temp_pile(...)`。
- 维护：不要生成卡牌，不读取卡牌数据，不清理临时牌堆，不决定奖励选择或确认流程。
- 改进：临时牌堆场景路径可以配置化；如果多个奖励页需要统一释放策略，再单独评估生命周期模块。

### presenters

#### `scene/in_scene/rewards/presenters/CraftConnectionLinePresenter.gd`

- 用途：显示合成面板槽位之间的连接线。
- 维护：不要判断配方，不创建卡牌。
- 改进：连接线样式可以继续资源化。

#### `scene/in_scene/rewards/presenters/CraftPreviewCleanupPresenter.gd`

- 用途：释放合成素材槽和结果槽预览卡节点，并返回结果预览清空状态。
- 维护：不要创建预览卡，不刷新配方，不更新结果描述，也不修改牌组。
- 改进：如果预览状态继续扩展，可以把返回字典改成更明确的状态对象。

#### `scene/in_scene/rewards/presenters/CraftPreviewCardConfigurator.gd`

- 用途：配置已完成数据写入的合成预览卡尺寸、位置和 tooltip 鼠标过滤状态。
- 维护：不要创建预览卡，不读取真实卡数据，也不要管理临时牌堆生命周期。
- 改进：如果素材槽和结果槽预览 UI 需要不同样式，可以继续通过参数传入，不要读取合成页状态。

#### `scene/in_scene/rewards/presenters/CraftResultDescriptionPanelPresenter.gd`

- 用途：配置合成结果描述面板基础样式。
- 维护：不要读取卡牌描述，不计算位置。
- 改进：与共享 tooltip 样式合并时，优先保持入口参数稳定。

#### `scene/in_scene/rewards/presenters/CraftResultDescriptionContentPresenter.gd`

- 用途：读取合成结果预览卡说明文本，并显示或隐藏结果描述面板。
- 维护：不要计算面板位置，不配置面板样式，不修改合成状态。
- 改进：如果卡牌说明数据结构稳定，可以减少 `raw_description` 兜底探测。

#### `scene/in_scene/rewards/presenters/CraftResultDescriptionPositionPresenter.gd`

- 用途：计算合成结果描述面板尺寸和屏幕内位置。
- 维护：不要写描述文本，不改合成状态。
- 改进：屏幕边界逻辑可复用到其他浮层。

#### `scene/in_scene/rewards/presenters/CraftResultPreviewPresenter.gd`

- 用途：把已创建的合成结果预览卡挂到结果槽，归零位置，设置选中态并连接结果卡点击回调。
- 维护：不要创建预览卡，不读取配方，不更新结果描述或连接线。
- 改进：如果后续结果预览需要入场动画，可以在这里增加动画参数，但不要让它读取槽位状态。

#### `scene/in_scene/rewards/presenters/CraftSelectionTitlePresenter.gd`

- 用途：根据当前选择槽位和已选槽位状态生成合成选择面板标题文案。
- 维护：不要生成选择卡，不排序牌组条目，不修改合成状态。
- 改进：标题文案后续可以资源化或接入本地化表。

#### `scene/in_scene/rewards/presenters/CraftSelectionCardStatePresenter.gd`

- 用途：应用合成选择列表卡牌的选中、变暗、禁用、hover 和点击入口。
- 维护：不要创建卡牌，不读取真实卡数据，不写选择状态，也不修改牌组。
- 改进：如果卡牌状态接口统一，可以减少 `has_method` 探测。

#### `scene/in_scene/rewards/presenters/CraftSlotPlaceholderPresenter.gd`

- 用途：刷新合成素材槽和结果槽占位符的显隐与文案。
- 维护：不要创建或释放预览卡，不判断配方，不修改合成选择状态。
- 改进：如果槽位数量扩展，可以把两个固定槽位改成列表输入。

#### `scene/in_scene/rewards/presenters/CraftSlotPreviewLayoutPresenter.gd`

- 用途：设置合成槽位预览锚点边距和尺寸兜底。
- 维护：不要创建预览卡，不读取配方。
- 改进：槽位布局参数可以改成导出资源。

#### `scene/in_scene/rewards/presenters/RewardCardDescriptionExtractor.gd`

- 用途：从真实卡牌节点读取奖励页展示用效果文本。
- 维护：不要创建卡牌，不改 DraftCard。
- 改进：结构化卡牌数据成熟后，优先减少节点属性探测。

#### `scene/in_scene/rewards/presenters/RewardCardTextureExtractor.gd`

- 用途：从真实卡牌或卡牌工厂缓存中读取奖励页卡面贴图。
- 维护：不要创建卡牌，不决定奖励流程。
- 改进：贴图读取失败可以统一返回占位贴图。

#### `scene/in_scene/rewards/presenters/RewardDraftCardDataApplier.gd`

- 用途：把真实卡牌读取到的数据写入奖励页 DraftCard。
- 维护：不要创建真实卡牌，不清理临时牌堆。
- 改进：写入字段可继续集中成明确 data 字典。

#### `scene/in_scene/rewards/presenters/RemoveDeckCardSelectionPresenter.gd`

- 用途：处理删除奖励页牌组卡牌的单选、取消选择和确认/返回按钮状态。
- 维护：不要删除卡牌，不创建卡牌，也不修改 `GlobalDB` 或运行时牌组。
- 改进：如果删除奖励页未来支持多选，可以把返回值改成选中集合。

#### `scene/in_scene/rewards/presenters/RemoveDeckDisplayCleaner.gd`

- 用途：清空删除奖励页的牌组卡牌节点、选中展示节点和展示状态。
- 维护：不要删除真实牌组数据，不关闭奖励页，也不生成新的卡牌。
- 改进：如果清理前需要播放批量退场动画，先新增动画 runner，再由主流程决定何时调用 cleaner。

#### `scene/in_scene/rewards/presenters/RemoveConfirmedCardUiCleaner.gd`

- 用途：确认删除动画完成后，从牌组网格移除已删除卡牌，释放节点并清空选中展示区域。
- 维护：不要删除真实牌组数据，不设置奖励提交状态，也不关闭奖励页。
- 改进：如果确认删除后需要更复杂的退场表现，先拆动画 runner，本模块只负责最终 UI 清理。

#### `scene/in_scene/rewards/presenters/RewardTooltipAdapter.gd`

- 用途：初始化、显示和隐藏奖励页卡牌 tooltip。
- 维护：不要判断奖励是否可领取，不读写牌组。
- 改进：可与 InScene 的 tooltip adapter 继续统一。

#### `scene/in_scene/rewards/presenters/ShopItemClearer.gd`

- 用途：清空商店商品 UI 和商品记录。
- 维护：不要生成商品，不处理购买。
- 改进：如果清理动画出现，先拆动画 runner，不要塞进 clear_items。

#### `scene/in_scene/rewards/presenters/ShopItemSlotPresenter.gd`

- 用途：把准备好的商店 DraftCard 包装成商品位 UI。
- 维护：不要选择卡牌，不消费时间币。
- 改进：价格标签样式可以资源化。

#### `scene/in_scene/rewards/presenters/ShopItemRegistry.gd`

- 用途：把已构建好的商品槽加入商店网格，记录商品卡和价格数据，并绑定购买点击信号。
- 维护：不要选择卡牌，不计算价格，也不要读取或修改牌组数据。
- 改进：如果以后商品槽需要统一埋点或可购买状态标记，可让本模块返回注册结果，由主流程决定后续动作。

#### `scene/in_scene/rewards/presenters/ShopPricingPresenter.gd`

- 用途：根据调用方传入的定价参数计算商店价格并更新价格标签。
- 维护：不要消费时间币，不生成商品，不处理购买。
- 改进：价格公式如果继续复杂化，可拆成更小规则模块；静态数值仍优先放在 `ShopPricingConfig.gd`。

#### `scene/in_scene/rewards/presenters/ShopPurchaseCardDetachPresenter.gd`

- 用途：购买成功后把卡牌从商品槽脱离出来并保持屏幕位置。
- 维护：不要消费时间币，不播放飞行动画，不写入或同步牌库。
- 改进：如果商品槽脱离前需要反馈动画，应单独新增动画模块。

#### `scene/in_scene/rewards/presenters/ShopPurchasedItemRecordRemover.gd`

- 用途：购买飞行动画完成后，从 `shop_cards` 和 `card_price_map` 移除已购卡牌记录。
- 维护：不要写入牌组，不同步抽牌堆，也不要释放或移动卡牌节点。
- 改进：如果购买完成后需要更多统计或埋点，优先让本模块返回结果，再由商店主流程决定后续动作。

### rules

#### `scene/in_scene/rewards/rules/CraftRecipeResolver.gd`

- 用途：查询合成配方结果，优先读取 `CraftRecipeBook`，资源缺失时回退旧字典。
- 维护：不要修改牌库，不创建卡牌，不改合成选择状态。
- 改进：如果旧 `CRAFTING_RECIPES` fallback 后续不再需要，可在确认资源稳定后删除。

#### `scene/in_scene/rewards/rules/CraftResultDeckIndexResolver.gd`

- 用途：计算合成结果写回前需要从牌组移除的倒序索引。
- 维护：不要修改 `GlobalDB`，不要添加结果卡，也不同步运行时抽牌堆。
- 改进：后续可和删除奖励页的牌组索引规则统一。

#### `scene/in_scene/rewards/rules/CraftResultDeckWriteProcessor.gd`

- 用途：把合成结果写入传入的牌组数组，按倒序索引移除素材牌并追加结果卡。
- 维护：不要计算素材牌索引，不同步运行时抽牌堆，也不要处理奖励页 UI 或关闭流程。
- 改进：如果 `deck_manager` 后续提供稳定的批量替换接口，可以让本模块返回写入计划，再由桥接层执行。

#### `scene/in_scene/rewards/rules/CraftSelectionEntryBuilder.gd`

- 用途：生成合成选择列表条目，并返回 pending 选择和可直接返回状态建议。
- 维护：不要创建卡牌节点，不写主脚本状态，不处理按钮或选择面板 UI。
- 改进：返回字典后续可换成更明确的状态对象。

#### `scene/in_scene/rewards/rules/RemoveDeckCardRemovalProcessor.gd`

- 用途：删除奖励页确认移除时，从 `deck_manager` 或 `GlobalDB.player_deck` 移除一张指定卡。
- 维护：不要同步运行时抽牌堆，不关闭奖励页，也不处理删除动画或 UI 节点。
- 改进：如果 deck_manager 接口稳定，可以把日志和 GlobalDB 回退进一步收口。

#### `scene/in_scene/rewards/rules/RewardDeckCardIdProvider.gd`

- 用途：为奖励页读取当前牌组卡牌 ID 列表，优先使用 `deck_manager.get_deck_card_ids()`，否则回退到 `GlobalDB.player_deck`。
- 维护：不要修改牌组，不创建卡牌，也不同步运行时抽牌堆。
- 改进：如果奖励页后续需要过滤临时卡或锁定卡，可以让调用方传入过滤规则，不要把页面状态写进 provider。

#### `scene/in_scene/rewards/rules/ShopEraWeightSelector.gd`

- 用途：根据商店权重随机选择目标时代。
- 维护：不要读取 `CardDataPool`，不创建卡牌，不处理刷新或购买。
- 改进：权重配置可独立成资源，方便调试和测试。

#### `scene/in_scene/rewards/rules/ShopGenerationDependencyGuard.gd`

- 用途：检查商店生成前 `deck_manager` 和 `card_factory` 是否可用，并返回是否允许继续生成。
- 维护：不要生成商品，不修改生成锁，也不要访问商店 UI。
- 改进：如果未来还有更多生成前置条件，可以让调用方传入显式配置，避免读取页面状态。

#### `scene/in_scene/rewards/rules/ShopPurchaseValidator.gd`

- 用途：购买前调用时间币消费入口并输出成败日志。
- 维护：不要移动卡牌，不修改商品列表，不执行飞行动画。
- 改进：后续可把日志交给商店 logger，validator 只返回结果。

#### `scene/in_scene/rewards/rules/ShopRefreshPurchaseProcessor.gd`

- 用途：处理商店刷新按钮的费用计算、时间币消费、成败日志和刷新次数结果。
- 维护：不要生成商品，不更新价格标签，也不要处理时代升级。
- 改进：后续可和升级费用结算模块共享更小的费用消费 helper。

#### `scene/in_scene/rewards/rules/ShopUpgradePurchaseProcessor.gd`

- 用途：处理商店升级按钮的费用计算、时间币消费、升级次数和本地时代偏移结果。
- 维护：不要生成商品，不更新价格标签，也不要读取或修改全局时代。
- 改进：后续可和刷新费用结算模块共享更小的费用消费 helper。

### resources

#### `scene/in_scene/rewards/resources/CraftRecipeBook.gd`

- 用途：保存合成配方表并提供只读查询。
- 维护：不要修改牌组，不创建卡牌，不访问场景树或奖励页 UI。
- 改进：新增配方优先改 `default_craft_recipe_book.tres`，不要回到 `CraftReward.gd` 写硬编码字典。

#### `scene/in_scene/rewards/resources/CraftRecipeEntry.gd`

- 用途：记录一条合成配方的静态数据：主卡、副卡和结果卡。
- 维护：不要检查卡牌是否存在，不处理正反向匹配逻辑。
- 改进：如果配方后续需要成本、条件或时代限制，优先给 entry 加静态字段，再由 resolver 或规则模块读取。

#### `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`

- 用途：默认合成配方资源，目前承载 `wind + tower -> tornado` 和 `lighting + earthquake -> poison`。
- 维护：这是数据资产，不是脚本模块；新增配方时只改资源内容，并保留 `CraftRecipeBook.gd` 的只读职责。
- 改进：等编辑器缓存稳定后，可以给 `.tres` 补 `uid`，但不要把忽略的 `.gd.uid` 加入提交。

#### `scene/in_scene/rewards/resources/ShopPricingConfig.gd`

- 用途：保存商店经济定价的静态参数，包括基础价格、商品位价格步进、刷新/升级基础费用和费用增量。
- 维护：不要在这里计算价格，不消费时间币，不生成或购买商品，也不要读取场景节点。
- 改进：如果后续出现折扣、时代倍率或限时促销，优先新增独立规则模块读取本资源，不要让 Resource 承担运行时流程。

#### `scene/in_scene/rewards/resources/default_shop_pricing_config.tres`

- 用途：默认商店定价数据资产，当前等价旧导出值：基础价格 50、商品位步进 5、刷新基础费用 50、升级基础费用 100、费用增量 25。
- 维护：这是数据资产，不是脚本模块；调参时只改静态数值，不写运行态次数、时间币余额或商品节点。
- 改进：如果不同章节需要不同商店经济参数，可以新增多份 `.tres`，由场景或上层流程选择资源。

#### `scene/in_scene/rewards/resources/ShopEraWeightConfig.gd`

- 用途：保存商店按时代选卡的静态权重参数，包括当前时代、前一个时代、下一个时代和下两个时代。
- 维护：不要在这里读取卡池，不生成卡牌，不决定最终商品，也不要保存运行时选中的时代。
- 改进：如果后续需要按章节或难度调整商店卡池倾向，可以新增多份 `.tres`，由上层流程选择资源。

#### `scene/in_scene/rewards/resources/default_shop_era_weight_config.tres`

- 用途：默认商店时代权重数据资产，当前等价旧导出值：当前时代 0.85、前一个时代 0.05、下一个时代 0.09、下两个时代 0.01。
- 维护：这是数据资产，不是脚本模块；调参时只改静态权重，不写全局时代、随机数结果或卡牌 ID。
- 改进：如果未来支持事件商店或特殊房间商店，可以通过替换资源改变时代倾向，而不是改 `ShopManager.gd`。

## HexMap 拆分模块

这些文件服务于 `scene/in_scene/hex_map.gd`。详细数据契约看 `docs/hex-map-ultimate-operation-guide.md`，这里只列每个文件的维护入口。

### bridges

#### `scene/in_scene/hex_map_modules/bridges/CardManagerLocator.gd`

- 用途：统一查找局内运行时 `CardManager`。
- 维护：保持旧查找顺序，不要写卡牌或牌组状态。
- 改进：可与奖励页 locator 合并成共享 locator。

#### `scene/in_scene/hex_map_modules/bridges/HexMapSceneBridge.gd`

- 用途：集中查找 HexMap 依赖的局内场景节点。
- 维护：只改路径和兜底，不修改节点状态。
- 改进：场景路径稳定后可减少递归查找。

### destruction

#### `scene/in_scene/hex_map_modules/destruction/TileDestructionBatchQueue.gd`

- 用途：调度高度超限地块的批量销毁节奏。
- 维护：不要执行真实 mutation，只调用传入的 `perform_destruction`。
- 改进：队列状态可以增加调试快照，方便排查连锁销毁。

#### `scene/in_scene/hex_map_modules/destruction/TileDestructionMutationService.gd`

- 用途：执行单个地块销毁的 VFX、节点释放和数据清理。
- 维护：不要决定批处理节奏，不重新生成地图。
- 改进：VFX 与数据 mutation 可以继续拆成两个步骤。

### elevation

#### `scene/in_scene/hex_map_modules/elevation/TileElevationService.gd`

- 用途：编排单个地块高度变化、动画和越界销毁调用。
- 维护：不要刷新敌人列表或奖励状态，仍由 HexMap 处理。
- 改进：高度规则和高度表现可以继续分离。

### factory

#### `scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd`

- 用途：初始建图阶段把地貌挂到地块 stack。
- 维护：不要创建基础 stack，不写 `stack_nodes`。
- 改进：地貌 sprite 收编和阵营分组可以继续拆细。

#### `scene/in_scene/hex_map_modules/factory/TileStackFactory.gd`

- 用途：创建基础地块 `Area2D`、sprite、碰撞体和 metadata。
- 维护：不要挂接地貌，不连接输入信号。
- 改进：碰撞体和基础 sprite 创建可以继续按 presenter/factory 分层。

#### `scene/in_scene/hex_map_modules/factory/TileStackInitializationService.gd`

- 用途：地块创建完成后的 metadata、输入信号和高度标签初始化。
- 维护：不要主动查找场景树，不读取 HexMap 成员。
- 改进：metadata 写入可以集中成常量 key 表。

#### `scene/in_scene/hex_map_modules/factory/TileStackRebuildService.gd`

- 用途：集中单格 stack 重建边界。
- 维护：不要改成全图重建，不引入对象池副作用。
- 改进：后续可真正局部更新 sprite 和 metadata，减少释放重建。

### generation

#### `scene/in_scene/hex_map_modules/generation/LandformPlacementService.gd`

- 用途：把地貌实例投放到已生成的 `map_data`。
- 维护：可以写 `map_data.landform`，但不要创建地块节点。
- 改进：地貌投放配额可资源化，便于关卡调参。

#### `scene/in_scene/hex_map_modules/generation/MapGenerationService.gd`

- 用途：生成局内地图纯规则数据。
- 维护：不要创建节点，不实例化地貌，不写 `GlobalClock`。
- 改进：地图模式可以注册化，避免 `match` 继续膨胀。

### height_view

#### `scene/in_scene/hex_map_modules/height_view/HeightViewIndicatorPresenter.gd`

- 用途：创建、移除和更新平铺高度视图指示器。
- 维护：不要改真实高度数据。
- 改进：光柱、数字标签样式可以资源化。

#### `scene/in_scene/hex_map_modules/height_view/HeightViewMapTransitionRunner.gd`

- 用途：整图压平成高度视图，或恢复到 3D 视图。
- 维护：不要改 `map_data` 高度，不生成新地块。
- 改进：单格压平和恢复可继续拆成小服务。

#### `scene/in_scene/hex_map_modules/height_view/HeightViewStateSynchronizer.gd`

- 用途：计算平铺视图下地块位置、缓存和同步状态。
- 维护：不要播放动画，不创建指示器。
- 改进：缓存字段可常量化，减少 metadata 拼写风险。

### input

#### `scene/in_scene/hex_map_modules/input/HexMapInputCoordinator.gd`

- 用途：分发地块鼠标输入流程。
- 维护：不要判断卡牌范围，不写 shader，不打开具体奖励界面。
- 改进：输入结果可以结构化，减少对回调副作用的依赖。

#### `scene/in_scene/hex_map_modules/input/TargetHoverController.gd`

- 用途：编排卡牌选中后的地块 hover 中心。
- 维护：不要计算卡牌范围，不查找节点路径。
- 改进：hover 状态可以返回结果字典，减少直接改数组。

### presenters

#### `scene/in_scene/hex_map_modules/presenters/EnemyIntentMapPresenter.gd`

- 用途：显示或清理地图侧敌人意图高亮和目标涟漪。
- 维护：不要生成时间轴意图，不改敌人行为。
- 改进：英文注释可补成中文职责注释，保持项目文档风格统一。

#### `scene/in_scene/hex_map_modules/presenters/HexMapCollisionPresenter.gd`

- 用途：构建碰撞多边形并刷新地块可交互状态。
- 维护：不要处理 hover shader，不改目标规则。
- 改进：碰撞配置可资源化，减少导出变量散落。

#### `scene/in_scene/hex_map_modules/presenters/HexMapVisualStatePresenter.gd`

- 用途：处理地图普通视觉状态、高亮和 shader 参数。
- 维护：不要写 `map_data`，不要推进奖励状态。
- 改进：视觉状态枚举可常量化，减少字符串状态。

#### `scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd`

- 用途：处理结算奖励在地图上的 hover、tooltip 和点击表现。
- 维护：不要消费奖励，不打开具体奖励页。
- 改进：奖励可点击状态可以和 controller 的规则结果分离。

#### `scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd`

- 用途：显示和清理卡牌目标 AOE hover 表现。
- 维护：不要判断目标是否合法，不执行卡牌效果。
- 改进：AOE 范围显示可继续和遮挡表现分离。

### registrars

#### `scene/in_scene/hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd`

- 用途：把外部渲染节点注册进地块 metadata 的渲染列表。
- 维护：不要创建地块，不决定高度视图状态。
- 改进：注册来源可以加标签，便于销毁时分类清理。

#### `scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd`

- 用途：注册运行时生成的地貌或建筑。
- 维护：不要执行地貌行为，不推进回合。
- 改进：运行时地貌与初始地貌挂接流程可以进一步统一。

### rewards

#### `scene/in_scene/hex_map_modules/rewards/SettlementRewardController.gd`

- 用途：管理地图结算奖励的状态读取和点击规则。
- 维护：不要负责奖励页 UI，也不要移动卡牌。
- 改进：奖励状态可以改成明确 resource，减少 metadata 依赖。

### rules

#### `scene/in_scene/hex_map_modules/rules/HexCoordRules.gd`

- 用途：集中六边形坐标和像素坐标换算。
- 维护：不要读取节点，不改地图数据。
- 改进：坐标规则若扩展地图模式，先补测试或手动验证用例。

#### `scene/in_scene/hex_map_modules/rules/HexTargetRules.gd`

- 用途：处理玩家 hover 和点击前目标判断。
- 维护：时间轴执行时的最终校验要同步看 `TimelineCommandTargetRules.gd`。
- 改进：目标规则可以按卡牌类型拆分。

#### `scene/in_scene/hex_map_modules/rules/HexTerrainRules.gd`

- 用途：维护地形类型、高度、显示和规则映射。
- 维护：不要创建地块节点，不处理 hover。
- 改进：地形表可资源化，减少硬编码字符串。

#### `scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd`

- 用途：时间轴命令执行时做最终目标校验。
- 维护：必须和 `HexTargetRules.gd` 保持一致，避免 hover 能放但执行失败。
- 改进：与 hover 目标规则共享核心规则对象。

### runners

#### `scene/in_scene/hex_map_modules/runners/MapIntroRevealRunner.gd`

- 用途：播放地图入场 reveal 动画。
- 维护：不要生成地图，不改敌人列表。
- 改进：入场动画参数可资源化。

### turn

#### `scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd`

- 用途：执行地貌、建筑和敌人的回合行为。
- 维护：不要生成新地图，不处理玩家输入。
- 改进：不同阵营或行为类型可以拆成策略对象。

### ui

#### `scene/in_scene/hex_map_modules/ui/TargetSelectionTooltipAdapter.gd`

- 用途：把地图目标选择状态转成 tooltip 显示。
- 维护：不要判断目标是否合法，不执行卡牌。
- 改进：可与 InScene 光标 tooltip 控制器统一样式。

## 常见改动怎么做

### 新增一个低风险 UI 表现

1. 先找已有 `presenters/` 或 `ui/` 模块。
2. 如果只是样式、显隐、位置或 tooltip，优先加到对应 presenter。
3. 如果需要新模块，保持入口只接收节点和配置，不主动查找场景树。
4. 回归检查至少加载涉及场景。

### 新增一个规则判断

1. 先找 `rules/`。
2. 规则模块只返回布尔值、坐标、字典或 id，不写节点。
3. 如果规则会影响 hover 和执行两个阶段，两个阶段必须同步修改。
4. 不要把调试 UI 或动画放进规则模块。

### 新增一个跨系统查找

1. 先找 `bridges/`。
2. 保持旧查找顺序，新增兜底要写清楚原因。
3. 不要在 bridge 里缓存玩法状态。
4. 查找不到时的旧行为不能静默改变。

### 改奖励页卡牌展示

1. 创建真实卡牌看 `RewardRealCardSpawner.gd`。
2. 读取卡面贴图看 `RewardCardTextureExtractor.gd`。
3. 读取描述看 `RewardCardDescriptionExtractor.gd`。
4. 写入 DraftCard 看 `RewardDraftCardDataApplier.gd`。
5. Tooltip 看 `RewardTooltipAdapter.gd`。

不要把真实卡牌生成、数据读取、DraftCard 写入和 tooltip 混回同一个奖励页脚本。

### 改商店购买流程

当前购买顺序是：

```text
ShopPurchaseValidator
-> ShopPurchaseCardDetachPresenter
-> RewardCardFlyToDeckAnimator
-> RewardDeckSyncBridge
-> ShopPurchasedItemRecordRemover
```

维护要求：

- 改价格和扣时间币，看 `ShopPurchaseValidator.gd`。
- 改卡牌从商品槽脱离，看 `ShopPurchaseCardDetachPresenter.gd`。
- 改飞行动画，看 `RewardCardFlyToDeckAnimator.gd`。
- 改写入牌组和同步抽牌堆，看 `RewardDeckSyncBridge.gd`。
- 改已购商品记录移除，看 `ShopPurchasedItemRecordRemover.gd`。

## 固定回归清单

文档批次：

```powershell
git diff --check
```

代码批次至少运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
```

按改动范围加载场景：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . res://scene/in_scene/in_scene.tscn --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . res://scene/in_scene/rewards/shop.tscn --quit --no-header
```

如果改了地图：

```text
检查普通地图视图。
检查高度视图切换和恢复。
检查敌人意图 hover。
检查结算奖励 hover 和领取状态。
```

如果改了拖拽：

```text
检查普通拖拽。
检查时间轴展开和格子预览。
检查非法放置拒绝提示。
检查放置动画结束后的手牌、时间轴和弃牌状态。
```

如果改了奖励页：

```text
检查奖励卡显示。
检查 tooltip。
检查购买或领取后的飞入牌库。
检查 deck_manager 和局内抽牌堆同步。
```

## 当前最值得继续优化的方向

### timeline_ui 剩余表现边界优先级更高

`timeline_ui.gd` 已经拆出展开遮罩表现、背景网格构建、网格交互表现、顶部锚点布局、网格预览样式、TimelineManager 查找、敌方意图 overlay、行动方格放置动画、行动容器几何计算、行动方块视觉节点创建、行动整体形状视觉层、清理动画残影创建、原容器运行时视觉清理、残影 tween 播放和行动块 hover 状态通知，并已完成 `TimelineVisualConfig` 首批纯视觉参数资源化。当前不要硬拆 `_on_action_placed()` 的剩余生成编排；后续只在需要新增纯视觉调参时小批进入。

### out_scene_map_exp 已完成结算与揭示动画首批拆分

`out_scene_map_exp.gd` 已拆出 `RoomResolutionController.gd`、`ChapterRevealAnimationRunner.gd` 和 `OutScenePayloadBridge.gd`。当前返回战斗后的结算读取、boss 后 tier 推进判断、坐标解析、章节揭示地块动画和切场前 payload 注入已经有独立模块承接。房间完成状态回写目前缺少既有状态字段，继续前要先设计数据契约；不要同批改地图移动、镜头限制和场景切换 executor。

本轮已审查房间完成状态回写契约，结论是暂不复用任何现有字段。`path_gone` 表示路径坍塌记录，不能表示房间是否完成；`active_room_context` 与 `pending_room_resolution` 只负责跨场景上下文和一次性返回 payload，不能作为长期状态；`tile_data` 仍应保持“坐标 -> 房间类型”的简单逻辑地图，避免影响 `map_renderer.gd`、移动判断和存档恢复。后续如果需要房间完成状态，应新增独立字典，例如按 `Vector2i` 记录 `completed`、`cleared`、`reward_claimed` 等语义，再单独补 `Saver.gd` 保存/读取和局外视觉刷新。这个实现应作为单独批次处理。

### ShopManager 剩余边界已经接近停止点

`ShopManager.gd` 的购买路径、刷新费用结算、升级费用结算、CardDataPool 读取桥接、商品槽注册、生成依赖检查、静态定价配置、时代权重配置和隐藏临时牌堆创建都已经拆出。`_update_price_display()` 已经转发给 `ShopPricingPresenter`，定价数值来自 `ShopPricingConfig`，时代权重来自 `ShopEraWeightConfig`，临时牌堆隐藏创建来自 `RewardTempPileFactory.create_hidden_temp_pile(...)`，继续围绕这些配置和创建步骤硬拆收益很低。

```text
_generate_shop_items()
open_shop()
```

如果继续处理商店，本轮已经确认单个商品生成编排会牵动 `draft_card_factory`、`temp_pile`、`deck_manager`、UI 注册和异步数据提取等过多状态，不建议硬拆。临时牌堆释放仍绑定异步生成循环，除非要统一多个奖励页的释放策略，否则停止 ShopManager 生成链拆分。

### CraftReward 继续清理页面专属小边界

`CraftReward.gd` 已经拆出配方查询、合成配方资源、合成移除索引、合成结果牌组写入、选择条目构建、选择卡状态、连接线、预览清理、预览卡 UI 配置、结果预览挂载、结果描述样式/内容/定位、选择标题、槽位占位符、槽位预览布局、奖励页通用卡牌读取模块和只读牌组来源模块，并且 `_create_preview_card()` 已复用 `RewardDraftCardFactory`。下一步可重新扫描剩余大函数：

```text
_refresh_result_preview()
```

`_create_preview_card()` 剩余临时牌堆创建、真实卡生成、数据写入和释放临时牌堆属于同一异步编排，不建议继续硬拆。`_apply_crafting_result_to_deck()` 剩余同步运行时抽牌堆和关闭流程也已经接近页面编排，不建议继续硬拆。`CRAFTING_RECIPES` 已资源化，后续不要再围绕 Craft 页面硬拆；除非新增配方字段，否则转向 timeline_ui 或 out_scene_map_exp。

### RemoveReward 继续清理页面专属小边界

`RemoveReward.gd` 已经复用奖励页通用的 CardManager 查找、临时牌堆、真实卡牌生成、真实卡牌清理、DraftCard 数据写入、tooltip、牌组同步和只读牌组来源模块。删除页专属的牌组卡牌单选 presenter、牌组显示清理 cleaner、删牌数据处理规则和确认删除后的 UI 清理也已经拆出。

下一步可继续扫描：

```text
_on_confirm_pressed()
```

下一步不要急着继续拆完整确认删除动画。它剩余部分主要是选择保护、按钮禁用、tween 创建、删牌入口、奖励提交和关闭流程，已经接近页面流程编排。除非后续要统一多个奖励页的确认动画，否则建议停止 RemoveReward，转向 timeline_ui 或 out_scene_map_exp。

### 不要急着继续拆 HexMap

`hex_map.gd` 已经进入维护阶段。除非新增地图规则或修 bug，否则优先维护已有模块边界，不要为了行数继续机械搬函数。

### DragShapeController 成功收尾暂时停止

拖拽放置动画前后的状态已经拆出不少，玩家 `TimelineAction` 创建、时间轴提交、成功后的卡牌视觉复原和弃牌移动已经分别进入 `DragPlayerActionFactory.gd`、`DragTimelineActionSubmitter.gd`、`DragSuccessCardVisualRestorer.gd` 与 `DragSuccessDiscardMover.gd`。后续可关注放置成功后的 UI 收尾流程，但要先写清楚时间轴收起、主状态清理和回手牌 fallback 的边界。

本轮已经评估：

```text
end_dragging_success() 中成功后 UI/时间轴收起只是既有 DragTimelineUiStateController.collapse(timeline_ui) 的单行调用
_return_card_to_hand() 的失败 fallback 同时触碰手牌、timeline toggle、tooltip、目标状态和当前卡牌状态
```

当前不要继续围绕成功收尾硬拆。除非未来要统一多处回手牌 fallback，否则保留 `DragShapeController.gd` 作为 composition root。

### InScene 剩余大块需要更强回归

`in_scene.gd` 剩余的 `_ready()`、回合推进和场景切换 wrapper 风险较高。继续拆前，先准备更完整的手动验证路线。

## 最后的维护原则

拆分不是目标，稳定的边界才是目标。

如果一个新模块需要知道太多主脚本成员，先停下来重新划边界。好的模块应该能用一句中文说清楚职责，也应该能用一句中文说清楚它不负责什么。
