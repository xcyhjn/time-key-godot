# 第 9 次落地：提取高度视图 pillar 和 label 表现

日期：2026-06-04

## 本次处理范围

这次把高度视图中的光柱和数字表现从 `hex_map.gd` 中拆出来。高度视图切换、地块压平和恢复逻辑、地块高度数据变更仍然留在 `hex_map.gd`。

现有的光柱创建、数字创建、hover 浮动和平铺视图高度数字弹跳反馈都保持原行为。

## 新增模块

### `scene/in_scene/hex_map_modules/height_view/HeightViewIndicatorPresenter.gd`

这个模块负责：

- 创建 `HeightIndicatorLine` 光柱。
- 创建 `HeightIndicatorLabel` 高度数字。
- 使用既有的地块 metadata key 存放指示器节点。
- 在平铺高度视图中播放光柱 hover 浮动。
- 平铺视图里发生高度变化后，播放数字缩放和颜色反馈。
- 离开高度视图时移除指示器节点，并停止光柱 tween。

主要函数：

- `create_indicator(stack, original_height, config)`：为单个地块创建光柱和数字。
- `remove_indicator(stack)`：移除指示器节点，并清理相关 metadata。
- `start_pillar_floating(stack, config)`：启动循环的光柱浮动动画。
- `stop_pillar_floating(stack, config)`：停止循环浮动，并保持旧行为的视觉基线。
- `animate_label_height(stack, visual_height, config)`：更新高度数字并播放弹跳反馈。
- `_get_stack_sprites(stack)`：安全读取地块 sprite metadata。
- `_get_indicator_position(top_sprite, height, config)`：在平铺或 3D 视图中计算光柱和数字锚点。
- `_create_pillar(stack, original_height, position, config)`：创建 `Line2D` 光柱并写入 metadata。
- `_create_label(stack, original_height, position, config)`：创建 `Label` 数字并写入 metadata。
- `_queue_meta_node_free(stack, meta_key)`：释放地块 metadata 中引用的指示器节点。
- `_kill_pillar_tween(stack)`：停止并清理 presenter 持有的光柱 tween。

## HexMap 的变化

- 新增 `HEIGHT_VIEW_INDICATOR_PRESENTER` preload 和 `_height_view_indicator_presenter`。
- 从 `hex_map.gd` 中移除 `height_view_pillar_tweens`，现在由 presenter 持有这些 tween。
- 以下函数缩减为 presenter 包装：
  - `_create_height_indicator`
  - `_remove_height_indicator`
  - `_start_pillar_floating_animation`
  - `_stop_pillar_floating_animation`
- 新增 `_build_height_view_indicator_presenter_config()`，集中收集当前导出值。
- `animate_elevation_change()` 中平铺视图高度数字弹跳逻辑改为调用 `HeightViewIndicatorPresenter.animate_label_height()`。

## 可调变量

这些变量仍然在 `hex_map.gd` 中导出，并通过 `_build_height_view_indicator_presenter_config()` 传给 presenter：

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

同时会传入这些运行时值：

- `ele_anim_duration`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `current_view_state`

## 调整说明

- 想隐藏光柱但保留数字，关闭 `show_height_pillars`。
- 想隐藏数字但保留光柱，关闭 `show_height_labels`。
- 想同时移动光柱和数字，调 `height_view_pillar_offset`。
- 想只移动数字相对光柱锚点的位置，调 `height_label_offset`。
- 想改变平铺视图里的 hover 浮动，调 `height_view_hover_speed` 和 `height_view_hover_amplitude`。
- 当前 `stop_pillar_floating()` 保留旧行为：停止循环后 tween 到当前 y 值。如果以后要回到原始锚点，那应该作为单独的行为改动处理。

## 验证结果

本次落地后已执行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- 三条命令退出码都是 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错。
- headless 退出时仍会输出既有的资源释放提示。
- 增加显式 `Tween` 类型后，没有留下新的解析错误、脚本缺失或 preload 错误。

## 手动回归重点

- 切进平铺高度视图，确认每个有效地块仍显示预期的光柱或数字。
- 在平铺视图中 hover 地块，确认光柱浮动仍然播放。
- 移开 hover 后，确认不会累积多余的光柱 tween。
- 在平铺视图中使用升降高度效果，确认数字会更新并弹跳。
- 切回 3D 视图后，确认所有高度指示器都被移除。
