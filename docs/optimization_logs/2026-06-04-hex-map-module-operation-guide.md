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
- VFX 时间参数仍然通过 `_perform_tile_destruction()` 传给 `VFXManager`，不是队列模块自己播放。

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
- 普通 hover、AOE 和遮挡高亮已拆到 `HexMapVisualStatePresenter.gd`。下一步可以继续拆 `_on_stack_hover()` 的输入编排，或开始拆 tile elevation / destruction mutation。
