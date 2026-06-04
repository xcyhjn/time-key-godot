# 第 12 次落地：提取血条和外部渲染节点注册

日期：2026-06-04

## 本次处理范围

这次把血条和外部渲染节点注册从 `hex_map.gd` 中拆出来。血条创建、开场延迟队列、BarManager 生命周期管理和视角同步仍然留在原来的 HexMap/BarManager 流程里。

旧行为保持不变：外部 UI 节点会加入地块 `sprites` metadata，并写入高度相关的 shader instance 参数，但不会被替换材质。

## 新增模块

### `scene/in_scene/ExternalRenderNodeRegistrar.gd`

这个模块负责：

- 找到某个 `landform` 对应的单个血条节点。
- 把血条或外部 UI 渲染节点注册进地块渲染列表。
- 递归收集支持的子渲染节点。
- 对已经有材质的节点写入 `block_idx` 和 `total_height` 这两个 instance shader 参数。
- 节点归属、生命周期和位置仍然交给 BarManager 和 HexMap 处理。

主要函数：

- `recollect_for_landform(entity, bar_manager, context)`：找到 `HealthBar_<entity_id>` 并注册它的渲染子节点。
- `register_node_at(coord, node, context)`：把任意外部节点注册到某个地图坐标。
- `find_health_bar_for_landform(entity, bar_manager)`：封装血条命名规则。
- `_get_or_init_stack_sprites(stack)`：读取或初始化地块 `sprites` metadata。
- `_get_stack_height(stack)`：安全读取地块高度 metadata，用于 shader 参数。
- `_collect_render_nodes(node, sprites_list, height)`：递归遍历节点树，并注册支持的渲染子节点。
- `_is_supported_render_node(node)`：当前允许 `Sprite2D`、`TextureRect` 和 `TextureProgressBar`。
- `_register_render_node(node, sprites_list, height)`：注册单个节点，并写入高度 shader 参数。

## HexMap 的变化

- 新增 `EXTERNAL_RENDER_NODE_REGISTRAR` preload 和 `_external_render_node_registrar`。
- `_find_health_bar_for_landform()` 改为把命名查找委托给 registrar。
- `recollect_sprites_for_landform()` 改为委托血条渲染子节点注册。
- `register_extra_render_node()` 改为委托任意外部节点注册。
- 从 `hex_map.gd` 中删除 `_find_and_register_ui_sprites()`。
- 新增 `_build_external_render_node_registrar_context()`。

## 可调变量

本次没有新增导出变量。

registrar 依赖这些既有运行时数据：

- `stack_nodes`
- 地块 `sprites` metadata
- 地块 `height` metadata
- 命名格式为 `HealthBar_<landform_instance_id>` 的血条节点

## 调整说明

- 想注册新的外部 UI 节点类型，就在 `_is_supported_render_node()` 中增加类型。
- 想调整高度 shader 参数，就改 `_register_render_node()`。
- 不要在这个模块里替换材质；血条和 UI 节点可能使用自己的专用 shader。
- 不要在这个模块里创建或释放血条；血条生命周期仍然归 BarManager。
- 如果注册节点时地图正处在平铺高度视图，HexMap 仍会调用 `_sync_stack_to_current_view()` 来同步位置。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

## 手动回归重点

- 开始战斗后，确认敌人和建筑上方仍然只出现一个血条。
- hover 带血条的地块，确认高亮和 shader 响应仍然影响已注册的 UI 节点。
- 切换平铺高度视图，确认血条保持相对位置。
- 对目标造成伤害或治疗后，确认血条视觉仍然更新。
