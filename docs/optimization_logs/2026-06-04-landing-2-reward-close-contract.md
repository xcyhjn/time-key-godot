# 第 2 次落地：统一奖励关闭协议

日期：2026-06-04

## 本次处理范围

这次把奖励场景的关闭入口改成明确的信号，不再让 `in_scene.gd` 扫描多个可能的按钮路径。奖励本身的业务逻辑不变，获得、删除、合成和商店仍然由各自场景判断什么时候允许关闭。

## 代码改动

给以下奖励脚本增加了 `reward_scene_close_requested(scene_instance: Node)` 信号：

- `scene/in_scene/rewards/AcquireReward.gd`
- `scene/in_scene/rewards/RemoveReward.gd`
- `scene/in_scene/rewards/CraftReward.gd`
- `scene/in_scene/rewards/ShopManager.gd`

每个奖励场景会在完成自己的关闭或退出清理后发出这个信号。`scene/in_scene/in_scene.gd` 在实例化奖励场景后直接连接这个信号，并删除了原来扫描多个按钮路径的 `_connect_exit_signal_for_external_scene()`。

## 验证结果

- `git diff --check` 通过。Godot 提示 `CraftReward.gd` 的行尾会从 CRLF 归一化为 LF。
- `Godot --headless --path . --quit --no-header` 退出码为 0。
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header` 退出码为 0。
- 局内场景加载时仍会输出既有的 TileSet atlas 报错和资源释放提示，没有出现奖励信号相关的解析错误。

## 仍需注意

- 奖励打开和初始化仍然使用 `has_method()` 调用 `set_deck_manager`、`open_shop` 和 `open`。等所有奖励场景拥有更完整的共享接口后，再继续收敛。
- `CraftReward.gd` 在本次编辑后行尾变成 LF。
