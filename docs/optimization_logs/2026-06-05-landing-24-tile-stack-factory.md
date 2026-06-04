# 第 24 批落地：基础地块栈工厂拆分

日期：2026-06-05

## 本次目标

本次开始拆 `hex_map.gd::_create_stack_at()`。这个函数仍然是 HexMap 中最大的耦合点之一，原本同时负责基础地块节点创建、地貌挂接、血条相关位置、shader 初始化、碰撞创建、metadata、输入信号和高度视图标签。

这一批只拆第一层基础地块栈工厂，不动地貌挂接和信号逻辑，目标是降低风险：

- 基础 `Area2D` 容器创建。
- 地块顶面和侧面 `Sprite2D` 创建。
- 基础 shader 材质复制与实例参数写入。
- 3D/平铺视图下基础地块层位置和显隐。
- `CollisionPolygon2D` 创建、z_index 和位置。
- `collision_node` metadata 写入。

## 修改文件

### `scene/in_scene/hex_map_modules/factory/TileStackFactory.gd`

新增基础地块栈工厂，主要职责是：

- `create_stack(config)`：创建基础地块栈，并返回 HexMap 后续流程需要的状态包。
- `_create_stack_container()`：创建 `Area2D` 容器并挂到 `map_root`。
- `_create_base_sprites()`：创建整根地块柱的基础 sprite。
- `_create_base_sprite()`：创建单层地块 sprite，并复制材质、写入 shader 参数。
- `_apply_base_sprite_view_state()`：处理 3D 和平铺视图下的基础层位置和显隐。
- `_create_collision_node()`：创建碰撞多边形并按 HexMap 传入的碰撞参数定位。

这个模块不直接处理：

- `stack_nodes` 写入。
- 地貌实例挂接。
- `owner_battle`、`location`、`target` 等地貌状态。
- 敌人分组。
- 血条。
- 鼠标输入信号。
- 高度数字标签。
- 入场 dissolve 状态。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TILE_STACK_FACTORY` preload。
- 新增 `_tile_stack_factory` 实例。
- `_create_stack_at()` 保留旧入口名，但基础节点创建改为调用 `TileStackFactory.create_stack()`。
- 新增 `_build_tile_stack_factory_config()`，集中把 HexMap Inspector 导出项和运行时视图状态打包给工厂。
- 地貌挂接、地貌 sprite 收编、`sprites/height/occupant` metadata、入场 dissolve、输入信号和高度视图标签仍留在 `_create_stack_at()`。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

工厂通过 `_build_tile_stack_factory_config()` 读取这些旧可调项：

- `tile_scale`
- `step_height`
- `REF_SCALE`
- `block_material`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `hitbox_z_index`
- `hitbox_width`
- `hitbox_base_height`
- `hitbox_top_width_ratio`
- `hex_top_tex`、`hex_side_tex` 和地形贴图数组

## 行为边界

本次保持以下行为不变：

- 地块坐标仍由 `_get_hex_pixel_pos(coord)` 计算。
- 顶层 sprite 仍使用顶部纹理，其余层仍使用侧面纹理。
- 基础 sprite 仍使用旧 offset `Vector2(-256, -400)`。
- 3D 视图下仍按 `current_step_h` 分层堆叠。
- 平铺视图下仍只显示顶层基础地块。
- 碰撞多边形仍由 `HexMapCollisionPresenter` 构造。
- 地貌实体挂接和 `Enemies` 分组仍留在 HexMap 旧流程里。

## 后续仍需拆分

`_create_stack_at()` 已经少掉基础节点创建部分，但还没有完全拆干净。后续建议继续分两步：

- 把地貌实例挂接和地貌 sprite 收编交给 `RuntimeLandformRegistrar` 或新的 `TileLandformAttachService.gd`。
- 把输入信号连接、高度视图标签和入场 dissolve 收尾抽成更小的初始化 helper。

更大的后续项仍然是：

- `CardManagerLocator.gd`
- `_on_step_next()` 回合行为 runner
- MainBoard tooltip 旧接口适配

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
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，地图能走过 `_create_stack_at()` 和 `TileStackFactory.create_stack()`。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB 泄漏提示和资源释放提示，本批没有改动这些旧问题。

手动回归时重点看：

- 普通战斗地图是否正常生成。
- 3D 视图下地块高度是否正确。
- 平铺视图下是否只显示顶层地块和高度标签。
- 鼠标 hover、点击、选卡 AOE 是否仍能命中地块碰撞。
- 带地貌的格子位置、血条位置和敌人分组是否仍正确。
