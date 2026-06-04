# 第 15 次落地：提取全图平铺和 3D 过渡循环

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中全图平铺和 3D 恢复循环拆到独立模块。拆分对象是 `_compress_to_single_height_view()` 和 `_restore_original_height_view()` 里的全图遍历、缓存写入、侧面块淡入淡出、节点 y 坐标恢复，以及高度指示器创建和移除。

`hex_map.gd` 仍然负责视角状态机、切换锁、敌人意图清理、奖励 tooltip 统一刷新和所有导出调参来源。本次不修改高度数据、不修改 `map_data`，也不改变平铺视图的视觉时序。

## 新增模块

### `scene/in_scene/hex_map_modules/height_view/HeightViewMapTransitionRunner.gd`

这个模块负责：

- 遍历整张战斗地图并进入平铺高度视图。
- 遍历整张战斗地图并恢复到 3D 视图。
- 记录进入平铺视图前的 `sprites_data`、`occupant_data`、`collision_data` 和 `health_bar_data`。
- 给地块 sprite 写入 `is_flat_view` shader instance 参数。
- 让侧面块淡出后隐藏，并在恢复 3D 时淡入。
- 让顶部块、实体、碰撞节点和血条按旧公式一起下落或恢复。
- 通过回调创建和移除高度指示器。

主要函数：

- `compress_to_flat(config)`：全图进入平铺高度视图的主入口。
- `restore_to_3d(config)`：全图恢复到 3D 视图的主入口。
- `_compress_stack_to_flat(stack, config)`：压平单个地块栈。
- `_restore_stack_to_3d(stack, config)`：恢复单个地块栈。
- `_build_stack_cache(stack, sprites, config)`：构建旧格式的恢复缓存。
- `_restore_cached_sprites(stack, cache, config)`：恢复缓存中的地块 sprite。
- `_restore_cached_node(data, config)`：恢复 occupant、collision 或 health_bar。
- `_tween_cached_node_to_flat(data, drop_delta, config)`：把缓存节点移动到平铺位置。
- `_restore_side_sprites(stack, config)`：恢复当前高度下的侧面块可见性。
- `_cleanup_stack_sprites(stack, config)`：通过 HexMap 注入回调读取地块渲染节点。
- `_get_stack_height(stack, config)`：通过 HexMap 注入回调读取高度，缺失时回退到 metadata。
- `_get_drop_delta(height, config)`：使用旧公式计算下落距离。
- `get_visual_node_position(node)`：读取 `Node2D` 或 `Control` 的本地位置。
- `_set_flat_shader_state(item, enabled)`：写入 `is_flat_view` instance 参数。
- `_fade_side_sprite_out(item, config)`：淡出侧面块，并在动画结束后隐藏。
- `_fade_side_sprite_in(item, config)`：恢复侧面块透明度。
- `_tween_node_y(node, target_y, config)`：通过 HexMap 注入的 tween 回调移动 y 坐标。
- `_create_tween(config)`：通过 HexMap 注入的 `create_tween()` 创建 Tween。
- `_create_height_indicator(stack, height, config)`：通过回调创建高度指示器。
- `_remove_height_indicator(stack, config)`：通过回调移除高度指示器。
- `_find_health_bar_for_occupant(occupant, config)`：通过回调查找 occupant 对应血条。

## HexMap 的变化

- 新增 `HEIGHT_VIEW_MAP_TRANSITION_RUNNER` preload。
- 新增 `_height_view_map_transition_runner` 实例。
- 新增 `_find_height_view_health_bar_for_occupant(occupant)`，把旧的 `HealthBar_<instance_id>` 查找规则留在 HexMap 内部。
- 新增 `_build_height_view_map_transition_runner_config()`，集中给 runner 注入运行时数据和回调。
- `_compress_to_single_height_view()` 现在只委托 `HeightViewMapTransitionRunner.compress_to_flat()`。
- `_restore_original_height_view()` 现在只委托 `HeightViewMapTransitionRunner.restore_to_3d()`。

## 与现有高度视图模块的边界

- `HeightViewMapTransitionRunner.gd` 负责整张地图切换时的全图循环和动画编排。
- `HeightViewStateSynchronizer.gd` 负责平铺视图中运行时新增或变化的单个地块同步。
- `HeightViewIndicatorPresenter.gd` 负责光柱和数字的具体表现。
- `hex_map.gd` 仍然是 composition root，负责状态切换、回调注入和切换后的奖励 tooltip 全量刷新。

## 可调变量

本次没有新增导出变量。

runner 通过 config 使用这些既有数据：

- `stack_nodes`
- `height_view_original_materials`
- `filler_block_spacing`
- `tile_scale`
- `REF_SCALE`
- `_cleanup_stack_sprites`
- `_get_stack_height`
- `_find_height_view_health_bar_for_occupant`
- `_tween_position_y`
- `create_tween`
- `_create_height_indicator`
- `_remove_height_indicator`

runner 当前沿用旧时长：

- `position_tween_duration = 0.3`
- `side_fade_duration = 0.3`

如果以后需要开放给策划调节，建议先把它们加到 `hex_map.gd` 的高度视图导出组，再从 `_build_height_view_map_transition_runner_config()` 传入。

## 调整说明

- 不要在 `HeightViewMapTransitionRunner.gd` 中修改地块高度或 `map_data`。
- 不要在 runner 中直接查找 BarManager，血条查找必须通过 HexMap 注入。
- 不要在 runner 中直接操作奖励 tooltip；切换完成后的 tooltip 全量刷新仍在 `toggle_height_view()` 中。
- 如果修改侧面块淡入淡出时机，要同时检查平铺视图切入和恢复 3D 两条路径。
- 如果运行时新增地块在平铺视图中出现异常，优先检查 `HeightViewStateSynchronizer.gd`，不是本模块。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- 三条命令退出码都是 0。
- 局内场景加载时仍会输出既有 TileSet atlas 报错。
- headless 退出时仍会输出既有资源释放提示。
- 没有新增 parser、preload 或缺失脚本错误。

## 手动回归重点

- 点击高度视图按钮进入平铺视图，确认侧面块淡出、顶部块下落、实体和血条一起移动。
- 平铺视图中确认每个地块的高度光柱和数字仍然出现。
- 点击高度视图按钮恢复 3D，确认侧面块淡入、顶部块和实体回到原位置。
- 在平铺视图中 hover 地块，确认光柱 hover 动画不受影响。
- 在结算奖励 tooltip 可见时切换视图，确认 tooltip 在切换后仍能重新定位。
- 在平铺视图中建造或生成运行时地貌，确认单地块同步仍由 `HeightViewStateSynchronizer.gd` 正常接管。
