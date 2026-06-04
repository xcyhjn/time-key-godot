# HexMap 模块操作指南

日期：2026-06-04

## 这份文档用来做什么

这份文档记录当前 `hex_map.gd` 的拆分结果。组员后续查看、调参或继续拆模块时，可以先看这里，不需要每次重新读完整的战斗地图脚本。

当前重构原则是保持行为不变。除非后续明确要改玩法，否则不要在拆模块时顺手改变卡牌目标、地图生成、奖励可用性或地块销毁时机。

## 已提取模块

### `scene/in_scene/hex_map_modules/rules/HexCoordRules.gd`

这个模块负责战斗地图的纯坐标计算：

- 生成扇形地图的轴坐标。
- 生成圆形地图的轴坐标。
- 计算轴坐标到原点的距离。
- 把轴坐标转换成项目里的像素坐标。

主要调用方：

- `hex_map.gd::_get_fan_coords()`
- `hex_map.gd::_get_circular_coords()`
- `hex_map.gd::_get_hex_pixel_pos()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `fan_radius`
- `fan_angle_span`
- `map_radius`
- `spacing_x`
- `spacing_y`
- `tile_scale`

调整时注意：

- 想加宽或收窄扇形战斗地图，调 `fan_angle_span`。
- `spacing_x`、`spacing_y` 和 `tile_scale` 最好一起看，因为它们同时影响视觉布局和 hitbox 对齐。

### `scene/in_scene/hex_map_modules/rules/HexTerrainRules.gd`

这个模块负责地形和高度的纯规则：

- 计算扇形地图的地形层级。
- 使用固定随机源按层级生成高度。
- 按房间类型计算圆形房间的高度范围。
- 把高度映射成地形类型。
- 提供地形和地貌的调试名称。

主要调用方：

- `hex_map.gd::_generate_fan_map_data()`
- `hex_map.gd::_generate_circular_map_data()`
- `hex_map.gd::get_terrain_from_height()`
- `hex_map.gd::terrain_type_to_string()`
- `hex_map.gd::terrain_name()`
- `hex_map.gd::landform_name()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `inner_tier_radius`
- `base_h_min`
- `base_h_max`
- `elite_h_bonus`
- `terrain_by_height`

调整时注意：

- 想按高度调整视觉地形，改 `terrain_by_height`，这不会改变高度生成。
- 想调整圆形房间的危险曲线，改 `base_h_min`、`base_h_max` 和 `elite_h_bonus`。
- 扇形地图的层级概率现在还是代码常量。只有策划需要频繁迭代时，才建议改成导出变量。

### `scene/in_scene/hex_map_modules/destruction/TileDestructionBatchQueue.gd`

这个模块负责高度超限地块销毁的队列编排：

- 接收高度超限后的地块销毁请求。
- 在短时间内合并多个请求。
- 按批次启动销毁回调。
- 等待每个地块移除队列 metadata 后再继续。

主要调用方：

- `hex_map.gd::_queue_tile_destruction_and_wait()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `tile_destruction_batch_size`
- `tile_destruction_batch_collect_delay`
- `tile_destruction_batch_interval`
- `tile_destruction_shake_count`
- `tile_destruction_shake_step_duration`
- `tile_destruction_shake_distance`
- `tile_destruction_dissolve_duration`

调整时注意：

- `tile_destruction_batch_size` 决定一次消失多少个超限地块。
- `tile_destruction_batch_collect_delay` 应保持较小，它只是把同一波地形变化收集到一起。
- VFX 时间参数会经由 `_perform_tile_destruction()` 包装入口传给 `TileDestructionMutationService.gd`，不是队列模块自己播放。

### `scene/in_scene/hex_map_modules/rules/HexTargetRules.gd`

这个模块负责卡牌放置前的地图目标规则：

- 判断当前选中卡牌能不能选中某个地块。
- 根据卡牌的绝对效果范围收集地图地块。
- 统一建造、伤害、恢复或治疗、中毒、升降高度、清除和未知效果的目标语义。

主要调用方：

- `hex_map.gd::_is_stack_valid_target()`
- `hex_map.gd::_update_aoe_display()`
- `in_scene.gd::is_valid_target()`

目前没有新增导出变量。

规则说明：

- `built/build` 要求地块没有 occupant，并且 `map_data.landform/landform_in` 为空。
- `damage` 要求效果范围内至少有一个支持 `take_damage()` 的存活实体。
- `recover/heal` 要求效果范围内至少有一个可治疗实体；如果有 HP 字段，满血实体不算有效目标。
- `poison` 要求效果范围内至少有一个支持 `add_status()` 的存活实体。
- `elevation` 对任意有效地形地块都保持有效。
- `clear` 走无地图目标的时间轴清除路径，所以保持有效。
- 未知效果保持有效，避免重构期间误伤原型卡。
- 不要把 shader、tooltip 或 `CardManager` 查找逻辑写进这个模块。
- timeline command 仍然负责执行时的最终校验；这里是给玩家反馈和点击拦截用的预检规则。

### `scene/in_scene/hex_map_modules/presenters/EnemyIntentMapPresenter.gd`

这个模块负责敌人意图在地图上的表现：

- 高亮敌人意图来源地块。
- 创建或复用目标波纹覆盖层。
- 清除预览时恢复来源地块原本的 shader 状态。
- 把地图侧意图表现和时间轴规则、tooltip UI 分开。

主要调用方：

- `hex_map.gd::show_enemy_intent_preview()`
- `hex_map.gd::clear_enemy_intent_preview()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `enemy_intent_frame_tex`
- `enemy_intent_target_shader`
- `enemy_intent_overlay_scale`
- `enemy_intent_overlay_z_index`
- `enemy_intent_source_valid_highlight_blend`
- `enemy_intent_source_valid_selected_blend`
- `enemy_intent_source_self_highlight_blend`
- `enemy_intent_source_self_selected_blend`
- `enemy_intent_source_invalid_highlight_blend`
- `enemy_intent_source_invalid_selected_blend`
- `enemy_intent_target_ripple_speed`
- `enemy_intent_target_ripple_density`
- `enemy_intent_target_min_alpha`
- `enemy_intent_target_max_alpha`
- `hitbox_width`
- `hitbox_base_height`
- `hitbox_offset_x`
- `hitbox_offset_y`

