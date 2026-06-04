# 第 10 次落地：提取运行时地貌注册

日期：2026-06-04

## 本次处理范围

这次把运行时地貌注册和视觉刷新逻辑从 `hex_map.gd` 中拆出来。场景级信号、生成 VFX、交互刷新和拓扑事件仍然留在 `hex_map.gd`。

建造卡、村庄扩张、雷达召唤，以及旧路径里先写 `map_data` 再调用 `add_landform_visual_at()` 的行为都保持不变。

## 新增模块

### `scene/in_scene/RuntimeLandformRegistrar.gd`

这个模块负责：

- 把运行时地貌字段写回 `map_data`。
- 把 `landform` 实体挂到对应地块，并刷新地块 `occupant` metadata。
- 给运行时实体应用敌方或中立分组。
- 调用地貌自己的 `attach_visual()` 扩展点。
- 给生成的地貌 sprite 应用 HexMap block shader 参数。
- 运行时视觉变化后重新收集地块渲染 sprite。

主要函数：

- `register_landform(coord, entity, landform_type, context)`：注册新创建的运行时地貌实例，并返回结果字典。
- `refresh_visual_at(coord, context)`：兼容旧流程，适用于已经先写入 `map_data` 后再请求视觉刷新的路径。
- `attach_entity_to_stack(coord, entity, stack, context)`：把实体重新挂到地块，并计算 3D 锚点位置。
- `apply_landform_group(entity)`：把实体加入 `Enemies` 或 `Middle` 分组，保留旧逻辑里“不主动移除旧分组”的行为。
- `cleanup_stack_sprites(stack)`：从缓存 sprite、地块直接 sprite、occupant sprite 和 occupant 子 sprite 中重建 `sprites` metadata。
- `_get_stack_height(stack)`：安全读取地块高度 metadata。
- `_get_stack_sprites(stack)`：安全读取已有 `sprites` metadata。
- `_append_stack_render_sprite(list, sprite)`：加入一个有效渲染节点，并过滤状态图标和重复项。
- `_write_map_data(coord, entity, resolved_type, map_data)`：写入 `landform`、`landform_in` 和 `landform_type`。
- `_attach_visual_sprite(coord, entity, stack, tile_data, context)`：调用 `attach_visual()`，并做材质和 shader 后处理。
- `_resolve_landform_sprite(coord, entity, stack)`：优先使用 `entity.tex`，否则回退到 `LandformSprite_x_y`。
- `_apply_landform_sprite_shader(sprite, height, context)`：复制 block material，并写入每实例 shader 参数。
- `_resolve_landform_type(entity, landform_type)`：优先使用显式类型，否则回退到 `entity.landform_name`。
- `_is_enemy_landform(entity)`：判断该实体是否需要触发敌人列表刷新。
- `_make_result(success, stack, is_enemy)`：构造 `hex_map.gd` 消费的结果。

## HexMap 的变化

- 新增 `RUNTIME_LANDFORM_REGISTRAR` preload 和 `_runtime_landform_registrar`。
- `register_runtime_landform()` 缩减为：
  - 调用 `RuntimeLandformRegistrar.register_landform()`。
  - 把地块同步到当前 3D 或平铺视图。
  - 播放运行时生成 VFX。
  - 对敌方地貌发出 `enemy_roster_changed`。
  - 刷新地块交互状态。
  - 发出 `tile_topology_changed`。
- `add_landform_visual_at()` 缩减为：
  - 调用 `RuntimeLandformRegistrar.refresh_visual_at()`。
  - 把地块同步到当前视图。
  - 对敌方地貌发出 `enemy_roster_changed`。
- `_cleanup_stack_sprites()` 改为包装 `RuntimeLandformRegistrar.cleanup_stack_sprites()`。
- 新增 `_build_runtime_landform_registrar_context()`，让 registrar 通过上下文字典读取数据，而不是直接依赖 HexMap 成员。

## 可调变量

这些变量仍然在 `hex_map.gd` 中导出，并通过 `_build_runtime_landform_registrar_context()` 传给 registrar：

- `step_height`
- `tile_scale`
- `REF_SCALE`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- `current_view_state`

运行时生成 VFX 的导出变量仍然归 `hex_map.gd` 管，因为 VFX 不属于注册逻辑：

- `runtime_landform_spawn_vfx_enabled`
- `runtime_landform_spawn_vfx_duration`

## 调整说明

- 想调整逻辑实体锚点，改 `hitbox_offset_x`、`hitbox_offset_y` 或 `landform_instance_offset`。
- 想调整可见 sprite 偏移，继续使用现有地貌视觉偏移流程；本模块不改变 `landform_sprite_offset`。
- 想关闭运行时生成特效，关闭 `runtime_landform_spawn_vfx_enabled`。
- 想调整生成特效速度，改 `runtime_landform_spawn_vfx_duration`。
- 不要在 registrar 里发出 `enemy_roster_changed` 或 `tile_topology_changed`；这些仍然是 `hex_map.gd` 的场景级编排事件。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

## 手动回归重点

- 打出建造卡，确认新实体会出现在选中的地块上。
- 如果能触发村庄或雷达运行时生成，确认新实体会落在正确地块。
- 确认敌方运行时生成仍会刷新敌人时间轴和敌人列表消费者。
- 在运行时生成前先切到平铺高度视图，确认生成实体会立即对齐到平面。
- 确认生成实体仍会收到 block shader 参数，并参与 hover 和高亮效果。
