# 第 3 次落地：提取六边形坐标规则

日期：2026-06-04

## 本次处理范围

这次只从 `hex_map.gd` 中提取纯坐标规则。`hex_map.gd` 里保留原来的包装函数，所以现有调用点和运行行为都不需要改。

## 代码改动

- 新增 `scene/in_scene/hex_map_modules/rules/HexCoordRules.gd`。
- 把扇形地图坐标采样、圆形地图坐标采样、轴坐标距离计算和轴坐标到像素坐标转换移到静态规则函数中。
- `scene/in_scene/hex_map.gd` 通过 preload 使用 `HexCoordRules`，并继续通过 `_get_fan_coords()`、`_get_circular_coords()` 和 `_get_hex_pixel_pos()` 这些旧入口对外工作。

## 验证结果

- `git diff --check` 通过。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，没有出现坐标模块解析错误。

## 仍需注意

- 这只是第一段拆分。地形高度规则、目标校验和敌人意图候选排序还混在 `hex_map.gd` 中。
- 后续可以考虑和 `scene/out_scene/HexUtils.gd` 合并通用轴坐标工具，但这次刻意不碰局外地图。
