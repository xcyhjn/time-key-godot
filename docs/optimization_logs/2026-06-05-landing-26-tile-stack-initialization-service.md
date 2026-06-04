# 第 26 批落地：地块栈初始化收尾服务拆分

日期：2026-06-05

## 本次目标

本次继续拆 `hex_map.gd::_create_stack_at()` 的最后一段收尾逻辑。上一批已经把基础地块栈创建拆到 `TileStackFactory.gd`，把初始地貌挂接拆到 `TileLandformAttachService.gd`。剩下的直接写 metadata、连接输入信号、入场 dissolve 初始值和平铺高度标签仍留在 `hex_map.gd`。

这次新增 `TileStackInitializationService.gd`，只搬运这些收尾行为，不修改地图生成、地貌挂接、卡牌目标规则和奖励规则。

## 修改文件

### `scene/in_scene/hex_map_modules/factory/TileStackInitializationService.gd`

新增地块栈初始化收尾服务，主要职责是：

- 写入 `sprites` metadata，供高亮、入场动画、视图切换和销毁效果读取。
- 写入 `height` metadata，供高度视图、目标遮挡、奖励定位和地块升降读取。
- 写入 `occupant` metadata。即使占用者为空，也保持写入，延续旧逻辑中 `has_meta("occupant")` 可以成立的行为。
- 在地图入场揭示期间调用 HexMap 注入的 dissolve 回调，把新地块初始设为隐藏。
- 连接 `mouse_entered`、`mouse_exited` 和 `input_event` 信号。
- 在平铺高度视图下调用 HexMap 注入的高度标签回调。

这个模块不直接处理：

- `stack_nodes` 写入。
- 基础 `Area2D`、sprite 和碰撞体创建。
- 初始地貌挂接和地貌 sprite 收编。
- hover、点击、结算奖励、敌人意图或卡牌 AOE 的具体判断。
- 高度指示器的真实节点结构。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TILE_STACK_INITIALIZATION_SERVICE` preload。
- 新增 `_tile_stack_initialization_service` 实例。
- `_create_stack_at()` 在基础栈创建和地貌挂接后，调用 `TileStackInitializationService.initialize()`。
- 新增 `_build_tile_stack_initialization_config()`，集中把旧状态和回调注入服务。
- `_create_stack_at()` 现在只保留地块创建链路的编排职责：基础栈、初始地貌、初始化收尾。

## 配置字段说明

`_build_tile_stack_initialization_config()` 目前传给服务的字段如下：

- `sprites`：最终写入 stack metadata 的 sprite 列表。
- `height`：最终写入 stack metadata 的地块高度。
- `occupant`：最终写入 stack metadata 的地貌占用者，可以为空。
- `intro_reveal_active`：当前是否处于地图入场揭示动画。
- `is_flat_view`：当前是否处于平铺高度视图。
- `set_stack_dissolve`：HexMap 的 `_set_stack_dissolve_blend()` 回调。
- `mouse_entered_callback`：已经绑定当前 stack 的 hover 进入回调。
- `mouse_exited_callback`：已经绑定当前 stack 的 hover 离开回调。
- `input_event_callback`：已经绑定当前 stack 的输入回调。
- `create_height_indicator`：HexMap 的 `_create_height_indicator()` 回调。

这些字段都是运行时快照或回调，不是新的 Inspector 可调项。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

仍由旧导出变量间接影响本流程的内容包括：

- 地图入场揭示相关参数，例如 `map_intro_reveal_enabled` 和 `map_intro_reveal_lock_interaction`。
- 高度视图相关参数，例如 `show_height_pillars`、`show_height_labels` 和高度标签样式。
- 碰撞与输入相关参数仍在 `HexMapCollisionPresenter.gd` 和 HexMap 导出项中管理。

## 行为边界

本次保持以下行为不变：

- `sprites/height/occupant` metadata 的 key 不变。
- `occupant` 为空时仍写入 metadata。
- 入场揭示期间仍把新地块 dissolve 初始值设为 `1.0`。
- 鼠标进入、离开和输入信号仍连接到原来的 HexMap 回调。
- 平铺视图下仍通过 `_create_height_indicator()` 创建高度数字。
- `refresh_tile_visual()` 仍然通过删除旧 stack 后重新走 `_create_stack_at()`。

## 当前完成度回顾

地块创建链路现在已经拆成三层：

- `TileStackFactory.gd`：创建基础 stack、地块层 sprite 和碰撞体。
- `TileLandformAttachService.gd`：挂接初始地貌，并收编地貌 sprite。
- `TileStackInitializationService.gd`：写 metadata、连接输入、处理入场 dissolve 和高度标签。

`hex_map.gd::_create_stack_at()` 现在主要承担 composition root 职责，负责按旧顺序串联三层服务。

## 后续仍需拆分

建议下一步优先看这些模块：

- `CardManagerLocator.gd`：统一 HexMap、奖励、商店等脚本里的 CardManager 查找链。
- `TileTurnBehaviorRunner.gd`：拆 `_on_step_next()` 里旧建筑和敌人的回合行为遍历。
- MainBoard tooltip 适配器：把目标 hover tooltip 的旧 UI 调用从 HexMap 中继续变薄。
- `refresh_tile_visual()` 局部重绘：减少删除旧 stack 后完整重建的粗粒度流程。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图已经走过 `TileStackInitializationService.initialize()`。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB 泄漏提示和资源释放提示，本批没有改动这些旧问题。

手动回归时重点看：

- 地图是否能正常生成。
- hover 地块是否仍能触发卡牌 AOE、敌人意图和结算奖励表现。
- 点击合法和非法目标是否仍走旧逻辑。
- 平铺高度视图下高度数字是否正常出现。
- 地图入场揭示期间地块是否仍按原逻辑逐步显现。
