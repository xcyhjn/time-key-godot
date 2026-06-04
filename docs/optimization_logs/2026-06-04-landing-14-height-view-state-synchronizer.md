# 第 14 次落地：提取平铺和 3D 视角同步状态

日期：2026-06-04

## 本次处理范围

这次把平铺和 3D 高度视图中的缓存辅助函数、单地块同步逻辑从 `hex_map.gd` 中拆出来。全局高度视图切换流程、全图压平和恢复动画循环，以及高度变更逻辑仍然留在 `hex_map.gd`。

实体、血条、碰撞节点、结算奖励 tooltip，以及平铺视图中新增的运行时地貌，都保持原来的同步行为。

## 新增模块

### `scene/in_scene/hex_map_modules/height_view/HeightViewStateSynchronizer.gd`

这个模块负责：

- 计算某个地块在平铺视图中的下落距离。
- 构建和更新 3D 原始位置缓存。
- 从 `Node2D` 和 `Control` 节点读取当前位置。
- 通过直接赋值或 HexMap 注入的 tween 回调同步节点 y 坐标。
- 在运行时改变某个地块后，把这个地块同步到当前平铺或 3D 状态。
- 通过注入回调解耦血条查找和奖励 tooltip 刷新。

主要函数：

- `get_drop_delta(stack, config)`：根据高度和间距配置计算平铺视图 y 偏移。
- `get_stack_height(stack)`：安全读取地块高度 metadata。
- `get_or_create_cache(stack, sprites, config)`：构建或复用单地块原始位置缓存。
- `get_or_add_sprite_cache(cache, sprite)`：把运行时创建的地块子 sprite 加入缓存。
- `get_visual_node_position(node)`：读取 `Node2D` 或 `Control` 的本地位置。
- `sync_node_position_y(node, target_y, animate, config)`：直接写入或 tween 到目标 y 坐标。
- `sync_cached_node_to_flat(cache, key, node, drop_delta, animate, config)`：补充或更新缓存节点数据，并应用平铺偏移。
- `sync_stack_to_current_view(coord, animate, config)`：运行时视觉变化后同步单个地块。
- `_build_health_bar_cache_data(occupant, config)`：为地貌 occupant 构建 `health_bar_data`。
- `_find_health_bar_for_occupant(occupant, config)`：通过注入回调解析血条节点。

## HexMap 的变化

- 新增 `HEIGHT_VIEW_STATE_SYNCHRONIZER` preload 和 `_height_view_state_synchronizer`。
- 以下辅助函数改成包装 synchronizer：
  - `_get_height_view_drop_delta()`
  - `_get_or_create_height_view_cache()`
  - `_get_or_add_sprite_cache()`
  - `_get_visual_node_position()`
  - `_sync_node_position_y()`
  - `_sync_cached_node_to_flat()`
  - `_sync_stack_to_current_view()`
- 新增 `_build_height_view_state_synchronizer_config()`。
- 新增 `_update_settlement_reward_tooltip_position_for_stack()`，作为回调桥接奖励 tooltip 的位置刷新。

## 可调变量

本次没有新增导出变量。

synchronizer 通过 config 使用这些既有运行时和导出数据：

- `stack_nodes`
- `height_view_original_materials`
- `filler_block_spacing`
- `tile_scale`
- `REF_SCALE`
- `current_view_state`
- `_cleanup_stack_sprites`
- `_find_health_bar_for_landform`
- `_tween_position_y`
- `_settlement_reward_presenter.update_tooltip_position`

## 调整说明

- 想调整平铺视图的垂直偏移，继续调整 `filler_block_spacing`、`tile_scale` 和 `REF_SCALE` 这一套流程。
- 想调整同步包装函数的 tween 时长，改 `_build_height_view_state_synchronizer_config()` 里的 `position_tween_duration`。
- 在手动高度视图回归稳定前，不要把全图 `_compress_to_single_height_view()` 和 `_restore_original_height_view()` 搬进这个模块。
- 不要在这个模块里修改地块高度或 `map_data`，升降高度仍然留在 `hex_map.gd`。
- 奖励 tooltip 位置刷新通过回调完成，所以这个模块不直接依赖 `SettlementRewardPresenter`。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

## 手动回归重点

- 切进平铺高度视图，确认顶部 sprite、occupant、碰撞节点和血条一起下落。
- 在平铺视图中生成或建造运行时地貌，确认新地貌会立即对齐到平面。
- 触发结算奖励 tooltip，确认平铺和 3D 切换后 tooltip 位置会更新。
- 切回 3D 视图后，确认缓存的原始位置能正确恢复。
- 在平铺视图中使用升降高度效果，确认缓存位置保持稳定。