调整时注意：

- 想调整目标波纹运动和透明度，改 `enemy_intent_target_*`。
- 想调整来源地块在有效、自身包含、无效状态下的高亮强度，改 `enemy_intent_source_*_blend`。
- `enemy_intent_source_highlight_width` 仍保留给旧的来源覆盖层实验，但当前 presenter 不使用它。

### `scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd`

这个模块负责结算奖励的地图表现：

- 给符合条件的地块 sprite 写入结算奖励 shader instance 参数。
- 创建或复用 `SettlementRewardTooltip` 面板。
- 根据奖励信息或地貌自定义文案刷新 tooltip 文本。
- 播放 tooltip hover 缩放动画。
- 在 3D 和平铺高度视图切换后重新定位奖励 tooltip。
- 离开奖励模式或地块失去资格时清理奖励表现。

主要调用方：

- `hex_map.gd::exit_settlement_reward_mode()`
- `hex_map.gd::_collect_settlement_reward_stacks()`
- `hex_map.gd::_handle_settlement_reward_hover()`
- `hex_map.gd::mark_settlement_reward_used()`
- `hex_map.gd::_sync_stack_to_current_view()`
- `hex_map.gd::_refresh_all_settlement_reward_tooltip_positions()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `settlement_reward_tooltip_offset`
- `settlement_reward_tooltip_size`
- `settlement_reward_tooltip_font_size`
- `settlement_reward_tooltip_z_index`
- `settlement_reward_highlight_color`
- `settlement_reward_hover_color`
- `settlement_reward_highlight_blend`
- `settlement_reward_hover_blend`
- `settlement_reward_tooltip_hover_scale`

调整时注意：

- 奖励资格和 payload 创建仍然留在 `hex_map.gd`，不要把奖励状态规则放进 presenter。
- 想移动 tooltip，改 `settlement_reward_tooltip_offset`。
- 想调整奖励视觉强度，改 `settlement_reward_highlight_blend` 和 `settlement_reward_hover_blend`。
- tooltip 定位还会读取 `step_height`、`tile_scale`、`REF_SCALE` 和 `current_view_state`。

### `scene/in_scene/hex_map_modules/presenters/HexMapCollisionPresenter.gd`

这个模块负责战斗地图的碰撞箱和鼠标输入开关：

- 根据 hitbox 导出变量构建六边形 `CollisionPolygon2D` 点集。
- 重新应用单个地块的碰撞箱位置、形状和 z-index。
- 批量重建当前地图里的所有碰撞箱。
- 根据当前阶段刷新所有地块的 `input_pickable`。
- 入场动画锁定时禁用所有地块输入。
- 结算奖励阶段只允许可领奖励地块接收输入。
- 普通闲置阶段在智能碰撞开启时只允许敌方建筑地块接收输入。

主要调用方：

- `hex_map.gd::_build_hitbox_polygon()`
- `hex_map.gd::_get_safe_hitbox_z_index()`
- `hex_map.gd::_refresh_collision_for_stack()`
- `hex_map.gd::rebuild_all_collision_shapes()`
- `hex_map.gd::_refresh_stack_interactivity()`
- `hex_map.gd::_stack_has_enemy_building()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `enable_smart_collision_interaction`
- `hitbox_width`
- `hitbox_base_height`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `hitbox_top_width_ratio`
- `hitbox_z_index`
- `map_intro_reveal_lock_interaction`
- `step_height`
- `tile_scale`

运行时会读取：

- `stack_nodes`
- 地块 `height`、`occupant` 和 `collision_node` metadata
- `_tiles_interactive_master_enabled`
- 当前视图状态
- 当前是否处于入场动画
- 当前是否处于结算奖励模式
- HexMap 注入的奖励资格回调

