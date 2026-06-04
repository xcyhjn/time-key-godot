# 第 31 批落地：HexMap 场景节点桥接拆分

日期：2026-06-05

## 本次目标

本次集中处理 `HexMap` 中散落的场景节点路径。

在当前局内场景里，地图需要和 UI、相机、时间轴和血条系统协作。旧代码中这些路径散落在 `hex_map.gd` 里，例如：

- `../../ui/HeightViewToggleButton`
- `../../ui/TimelineSystem/EnemyIntentManager`
- `../Camera2D`
- `../../ui/TotalEnemyHealthBar`
- `BarManager`

这些路径本身仍然合理，但散落在主控脚本里会让后续改场景树很危险。本批新增 `HexMapSceneBridge.gd` 集中维护这些查找。

## 修改文件

### `scene/in_scene/hex_map_modules/bridges/HexMapSceneBridge.gd`

新增场景节点桥接模块，职责是：

- `find_height_view_button(hex_map)`：查找高度视图切换按钮。
- `find_enemy_intent_manager(hex_map)`：查找敌人意图管理器。
- `find_camera(hex_map)`：查找局内地图相机。
- `find_total_enemy_health_bar(hex_map)`：查找总敌方血量条。
- `find_bar_manager(hex_map)`：查找 HexMap 子节点 `BarManager`。
- `find_health_bar_for_occupant(hex_map, occupant)`：按旧命名规则查找单体血条。

这个模块不处理：

- 高度视图按钮点击后的状态切换。
- 敌人意图 hover 的规则和表现。
- 相机缩放逻辑。
- 血条创建、刷新或销毁。
- 任何地图玩法数据。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `HEX_MAP_SCENE_BRIDGE` preload。
- 新增 `_hex_map_scene_bridge` 实例。
- 高度视图按钮查找改为委托 bridge。
- 入场动画、外部渲染节点注册、高度视图和升降服务所需的 `BarManager` 查找改为委托 bridge。
- 总敌方血量条、敌人意图管理器、地图相机查找改为委托 bridge。
- 单体血条仍沿用旧命名规则 `HealthBar_<instance_id>`，但规则集中在 bridge。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

本次集中维护的是场景路径，不是策划参数。后续如果调整局内节点树，优先检查 `HexMapSceneBridge.gd`。

## 行为边界

本次保持以下行为不变：

- 高度视图按钮仍连接 `_on_height_view_toggle_pressed()`。
- 总敌方血量条仍从 UI 节点读取。
- 敌人意图 hover 仍转发给 `EnemyIntentManager.handle_map_stack_hover()`。
- 相机缩放输入仍只在相机有 `zoom_input_enabled` 属性时写入。
- `BarManager` 仍作为 `HexMap` 子节点使用。
- 单体血条仍按 `HealthBar_<occupant_instance_id>` 命名查找。

## 当前完成度回顾

已完成：

- `HexMap` 中高度按钮、敌人意图、相机、总血条和 BarManager 的硬路径明显减少。
- Bridge 不缓存节点，因此场景切换时不会保留旧引用。
- Bridge 不持有玩法状态，只负责即时查找。

仍待处理：

- `in_scene.gd`、`TimelineManager.gd`、`DragShapeController.gd` 中仍存在各自的地图路径查找。
- 更彻底的方案是由 `in_scene.gd` 在初始化时显式注入依赖，但这会跨多个系统，本批不做。

## 后续建议

下一批建议处理 `refresh_tile_visual()` 的局部重绘边界。风险较高时，可以先抽服务承接现有“删除旧 stack 后重建”的流程，并在文档中标明后续真正局部复用 metadata 的 TODO。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 高度视图按钮是否仍能切换。
- 敌人意图 hover 是否仍能高亮来源和目标。
- 拖拽或时间轴期间相机缩放输入是否仍能被锁定和恢复。
- 总敌方血量条是否仍能刷新。
- 单体血条在入场、高度视图、升降和地貌注册后是否仍能跟随。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
