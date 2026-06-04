# 第 32 批落地：单格重建边界服务拆分

日期：2026-06-05

## 本次目标

本次处理 `refresh_tile_visual()` 的局部重绘边界。

旧实现虽然名字叫局部刷新，但逻辑仍写在 `HexMap` 主控里：

- 读取 `map_data[coord]`。
- 如果 `stack_nodes` 里已有旧节点，就 `queue_free()`。
- 从 `stack_nodes` 删除旧坐标。
- 调用 `_create_stack_at(coord, data)` 重新创建地块。
- 调用 `_refresh_stack_interactivity()` 恢复输入状态。

这批不直接做真正的对象池或 metadata 复用，因为 stack 上关联了 `sprites`、`height`、`occupant`、`collision_node`、高度视图缓存、奖励 tooltip 和血条注册。直接复用节点风险较高。当前先新增 `TileStackRebuildService.gd`，把“单格重建”作为明确边界集中起来。

## 修改文件

### `scene/in_scene/hex_map_modules/factory/TileStackRebuildService.gd`

新增单格重建服务，职责是：

- `rebuild(coord, map_data, stack_nodes, config)`：按坐标重建一个 stack。
- `_remove_old_stack(coord, stack_nodes)`：释放旧 stack，并同步 `stack_nodes`。
- 通过回调调用 HexMap 的 `_create_stack_at()`。
- 通过回调调用 HexMap 的 `_refresh_stack_interactivity()`。

这个模块不处理：

- 地块真实销毁规则。
- 地块升降动画。
- 高度视图压缩和恢复。
- 血条创建或重绑。
- 奖励 tooltip 创建或刷新。
- 材质复用、对象池或 draw call 合批。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TILE_STACK_REBUILD_SERVICE` preload。
- 新增 `_tile_stack_rebuild_service` 实例。
- `refresh_tile_visual(coord)` 改为委托服务。
- 新增 `_build_tile_stack_rebuild_service_config()`，把 `_create_stack_at()` 和 `_refresh_stack_interactivity()` 作为回调传入服务。
- `refresh_landform_visual(coord)` 从全图 `build_map_pipeline()` 改成单格 `refresh_tile_visual(coord)`。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

本次只是集中单格重建边界，不改变以下导出变量的读取位置：

- 地块纹理、材质和尺寸参数仍由 `_build_tile_stack_factory_config()` 消费。
- 地貌偏移、hitbox 偏移和高度视图状态仍由原有 factory / service 消费。
- 地块交互参数仍由 `_refresh_stack_interactivity()` 和输入模块消费。

## 行为边界

本次保持以下行为不变：

- 单格刷新仍会删除旧 stack，再完整走 `_create_stack_at()`。
- `_create_stack_at()` 仍负责写入 `stack_nodes[coord]`。
- `TileStackFactory.gd`、`TileLandformAttachService.gd`、`TileStackInitializationService.gd` 的创建顺序不变。
- 重建后仍刷新地块交互状态。

本次有一个刻意收窄：

- `refresh_landform_visual(coord)` 不再触发全图 `build_map_pipeline()`，而是走单格刷新。这个函数当前没有外部调用；如果后续恢复使用，它的名字也更符合单格地貌刷新语义。

## 当前完成度回顾

已完成：

- 单格重建边界离开 `HexMap` 主控。
- `HexMap` 只保留刷新入口和回调配置。
- 全图刷新式的 `refresh_landform_visual()` 被收敛到单格刷新入口。

仍待处理：

- 旧 stack 被删除时，原 stack 上的高度视图缓存、奖励 tooltip、外部血条绑定和 shader 状态不会被主动迁移。
- 真正性能优化需要继续设计“哪些 metadata 保留，哪些节点重建”的白名单。
- 如果后续要做对象池，应该先从 tooltip、overlay、label 这类纯表现节点开始，不要直接池化承载规则数据的 stack。

## 后续建议

下一步如果继续优化本区域，建议按这个顺序推进：

1. 先记录单格重建前后的 `Object Count` 和局内重绘尖峰。
2. 为 `TileStackRebuildService.gd` 增加 metadata 迁移计划，但先只迁移只读表现缓存。
3. 单独验证高度视图、奖励 tooltip、血条跟随和敌人意图 hover。
4. 再考虑对象池或材质复用，不要和规则拆分混在同一批。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 运行时新增地貌后，单格视觉是否正常刷新。
- 高度视图下刷新单格后，数字标签是否重新出现。
- 奖励模式下刷新单格后，奖励 tooltip 是否需要重新收集。
- 单体血条是否仍能通过 BarManager 和外部渲染注册流程重新关联。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