调整时注意：

- 这个模块只负责碰撞和 `input_pickable`，不要把 hover shader、AOE 高亮、敌人意图或奖励 tooltip 写进来。
- 奖励资格仍由 HexMap 判断，presenter 只消费回调结果。
- `rebuild_all_collision_shapes()` 仍然保留在 HexMap 上，外部工具或调试脚本可以继续调用旧入口。
- 如果修改 hitbox 形状，必须同时检查 3D 视图和平铺视图下的点击区域是否贴合顶部地块。

### `scene/in_scene/hex_map_modules/presenters/HexMapVisualStatePresenter.gd`

这个模块负责普通地图视觉状态写入：

- 清理 AOE、选中地块和当前活动地块的高亮。
- 把新的 AOE 范围写入有效或无效 hover 状态。
- 清理动态遮挡的 `dissolve_blend`。
- 在 3D 视图下计算目标地块前方需要半透明处理的遮挡柱。
- 按 TileVisualState 数字写入地块 shader 高亮颜色和 blend。
- 提供通用 shader 参数 Tween，供取消选中和遮挡恢复复用。

主要调用方：

- `hex_map.gd::_clear_all_aoe_highlights()`
- `hex_map.gd::_update_aoe_display()`
- `hex_map.gd::_clear_occlusion_effects()`
- `hex_map.gd::_update_occlusion()`
- `hex_map.gd::_tween_shader_param()`
- `hex_map.gd::change_tile_state()`

目前没有新增导出变量。旧视觉参数仍然是代码常量：

- tile state tween 时长：`0.15`
- 遮挡清理时长：`0.12`
- 遮挡淡入时长：`0.25`
- 遮挡 dissolve 强度：`0.8`
- 遮挡横向判断系数：`0.8`

运行时会读取：

- `stack_nodes`
- 地块 `sprites` 和 `height` metadata
- `current_aoe_stacks`
- `selected_stack`
- `active_stack`
- `currently_occluding_stacks`
- 当前是否为平铺视图
- `hitbox_width`
- `step_height`、`tile_scale` 和 `REF_SCALE`

调整时注意：

- 这个模块只负责视觉写入，不判断卡牌目标是否合法。
- AOE 范围收集仍由 `HexTargetRules` 提供，HexMap 只把结果交给 presenter。
- MainBoard tooltip 更新仍留在 `hex_map.gd::_update_aoe_display()`。
- 敌人意图 overlay 仍归 `EnemyIntentMapPresenter.gd`。
- 结算奖励高亮和 tooltip 仍归 `SettlementRewardPresenter.gd`。

### `scene/in_scene/hex_map_modules/height_view/HeightViewIndicatorPresenter.gd`

这个模块负责高度视图的光柱和数字：

- 创建和移除高度光柱 `Line2D`。
- 创建和更新高度数字 `Label`。
- 平铺高度视图激活时，持有并播放光柱 hover tween。
- 平铺视图里高度变化后播放数字反馈。
- 指示器节点仍然存放在既有地块 metadata key 中。

主要调用方：

- `hex_map.gd::_create_height_indicator()`
- `hex_map.gd::_remove_height_indicator()`
- `hex_map.gd::_start_pillar_floating_animation()`
- `hex_map.gd::_stop_pillar_floating_animation()`
- `hex_map.gd::animate_elevation_change()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `show_height_pillars`
- `show_height_labels`
- `height_view_pillar_length`
- `height_view_pillar_width`
- `height_view_pillar_color`
- `height_view_pillar_offset`
- `height_label_font_size`
- `height_label_color`
- `height_label_outline_color`
- `height_label_outline_size`
- `height_label_offset`
- `height_label_font`
- `height_view_hover_speed`
- `height_view_hover_amplitude`

调整时注意：

- `show_height_pillars` 和 `show_height_labels` 可以分别开关光柱和数字。
- `height_view_pillar_offset` 会一起移动光柱和数字锚点。
- `height_label_offset` 只移动数字相对光柱锚点的位置。
- `height_view_hover_speed` 和 `height_view_hover_amplitude` 控制 hover 浮动。
- presenter 不修改地块高度数据；升降高度仍然留在 `hex_map.gd`。

### `scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd`

这个模块负责运行时地貌注册：

- 注册新创建的运行时地貌实体。
- 兼容旧路径：当 `map_data` 已经先被写入时，只刷新地貌视觉。
- 把实体挂到地块上，并更新 `occupant` metadata。
- 应用敌方或中立分组，但不负责发场景级信号。
- 给地貌 sprite 应用 block shader 参数。
- 视觉变化后重新收集地块渲染 sprite。

主要调用方：

- `hex_map.gd::register_runtime_landform()`
- `hex_map.gd::add_landform_visual_at()`
- `hex_map.gd::_cleanup_stack_sprites()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `step_height`
- `tile_scale`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- `runtime_landform_spawn_vfx_enabled`
- `runtime_landform_spawn_vfx_duration`

调整时注意：

