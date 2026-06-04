# 第 17 次落地：提取地图碰撞和输入刷新表现

日期：2026-06-05

## 本次处理范围

这次把 `hex_map.gd` 中和地块碰撞、点击输入开关相关的逻辑拆到独立 presenter。拆分对象包括六边形碰撞多边形构建、碰撞 z-index 夹紧、单个地块碰撞箱刷新、全图碰撞箱重建、敌方建筑地块判断，以及根据入场动画、结算奖励、卡牌选取状态刷新 `input_pickable`。

`hex_map.gd` 仍然负责导出变量、当前视图状态、奖励资格判断、卡牌选择状态判断、入场状态判断和旧公共入口。本次不改变 hitbox 公式、不改变奖励资格规则、不改变卡牌目标规则，也不修改 hover、AOE 或敌人意图表现。

## 新增模块

### `scene/in_scene/hex_map_modules/presenters/HexMapCollisionPresenter.gd`

这个模块负责：

- 根据 `hitbox_width`、`hitbox_base_height` 和 `hitbox_top_width_ratio` 构建六边形碰撞点集。
- 把 `hitbox_z_index` 夹紧到 Godot CanvasItem 的安全范围。
- 读取地块 `height` metadata，并按当前 3D/平铺视图计算碰撞箱 y 坐标。
- 读取地块 `collision_node` metadata，并只刷新 `CollisionPolygon2D`。
- 批量重建当前 `stack_nodes` 中所有有效地块的碰撞箱。
- 判断地块 `occupant` 是否属于 `Enemies` 分组。
- 根据入场动画锁、结算奖励模式、全局交互开关、智能碰撞开关和卡牌目标选择状态，统一写入 `input_pickable`。

主要函数：

- `build_hitbox_polygon(config)`：构建六边形碰撞点集。
- `refresh_stack_interactivity(config)`：刷新所有地块是否接收鼠标输入。
- `refresh_collision_for_stack(stack, config)`：刷新单个地块的碰撞箱。
- `rebuild_all_collision_shapes(config)`：重建整张地图的碰撞箱。
- `get_safe_hitbox_z_index(config)`：夹紧碰撞箱 z-index。
- `stack_has_enemy_building(stack)`：判断地块上是否有敌方建筑。
- `get_collision_node(stack)`：从 metadata 读取碰撞节点。
- `get_stack_height(stack)`：从 metadata 读取地块高度。
- `_refresh_settlement_reward_interactivity(stack_nodes, config)`：结算奖励阶段刷新可点击地块。
- `_set_all_stacks_pickable(stack_nodes, is_pickable)`：批量设置输入开关。
- `_get_scale_ratio(config)`：计算 `tile_scale / REF_SCALE`。

## HexMap 的变化

- 新增 `HEX_MAP_COLLISION_PRESENTER` preload。
- 新增 `_hex_map_collision_presenter` 实例。
- `_stack_has_enemy_building()` 保留旧函数名，内部委托 presenter。
- `_refresh_stack_interactivity()` 保留旧函数名，内部委托 presenter。外部脚本通过 `has_method("_refresh_stack_interactivity")` 调用时不需要改。
- `_build_hitbox_polygon()` 保留旧函数名，内部委托 presenter。
- `_get_safe_hitbox_z_index()` 保留旧函数名，内部委托 presenter。
- `_refresh_collision_for_stack(stack)` 保留旧函数名，内部委托 presenter。
- `rebuild_all_collision_shapes()` 保留旧函数名，内部委托 presenter。
- 新增 `_build_collision_presenter_config()`，集中把导出变量、运行时状态和奖励资格回调传给 presenter。

## 可调变量

本次没有新增导出变量。所有调参仍然在 `hex_map.gd` 中完成：

- `enable_smart_collision_interaction` 控制普通状态下是否只让敌方建筑地块接收输入。
- `hitbox_width` 控制碰撞六边形宽度。
- `hitbox_base_height` 控制碰撞六边形基础高度。
- `hitbox_offset_x` 和 `hitbox_offset_y` 控制碰撞箱相对地块顶部的偏移。
- `hitbox_top_width_ratio` 控制六边形顶部边宽度比例。
- `hitbox_z_index` 控制碰撞调试显示层级，并会被夹紧到安全范围。
- `map_intro_reveal_lock_interaction` 控制地图入场期间是否禁用地块输入。
- `step_height`、`tile_scale` 和 `REF_SCALE` 共同影响 3D 视图下碰撞箱 y 坐标。

## 调整说明

- 不要在 `HexMapCollisionPresenter.gd` 中处理 hover shader、AOE 范围、敌人意图 overlay 或奖励 tooltip。
- 不要在 presenter 中直接读取 CardManager；卡牌目标选择状态由 HexMap 计算后传入。
- 不要在 presenter 中直接判断奖励资格；奖励资格由 HexMap 的 `_is_settlement_reward_stack_available()` 回调提供。
- 如果修改 `build_hitbox_polygon()`，必须同时检查 `rebuild_all_collision_shapes()` 和 `_create_stack_at()` 中新建碰撞箱的结果。
- 如果修改 `refresh_stack_interactivity()`，必须检查普通状态、卡牌选取状态、入场动画状态和结算奖励状态四条路径。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- `git diff --check` 退出码为 0。
- Godot headless 项目加载和局内场景加载退出码为 0。
- headless 当前仍会输出项目既有的 `GlobalClock`、`Signal_Bus`、`Dialogic`、`TimelineAction` 和 TileSet atlas 噪声。
- 本次没有新增 `HexMapCollisionPresenter.gd` 或 collision presenter 相关解析错误。

## 手动回归重点

- 进入战斗地图，确认普通状态下 hover/click 仍能命中有敌方建筑的地块。
- 选中需要地块目标的卡牌，确认所有合法候选地块仍能接收输入。
- 地图入场动画期间确认地块输入按 `map_intro_reveal_lock_interaction` 被锁住。
- 战斗结算奖励阶段确认只有可领奖励地块能接收输入。
- 切换平铺和 3D 视图后，确认碰撞区域仍贴近地块顶部。
- 调整 hitbox 导出变量后调用 `rebuild_all_collision_shapes()`，确认点击区域按新参数刷新。
