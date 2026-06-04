# 第 27 批落地：CardManager 查找服务拆分

日期：2026-06-05

## 本次目标

本次开始处理 `HexMap` 的跨系统耦合。第一刀只拆 CardManager 查找链，不改卡牌管理器生命周期，不改奖励、商店或 MainBoard 现有创建方式。

`hex_map.gd::get_card_manager()` 原本同时知道：

- `MainBoard` group。
- `MainBoard.manager_instance`。
- tree root 上的 `card_manager` metadata。
- current_scene 上的 `card_manager` metadata。
- metadata 指向已释放实例时的清理方式。

这些逻辑属于跨场景查找适配，不应该长期留在地图主控里。本批新增 `CardManagerLocator.gd` 统一管理。

## 修改文件

### `scene/in_scene/hex_map_modules/bridges/CardManagerLocator.gd`

新增 CardManager 查找服务，职责是：

- 从 `MainBoard` group 中读取 `manager_instance`。
- 从 tree root 的 `card_manager` metadata 中读取缓存实例。
- 从 current_scene 的 `card_manager` metadata 中读取缓存实例。
- 遇到已经释放的 metadata 实例时主动移除 metadata，避免二次进入局内时返回旧引用。

查找顺序保持旧行为：

1. `MainBoard.manager_instance`
2. `get_tree().root.get_meta("card_manager")`
3. `get_tree().current_scene.get_meta("card_manager")`

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `CARD_MANAGER_LOCATOR` preload。
- 新增 `_card_manager_locator` 实例。
- `get_card_manager()` 保留为 HexMap 的公共入口，但内部改为 `return _card_manager_locator.find(get_tree())`。
- 删除 HexMap 内部的 `_get_valid_card_manager_from_meta()`，让 metadata 清理逻辑集中到 locator。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

`CardManagerLocator.gd` 不持有状态，不需要在 Inspector 中配置。

## 行为边界

本次保持以下行为不变：

- CardManager 正常路径仍优先来自 `MainBoard.manager_instance`。
- root metadata 和 current_scene metadata 仍作为兜底。
- 无效 metadata 仍会被清理。
- `HexMap.get_card_manager()` 这个外部入口保留，调用方不需要迁移。
- 奖励、商店和其他脚本中的 CardManager 查找链暂不处理，避免一次跨太多系统。

## 当前完成度回顾

已完成：

- `HexMap` 中 CardManager 多路径查找逻辑已经移出。
- CardManager 生命周期兜底集中到 `CardManagerLocator.gd`。

仍待处理：

- 奖励脚本和商店脚本里仍可能有重复 CardManager 查找链。
- 如果后续确认所有入口都能使用 locator，可以逐步迁移这些脚本。

## 后续建议

下一批建议继续按计划抽 `_on_step_next()`，新增 `TileTurnBehaviorRunner.gd`，把旧建筑回合行为遍历移出 HexMap。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 进入局内后 CardManager 是否正常注册。
- 选中卡牌后地图 hover 是否能读取当前卡牌。
- 右键取消选中是否正常。
- 二次进入局内时不出现已释放 CardManager 引用。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，日志中 CardManager 仍正常注册到 root 和 current_scene，并在退出时注销。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