- 建造卡、村庄扩张、雷达召唤和未来的运行时实体创建都优先走 `register_runtime_landform()`。
- 只有旧流程已经写过 `map_data` 并且只需要视觉刷新时，才使用 `add_landform_visual_at()`。
- `enemy_roster_changed`、`tile_topology_changed` 和 VFX 播放继续留在 `hex_map.gd`。
- 实体锚点通过 `hitbox_offset_x`、`hitbox_offset_y` 和 `landform_instance_offset` 调整。
- 运行时生成 VFX 通过 `runtime_landform_spawn_vfx_enabled` 和 `runtime_landform_spawn_vfx_duration` 调整。

### `scene/in_scene/hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd`

这个模块负责血条和外部渲染节点注册：

- 按现有 `HealthBar_<landform_instance_id>` 命名规则，从 BarManager 中找到血条。
- 把血条和外部 UI 渲染节点注册到地块 `sprites` metadata。
- 递归收集 `Sprite2D`、`TextureRect` 和 `TextureProgressBar` 子节点。
- 写入高度 shader instance 参数，但不替换 UI 节点材质。

主要调用方：

- `hex_map.gd::recollect_sprites_for_landform()`
- `hex_map.gd::register_extra_render_node()`
- `hex_map.gd::_find_health_bar_for_landform()`

目前没有新增导出变量。

运行时会读取：

- `stack_nodes`
- 地块 `sprites` metadata
- 地块 `height` metadata

调整时注意：

- 新增外部渲染节点类型时，在 `_is_supported_render_node()` 中加类型。
- 血条创建和删除仍然归 BarManager；这个模块只负责让血条加入地图共享视觉效果。
- 平铺和 3D 视图下的位置同步仍然归 HexMap 的视图同步流程。

### `scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd`

这个模块负责 timeline command 执行时的最终目标校验：

- 执行时解析地块 occupant 和坐标。
- 把建造、伤害、恢复或治疗、中毒接收者校验集中到一个纯辅助模块。
- 给 command 脚本提供共享的动态属性检查。

主要调用方：

- `BuiltCommand.gd`
- `DamageCommand.gd`
- `RecoverCommand.gd`
- `PoisonCommand.gd`
- `ElevationCommand.gd`

目前没有新增导出变量。

运行时会读取：

- `EffectCommand.target_tiles`
- `EffectCommand.hex_map`
- 地块 `occupant` metadata
- `hex_map.stack_nodes`
- `hex_map.map_data`

调整时注意：

- `HexTargetRules.gd` 是放置前校验，`TimelineCommandTargetRules.gd` 是执行时校验。
- 不要把 VFX、等待逻辑、卡牌 JSON 解析或实体创建写进这个辅助模块。
- 新 timeline command 需要目标检查时，在这里加一个小 helper，让 command 继续只关心副作用。

### `scene/in_scene/hex_map_modules/height_view/HeightViewStateSynchronizer.gd`

这个模块负责平铺和 3D 视图之间的单地块同步辅助：

- 管理平铺和 3D 视图的原始位置缓存。
- 计算单个地块在平铺视图中的下落距离。
- 运行时改变单个地块后，把它同步到当前视图。
- 通过直接赋值或 HexMap 注入的 tween 回调移动 `Node2D` 和 `Control`。
- 通过回调解耦血条查找和结算 tooltip 刷新。

主要调用方：

- `hex_map.gd::_sync_stack_to_current_view()`
- `hex_map.gd::_get_or_create_height_view_cache()`
- `hex_map.gd::_sync_cached_node_to_flat()`
- `hex_map.gd::_sync_node_position_y()`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `filler_block_spacing`
- `tile_scale`

运行时会读取：

- `height_view_original_materials`
- `stack_nodes`
- 地块 `height`、`sprites`、`occupant` 和 `collision_node` metadata

调整时注意：

- 生成地貌、重建视觉或注册外部渲染节点后，可以使用这个模块做运行时单地块同步。
- 不要在这里修改地块高度或 `map_data`，这个模块只处理缓存和视觉 y 坐标。

### `scene/in_scene/hex_map_modules/height_view/HeightViewMapTransitionRunner.gd`

这个模块负责整张地图进入平铺高度视图和恢复 3D 视图的循环：

- 遍历 `stack_nodes`，对每个地块执行平铺或恢复。
- 进入平铺视图前写入恢复缓存。
- 给地块 sprite 写入 `is_flat_view` shader instance 参数。
- 侧面块进入平铺视图时淡出并隐藏，恢复 3D 时重新显示并淡入。
- 顶部块、occupant、collision 和 health_bar 按旧公式下落或恢复。
- 通过 HexMap 注入回调创建和移除高度指示器。

主要调用方：

- `hex_map.gd::_compress_to_single_height_view()`
- `hex_map.gd::_restore_original_height_view()`

目前没有新增导出变量。

运行时会读取：

- `stack_nodes`
- `height_view_original_materials`
- `filler_block_spacing`
- `tile_scale`
- `REF_SCALE`
- 地块 `height`、`sprites`、`occupant` 和 `collision_node` metadata

