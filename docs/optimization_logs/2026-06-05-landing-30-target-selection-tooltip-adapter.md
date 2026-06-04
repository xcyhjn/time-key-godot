# 第 30 批落地：目标选择 Tooltip 适配器拆分

日期：2026-06-05

## 本次目标

本次处理 `HexMap` 和 `MainBoard` 之间的一个小但明确的 UI 耦合点。

在上一阶段里，卡牌目标 hover 的范围收集与展示计划已经交给 `TargetAoeHoverPresenter.gd`，但 `HexMap` 仍然直接判断并调用：

```gdscript
MainBoard.update_target_selection_hover(center_stack, card)
```

这会让地图主控知道 MainBoard 的旧函数名和参数格式。为了后续能把 MainBoard tooltip 改成信号、显式注入或独立 UI controller，本批新增一个很薄的适配器。

## 修改文件

### `scene/in_scene/hex_map_modules/ui/TargetSelectionTooltipAdapter.gd`

新增 MainBoard tooltip 适配器，职责是：

- 接收 `main_board` 和 `display_plan`。
- 检查 MainBoard 是否有效。
- 检查 MainBoard 是否仍暴露旧接口 `update_target_selection_hover()`。
- 从 `display_plan` 读取 `tooltip_stack` 和 `tooltip_card`。
- 只有 `tooltip_stack` 是有效 `Area2D` 时才调用旧接口。

这个模块不负责：

- 判断卡牌目标是否合法。
- 收集 AOE 范围。
- 写地图 shader 状态。
- 创建或销毁 tooltip。
- 修改 MainBoard 的接口名。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TARGET_SELECTION_TOOLTIP_ADAPTER` preload。
- 新增 `_target_selection_tooltip_adapter` 实例。
- `_update_target_selection_tooltip()` 保留为 HexMap 内部入口，但函数体改为委托 adapter。
- `HexMap` 不再直接判断 `MainBoard.update_target_selection_hover()`。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

`TargetSelectionTooltipAdapter.gd` 只消费 `display_plan` 中的运行时数据：

- `tooltip_stack`
- `tooltip_card`

## 行为边界

本次保持以下行为不变：

- MainBoard 的旧接口名仍是 `update_target_selection_hover(center_stack, card)`。
- 合法目标、非法目标和范围目标的展示计划仍由 `TargetAoeHoverPresenter.gd` 生成。
- AOE 地块高亮仍由 `HexMapVisualStatePresenter.gd` 应用。
- 无目标卡牌或无有效 `tooltip_stack` 时仍不会刷新 MainBoard tooltip。

## 当前完成度回顾

已完成：

- MainBoard tooltip 旧接口调用离开 `HexMap` 主控。
- `TargetAoeHoverPresenter.gd` 继续保持纯计划生成职责。
- `HexMap` 只负责把计划转交给 adapter。

仍待处理：

- MainBoard 旧函数名暂时保留。
- 后续如果 MainBoard tooltip 改成信号或专用 UI controller，只需要替换 adapter 内部实现。

## 后续建议

下一批建议集中处理 `HexMap` 中散落的场景节点路径，例如高度视图按钮、敌人意图管理器、Camera2D、总血条和 BarManager，把这些查找放进统一的 `HexMapSceneBridge.gd`。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归时重点看：

- 选中卡牌后 hover 合法地块。
- 选中卡牌后 hover 非法地块。
- AOE 卡牌 hover 范围地块。
- 无目标卡牌或没有有效目标时 MainBoard tooltip 不误刷新。

验证结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，局内建图、CardManager 注册和场景初始化日志正常。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB/RID/resource 退出提示，本批没有改动这些旧问题。
