# 第 16 次落地：提取地图初始入场流程

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中地图初次生成后的涟漪入场流程拆到独立模块。拆分对象包括是否播放入场的判断、入场状态保存、延迟血条队列、按距离分层播放 `dissolve_blend` Tween、入场期间隐藏辅助 UI、入场结束后恢复交互和发出完成信号。

`hex_map.gd` 仍然是战斗地图的 composition root。它继续负责导出变量、场景节点路径、Tween/Timer 创建、BarManager 调用、总血量条查找、地块交互刷新，以及 `enemy_roster_changed` 和 `map_intro_reveal_finished` 信号。

## 新增模块

### `scene/in_scene/hex_map_modules/runners/MapIntroRevealRunner.gd`

这个模块负责：

- 判断当前地图构建是否应该播放初始入场。
- 保存入场状态，包括 `is_active`、`has_played`、`pending_requests` 和 `pending_request_ids`。
- 记录 BarManager 在入场期间发来的单体血条创建请求。
- 用实体实例 id 给延迟血条请求去重，runner 不直接依赖 `landform` 类名。
- 收集 `stack_nodes` 中有效的 `Area2D` 地块。
- 计算从地图左下角外推的涟漪源点。
- 按距离把每个地块分到不同延迟层。
- 通过 HexMap 注入的 Tween 创建函数播放 `dissolve_blend` 从 `1.0` 到 `0.0` 的恢复动画。
- 通过 HexMap 注入的 Timer 创建函数等待整张地图入场结束。
- 入场结束后补发延迟血条请求、恢复辅助 UI、刷新地块交互，并通知 HexMap 发出完成信号。

主要函数：

- `begin_if_needed(config)`：根据导出配置和当前视图状态决定是否开启入场。
- `is_active(state)`：给 HexMap 的旧查询入口使用。
- `should_defer_health_bars(state)`：给 BarManager 的延迟判断使用。
- `queue_health_bar_request(state, landform_in, situation, x, y)`：缓存入场期间的血条创建请求，参数只要求是有效对象。
- `play(config)`：播放整张地图的涟漪入场。
- `finish(config)`：执行入场结束收尾。
- `flush_health_bar_requests(config)`：补发缓存的单体血条创建请求。
- `set_auxiliary_visuals_visible(is_visible, config)`：隐藏或恢复 BarManager 与总血量条。
- `_clear_pending_health_bar_requests(state)`：清空延迟请求队列和去重表。
- `_get_reveal_stacks(config)`：收集有效地块栈。
- `_get_reveal_origin(stacks, config)`：计算左下角外推源点。
- `_set_stack_dissolve_from_tween(value, stack, config)`：Tween 写入 dissolve 的回调入口。
- `_set_stack_dissolve(stack, value, config)`：通过 HexMap 回调写地块渲染节点的 `dissolve_blend`。
- `_create_tween(config)`：通过 HexMap 回调创建 Tween。
- `_call_config_callback(config, key)`：调用 HexMap 注入的收尾回调。

## HexMap 的变化

- 新增 `MAP_INTRO_REVEAL_RUNNER` preload。
- 新增 `_map_intro_reveal_runner` 实例。
- 用 `_map_intro_reveal_state` 字典替代原来的 `_map_intro_reveal_in_progress`、`_has_played_map_intro_reveal`、`_pending_intro_health_bar_requests` 和 `_pending_intro_health_bar_request_ids`。
- 新增 `_build_map_intro_reveal_config()`，集中把导出变量、运行时状态和回调传给 runner。
- 新增 `_create_map_intro_reveal_tween()`，让 runner 创建的 Tween 仍然挂在 HexMap 节点上。
- 新增 `_create_map_intro_reveal_timer(duration)`，让 runner 使用 HexMap 所在 SceneTree 的 timer。
- 新增 `_get_intro_bar_manager()`，把 BarManager 路径留在 HexMap 内。
- 新增 `_create_intro_health_bar(landform_in, situation, x, y)`，由 HexMap 调用 BarManager 的 `Create_Blood_Bar()`。
- 新增 `_emit_intro_enemy_roster_changed()` 和 `_emit_map_intro_reveal_finished()`，由 HexMap 负责发信号。
- `is_map_intro_reveal_active()`、`should_defer_intro_health_bars()` 和 `queue_intro_health_bar_request()` 保留旧函数名，内部改为委托 runner。
- `_refresh_stack_interactivity()` 和 `_create_stack_at()` 改为通过 `is_map_intro_reveal_active()` 读取入场状态。

## 可调变量

本次没有新增导出变量。所有调参仍然在 `hex_map.gd` 的“地块入场动画”导出组里完成：

- `map_intro_reveal_enabled` 控制是否播放初始入场。
- `map_intro_reveal_tile_duration` 控制单个地块恢复动画时长。
- `map_intro_reveal_wave_delay` 控制相邻涟漪层之间的延迟。
- `map_intro_reveal_wave_pixel_step` 控制按像素距离分层的宽度。
- `map_intro_reveal_finish_delay` 控制最后一层结束后额外等待多久再恢复 UI。
- `map_intro_reveal_origin_padding` 控制涟漪源点相对地图左下角外推多少。
- `map_intro_reveal_hide_health_ui` 控制入场期间是否隐藏 BarManager 和总血量条。
- `map_intro_reveal_lock_interaction` 控制入场期间是否锁住地块输入。
- `map_intro_reveal_trans_type` 和 `map_intro_reveal_ease_type` 控制 Tween 曲线。

## 调整说明

- 不要在 `MapIntroRevealRunner.gd` 中生成地图、创建地块、修改 `map_data` 或判断地形规则。
- 不要在 runner 中直接查找 `BarManager` 或 `TotalEnemyHealthBar`，这些节点路径必须留在 `hex_map.gd`。
- 不要在 runner 中直接发 Godot signal，信号由 HexMap 回调发出。
- 不要在 runner 中新增 `landform`、敌人脚本或奖励脚本的类型注解，避免模块加载顺序反向影响地图编译。
- 如果要改入场动画的视觉公式，优先改 `play(config)` 中的分层和 Tween 逻辑。
- 如果要改血条延迟策略，优先改 `queue_health_bar_request()` 和 `flush_health_bar_requests()`。
- 如果要改入场期间是否能 hover 或点击地块，应检查 `map_intro_reveal_lock_interaction` 和 `hex_map.gd::_refresh_stack_interactivity()`。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
```

结果：

- `git diff --check` 退出码为 0。
- Godot headless 退出码为 0。
- headless 当前仍会输出项目既有的 `GlobalClock`、`Signal_Bus`、`Dialogic` 和 `TimelineAction` 解析噪声。
- 本次没有新增 `MapIntroRevealRunner.gd`、`hex_map.gd` 或 preload 路径相关的解析错误。

## 手动回归重点

- 进入一场普通战斗，确认地图仍然从左下方向按涟漪层出现。
- 入场期间确认地块不可被 hover 或点击，除非关闭 `map_intro_reveal_lock_interaction`。
- 入场期间确认 BarManager 和总血量条会按 `map_intro_reveal_hide_health_ui` 隐藏。
- 入场结束后确认单体血条和总血量条恢复显示。
- 入场结束后确认教程、开局回合启动和敌人意图刷新仍能收到 `map_intro_reveal_finished`。
- 第二次刷新单个地块或运行时建造地貌时，确认不会重新播放整张地图入场。