调整时注意：

- 这个模块只管全图切换动画，不管运行时单格新增。运行时单格新增仍然看 `HeightViewStateSynchronizer.gd`。
- 血条查找、Tween 创建和高度指示器创建都通过 HexMap 回调注入，不要在 runner 里直接依赖场景节点路径。
- 切换后的奖励 tooltip 全量刷新仍然在 `toggle_height_view()` 中。
- 不要在这里修改地块高度或 `map_data`。

### `scene/in_scene/hex_map_modules/runners/MapIntroRevealRunner.gd`

这个模块负责战斗地图初次生成后的涟漪入场流程：

- 判断当前构建是否应该播放初始入场。
- 管理入场是否正在播放、是否已经播放过，以及延迟血条请求队列。
- 按地块到左下源点的距离分层创建 Tween。
- 通过 `dissolve_blend` 让地块从完全湮灭恢复到正常显示。
- 入场期间隐藏 BarManager 和总血量条，结束后恢复。
- 入场结束后补发单体血条创建请求。
- 入场结束后回调 HexMap 刷新地块交互、敌人列表和 `map_intro_reveal_finished` 信号。

主要调用方：

- `hex_map.gd::_begin_map_intro_reveal_if_needed()`
- `hex_map.gd::is_map_intro_reveal_active()`
- `hex_map.gd::should_defer_intro_health_bars()`
- `hex_map.gd::queue_intro_health_bar_request()`
- `hex_map.gd::_play_map_intro_reveal()`
- `hex_map.gd::_finish_map_intro_reveal()`

相关导出变量仍然在 `hex_map.gd` 的“地块入场动画”导出组中调整：

- `map_intro_reveal_enabled`
- `map_intro_reveal_tile_duration`
- `map_intro_reveal_wave_delay`
- `map_intro_reveal_wave_pixel_step`
- `map_intro_reveal_finish_delay`
- `map_intro_reveal_origin_padding`
- `map_intro_reveal_hide_health_ui`
- `map_intro_reveal_lock_interaction`
- `map_intro_reveal_trans_type`
- `map_intro_reveal_ease_type`

运行时会读取：

- `stack_nodes`
- `_map_intro_reveal_state`
- 地块 `sprites` metadata
- HexMap 注入的 Tween、Timer、BarManager、总血量条、血条创建和信号回调

调整时注意：

- 这个模块只管入场流程，不生成地图、不修改 `map_data`，也不决定敌人或地貌规则。
- BarManager 和总血量条的路径仍然留在 `hex_map.gd`，runner 只能通过回调拿到节点。
- `map_intro_reveal_lock_interaction` 的交互锁仍由 `hex_map.gd::_refresh_stack_interactivity()` 执行，runner 只维护状态。
- 如果以后要加新入场样式，优先在 runner 中新增播放分支，并继续让可调参数从 HexMap 导出组传入。

### `scene/in_scene/hex_map_modules/elevation/TileElevationService.gd`

这个模块负责单个地块的高度升降编排：

- 等待同一地块上一个升降流程结束。
- 计算真实目标高度、视觉展示高度，以及是否需要在表现结束后销毁地块。
- 同步写入地块 `height` metadata、`map_data[coord].height` 和 `GlobalClock.tile_h_pool`。
- 在 3D 视图下移动地块贴图、碰撞箱、地貌实体和必要的外部血条。
- 在 3D 视图升降动画结束后，补充或移除底部侧面块，并刷新 shader 高度下标。
- 在平铺高度视图下，只更新后台 3D 位置缓存、侧面块队列和高度数字反馈，不播放整柱 3D 移动。
- 高度超过上下限时，调用 HexMap 注入的销毁队列入口，不直接删除地块。

主要调用方：

- `hex_map.gd::animate_elevation_change()`
- `scene/in_scene/timeline/commands/ElevationCommand.gd`

相关导出变量仍然在 `hex_map.gd` 中调整：

- `ele_anim_duration`
- `ele_shake_intensity`
- `ele_shake_duration`
- `ele_trans_type`
- `ele_ease_type`
- `elevation_move_distance`
- `filler_block_spacing`
- `elevation_resolution_padding`
- `max_height`
- `min_height`
- `tile_destruction_batch_size`
- `tile_destruction_batch_collect_delay`
- `tile_destruction_batch_interval`
- `tile_destruction_shake_count`
- `tile_destruction_shake_step_duration`
- `tile_destruction_shake_distance`
- `tile_destruction_dissolve_duration`

运行时会读取：

- `stack_nodes`
- `map_data`
- `GlobalClock.tile_h_pool`
- `height_view_original_materials`
- 地块 `height`、`sprites`、`collision_node` 和 `occupant` metadata
- HexMap 注入的侧面贴图、Tween 创建、血条查找、高度数字动画、销毁队列和拓扑变化信号回调

调整时注意：

