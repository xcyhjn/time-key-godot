# 第 8 次落地：提取结算奖励 tooltip 和高亮表现

日期：2026-06-04

## 本次处理范围

这次把结算奖励的 tooltip 和高亮表现从 `hex_map.gd` 中拆出来。奖励资格判断、奖励 payload 创建、点击信号发出和已领取状态修改仍然留在 `hex_map.gd`。

公开流程保持不变：胜利结算模式会扫描可奖励地块，hover 时显示强调，点击后发出 `settlement_reward_requested`，随后 Main 继续调用 `mark_settlement_reward_used()`。

## 新增模块

### `scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd`

这个模块负责：

- 给可奖励地块的 sprite 写入结算奖励 shader instance 参数。
- 在每个地块下创建或复用 `SettlementRewardTooltip` 面板。
- 根据奖励信息或地貌自定义文案刷新 tooltip 文本。
- 播放 tooltip hover 缩放动画。
- 在地图切换 3D 视角和平铺高度视图时重新定位 tooltip。
- 离开奖励模式时删除 tooltip 节点，并清理奖励 shader 参数。

主要函数：

- `refresh_stack(stack, reward_info, is_available, config)`：刷新单个地块的奖励 shader 状态和 tooltip 文本。
- `set_hover_state(stack, is_available, is_hovered, config)`：应用 hover 强度并播放 tooltip 缩放动画。
- `clear_stack(stack, config)`：清理高亮状态并删除 tooltip。
- `apply_shader_state(stack, is_available, is_hovered, config)`：只给使用 HexMap block shader 的 sprite 写入 shader 参数。
- `ensure_tooltip(stack, config)`：创建或复用地块子节点 tooltip。
- `refresh_tooltip(stack, reward_info, config)`：更新 tooltip 文本。
- `update_tooltip_position(stack, config)`：按地块高度和当前地图视图放置 tooltip。
- `animate_tooltip(stack, is_hovered, config)`：取消旧 tween，并启动新的缩放 tween。
- `remove_tooltip(stack)`：停止 tooltip tween，并把面板加入删除队列。
- `refresh_positions(stacks, config)`：高度视图切换后刷新所有已追踪 tooltip 的位置。
- `_get_stack_sprites(stack)`：安全读取地块 metadata 中注册的渲染 sprite。
- `_build_tooltip_text(reward_info)`：从地貌自定义文本或奖励标签生成显示文本。
- `_kill_tooltip_tween(stack)`：替换或删除面板前停止已有 tween。

## HexMap 的变化

- 新增 `SETTLEMENT_REWARD_PRESENTER` preload 和 `_settlement_reward_presenter`。
- 这些玩法和规则函数仍然留在 `hex_map.gd`：
  - `enter_settlement_reward_mode`
  - `exit_settlement_reward_mode`
  - `_collect_settlement_reward_stacks`
  - `_get_settlement_reward_landform`
  - `_is_valid_settlement_reward_landform`
  - `_matches_settlement_reward_bind_state`
  - `_build_settlement_reward_info`
  - `_is_settlement_reward_stack_available`
  - `_is_settlement_reward_used`
  - `_handle_settlement_reward_hover`
  - `_handle_settlement_reward_click`
  - `mark_settlement_reward_used`
- 新增 `_build_settlement_reward_presenter_config()`，集中传递当前导出调参值。
- `_sync_stack_to_current_view()` 中直接刷新 tooltip 位置的逻辑改为调用 `SettlementRewardPresenter.update_tooltip_position()`。
- 高度视图切换后的全量 tooltip 位置刷新改为调用 `SettlementRewardPresenter.refresh_positions()`。
- 从 `hex_map.gd` 中移除了旧的表现辅助函数：
  - `_apply_settlement_reward_shader`
  - `_ensure_settlement_reward_tooltip`
  - `_refresh_settlement_reward_tooltip`
  - `_update_settlement_reward_tooltip_position`
  - `_animate_settlement_reward_tooltip`
  - `_remove_settlement_reward_tooltip`

## 可调变量

这些变量仍然在 `hex_map.gd` 中导出，并通过 `_build_settlement_reward_presenter_config()` 传给 presenter：

- `settlement_reward_tooltip_offset`：控制 tooltip 相对地块顶部的偏移。
- `settlement_reward_tooltip_size`：控制 tooltip 面板固定尺寸和 pivot。
- `settlement_reward_tooltip_font_size`：控制 tooltip 文本字号。
- `settlement_reward_tooltip_z_index`：控制 tooltip 在地图和建筑上方的绘制顺序。
- `settlement_reward_highlight_color`：控制可领取奖励的常驻高亮颜色。
- `settlement_reward_hover_color`：控制 hover 覆盖颜色。
- `settlement_reward_highlight_blend`：控制常驻奖励高亮强度。
- `settlement_reward_hover_blend`：控制 hover 时的 shader 运动和强调强度。
- `settlement_reward_tooltip_hover_scale`：控制 tooltip hover 缩放比例。

presenter 还会读取这些运行时值：

- `step_height`
- `tile_scale`
- `REF_SCALE`
- `current_view_state`
- `block_material.shader`

这些运行时值不是给策划单独调的奖励参数，它们只是确保 tooltip 位置和 shader 过滤与当前 HexMap 状态一致。

## 调整说明

- 想整体移动奖励 tooltip，调 `settlement_reward_tooltip_offset`。
- 想避免文字或布局跳动，除非文案明显变化，否则不要频繁改 `settlement_reward_tooltip_size`。
- 想让奖励建筑更克制，降低 `settlement_reward_highlight_blend`。
- 想让 hover 反馈更轻，降低 `settlement_reward_hover_blend` 或 `settlement_reward_tooltip_hover_scale`。
- tooltip 节点名仍然是 `SettlementRewardTooltip`，方便沿用现有调试搜索。
- 奖励资格判断不属于 presenter。不要把地貌状态检查或奖励 payload 修改写进 `SettlementRewardPresenter`。

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
- 没有引入新的解析错误、脚本缺失或 preload 错误。

## 手动回归重点

- 进入胜利结算奖励模式，确认符合条件的敌方建筑显示 tooltip 和高亮。
- hover 可领取奖励地块，确认高亮和 tooltip 缩放变化正常。
- 点击可领取奖励地块，确认 `settlement_reward_requested` 仍然能打开奖励流程。
- 确认奖励后，tooltip 仍可显示，但该地块不再可交互或高亮。
- reward tooltip 可见时切换平铺高度视图，tooltip 应重新定位且不漂移。
