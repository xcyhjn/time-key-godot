# 第 1 次落地：清理不必要的兜底代码

日期：2026-06-04

## 本次处理范围

这次只清理低风险的防御分支，目标是让固定 autoload 和固定场景节点的调用更直接。动态插件、卡牌和奖励协议仍然保留保护判断，等后续接口统一后再继续收敛。

同时把本地 Claude 桌面配置加入 `.gitignore`，避免个人工具配置进入项目版本。

## 代码改动

`scene/in_scene/in_scene.gd`：

- 简化了 `TimelineManager` 的信号连接流程。
- 删除了缺失 `TimelineManager` 路径里已经失效的 `EffectProcessor` 连接分支。
- 把固定存在的 `GlobalClock`、`MapState`、`Signal_Bus`、`TimelineManager` 和 `HexMap` API 探测改成直接调用。
- 保留了和节点生命周期相关的 `is_instance_valid()` 判断。

`scene/in_scene/hex_map.gd`：

- 删除了已经废弃的注释版 `pick_landform` 实现。
- 把 `GlobalClock.tile_h_pool` 的属性探测改成直接读取高度池。
- 直接发出本脚本声明的 `tile_topology_changed` 信号。

## 验证结果

- `git diff --check` 通过。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，本次没有引入新的解析错误。

## 仍需注意

- 动态奖励场景仍然使用方法探测，等共享的奖励打开和关闭协议建立后再清理。
- 卡牌框架和实体行为仍然是多态调用，暂时继续保留方法探测。
- 既有导入和资源警告不属于本次改动范围。
