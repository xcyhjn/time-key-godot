# 第 11 次落地：集中卡牌目标限制

日期：2026-06-04

## 本次处理范围

这次把卡牌目标限制集中到 `HexTargetRules.gd`。HexMap 的 hover 高亮和 MainBoard 的目标 tooltip 使用同一个规则模块，避免两边判断不一致。

无效地图点击会在卡牌进入时间轴放置前被拦截。timeline command 仍然保留执行时的最后安全校验。

## 更新后的模块

### `scene/in_scene/hex_map_modules/rules/HexTargetRules.gd`

这个模块负责：

- 判断选中卡牌是否能以某个地块为目标。
- 把卡牌的 `effect_range` 展开为地图上的真实地块。
- 校验建造、伤害、恢复或治疗、中毒、升降高度和清除效果。
- 对未知或实验性效果保持宽松，避免重构过程中误伤旧内容。

主要函数：

- `is_stack_valid_target(stack, selected_card, context)`：HexMap 和 MainBoard 使用的主入口。
- `is_empty_build_target(stack, context)`：检查地块 `occupant`，以及 `map_data.landform/landform_in`。
- `get_effect_range_stacks(card, center_coord, stack_nodes)`：把卡牌已有 `get_absolute_effect_range()` 的结果转成有效 `Area2D` 地块。
- `_get_card_info(card)`：从 `card.card_info` 读取卡牌 JSON 字典。
- `_get_effects(card_info)`：安全读取 `effects`。
- `_get_stack_coord(stack, context)`：从显式 context 或 `stack_nodes.find_key()` 中解析目标地块坐标。
- `_range_has_target_for_effect(card, center_stack, center_coord, stack_nodes, effect_type)`：对 AOE 效果检查范围内至少有一个有效接收者。
- `_get_stack_occupant(stack)`：从地块 metadata 中读取存活的 occupant 节点。
- `_can_damage_entity(entity)`：与 `DamageCommand` 对齐，要求实体存在、支持 `take_damage()`，并且有 HP 时 HP 大于 0。
- `_can_recover_entity(entity)`：与 `RecoverCommand` 对齐，要求实体存在、支持 `heal()`，并且有 HP 字段时不能满血。
- `_can_poison_entity(entity)`：与 `PoisonCommand` 对齐，要求实体存在、支持 `add_status()`，并且有 HP 时 HP 大于 0。
- `_is_live_object(value)`：统一处理来自 CardManager 和地块 metadata 的动态对象有效性。

## 规则细节

`built` / `build`：

- 目标地块不能有存活的 `occupant`。
- 目标 `map_data` 必须存在。
- `map_data[coord].landform` 和 `landform_in` 都不能是存活对象。

`damage`：

- `effect_range` 内至少要有一个地块包含支持 `take_damage()` 的存活实体。
- 如果实体有 `HP`，则 `HP` 必须大于 0。

`recover` / `heal`：

- `effect_range` 内至少要有一个地块包含支持 `heal()` 的实体。
- 如果实体同时有 `HP` 和 `Max_Blood`，则 `HP` 必须小于 `Max_Blood`。

`poison`：

- `effect_range` 内至少要有一个地块包含支持 `add_status()` 的存活实体。
- 如果实体有 `HP`，则 `HP` 必须大于 0。

`elevation`：

- 任意有效地形地块仍然可作为目标。

`clear`：

- 仍然视为有效，因为清除卡走的是无地图目标的时间轴路径。

未知效果类型：

- 保持有效，避免重构期间阻塞已有原型卡。

## HexMap 的变化

- `_is_stack_valid_target()` 现在会把 `stack_nodes`、`map_data` 和中心坐标传入 `HexTargetRules`。
- 新增 `_build_hex_target_rules_context(center_stack)`。
- `_handle_tile_click()` 会在调用 `active_card.play_card(stack)` 前拦截无效卡牌目标。
- hover 高亮仍然使用 `TileVisualState.HOVER_TARGET_VALID` 和 `HOVER_TARGET_INVALID`，只是规则归属从 HexMap 移到了规则模块。

## MainBoard 的变化

- 新增 `HEX_TARGET_RULES` preload。
- `is_valid_target()` 改为委托 `HexTargetRules.is_stack_valid_target()`。
- 删除了重复的本地辅助函数：
  - `_is_empty_build_target()`
  - `_card_has_effect_type()`
- 新增 `_build_hex_target_rules_context(center_stack)`，让 tooltip 判断和地图 hover 判断读取同一批输入。

## 可调变量

本次没有新增导出变量。

目标校验依赖这些既有运行时字段：

- `card.card_info.effects`
- `card.get_absolute_effect_range(center_coord)`
- `hex_map.stack_nodes`
- `hex_map.map_data`
- 地块 `occupant` metadata
- 实体方法和属性：
  - `take_damage`
  - `heal`
  - `add_status`
  - `HP`
  - `Max_Blood`

## 调整说明

- 新增效果类型时，先扩展 `HexTargetRules.is_stack_valid_target()`。
- 如果新效果是 AOE，并且需要实体接收者，就增加一个小的 `_can_*_entity()` 辅助函数，再通过 `_range_has_target_for_effect()` 接入。
- 如果新效果作用于地形而不是实体，可以像 `elevation` 一样保持地块目标。
- 不要把 shader、tooltip 文本或时间轴放置逻辑写进 `HexTargetRules`。这些仍然属于 HexMap、MainBoard 和 DragShapeController。

## 验证方式

本次落地后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

## 手动回归重点

- 选择 `tower`，确认被占用地块显示无效目标状态，并且不能进入时间轴放置。
- 选择 `lighting`，确认 AOE 中心只有在范围内包含可伤害存活实体时才有效。
- 选择 `recover`，确认满血实体不会被当作有效恢复目标。
- 选择 `poison`，确认空地块和死亡目标无效。
- 选择 `earthquake`，确认地形地块仍然有效。
- 选择 `wind` 或 `tornado`，确认无地图目标的时间轴清除流程仍然从选卡开始。
