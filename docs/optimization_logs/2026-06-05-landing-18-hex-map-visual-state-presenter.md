# 第 18 次落地：提取普通地图视觉状态表现

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中普通地图 hover、AOE、高亮状态和动态遮挡的 shader 写入逻辑拆到独立 presenter。拆分对象包括 AOE 高亮清理、AOE 范围视觉状态应用、遮挡 dissolve 清理、3D 视图遮挡柱半透明计算、通用 shader 参数 Tween，以及 `TileVisualState` 状态机对应的高亮颜色和 blend 写入。

`hex_map.gd` 仍然负责 CardManager 查询、MainBoard tooltip 更新、卡牌目标合法性判断、AOE 范围收集入口、平铺视图高度光柱 hover、敌人意图 hover 转发，以及旧公共函数名。本次不改变卡牌目标规则、不改变敌人意图表现、不改变结算奖励表现，也不新增美术调参导出项。

## 新增模块

### `scene/in_scene/hex_map_modules/presenters/HexMapVisualStatePresenter.gd`

这个模块负责：

- 清理当前 AOE 范围、选中地块和活动地块的高亮状态。
- 把新的 AOE 范围统一写入有效或无效 hover 状态。
- 清理所有地块的遮挡 `dissolve_blend`。
- 在 3D 视图中判断目标地块前方哪些高柱需要半透明。
- 把地块 `TileVisualState` 数字转换为 shader 目标值。
- 给 `highlight_blend` 和 `is_selected_blend` 创建 Tween。
- 提供通用的 shader 参数 Tween，供遮挡和取消选中路径复用。
- 在没有 Tween 回调时立即写入目标值，避免测试或卸载场景中留下旧状态。

主要函数：

- `clear_aoe_highlights(current_aoe_stacks, selected_stack, active_stack, config)`：清理 AOE、选中和活动地块，并返回更新后的状态包。
- `apply_aoe_state(current_aoe_stacks, new_aoe_stacks, target_state, config)`：卸载旧范围并应用新范围视觉状态。
- `clear_occlusion_effects(stack_nodes, currently_occluding_stacks, config)`：清理动态遮挡 dissolve。
- `update_occlusion(target_stack, stack_nodes, currently_occluding_stacks, config)`：计算并应用 3D 遮挡半透明。
- `change_tile_state(stack, new_state, config)`：按状态机写入 shader 高亮。
- `tween_shader_param(stack, param_name, target_value, duration, config)`：通用 shader 参数 Tween。
- `get_stack_sprites(stack)`：读取地块渲染节点队列。
- `get_stack_height(stack)`：读取地块高度。
- `_get_tile_state_visual_config(new_state)`：把 TileVisualState 数字转换成颜色和 blend。
- `_apply_tile_state_immediately(stack, visual_config)`：没有 Tween 时立即应用状态。
- `_set_shader_param_immediately(sprites, param_name, target_value)`：没有 Tween 时立即写参数。
- `_create_tween(config)`：通过 HexMap 注入回调创建 Tween。

## HexMap 的变化

- 新增 `HEX_MAP_VISUAL_STATE_PRESENTER` preload。
- 新增 `_hex_map_visual_state_presenter` 实例。
- `_clear_all_aoe_highlights()` 保留旧函数名，内部委托 presenter，并回写 `current_aoe_stacks`、`selected_stack` 和 `active_stack`。
- `_update_aoe_display()` 仍负责取卡牌范围、判断中心地块是否合法、更新 MainBoard tooltip；视觉状态应用改为委托 presenter。
- `_clear_occlusion_effects()` 保留旧函数名，内部委托 presenter，并回写 `currently_occluding_stacks`。
- `_update_occlusion(target_stack)` 保留旧函数名，内部委托 presenter。
- `_tween_shader_param()` 保留旧函数名，内部委托 presenter。
- `change_tile_state()` 保留旧函数名，内部委托 presenter。
- 新增 `_build_visual_state_presenter_config()`，集中把 Tween 创建、当前视图、hitbox、遮挡参数和地块高度公式传给 presenter。

## 可调变量

本次没有新增导出变量。当前视觉参数仍然是代码常量：

- `tile_state_tween_duration = 0.15`
- `occlusion_clear_duration = 0.12`
- `occlusion_fade_duration = 0.25`
- `occlusion_dissolve_blend = 0.8`
- `occlusion_x_factor = 0.8`

已有导出变量仍然在 `hex_map.gd` 中生效：

- `hitbox_width` 影响遮挡横向判断。
- `step_height`、`tile_scale` 和 `REF_SCALE` 影响遮挡高度判断。

如果后续需要给策划调这些视觉时长和强度，建议先在 `hex_map.gd` 新增一个“普通地图视觉状态”导出组，再从 `_build_visual_state_presenter_config()` 传给 presenter。

## 调整说明

- 不要在 `HexMapVisualStatePresenter.gd` 中读取 CardManager。
- 不要在 presenter 中判断卡牌目标是否合法，合法性仍由 `HexTargetRules` 和 HexMap 包装入口提供。
- 不要在 presenter 中更新 MainBoard tooltip，tooltip 更新仍留在 `_update_aoe_display()`。
- 不要在 presenter 中处理敌人意图 overlay，那是 `EnemyIntentMapPresenter.gd` 的职责。
- 不要在 presenter 中处理结算奖励 tooltip 或奖励高亮，那是 `SettlementRewardPresenter.gd` 的职责。
- 如果修改 `TileVisualState` enum 顺序，必须同步检查 presenter 顶部的 `TILE_STATE_*` 常量。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- `git diff --check` 退出码为 0。
- Godot headless 项目加载和局内场景加载退出码为 0。
- headless 当前仍会输出项目既有的 `GlobalClock`、`Signal_Bus`、`Dialogic`、`TimelineAction` 和 TileSet atlas 噪声。
- 本次没有新增 `HexMapVisualStatePresenter.gd` 或普通视觉状态相关解析错误。

## 手动回归重点

- 未选中卡牌时 hover 地块，确认不会出现 AOE 高亮残留。
- 选中需要地块目标的卡牌后 hover 地块，确认范围内地块全部显示有效或无效高亮。
- 选中卡牌后移动 hover 目标，确认旧 AOE 范围能恢复 IDLE。
- 右键取消卡牌选择，确认选中描边和 AOE 高亮都清空。
- 3D 视图中 hover 被前景高柱遮挡的目标，确认前景柱会半透明。
- 平铺视图中 hover 地块，确认动态遮挡不会触发，高度光柱 hover 仍正常。
- 敌人意图预览和结算奖励 tooltip 分别回归一次，确认没有被普通视觉 presenter 接管后破坏。
