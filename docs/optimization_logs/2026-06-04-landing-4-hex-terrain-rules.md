# 第 4 次落地：提取地形和高度规则

日期：2026-06-04

## 本次处理范围

这次从 `hex_map.gd` 中提取纯地形和高度规则。`hex_map.gd` 继续保留原有包装函数，避免影响调用点。

## 已完成模块

- `HexCoordRules`：负责扇形和圆形坐标采样，以及轴坐标到像素坐标转换。
- `HexTerrainRules`：负责扇形层级计算、高度随机、圆形房间高度范围、高度到地形的兜底映射，以及地形和地貌调试名称。

## 代码改动

- 新增 `scene/in_scene/HexTerrainRules.gd`。
- `scene/in_scene/hex_map.gd` 改为委托以下逻辑：
  - 扇形地图层级计算。
  - 按层级随机高度。
  - 圆形房间随机高度。
  - 按高度查地形。
  - 地形和地貌调试名称。

## 还需要继续提取

- 目标校验和卡牌范围检查。
- 敌人意图地图表现。
- 结算奖励高亮和 tooltip。
- 高度视图状态，以及 pillar 和 label 管理。
- 地块销毁队列和高度超限批处理。
- 地貌放置和运行时地貌注册。

## 验证结果

- `git diff --check` 通过。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，没有出现地形规则解析错误。
