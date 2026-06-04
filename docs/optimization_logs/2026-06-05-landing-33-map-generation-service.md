# 第 33 批落地：地图数据生成服务拆分

日期：2026-06-05

## 本次目标

本次开始拆 `HexMap` 中的地图生成与地貌投放区域。第一批只处理“地图规则数据生成”，不处理地貌投放。

旧代码里，`HexMap` 直接负责：

- 根据 `map_generation_mode` 选择扇形或圆形地图。
- 初始化随机数 seed。
- 采样六边形坐标。
- 写入 `map_data`。
- 处理扇形地图中心的 `nexus_core`。
- 根据高度决定地形类型。

这些逻辑不需要知道场景节点，因此本批新增 `MapGenerationService.gd` 承接。

## 修改文件

### `scene/in_scene/hex_map_modules/generation/MapGenerationService.gd`

新增地图数据生成服务，职责是：

- `generate(config)`：根据模式生成新的 `map_data`。
- `generate_fan(config)`：生成扇形战斗地图数据。
- `generate_circular(config)`：生成圆形随机地图数据。
- `_populate_map_data(coords, shape_type, config)`：把坐标数组转换成地块数据。
- `_prepare_rng(rng, config)`：保持旧 seed 行为。
- `_add_nexus_core_tile(next_map_data, config)`：写入扇形地图中心核心地块。
- `_build_tile_data(coord, shape_type, rng, config)`：生成单格规则数据。

这个模块不处理：

- 地貌实例化。
- 中立/敌方地貌配额。
- `GlobalClock.tile_h_pool`。
- 节点创建、sprite、碰撞体和高度视图。
- 血条、奖励、敌人意图或 UI。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `MAP_GENERATION_SERVICE` preload。
- 新增 `_map_generation_service` 实例。
- `_generate_map_data()` 改为调用 service 并接收返回的 `map_data`。
- `_generate_fan_map_data()` 和 `_generate_circular_map_data()` 保留旧入口名，但内部委托 service，方便旧调试调用不失效。
- 新增 `_build_map_generation_service_config()`，把 Inspector 导出变量打包给 service。
- `get_terrain_from_height()` 等公共工具入口暂时保留在 `HexMap`，避免影响其他调用方。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

仍由 HexMap 导出并传给 service 的生成配置包括：

- `map_generation_mode`
- `use_external_seed`
- `nexus_height`
- `fan_radius`
- `inner_tier_radius`
- `fan_angle_span`
- `map_radius`
- `base_h_min`
- `base_h_max`
- `elite_h_bonus`
- `spacing_x`
- `spacing_y`
- `tile_scale`
- `terrain_by_height`

运行时上下文包括：

- `received_text`
- `incoming_map_seed`
- `room_type`
- `rng`

## 行为边界

本次保持以下行为不变：

- `map_generation_mode = 0` 仍生成扇形战斗地图。
- `map_generation_mode = 1` 仍生成圆形随机地图。
- 无效模式仍报错并回退到扇形地图。
- 外部 seed 仍优先使用 `incoming_map_seed`，没有时使用 `received_text.hash()`。
- 扇形地图中心仍是 `(0, 0)` 的 `nexus_core`。
- `map_data` 单格字段仍是 `height`、`terrain`、`terrain_type`、`landform`，扇形地图额外带 `tier`。

## 当前完成度回顾

已完成：

- 地图规则数据生成离开 `HexMap`。
- HexMap 只保留导出变量和薄入口。
- 地貌投放仍留在 HexMap，等待下一批单独拆。

仍待处理：

- `_assign_terrains_and_enemies()`、`_place_from_pool()` 和 `landform_pick()` 仍在 HexMap。
- `GlobalClock.tile_h_pool` 的清理和填充仍在 HexMap。
- 地貌实例仍在 HexMap 中创建并加入 `Enemies/Middle` 分组。

## 后续建议

下一批建议新增 `LandformPlacementService.gd`，把高度池、地貌配额、探针选点和地貌实例创建集中起来。拆这块时要严格保持旧顺序：先中立地貌，再敌方地貌；每种地貌至少尝试一个，再随机补足配额。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 默认局内战斗是否仍生成 49 格左右的扇形地图。
- 外部 seed 进入同一房间时地图是否可复现。
- 圆形地图调试模式是否仍能生成。
- 中心核心地块是否仍存在。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图日志显示 `map_tiles=49`，CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