- `TileElevationService.gd` 只管升降过程，不直接调用 BarManager 路径、不直接发奖励、不直接改敌人列表。
- 超限地块的真实删除已经由 `hex_map.gd::_perform_tile_destruction()` 包装入口委托给 `TileDestructionMutationService.gd`，队列仍然在 `TileDestructionBatchQueue.gd` 中。
- 平铺视图下的高度变化必须同时维护 `height_view_original_materials`，否则切回 3D 后地貌、碰撞箱和血条会错位。
- 新增侧面块时必须刷新 `block_idx` 和 `total_height`，否则 hover、遮挡和平铺 shader 会读到旧层数。
- 地块真实删除已经拆到 `TileDestructionMutationService.gd`，不要把删除数据字典或旧静态库清理再塞回升降服务。

### `scene/in_scene/hex_map_modules/destruction/TileDestructionMutationService.gd`

这个模块负责高度超限后的单个地块真实删除：

- 读取地块 `sprites` metadata，并调用 `VFXManager.play_tile_destruction_vfx()` 播放销毁表现。
- 保存并释放地块 occupant。
- 释放地块 `Area2D` 根节点。
- 从 `GlobalClock.tile_h_pool` 中移除被销毁坐标。
- 从 `stack_nodes` 和 `map_data` 中删除被销毁坐标。
- 刷新地块输入状态。
- 清理 `iron_mine.Library` 和 `village.Library` 这类旧静态坐标库。
- 通过 HexMap 注入回调发出 `enemy_roster_changed` 和 `tile_topology_changed`。

主要调用方：

- `hex_map.gd::_perform_tile_destruction()`
- `TileDestructionBatchQueue.gd` 通过 HexMap 包装入口间接调用

相关导出变量仍然在 `hex_map.gd` 中调整：

- `tile_destruction_shake_count`
- `tile_destruction_shake_step_duration`
- `tile_destruction_shake_distance`
- `tile_destruction_dissolve_duration`

运行时会读取：

- `stack_nodes`
- `map_data`
- `GlobalClock.tile_h_pool`
- 地块 `sprites` 和 `occupant` metadata
- `iron_mine.Library`
- `village.Library`
- HexMap 注入的 `VFXManager`、`SceneTree`、交互刷新和信号回调

调整时注意：

- 这个模块只做单个地块 mutation，不做批处理节奏。批处理仍归 `TileDestructionBatchQueue.gd`。
- 不要在这里决定地块是否应该销毁；高度上下限判断仍在 `TileElevationService.gd`。
- 如果新增会缓存坐标的旧建筑脚本，需要把它加入 HexMap 传入的 `landform_library_holders`。
- 如果后续要清理更多跨系统状态，例如奖励模式缓存或敌人意图缓存，优先通过 HexMap 提供明确回调，不要让服务直接查找其他 controller。

### `scene/in_scene/hex_map_modules/input/HexMapInputCoordinator.gd`

这个模块负责战斗地图地块鼠标输入的流程分发：

- 处理 `Area2D.input_event` 传入的鼠标点击。
- 左键根据当前模式转发到结算奖励点击或普通地块点击。
- 右键转发到取消卡牌选择。
- 普通地块点击时，有选中卡牌就尝试打出卡牌，没有选中卡牌就切换地块选中高亮。
- 鼠标进入或离开地块时，优先处理结算奖励 hover。
- 普通 hover 下继续处理平铺高度光柱、hovered stack 列表、AOE 刷新和敌人意图 hover 转发。

主要调用方：

- `hex_map.gd::_on_stack_input()`
- `hex_map.gd::_handle_tile_click()`
- `hex_map.gd::_on_stack_hover()`

目前没有新增导出变量。

运行时会读取：

- `current_settlement_reward_mode`
- `is_visuals_locked`
- `current_view_state`
- `hovered_stacks`
- `selected_stack`
- `height_view_hovered_stack`
- HexMap 注入的 CardManager 查询、目标合法性检查、状态切换、奖励点击/hover、光柱动画、AOE 刷新和敌人意图 hover 回调

调整时注意：

- 这个模块只负责编排输入，不直接判断卡牌范围，不直接写 shader，也不直接查找奖励 UI 或 TimelineSystem。
- `HexMapInputCoordinator.gd` 返回需要回写的状态，`hex_map.gd::_apply_input_coordinator_result()` 负责写回 HexMap 成员变量。
- 敌人意图管理器路径暂时还留在 `hex_map.gd::_handle_enemy_intent_stack_hover()`，后续拆 TimelineSystem 依赖时再处理。
- 如果要删除 `active_card.has_method("play_card")` 兜底，应先确认战斗内所有可选卡牌都继承或实现同一出牌接口。

### `scene/in_scene/hex_map_modules/input/TargetHoverController.gd`

这个模块负责卡牌选中后的目标 hover 编排：

- 清理 `hovered_stacks` 中已经释放的地块。
- 按旧规则从当前 hover 地块里选择屏幕 y 值最大的前景地块。
- 没有选中卡牌时，触发 AOE 清理、遮挡清理，并隐藏 MainBoard 的 `cursor_tooltip`。
- 当前前景地块变化时，触发 AOE 更新和动态遮挡更新。
- 把需要回写的 `hovered_stacks` 和 `active_stack` 返回给 HexMap。

