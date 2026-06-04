# 第 28 批落地：建筑回合行为 Runner 拆分

日期：2026-06-05

## 本次目标

本次继续处理 `HexMap` 的跨系统耦合，把 `_on_step_next()` 里的旧建筑回合行为遍历拆到独立 runner。

旧逻辑有一个重要顺序：

1. 先遍历 `iron_mine.Library` 中记录的矿井坐标。
2. 再遍历 `map_data` 中其他地貌。

这个顺序可能影响矿井对周围建筑的 `willing` 行为，所以本批只搬运逻辑，不改建筑接口、不改执行顺序。

## 修改文件

### `scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd`

新增建筑回合行为 runner，职责是：

- 接收 `step`、`behavior`、`map_data` 和 `rng`。
- 先处理 `iron_mine.Library` 中仍有效的铁矿。
- 再处理 `map_data` 中其他有效地貌。
- 调用旧接口 `Behavior(step, map_data, null, behavior, rng)`。

这个模块不处理：

- `Signal_Bus.step_next` 连接。
- 建筑行为接口重命名。
- 地貌意图、时间轴排布或奖励状态。
- `map_data` 字段迁移。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TILE_TURN_BEHAVIOR_RUNNER` preload。
- 新增 `_tile_turn_behavior_runner` 实例。
- `_on_step_next(step, behavior)` 保留旧信号入口，但内部只调用 runner。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

## 行为边界

本次保持以下行为不变：

- `Signal_Bus.step_next` 仍由 `HexMap` 连接。
- iron_mine 仍优先执行。
- 其他地貌仍在第二轮执行。
- 仍调用旧 `Behavior()` 方法。
- `Other` 参数仍传 `null`。
- `rng` 仍使用 HexMap 持有的随机数生成器。

## 当前完成度回顾

已完成：

- `_on_step_next()` 已变成薄入口。
- 旧建筑行为遍历已集中到 `TileTurnBehaviorRunner.gd`。

仍待处理：

- 建筑脚本仍使用旧 `Behavior()` 鸭子类型接口。
- `has_method("Behavior")` 暂时保留，等所有建筑统一契约后再考虑删除。

## 后续建议

下一批建议继续抽 `SettlementRewardController.gd`，把奖励资格扫描、`reward_info` 构造和 used 状态写入从 HexMap 中拆出。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 回合推进是否仍触发建筑行为。
- 铁矿是否仍优先影响周围地貌。
- 村庄、祭坛、牧场等非铁矿建筑是否仍在第二轮执行。
- 地块变化后敌人意图和时间轴继续正常。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
