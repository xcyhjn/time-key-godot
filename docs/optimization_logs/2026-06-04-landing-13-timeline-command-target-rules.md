# 第 13 次落地：提取 timeline command 目标最终校验

日期：2026-06-04

## 本次处理范围

这次把 timeline command 脚本里重复的运行时目标校验提取出来。各 command 脚本仍然负责自己的执行副作用，例如 VFX、伤害、治疗、状态调用、创建注册和等待时机。

`HexTargetRules.gd` 仍然是放置前、面向玩家反馈的规则模块。本次新增的是执行时最后一层安全校验。

## 新增模块

### `scene/in_scene/timeline/commands/TimelineCommandTargetRules.gd`

这个模块负责：

- 从目标地块读取存活的 `occupant`。
- 从 `hex_map.stack_nodes` 中解析地块坐标。
- 根据地块 occupant 和 `map_data` 校验建造目标。
- 校验伤害、恢复或治疗、中毒的实体接收者。
- 为 command 脚本提供共享的动态属性检查。

主要函数：

- `get_occupant(tile)`：从地块 `occupant` metadata 读取存活 Node。
- `get_tile_coord(tile, hex_map)`：在 `hex_map.stack_nodes` 中查找地块坐标。
- `can_build_on_tile(tile, hex_map)`：执行时的最终建造校验。
- `is_map_data_empty_at(coord, hex_map)`：检查 `map_data.landform` 和 `map_data.landform_in`。
- `can_damage_entity(entity)`：要求实体支持 `take_damage()`，并且有 HP 时仍然存活。
- `can_recover_entity(entity)`：要求实体支持 `heal()`，并且有 HP 字段时不能满血。
- `can_apply_poison(entity)`：要求实体支持 `add_status()`，并且有 HP 时仍然存活。
- `object_has_property(target, property_name)`：共享的动态属性查询。

## 指令脚本变化

### `BuiltCommand.gd`

- 新增 `TARGET_RULES` preload。
- 用 `TimelineCommandTargetRules` 替换本地的 `_can_build_on_tile()`、`_get_tile_coord()`、`_is_map_data_empty_at()` 和 `_object_has_property()`。
- 给 `_init()`、`execute()`、`_create_creation()`、`_register_creation()` 和 `_notify_map_changed()` 增加了函数注释。

### `DamageCommand.gd`

- 新增 `TARGET_RULES` preload。
- 使用 `TARGET_RULES.get_occupant()` 和 `TARGET_RULES.can_damage_entity()`。
- 给 `_init()` 和 `execute()` 增加了函数注释。

### `RecoverCommand.gd`

- 新增 `TARGET_RULES` preload。
- 删除本地 `_can_recover_entity()`。
- 使用 `TARGET_RULES.get_occupant()` 和 `TARGET_RULES.can_recover_entity()`。
- 给 `_init()` 和 `execute()` 增加了函数注释。

### `PoisonCommand.gd`

- 新增 `TARGET_RULES` preload。
- 删除本地 `_can_apply_poison()`。
- 使用 `TARGET_RULES.get_occupant()` 和 `TARGET_RULES.can_apply_poison()`。
- 更新文件头说明，并给 `_init()` 和 `execute()` 增加了函数注释。

### `ElevationCommand.gd`

- 新增 `TARGET_RULES` preload。
- 使用 `TARGET_RULES.object_has_property()` 读取 `elevation_resolution_padding`。
- 给 `_init()`、`execute()` 和 `_wait_for_tiles_to_finish()` 增加了函数注释。

## 可调变量

本次没有新增导出变量。

运行时会读取这些数据：

- `EffectCommand.target_tiles`
- `EffectCommand.hex_map`
- 地块 `occupant` metadata
- `hex_map.stack_nodes`
- `hex_map.map_data`
- 实体方法和属性：
  - `take_damage`
  - `heal`
  - `add_status`
  - `HP`
  - `Max_Blood`

## 调整说明

- 新 command 需要目标接收者校验时，把小型判断函数加到 `TimelineCommandTargetRules.gd`。
- 玩法副作用继续留在各自 command class。
- 卡牌 JSON 和 effect 解析继续留在 `EffectProcessor.gd`。
- 放置预览和点击拦截继续留在 `HexTargetRules.gd`。
- 如果 `HexTargetRules` 修改了某条目标规则，要同步检查这层最终校验是否也需要调整。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

## 手动回归重点

- Damage command 仍然能伤害存活目标，并跳过空地块和死亡地块。
- Recover command 仍然能治疗受伤且可治疗的实体，并跳过满血实体。
- Poison command 仍然能给存活且支持状态的实体施加中毒。
- Built command 仍然只在空地块创建塔等实体。
- Elevation command 仍然会等待高度动画和销毁流程完成。