主要调用方：

- `hex_map.gd::_update_highlight()`
- `HexMapInputCoordinator.gd` 通过 HexMap 注入的 `update_highlight` 回调间接触发它
- `clear_enemy_intent_preview(true)` 重新恢复卡牌 hover 时也会经过旧 `_update_highlight()` 入口

目前没有新增导出变量。

运行时会读取：

- `hovered_stacks`
- `active_stack`
- HexMap 注入的当前选中卡牌读取回调
- HexMap 注入的 MainBoard 读取回调
- HexMap 注入的 AOE 清理、遮挡清理、AOE 更新和遮挡更新回调

调整时注意：

- 这个模块只决定“当前 hover 中心是谁”和“中心变化时调哪些旧入口”，不要在这里计算卡牌范围。
- `_update_aoe_display()` 仍留在 `hex_map.gd`，因为它还同时依赖 `HexTargetRules`、`stack_nodes`、目标合法性和 MainBoard tooltip。
- CardManager 查找仍保留在 `hex_map.gd::get_card_manager()`，后续统一 `CardManagerLocator.gd` 时再收敛。
- Tooltip 的具体文案和位置更新仍由 MainBoard 负责，这里只保留无卡牌时隐藏旧 tooltip 的行为。

### `scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd`

这个模块负责生成卡牌目标 AOE hover 的展示计划：

- 根据 `card + center_stack + stack_nodes` 计算新的 AOE 地块列表。
- 通过 HexMap 注入的目标合法性回调，决定合法目标高亮或非法目标高亮。
- 返回 tooltip 请求，让 HexMap 继续调用 MainBoard 的旧 tooltip 接口。

主要调用方：

- `hex_map.gd::_update_aoe_display()`

目前没有新增导出变量。

运行时会读取：

- `stack_nodes`
- `TileVisualState.HOVER_TARGET_VALID` 对应的状态值
- `TileVisualState.HOVER_TARGET_INVALID` 对应的状态值
- HexMap 注入的 `_is_stack_valid_target()` 回调
- `HexTargetRules.get_effect_range_stacks()` 规则函数

调整时注意：

- 这个模块只生成展示计划，不直接写 shader，不直接改 `current_aoe_stacks`。
- MainBoard tooltip 的真实调用仍在 `hex_map.gd::_update_target_selection_tooltip()`，后续如果要拆 UI controller，可以从这个函数继续下手。
- 目标合法性仍走 HexMap 旧入口，主要是为了暂时保留 CardManager 生命周期兜底；等 `CardManagerLocator.gd` 完成后再考虑把合法性上下文进一步收窄。

### `scene/in_scene/hex_map_modules/factory/TileStackFactory.gd`

这个模块负责创建基础地块栈：

- 创建 `Area2D` 地块容器，并挂到 HexMap 传入的 `map_root`。
- 创建基础顶面和侧面 `Sprite2D`。
- 复制 `block_material`，并写入基础 shader 实例参数。
- 按 3D/平铺视图处理基础地块层的位置和显隐。
- 创建 `CollisionPolygon2D`，写入碰撞形状、z_index 和位置。
- 写入 `collision_node` metadata。

主要调用方：

- `hex_map.gd::_create_stack_at()`

目前没有新增导出变量。

运行时会读取：

- HexMap 传入的 `map_root`
- HexMap 传入的地块像素位置
- 地块高度
- 当前视图是否平铺
- `tile_scale`
- `current_step_h`
- 顶面/侧面纹理
- `block_material`
- 碰撞多边形、z_index 和偏移

调整时注意：

- 这个模块不写 `stack_nodes`，地图拓扑仍由 HexMap 持有。
- 这个模块不挂接地貌实体，也不处理 `owner_battle`、`Enemies` 分组和血条。
- 地貌挂接由 `TileLandformAttachService.gd` 负责，metadata 与输入信号收尾由 `TileStackInitializationService.gd` 负责。
- 后续如果继续拆局部重绘，优先保持基础工厂只理解“基础地块栈”，不要让它开始处理奖励、卡牌或回合行为。

### `scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd`

这个模块负责初始建图阶段的地貌挂接：

- 从地块数据里读取 `landform` 实例。
- 按旧公式设置地貌位置，并挂到 stack 下。
- 调用地貌自己的 `attach_visual()`。
- 收编 stack 下的 `LandformSprite_*`。
- 收编地貌实例自己的子 `Sprite2D`。
- 给这些地貌 sprite 复制 `block_material`，并写入 `block_idx`、`total_height` 和平铺视图参数。
- 设置 `owner_battle`。
- 按旧逻辑只给敌方地貌加入 `Enemies` 分组。

主要调用方：

- `hex_map.gd::_create_stack_at()`

目前没有新增导出变量。

运行时会读取：

