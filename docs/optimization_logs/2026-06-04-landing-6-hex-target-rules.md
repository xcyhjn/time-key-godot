# 第 6 次落地：提取地图侧目标规则入口

日期：2026-06-04

## 本次处理范围

这次从 `hex_map.gd` 中提取地图侧的目标和范围入口。行为保持原样：只要当前有选中的卡牌，并且目标地块本身有效，就仍然视为可选目标。本次不引入新的卡牌目标限制。

## 已完成模块

- `HexCoordRules`：负责扇形和圆形坐标采样，以及轴坐标到像素坐标转换。
- `HexTerrainRules`：负责扇形层级、高度、地形和地貌名称。
- `TileDestructionBatchQueue`：负责高度超限销毁队列编排。
- `HexTargetRules`：负责选中卡牌的目标校验入口，以及按卡牌效果范围收集地块。

## 代码改动

- 新增 `scene/in_scene/HexTargetRules.gd`。
- `hex_map.gd` 委托了以下逻辑：
  - `_is_stack_valid_target()`
  - `_update_aoe_display()` 中的 AOE 效果范围地块收集。
- AOE shader 状态、tooltip 刷新、遮挡刷新和 `CardManager` 查找仍然留在 `hex_map.gd`。

## 还需要继续提取

- 具体卡牌目标限制，如果后续玩法需要。
- 敌人意图地图表现。
- 结算奖励高亮和 tooltip。
- 高度视图状态，以及 pillar 和 label 管理。
- 地貌放置和运行时地貌注册。

## 验证结果

- `git diff --check` 通过。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，没有出现目标规则解析错误。
