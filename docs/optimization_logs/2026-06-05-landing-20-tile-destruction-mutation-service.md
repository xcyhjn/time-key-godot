# 第 20 次落地：提取地块真实销毁服务

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中高度超限后的真实删除流程拆到独立 mutation 服务。拆分对象包括销毁 VFX 等待、地块节点释放、occupant 释放、`GlobalClock.tile_h_pool` 清理、`stack_nodes` 和 `map_data` 删除、旧建筑静态坐标库清理、地块交互刷新，以及销毁后的敌人列表和地图拓扑信号。

`TileDestructionBatchQueue.gd` 仍然负责销毁队列和批处理节奏；`TileElevationService.gd` 仍然负责高度变化和超限判断。本次只处理单个地块真正被删除时的 mutation，不改变销毁触发条件、不改变批处理数量、不改变 VFX 参数，也不改时间轴等待逻辑。

## 新增模块

### `scene/in_scene/hex_map_modules/destruction/TileDestructionMutationService.gd`

这个模块负责：

- 从地块 metadata 中读取 `sprites` 队列。
- 调用项目现有 `VFXManager.play_tile_destruction_vfx()` 播放抖动和溶解表现。
- 保存地块上的 `occupant` 引用。
- 对地块 `Area2D` 执行 `queue_free()`。
- 根据 `map_data[coord].height` 从 `GlobalClock.tile_h_pool` 中移除坐标。
- 从 `stack_nodes` 和 `map_data` 中删除坐标。
- 调用 HexMap 注入的交互刷新回调。
- 从 `iron_mine.Library` 和 `village.Library` 中清理坐标。
- 释放 occupant。
- 调用 HexMap 注入的 `enemy_roster_changed` 和 `tile_topology_changed` 信号回调。

主要函数：

- `perform(stack, coord, config)`：单个地块真实销毁入口。
- `_get_stack_sprites(stack)`：读取销毁 VFX 需要的渲染节点队列。
- `_play_destruction_vfx(sprites, config)`：等待 VFXManager 播放销毁表现。
- `_get_stack_occupant(stack)`：保存地块 occupant。
- `_queue_free_stack(stack)`：释放地块根节点。
- `_remove_height_pool_entry(coord, config)`：清理全局高度池。
- `_remove_map_entries(coord, config)`：清理 `stack_nodes` 和 `map_data`。
- `_refresh_stack_interactivity(config)`：通知 HexMap 刷新输入状态。
- `_remove_coord_from_landform_libraries(coord, config)`：清理旧建筑静态坐标库。
- `_queue_free_occupant(occupant)`：释放 occupant。
- `_emit_destruction_signals(config)`：通知 HexMap 发出销毁后的场景级信号。

## HexMap 的变化

- 新增 `TILE_DESTRUCTION_MUTATION_SERVICE` preload。
- 新增 `_tile_destruction_mutation_service` 实例。
- `_perform_tile_destruction(stack, coord)` 保留旧入口名，内部委托 `TileDestructionMutationService.perform()`。
- 删除 `_remove_destroyed_coord_from_landform_libraries()`，对应逻辑进入 mutation 服务。
- 新增 `_build_tile_destruction_mutation_service_config()`，集中传入 `VFXManager`、`SceneTree`、数据字典、VFX 调参、旧静态库持有者、交互刷新和信号回调。
- 新增 `_emit_enemy_roster_changed()`，给服务统一发出敌人列表变化信号。

## 可调变量

本次没有新增导出变量。以下已有导出项仍在 `hex_map.gd` 中调整：

- `tile_destruction_shake_count`：销毁前左右抖动次数。
- `tile_destruction_shake_step_duration`：单次抖动耗时。
- `tile_destruction_shake_distance`：抖动位移距离。
- `tile_destruction_dissolve_duration`：像素溶解消失耗时。
- `tile_destruction_batch_size`：销毁队列每批处理数量，仍由 `TileDestructionBatchQueue.gd` 使用。
- `tile_destruction_batch_collect_delay`：同一波销毁请求的收集等待，仍由队列使用。
- `tile_destruction_batch_interval`：批次间隔，仍由队列使用。

如果新增会缓存坐标的旧建筑脚本，需要在 `hex_map.gd::_build_tile_destruction_mutation_service_config()` 的 `landform_library_holders` 中加入对应类或对象。

## 调整说明

- 不要在 `TileDestructionMutationService.gd` 中判断高度是否越界；越界判断仍在 `TileElevationService.gd`。
- 不要在 mutation 服务中处理销毁队列节奏；队列收集、分批和等待仍在 `TileDestructionBatchQueue.gd`。
- 不要让 mutation 服务直接查找 BarManager、奖励 presenter 或敌人意图 presenter。需要额外清理时，通过 HexMap 注入清晰的回调。
- 如果修改 `VFXManager.play_tile_destruction_vfx()` 的参数顺序，必须同步检查本模块的 `_play_destruction_vfx()`。
- 如果未来对象池接管地块节点释放，优先替换 `_queue_free_stack()` 和 `_queue_free_occupant()`，不要改队列模块。

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
- 本次没有新增 `TileDestructionMutationService.gd` 或销毁入口相关解析错误。

## 手动回归重点

- 使用升高效果把地块推到超过 `max_height`，确认销毁 VFX 播放后地块被删除。
- 使用降低效果把地块降到 `min_height` 或以下，确认销毁 VFX 播放后地块被删除。
- 销毁带敌方建筑的地块，确认总敌人血量和敌人列表刷新。
- 销毁 `iron_mine` 或 `village` 相关地块，确认旧静态 `Library` 不再保留被删坐标。
- 销毁后继续 hover 或选择卡牌目标，确认被删除坐标不会再接收输入。
- 连续多地块超限时，确认 `TileDestructionBatchQueue.gd` 的分批节奏没有变化。