- 地块数据里的 `landform`
- 基础地块栈
- 基础 `sprites` 列表
- 地块高度
- `current_step_h`
- `tile_scale`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- 当前是否平铺视图
- HexMap 自身作为 `owner_battle`

调整时注意：

- 这个模块只服务初始建图路径，运行时建造仍由 `RuntimeLandformRegistrar.gd` 负责。
- 这里暂时不写 `location/target` 和 `map_data`，保持旧 `_create_stack_at()` 行为。
- 这里暂时不把中立地貌加入 `Middle` 分组，保持旧 `_create_stack_at()` 行为。
- 如果后续要统一初始建图和运行时注册，建议先抽共享的 sprite 收编工具，再合并数据写入流程。

### `scene/in_scene/hex_map_modules/factory/TileStackInitializationService.gd`

这个模块负责地块栈创建完成后的初始化收尾：

- 写入 `sprites`、`height`、`occupant` metadata。
- 在地图开场揭示动画期间，把新建地块的 dissolve 初始值设为完全隐藏。
- 连接地块 `mouse_entered`、`mouse_exited` 和 `input_event` 信号。
- 在平铺高度视图下，通过 HexMap 注入的回调创建高度数字标签。

主要调用方：

- `hex_map.gd::_create_stack_at()`

目前没有新增导出变量。

运行时会读取：

- 基础地块栈。
- 地块 sprite 列表。
- 地块高度。
- 地貌占用者。
- 当前是否处于地图入场揭示。
- 当前是否处于平铺高度视图。
- HexMap 注入的 dissolve、高度标签和输入信号回调。

调整时注意：

- 这个模块不直接读取 `stack_nodes`、`current_view_state` 或任何 HexMap 成员变量，所有状态必须通过 `_build_tile_stack_initialization_config()` 传入。
- 鼠标回调在 HexMap 里提前绑定 stack，服务只负责连接信号，不判断 hover、点击、奖励模式或敌人意图模式。
- `occupant` 为空时仍然写入 metadata，保持旧逻辑里 `has_meta("occupant")` 可以成立。
- 高度标签仍走 `_create_height_indicator()` 回调，因此不要在这里直接依赖 `HeightViewIndicatorPresenter.gd`。

## 修改后的验证清单

改动任意已提取模块后，先运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

当前已知噪声：

- 局内战斗场景会输出既有的 TileSet atlas 报错。
- headless 退出时会输出既有资源释放提示。
- 新的解析错误、脚本缺失、preload 失败或退出码变化，都要当成阻塞问题处理。

## 手动回归路径

解析检查通过后，在编辑器里重点走这些路径：

- 启动一场普通战斗，确认地图能正常生成。
- hover 或选择卡牌目标，确认 AOE 高亮仍然跟随卡牌效果范围。
- 使用升降高度效果，把地块推到上下限外，确认批量销毁仍然能完成。
- 如果当前测试存档能走房间流程，确认战斗结束后仍能返回局外。

## 下一批拆分候选

建议按这个优先级继续：

- 等目标限制稳定后，再清理敌人和结算 tooltip 的边界情况。
- 手动高度视图回归稳定后，再考虑把高度视图切换状态机本身拆成更薄的 controller。
- 地图开场血条延迟队列已经拆到 `MapIntroRevealRunner.gd`。下一步如果继续拆 BarManager 耦合，应优先看血条创建接口和总血量刷新接口。
- 碰撞和 input_pickable 已拆到 `HexMapCollisionPresenter.gd`。
- 普通 hover、AOE 和遮挡高亮已拆到 `HexMapVisualStatePresenter.gd`。
- 地块升降编排已拆到 `TileElevationService.gd`。
- 地块真实删除和旧静态库清理已拆到 `TileDestructionMutationService.gd`。
- 地块输入编排已拆到 `HexMapInputCoordinator.gd`。
- `_update_highlight()` 的 hover 中心选择和刷新触发已拆到 `TargetHoverController.gd`。
- `_update_aoe_display()` 的范围结果、目标状态和 tooltip 请求已拆到 `TargetAoeHoverPresenter.gd`。
- MainBoard tooltip 的具体 UI controller 仍未拆，当前只在 HexMap 中保留一层旧接口适配。
- `_create_stack_at()` 的基础地块容器、基础 sprite 和碰撞体已拆到 `TileStackFactory.gd`。
- `_create_stack_at()` 的初始地貌挂接和地貌 sprite 收编已拆到 `TileLandformAttachService.gd`。
- `_create_stack_at()` 的 metadata、输入信号、高度标签和入场 dissolve 收尾已拆到 `TileStackInitializationService.gd`。
- `refresh_tile_visual()` 仍然通过删除旧 stack 再调用 `_create_stack_at()` 重建，等工厂继续稳定后再拆局部重绘服务。
- 下一批建议优先拆 `CardManagerLocator.gd` 或 `_on_step_next()` 建筑回合行为 runner；如果继续沿地块链路，则看 `refresh_tile_visual()` 的局部重绘边界。
