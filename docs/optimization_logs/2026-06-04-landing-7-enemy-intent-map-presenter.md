# 第 7 次落地：提取敌人意图地图表现

日期：2026-06-04

## 本次处理范围

这次把地图侧的敌人意图表现从 `hex_map.gd` 中拆出来。`HexMap.show_enemy_intent_preview()` 和 `HexMap.clear_enemy_intent_preview()` 这两个公开入口保持不变。

敌人意图的规则结算、时间轴表现和 tooltip 协调仍然留在原来的敌人意图系统中。本模块只负责地图上的源地块高亮和目标地块波纹。

## 新增模块

### `scene/in_scene/hex_map_modules/presenters/EnemyIntentMapPresenter.gd`

这个模块负责：

- 通过地块 sprite 的 shader instance 参数高亮意图来源地块。
- 缓存来源地块原本的 shader 状态，并在清除预览时恢复。
- 在目标地块上创建或复用波纹覆盖层。
- 清除预览时隐藏所有目标覆盖层。
- 从 HexMap 的调参配置里读取目标波纹的 shader 参数。

主要函数：

- `show(intent_data, stack_nodes, source_color, target_color, config)`：根据已经解析好的 `EnemyIntentData` 在地图上显示一次意图。
- `clear()`：恢复来源高亮，并隐藏所有目标覆盖层。
- `_get_source_blend_config(intent_data, config)`：根据有效、自身包含或无效状态选择来源地块的混合强度。
- `_show_source_highlight(...)`：写入来源地块高亮，并记录恢复所需数据。
- `_show_target_overlay(...)`：显示或创建一个目标波纹覆盖层。
- `_hide_overlay(...)`：隐藏覆盖层，但不释放节点，减少 hover 时的重复分配。
- `_restore_source_highlight(...)`：恢复之前缓存的 shader 参数。
- `_ensure_overlay(...)`：创建或复用地块子节点 `Sprite2D`，并使用独立的 `ShaderMaterial`。
- `_update_overlay_transform(...)`：把覆盖层对齐到地块 hitbox，并按纹理尺寸计算缩放。
- `_configure_overlay_material(...)`：写入目标波纹的 shader 调参值。

## HexMap 的变化

- 新增 `_enemy_intent_map_presenter`。
- `show_enemy_intent_preview()` 缩减为委托调用。
- `clear_enemy_intent_preview()` 缩减为委托调用，并保留必要的卡牌 hover 刷新。
- 新增 `_build_enemy_intent_map_presenter_config()`，集中收集当前导出调参值。
- 从 `hex_map.gd` 中移除了地图表现相关的私有函数：
  - `_show_source_intent_highlight`
  - `_show_target_intent_overlay`
  - `_hide_intent_overlay`
  - `_restore_source_intent_highlight`
  - `_ensure_intent_overlay`
  - `_update_intent_overlay_transform`
  - `_configure_intent_overlay_material`

## 可调变量

这些变量仍然在 `hex_map.gd` 中导出，并通过 `_build_enemy_intent_map_presenter_config()` 传给 presenter：

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

覆盖层定位还会使用这些尺寸参数：

- `hitbox_width`
- `hitbox_base_height`
- `hitbox_offset_x`
- `hitbox_offset_y`

## 调整说明

- 想调整目标波纹的动效和透明度，改 `enemy_intent_target_*` 相关导出变量。
- 想调整来源地块的高亮强度，改 `enemy_intent_source_*_blend` 相关导出变量。
- `enemy_intent_source_highlight_width` 仍然保留给旧的来源覆盖层实验，但当前 presenter 不使用它，因为来源高亮直接写现有地块 sprite 的混合参数。
- 目标覆盖层会复用，不会在每次 hover 时反复创建和释放。

## 验证结果

- `git diff --check` 在写入本日志前已通过。
- 本日志写入后还需要跑：
  - Godot 项目 headless 加载。
  - Godot 局内战斗场景 headless 加载。
  - 编辑器内手动 hover 回归。

## 手动回归重点

- hover 一个有效意图的敌人时，来源地块应高亮，目标地块应显示波纹。
- hover 一个会影响自身的敌人意图时，来源高亮应使用 `EnemyIntentPresentationController` 提供的自身包含强度和颜色。
- hover 一个无效意图时，来源和目标表现应使用无效颜色和混合强度。
- 移开 hover 后，来源地块 shader 参数应恢复，目标覆盖层应隐藏。
