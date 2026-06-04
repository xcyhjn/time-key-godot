# 第 34 批落地：地貌投放服务拆分

日期：2026-06-05

## 本次目标

本次继续拆地图生成区域，把 `HexMap` 中的地貌投放职责拆到独立服务。

上一批已经把纯地图数据生成迁移到 `MapGenerationService.gd`。本批处理剩下的：

- 清理并重建 `GlobalClock.tile_h_pool`。
- 统计可放置地貌的有效地块数。
- 根据 `max_landform_ratio`、中立地貌上限和敌方地貌上限计算配额。
- 先投放中立地貌，再投放敌方地貌。
- 每个地貌池内部先保证每种脚本尝试一次，再随机补足剩余配额。
- 使用探针读取 `landform_rules` 并调用 `get_possible_coords()` 选择坐标。
- 创建地貌实例，写入 `map_data`，并加入 `Enemies` 或 `Middle` 分组。

## 修改文件

### `scene/in_scene/hex_map_modules/generation/LandformPlacementService.gd`

新增地貌投放服务，职责是：

- `assign(map_data, config)`：按旧顺序投放中立和敌方地貌。
- `place_from_pool(pool, max_count, map_data, config)`：从单个地貌池中投放指定数量。
- `pick_landform_coord(landform_script, map_data, config)`：使用探针选择合法坐标。
- `_rebuild_height_pool(map_data, config)`：重建 `tile_h_pool` 并统计有效地块。
- `_try_place_landform(landform_script, map_data, config)`：创建实例并登记到 `map_data`。
- `_build_candidate_pool(buffer, map_data, config)`：根据地貌高度规则建立候选池。

这个模块不处理：

- 地图坐标采样。
- 地块节点创建。
- 地貌 sprite 挂接。
- 血条创建。
- 奖励扫描。
- 敌人意图。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `LANDFORM_PLACEMENT_SERVICE` preload。
- 新增 `_landform_placement_service` 实例。
- `_assign_terrains_and_enemies()` 改为委托 service。
- 新增 `_build_landform_placement_service_config()`，把地貌池、配额、`GlobalClock.tile_h_pool` 和 owner 传给 service。
- `_place_from_pool()` 和 `landform_pick()` 保留为旧调试入口，但内部委托 service。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

仍由 HexMap 导出并传给 service 的投放配置包括：

- `max_landform_ratio`
- `Max_Neutral_landform`
- `Max_Enemy_landform`

仍由 HexMap 初始化并传给 service 的脚本池包括：

- `neutral_pool`
- `enemy_pool`

运行时上下文包括：

- `map_data`
- `rng`
- `GlobalClock.tile_h_pool`
- `owner_battle`
- `get_terrain_from_height()` 回调

## 行为边界

本次保持以下行为不变：

- 每次投放前仍清空旧 `GlobalClock.tile_h_pool`。
- `nexus_core` 仍不参与地貌有效地块计数。
- 没有 `terrain_type` 的地块仍会按高度补齐。
- 地貌投放仍先中立、再敌方。
- 每个地貌池仍先尝试每种脚本一个，再随机补足剩余配额。
- 有 `require_height` 的地貌仍从高度池里选候选坐标。
- 没有高度限制的地貌仍从整张 `map_data.keys()` 中选择。
- 地貌实例仍写入 `map_data[coord].landform` 和 `map_data[coord].landform_type`。
- 敌方地貌仍加入 `Enemies` 分组，中立地貌仍加入 `Middle` 分组。

## 当前完成度回顾

已完成：

- 地貌投放主体离开 `HexMap`。
- `HexMap` 地图生成区域现在只保留薄入口和配置构造。
- 地图生成与地貌投放已经分为两个 generation 服务。

仍待处理：

- `neutral_pool` 和 `enemy_pool` 仍在 HexMap 的 `_ready()` 里初始化。
- 地貌脚本仍依赖 `landform_rules`、`get_possible_coords()`、`Attitude` 等鸭子类型接口。
- 如果后续要继续清理兜底，建议先统一地貌基类契约，再改投放服务。

## 后续建议

下一步可以考虑把地貌池初始化也变成一个配置资源或专用 `LandformPoolProvider`。不过这会影响关卡配置和策划调参方式，建议等当前生成规则稳定后再做。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 默认局内地图是否仍生成中立和敌方地貌。
- 血条是否仍能为敌方地貌生成。
- 敌人意图、奖励扫描和目标选择是否仍能识别敌方地貌。
- 多次进入局内时 `GlobalClock.tile_h_pool` 是否没有残留异常。

验证结果：

- `git diff --check` 通过。
- 第一次验证暴露了 GDScript 严格类型问题：`Dictionary` 取值推断为 Variant，被项目按 error 处理；已补充显式类型。
- 修正后项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图日志显示 `map_tiles=49`，敌方血条唤起日志、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
