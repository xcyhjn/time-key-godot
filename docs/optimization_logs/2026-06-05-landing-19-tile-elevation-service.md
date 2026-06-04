# 第 19 次落地：提取地块升降编排服务

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中单个地块高度升降的大段流程拆到独立服务。拆分对象包括升降动画互斥等待、高度上下限判断、`map_data` 和高度池同步、3D 视图下整柱移动、平铺视图下后台 3D 缓存维护、侧面块补删、shader 高度下标刷新、高度数字反馈，以及超限后进入销毁队列的触发。

`hex_map.gd::animate_elevation_change()` 继续保留原入口名，时间轴 `ElevationCommand.gd` 和已有卡牌效果不需要改调用方式。本次不拆真实删除地块的 `_perform_tile_destruction()`，也不改变高度规则、销毁时机、血条命名规则或平铺/3D 视图切换行为。

## 新增模块

### `scene/in_scene/hex_map_modules/elevation/TileElevationService.gd`

这个模块负责：

- 等待同一地块上一次升降动画结束。
- 根据 `delta_height` 计算真实目标高度和视觉高度。
- 判断高度是否超过 `max_height` 或低于等于 `min_height`。
- 写入地块 `height` metadata、`map_data[coord].height` 和 `GlobalClock.tile_h_pool`。
- 在 3D 视图下移动地块贴图、碰撞箱、地貌实体和必要的外部血条。
- 在 3D 视图动画结束后补充或移除底部侧面块。
- 在平铺视图下更新 `height_view_original_materials` 中的原始 3D 坐标缓存。
- 在平铺视图下只播放高度数字变化反馈，不播放整柱升降。
- 刷新所有地块渲染节点的 `block_idx` 和 `total_height` shader 参数。
- 在高度越界后调用 HexMap 注入的销毁队列。
- 在非销毁升降完成后通过回调发出 `tile_topology_changed` 信号。

主要函数：

- `animate(stack, delta_height, config)`：升降编排入口。
- `_get_visual_height(new_height, min_height, should_destroy_after_elevation)`：计算表现阶段使用的高度。
- `_apply_height_data(stack, coord, old_height, visual_height, config)`：同步高度数据和高度池。
- `_animate_flat_view(...)`：平铺视图下的升降处理。
- `_animate_3d_view(...)`：3D 视图下的升降处理。
- `_shift_flat_cache_after_height_delta(cached_data, delta_y)`：维护平铺视图后台 3D 缓存。
- `_add_flat_side_sprites(...)`：平铺视图升高时补入隐藏侧面块。
- `_remove_bottom_sprites(sprites, remove_count, cached_data)`：降低高度时移除底部侧面块。
- `_collect_3d_moving_parts(stack, sprites, config)`：收集 3D 视图下需要一起移动的节点。
- `_schedule_3d_movement(...)`：安排震动和升降 Tween。
- `_finish_3d_sprite_mutation(...)`：动画结束后补删侧面块。
- `_create_side_sprite(side_tex, config)`：创建保持旧视觉参数的侧面块。
- `_queue_tile_destruction(stack, coord, config)`：把超限地块交回销毁队列。

## HexMap 的变化

- 新增 `TILE_ELEVATION_SERVICE` preload。
- 新增 `_tile_elevation_service` 实例。
- `animate_elevation_change(stack, delta_height)` 保留旧入口名，内部委托 `TileElevationService.animate()`。
- 新增 `_build_tile_elevation_service_config()`，集中传入地图数据、导出调参、当前视图状态和回调。
- 新增 `_create_tile_elevation_tween()`，让服务创建的 Tween 仍挂在 HexMap 节点下。
- 新增 `_get_elevation_health_bar_for_occupant()`，只把需要独立移动的外部血条交给服务。
- 新增 `_animate_elevation_height_label()`，让平铺视图高度数字动画继续复用 `HeightViewIndicatorPresenter`。
- 新增 `_emit_tile_topology_changed()`，给服务发出拓扑变化信号。

## 可调变量

本次没有新增导出变量。以下已有导出项仍在 `hex_map.gd` 中调整：

- `ele_anim_duration`：3D 升降移动耗时。
- `ele_shake_intensity`：升降前横向震动强度。
- `ele_shake_duration`：升降前震动时间。
- `ele_trans_type`：3D 升降 Tween 曲线类型。
- `ele_ease_type`：3D 升降 Tween 缓动方向。
- `elevation_move_distance`：平铺视图下后台 3D 缓存移动距离。
- `filler_block_spacing`：侧面块层间距，也是升降后补块的垂直间距。
- `elevation_resolution_padding`：时间轴等待升降和销毁结算后的额外缓冲，仍由 `ElevationCommand.gd` 读取。
- `max_height`：超过这个高度后，地块会在升降表现后进入销毁队列。
- `min_height`：低于或等于这个高度后，地块会先显示为 1 层，再进入销毁队列。
- `tile_destruction_batch_size`：同一批最多销毁的地块数量。
- `tile_destruction_batch_collect_delay`：收集同一波超限地块的短暂等待。
- `tile_destruction_batch_interval`：多批销毁之间的间隔。
- `tile_destruction_shake_count`、`tile_destruction_shake_step_duration`、`tile_destruction_shake_distance`、`tile_destruction_dissolve_duration`：真实销毁 VFX 参数。

## 调整说明

- 不要在 `TileElevationService.gd` 中直接查找 BarManager。血条查找必须通过 HexMap 注入回调完成。
- 不要在服务中直接调用 `tile_topology_changed.emit()`。需要发信号时走 `emit_tile_topology_changed` 回调。
- 不要把 `_perform_tile_destruction()` 搬进这个服务；真实删除和静态库清理已经拆到 `TileDestructionMutationService.gd`。
- 平铺视图下升降时必须维护 `height_view_original_materials`，否则切回 3D 后地貌、碰撞箱或血条会错位。
- 新增或删除侧面块后必须刷新 `block_idx` 和 `total_height`。
- 如果后续修改高度上下限规则，需要同时检查 `HexTargetRules.gd` 的放置预检和 `ElevationCommand.gd` 的等待逻辑。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- `git diff --check` 退出码为 0。
- Godot headless 项目加载退出码为 0。
- Godot headless 局内场景加载退出码为 0。
- headless 当前仍会输出项目既有的 `GlobalClock`、`Signal_Bus`、`Dialogic`、`TimelineAction`、TileSet atlas 和资源释放噪声。
- 本次没有新增 `TileElevationService.gd` 或 `hex_map.gd` 解析错误。

## 手动回归重点

- 在 3D 视图下使用升高地块效果，确认地块贴图、碰撞箱、地貌和血条一起移动。
- 在 3D 视图下降低地块高度，确认底部侧面块减少且顶部块保留。
- 把地块升到超过 `max_height`，确认升高表现后进入批量销毁队列。
- 把地块降到 `min_height` 或以下，确认先显示为 1 层，再进入销毁队列。
- 切到平铺高度视图后使用升降效果，确认高度数字更新，切回 3D 后地貌、碰撞箱和血条位置正确。
- 连续对同一个地块触发升降，确认 `is_animating` 等待仍能阻止动画互相覆盖。
