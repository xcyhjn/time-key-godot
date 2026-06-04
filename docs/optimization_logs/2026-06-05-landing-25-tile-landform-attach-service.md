# 第 25 批落地：初始地貌挂接服务拆分

日期：2026-06-05

## 本次目标

本次继续拆 `hex_map.gd::_create_stack_at()` 中剩余的地貌处理。上一批已经把基础地块栈创建拆到 `TileStackFactory.gd`，但 `_create_stack_at()` 里还保留了初始地貌实例的位置设置、挂接、`attach_visual()`、地貌 sprite 收编、shader 后处理、`owner_battle` 和敌人分组。

这次只拆初始建图路径的地貌挂接，不改运行时建造、地貌注册、局部重绘和奖励扫描。

## 为什么没有直接复用 RuntimeLandformRegistrar

`RuntimeLandformRegistrar.gd` 已经处理运行时新增地貌，但它和初始建图旧逻辑有几个细节差异：

- 运行时注册会写 `location`、`target`、`map_data.landform_in` 等字段；本次不想改变初始建图的数据写入顺序。
- 运行时注册会给非敌方地貌加入 `Middle` 分组；旧 `_create_stack_at()` 只给敌方加入 `Enemies`。
- 运行时注册优先解析一个地貌 sprite；旧 `_create_stack_at()` 会扫描 stack 下的 `LandformSprite_*`，也会扫描地貌实例自己的所有子 `Sprite2D`。

为了保持行为不变，本批新增了初始建图专用服务。后续如果要统一两条路径，可以在更多手动回归后再让两者共享更底层的 sprite 收编工具。

## 修改文件

### `scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd`

新增初始地貌挂接服务，主要职责是：

- 从地块数据中读取 `landform` 实例。
- 按旧公式设置地貌位置：`Vector2(hitbox_offset_x, top_block_y + hitbox_offset_y) + landform_instance_offset`。
- 把地貌实例挂到 stack 下。
- 调用 `landform.attach_visual(stack, height, current_step_h, tile_scale)`。
- 扫描 stack 下 `LandformSprite_*`，复制 `block_material` 并写入旧 shader 参数。
- 扫描地貌实例自己的子 `Sprite2D`，复制 `block_material` 并写入旧 shader 参数。
- 设置 `owner_battle`。
- 按旧逻辑只给敌方地貌加入 `Enemies` 分组。
- 返回 `occupant` 和更新后的 `sprites` 列表给 HexMap。

这个模块不直接处理：

- `stack_nodes` 写入。
- `map_data` 写入。
- `sprites/height/occupant` metadata 写入。
- 鼠标输入信号。
- 高度数字标签。
- 入场 dissolve。
- 血条实例化。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TILE_LANDFORM_ATTACH_SERVICE` preload。
- 新增 `_tile_landform_attach_service` 实例。
- `_create_stack_at()` 中原本的地貌挂接代码改为调用 `TileLandformAttachService.attach()`。
- 新增 `_build_tile_landform_attach_config()`，集中把旧地貌挂接所需的运行时参数传给服务。
- `_create_stack_at()` 继续负责写入 `sprites`、`height`、`occupant` metadata，以及输入信号、高度标签和入场 dissolve 收尾。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

服务通过 `_build_tile_landform_attach_config()` 使用这些旧参数：

- `tile_scale`
- `current_step_h`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- 当前是否平铺视图
- `owner_battle`

## 行为边界

本次保持以下行为不变：

- 初始地貌位置公式不变。
- 初始地貌仍调用自己的 `attach_visual()`。
- `LandformSprite_*` 和地貌子 `Sprite2D` 仍写入 `block_idx = height + 1`、`total_height = height + 2`。
- 平铺视图下地貌 sprite 仍写入 `is_flat_view = 1.0`。
- 旧逻辑只给敌方地貌加入 `Enemies` 分组，本批保持不变。
- `occupant` metadata 仍由 HexMap 写入。

## 后续仍需拆分

`_create_stack_at()` 现在还剩这些收尾职责：

- 写入 `sprites/height/occupant` metadata。
- 入场 dissolve 初始值。
- 鼠标 hover/input 信号连接。
- 平铺视图高度数字标签。

后续可选拆法：

- 新增 `TileStackInitializationService.gd`，只处理 metadata、输入信号和高度标签。
- 或先抽 `CardManagerLocator.gd`，因为当前大型剩余耦合里 CardManager 查找重复更明显。

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
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，初始建图已经走过 `TileLandformAttachService.attach()`。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB 泄漏提示和资源释放提示，本批没有改动这些旧问题。

手动回归时重点看：

- 初始地图上的敌人/中立地貌是否正常显示。
- 地貌贴图位置和血条锚点是否仍正确。
- 敌人是否仍进入 `Enemies` 分组，时间轴敌人意图是否能读取到敌人。
- 平铺视图下地貌贴图是否仍能跟随视图状态。
- 地块升降、销毁和回到局外流程是否仍正常。
