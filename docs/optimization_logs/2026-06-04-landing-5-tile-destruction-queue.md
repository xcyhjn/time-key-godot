# 第 5 次落地：提取地块销毁批处理队列

日期：2026-06-04

## 本次处理范围

这次只把高度超限后的地块销毁队列编排从 `hex_map.gd` 中拆出来。真正删除地块、修改 `map_data`、清理全局高度池和发信号的逻辑仍然留在 `hex_map.gd`。

## 已完成模块

- `HexCoordRules`：负责扇形和圆形坐标采样，以及轴坐标到像素坐标转换。
- `HexTerrainRules`：负责扇形层级计算、高度随机、圆形房间高度范围、高度到地形的兜底映射，以及地形和地貌调试名称。
- `TileDestructionBatchQueue`：负责队列元数据、批次收集延迟、批次大小、批次间隔，以及等待地块真正销毁完成。

## 代码改动

- 新增 `scene/in_scene/TileDestructionBatchQueue.gd`。
- `hex_map.gd` 原来的 pending/running 状态由 `_tile_destruction_queue` 接管。
- `_queue_tile_destruction_and_wait()` 缩减为薄包装，只传入当前调参值和 `_perform_tile_destruction` 回调。
- 删除了 `hex_map.gd` 中旧的私有队列辅助函数：
  - `_run_height_limit_destruction_batches`
  - `_perform_queued_tile_destruction`
  - `_wait_for_height_limit_destruction_batch`

## 还需要继续提取

- 目标校验和卡牌范围检查。
- 敌人意图地图表现。
- 结算奖励高亮和 tooltip。
- 高度视图状态，以及 pillar 和 label 管理。
- 地貌放置和运行时地貌注册。
- 地块销毁时的数据变更可以之后再看，但要等当前队列行为完成手动战斗回归后再动。

## 验证结果

- `git diff --check` 通过。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，没有出现队列脚本解析错误。
