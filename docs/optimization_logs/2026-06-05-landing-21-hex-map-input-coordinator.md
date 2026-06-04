# 第 21 次落地：提取地块输入协调器

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中地块点击和悬浮的输入流程拆到独立协调器。拆分对象包括左键点击分发、右键取消卡牌选择、普通地块点击、结算奖励点击、结算奖励 hover、平铺高度视图光柱 hover、`hovered_stacks` 列表维护、AOE 刷新触发，以及敌人意图 hover 转发。

本次不改变卡牌目标合法性规则，不改变 AOE 高亮表现，不改变奖励资格判断，也不改变敌人意图预览。`hex_map.gd` 仍保留 `_on_stack_input()`、`_handle_tile_click()` 和 `_on_stack_hover()` 旧入口名，因为这些函数已经被地块节点信号连接或可能被调试脚本调用。

## 新增模块

### `scene/in_scene/hex_map_modules/input/HexMapInputCoordinator.gd`

这个模块负责：

- 处理 `Area2D.input_event` 传入的鼠标事件。
- 左键根据当前模式转发到结算奖励点击或普通地块点击。
- 右键调用 HexMap 注入的取消卡牌选择入口。
- 有选中卡牌时，先调用 HexMap 的目标合法性检查，再调用卡牌 `play_card(stack)`。
- 没有选中卡牌时，切换地块选中高亮。
- 结算奖励模式下，hover 和 click 都转发到奖励系统入口。
- 平铺高度视图下，进入和离开地块时触发高度光柱浮动动画。
- 维护 `hovered_stacks` 列表，并触发 HexMap 的 `_update_highlight()`。
- 把地块 hover 转发给敌人意图系统。

主要函数：

- `handle_stack_input(viewport, event, shape_idx, stack, config)`：处理地块鼠标点击。
- `handle_tile_click(stack, config)`：处理普通地块左键点击。
- `handle_stack_hover(stack, is_entered, config)`：处理鼠标进入或离开地块。
- `_try_play_selected_card(stack, active_card, config)`：有选中卡牌时尝试进入出牌流程。
- `_select_idle_stack(stack, config)`：无选中卡牌时切换选中地块。
- `_handle_flat_view_hover(stack, is_entered, config)`：平铺高度视图下处理光柱 hover。
- `_update_hovered_stack_list(stack, is_entered, config)`：维护 hover stack 列表。
- `_get_card_manager(config)`：通过 HexMap 注入回调读取 CardManager。
- `_is_stack_valid_target(stack, config)`：通过 HexMap 注入回调检查目标合法性。
- `_change_tile_state(stack, state, config)`：通过 HexMap 注入回调写入地块状态。

## HexMap 的变化

- 新增 `HEX_MAP_INPUT_COORDINATOR` preload。
- 新增 `_hex_map_input_coordinator` 实例。
- `_on_stack_input()` 保留旧入口名，内部委托 `HexMapInputCoordinator.handle_stack_input()`。
- `_handle_tile_click()` 保留旧入口名，内部委托 `HexMapInputCoordinator.handle_tile_click()`。
- `_on_stack_hover()` 保留旧入口名，内部委托 `HexMapInputCoordinator.handle_stack_hover()`。
- 新增 `_build_hex_map_input_coordinator_config()`，集中传入当前模式、状态引用和回调。
- 新增 `_apply_input_coordinator_result()`，负责把 `selected_stack`、`hovered_stacks` 和 `height_view_hovered_stack` 回写到 HexMap。
- 新增 `_change_tile_state_from_input_coordinator()`，把输入协调器传来的状态数字转发给 `change_tile_state()`。
- 新增 `_handle_enemy_intent_stack_hover()`，暂时保留敌人意图管理器路径查找。

## 可调变量

本次没有新增导出变量。

已有输入相关状态仍然由 `hex_map.gd` 管理：

- `current_settlement_reward_mode`：决定点击和 hover 是否由结算奖励模式接管。
- `is_visuals_locked`：拖拽或结算阶段锁定普通 hover 视觉。
- `current_view_state`：决定是否需要播放平铺高度视图光柱 hover。
- `hovered_stacks`：当前鼠标覆盖的地块列表。
- `selected_stack`：无卡牌选中时玩家点击选中的地块。
- `height_view_hovered_stack`：平铺高度视图中当前 hover 的地块。

## 调整说明

- 不要在 `HexMapInputCoordinator.gd` 中直接读取 `stack_nodes`、`map_data` 或 CardManager 路径。
- 不要在协调器里计算 AOE 范围；AOE 仍由 `hex_map.gd::_update_highlight()` 和 `HexTargetRules.gd` 处理。
- 不要在协调器里写 shader 参数；地块视觉状态仍由 `HexMapVisualStatePresenter.gd` 处理。
- 敌人意图管理器路径仍留在 `hex_map.gd::_handle_enemy_intent_stack_hover()`，这是后续拆 TimelineSystem 依赖时的入口。
- `active_card.has_method("play_card")` 仍保留。它看起来偏兜底，但当前 `CardManager.current_selected_card` 类型是 `Node`，需要先统一出牌接口再删除。

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
- 本次没有新增 `HexMapInputCoordinator.gd` 或输入入口相关解析错误。

## 手动回归重点

- 普通状态下左键点击地块，确认选中高亮仍然切换。
- 选中卡牌后 hover 地块，确认 AOE 高亮和 tooltip 仍然刷新。
- 选中卡牌后点击合法目标，确认仍进入 DragShapeController 或时间轴放置流程。
- 选中卡牌后点击非法目标，确认不会进入出牌流程。
- 右键点击地图，确认卡牌取消选中并清理高亮。
- 平铺高度视图下 hover 地块，确认光柱浮动仍然正常。
- 结算奖励模式下 hover 和点击奖励建筑，确认 tooltip、hover 和奖励请求仍然正常。
- 鼠标 hover 敌人意图来源或目标时，确认敌人意图 tooltip 仍然响应。
