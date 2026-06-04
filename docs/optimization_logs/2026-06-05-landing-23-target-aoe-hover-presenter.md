# 第 23 批落地：目标 AOE Hover 展示计划拆分

日期：2026-06-05

## 本次目标

本次继续拆 `hex_map.gd::_update_aoe_display()`。上一批已经把“当前 hover 中心是谁”拆到了 `TargetHoverController.gd`，但真正进入 AOE 展示时，HexMap 里仍然混着范围收集、目标合法性判断、视觉状态应用和 MainBoard tooltip 更新。

这次只拆“展示计划”这一层，不改变 shader 状态机、不改变 `HexTargetRules` 的目标细则、不改变 MainBoard 的 tooltip 接口。

## 修改文件

### `scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd`

新增目标 AOE hover 展示计划模块，主要职责是：

- 根据 `card + center_stack + stack_nodes` 计算新的 AOE 地块列表。
- 通过 HexMap 注入的目标合法性回调，决定目标状态是合法高亮还是非法高亮。
- 生成 tooltip 请求，告诉 HexMap 是否需要继续调用 MainBoard 的旧 tooltip 接口。

这个模块不直接做：

- shader 状态写入。
- `current_aoe_stacks` 回写。
- MainBoard 查找。
- CardManager 查找。
- tooltip 文本或位置更新。

它会使用 `HexTargetRules.get_effect_range_stacks()`，因为范围收集本身已经是纯规则函数；目标合法性仍走 HexMap 注入的 `_is_stack_valid_target()`，避免本批改动 CardManager 查找链。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TARGET_AOE_HOVER_PRESENTER` preload。
- 新增 `_target_aoe_hover_presenter` 实例。
- `_update_aoe_display()` 保留旧入口名，但改为先读取 `display_plan`。
- 新增 `_build_target_aoe_hover_presenter_config()`，集中传入 `stack_nodes`、合法/非法状态值和目标合法性回调。
- 新增 `_update_target_selection_tooltip()`，把 presenter 产出的 tooltip 请求翻译回旧 `main_board.update_target_selection_hover(center_stack, card)` 调用。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动 Inspector 上的任何可调项。

仍由 `hex_map.gd` 管理的相关状态包括：

- `current_aoe_stacks`
- `TileVisualState.HOVER_TARGET_VALID`
- `TileVisualState.HOVER_TARGET_INVALID`
- MainBoard tooltip 调用入口
- 普通地图视觉状态 presenter 的配置

## 行为边界

本次保持以下行为不变：

- AOE 地块范围仍来自卡牌的 `get_absolute_effect_range(center_coord)`。
- 范围内所有地块仍统一写入合法或非法目标状态。
- 中心目标是否合法仍由 `_is_stack_valid_target()` 和 `HexTargetRules.is_stack_valid_target()` 决定。
- MainBoard tooltip 仍使用旧接口 `update_target_selection_hover(center_stack, card)`。
- 没有选中卡牌时的清理仍由 `TargetHoverController.gd` 和 HexMap 旧清理入口处理。

## 后续仍需拆分

目标 hover 链路现在分成三层：

- `HexMapInputCoordinator.gd`：只处理鼠标输入和 hover 列表维护。
- `TargetHoverController.gd`：只处理 hover 中心选择和刷新触发。
- `TargetAoeHoverPresenter.gd`：只生成 AOE 展示计划。

仍建议继续拆：

- `CardManagerLocator.gd`，统一 `hex_map.gd::get_card_manager()` 和奖励脚本里的重复查找。
- `_create_stack_at()` 的基础地块工厂。
- MainBoard tooltip controller，让 HexMap 最终只发“目标 hover changed”事件，而不直接调用 UI 方法。

## 验证记录

本批已经使用固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

结果：

- `git diff --check` 通过。
- 项目 headless 启动通过，没有新增脚本解析错误。
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，没有新增 `TargetAoeHoverPresenter` preload 或调用错误。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB 泄漏提示和资源释放提示，本批没有改动这些旧问题。

手动回归时重点看：

- 选中伤害卡、治疗卡、建造卡和地形升降卡后 hover 地块。
- 合法目标是否仍显示合法高亮。
- 非法目标是否仍显示非法高亮。
- AOE 范围是否仍跟随中心地块变化。
- tooltip 是否仍跟随中心地块刷新。
