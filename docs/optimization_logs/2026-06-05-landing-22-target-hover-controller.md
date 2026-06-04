# 第 22 批落地：目标 Hover 控制器拆分

日期：2026-06-05

## 本次目标

本次继续拆 `hex_map.gd`，目标是把 `_update_highlight()` 里的“卡牌目标 hover 编排”抽出去。旧逻辑同时做了几件事：清理失效 hover 地块、读取当前选中卡牌、读取 MainBoard、判断当前鼠标最前景地块、触发 AOE 高亮、触发动态遮挡、无卡牌时隐藏 tooltip。它不算特别长，但已经把输入状态、UI 查找和地图表现触发揉在一起。

这次只拆编排，不改卡牌范围规则、不改 shader 表现、不改 tooltip 文案，不改变玩家 hover 行为。

## 修改文件

### `scene/in_scene/hex_map_modules/input/TargetHoverController.gd`

新增目标 hover 控制器，主要职责是：

- 接收 `hovered_stacks` 和 `active_stack`。
- 清理已经释放的 hover 地块。
- 从当前 hover 地块里按旧规则选择 `global_position.y` 最大的前景地块。
- 当前没有选中卡牌时，触发 HexMap 注入的 AOE 清理、遮挡清理，并隐藏 MainBoard 的 `cursor_tooltip`。
- 当前 hover 中心变化时，触发 HexMap 注入的 `_update_aoe_display()` 和 `_update_occlusion()`。
- 返回需要 HexMap 回写的 `hovered_stacks` 与 `active_stack`。

这个模块不直接读取：

- `stack_nodes`
- `map_data`
- `CardManager` 路径
- `MainBoard` 路径
- `HexMapVisualStatePresenter`
- `HexTargetRules`

它只消费 HexMap 传入的回调，所以后续移动 CardManager 查找或 AOE 表现时，不需要再改 hover 中心选择逻辑。

### `scene/in_scene/hex_map.gd`

本次接入方式如下：

- 新增 `TARGET_HOVER_CONTROLLER` preload。
- 新增 `_target_hover_controller` 实例。
- `_update_highlight()` 保留旧入口名，但内部只调用 `TargetHoverController.update()`。
- 新增 `_build_target_hover_controller_config()`，集中注入当前选中卡牌读取、MainBoard 读取、AOE 清理、遮挡清理、AOE 更新和遮挡更新回调。
- 新增 `_get_current_selected_card_for_hover()`，继续复用旧 `get_card_manager()`。
- 新增 `_get_main_board_for_hover()`，继续复用旧 MainBoard group 查询。
- 新增 `_apply_target_hover_controller_result()`，只负责把控制器返回的状态写回 `hovered_stacks` 和 `active_stack`。

## 导出变量与可调参数

本次没有新增导出变量，也没有移动任何 Inspector 可调项。

仍然留在 `hex_map.gd` 中调节的相关表现参数包括：

- 普通地块视觉状态参数。
- 动态遮挡参数。
- 卡牌目标范围规则所需的地图上下文。
- MainBoard tooltip 的具体表现。

## 行为边界

本次保持以下行为不变：

- 未选中卡牌时，不触发 AOE 高亮和动态遮挡。
- 未选中卡牌时，会清理已有 AOE、清理遮挡，并隐藏目标 tooltip。
- 多个地块同时 hover 时，仍然选择屏幕 y 值最大的地块作为当前前景目标。
- 当前前景目标没有变化时，不重复刷新 AOE 和遮挡。
- AOE 范围计算仍由 `HexTargetRules` 和 `hex_map.gd::_update_aoe_display()` 处理。
- 目标是否合法仍由 `hex_map.gd::_is_stack_valid_target()` 处理。

## 后续仍需拆分

`_update_highlight()` 本身已经变薄，但目标 hover 链路还没有完全拆干净。后续可以继续拆：

- `_update_aoe_display()` 中的卡牌范围收集、目标合法性判断和 tooltip 更新。
- CardManager 多路径查找，建议落到独立 `CardManagerLocator.gd`。
- MainBoard tooltip 更新接口，后续可以由 UI controller 接管，HexMap 只发目标 hover 事件。

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
- `res://scene/in_scene/in_scene.tscn` headless 启动通过，没有新增 `TargetHoverController` preload 或调用错误。
- 输出里仍有既有 TileSet atlas 报错、ObjectDB 泄漏提示和资源释放提示，本批没有改动这些旧问题。

手动回归时重点看：

- 选中卡牌后 hover 地块，AOE 是否仍跟随前景地块变化。
- hover 非法目标时，非法目标灰边是否仍出现。
- hover 合法目标时，合法目标金边是否仍出现。
- 右键取消卡牌后，AOE、遮挡和 tooltip 是否清理。
- 敌人意图 hover 和结算奖励 hover 是否没有被普通卡牌 hover 打断。
