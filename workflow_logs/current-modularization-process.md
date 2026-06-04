# 模块化解耦流程归档

日期：2026-06-05

## 这份归档的用途

这份文件保存前面 HexMap 解耦过程的中间信息，以及后续继续拆 `in_scene.gd` 和其他大文件时需要遵守的流程。它不是最终说明文档。最终给组员和 AI 快速阅读的文档放在 `docs/` 下，中间过程都集中放在这里。

当前最终说明：

```text
docs/hex-map-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
```

## 已完成的 HexMap 解耦阶段

### 第一阶段：清理低风险兜底和重复路径

处理目标：

- 先减少明显重复的资源加载、信号连接和旧路径兜底。
- 不改变战斗规则、地图生成、时间轴结算和奖励行为。

形成的经验：

- 不要为了“看起来干净”删除跨场景生命周期保护。
- `is_instance_valid()`、meta 清理、切场景失败恢复仍然有价值。
- 可删的是重复查找和已经被统一入口替代的 fallback。

### 第二阶段：抽地图规则

已拆模块：

```text
scene/in_scene/hex_map_modules/rules/HexCoordRules.gd
scene/in_scene/hex_map_modules/rules/HexTerrainRules.gd
scene/in_scene/hex_map_modules/rules/HexTargetRules.gd
scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd
```

已完成职责：

- 坐标换算和邻接规则离开 `hex_map.gd`。
- 地形高度、地形类型和基础地形判定离开 `hex_map.gd`。
- 卡牌目标合法性和时间轴命令最终目标校验有了统一入口。

后续提醒：

- 新卡牌目标限制优先进入 rules 模块。
- Timeline command 不要重新写一套和地图 hover 不一致的校验。

### 第三阶段：抽地块表现和输入

已拆模块：

```text
scene/in_scene/hex_map_modules/presenters/HexMapCollisionPresenter.gd
scene/in_scene/hex_map_modules/presenters/HexMapVisualStatePresenter.gd
scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd
scene/in_scene/hex_map_modules/input/HexMapInputCoordinator.gd
scene/in_scene/hex_map_modules/input/TargetHoverController.gd
scene/in_scene/hex_map_modules/ui/TargetSelectionTooltipAdapter.gd
```

已完成职责：

- 地块碰撞和输入开关集中管理。
- 普通 hover、选中、AOE hover 的视觉状态由 presenter 负责。
- `HexMap` 不再自己决定 MainBoard tooltip 旧接口怎么调用。

后续提醒：

- 表现层不要改 `map_data`。
- 输入模块不要直接查 CardManager。
- MainBoard 旧接口未来可以继续收口，但不要和战斗规则一起改。

### 第四阶段：抽敌人意图、奖励、高度视图

已拆模块：

```text
scene/in_scene/hex_map_modules/presenters/EnemyIntentMapPresenter.gd
scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd
scene/in_scene/hex_map_modules/rewards/SettlementRewardController.gd
scene/in_scene/hex_map_modules/height_view/HeightViewIndicatorPresenter.gd
scene/in_scene/hex_map_modules/height_view/HeightViewStateSynchronizer.gd
scene/in_scene/hex_map_modules/height_view/HeightViewMapTransitionRunner.gd
```

已完成职责：

- 敌人意图来源和目标高亮由专门 presenter 处理。
- 结算奖励的 hover、tooltip、可点击状态和 used 状态扫描拆开。
- 高度视图的光柱、数字标签、平铺/3D 状态同步和整图恢复循环离开主脚本。

后续提醒：

- 奖励状态涉及跨场景和 UI 回调，不能随便删兜底。
- 高度视图修改时必须检查普通视图恢复、血条位置和外部渲染节点。

### 第五阶段：抽地块生命周期

已拆模块：

```text
scene/in_scene/hex_map_modules/factory/TileStackFactory.gd
scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd
scene/in_scene/hex_map_modules/factory/TileStackInitializationService.gd
scene/in_scene/hex_map_modules/factory/TileStackRebuildService.gd
scene/in_scene/hex_map_modules/elevation/TileElevationService.gd
scene/in_scene/hex_map_modules/destruction/TileDestructionBatchQueue.gd
scene/in_scene/hex_map_modules/destruction/TileDestructionMutationService.gd
```

已完成职责：

- 地块 stack 基础节点创建、地貌挂接、metadata 收尾分离。
- 单格重建不再直接散落在主脚本。
- 地块升降和销毁有独立服务处理数据、动画和节点清理。

后续提醒：

- 改 stack metadata 时必须看 `docs/hex-map-ultimate-operation-guide.md`。
- 新增外部渲染节点时走 registrar，不要自己塞进 metadata。

### 第六阶段：抽跨系统桥接

已拆模块：

```text
scene/in_scene/hex_map_modules/bridges/CardManagerLocator.gd
scene/in_scene/hex_map_modules/bridges/HexMapSceneBridge.gd
scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd
scene/in_scene/hex_map_modules/registrars/RuntimeLandformRegistrar.gd
scene/in_scene/hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd
scene/in_scene/hex_map_modules/generation/MapGenerationService.gd
scene/in_scene/hex_map_modules/generation/LandformPlacementService.gd
scene/in_scene/hex_map_modules/runners/MapIntroRevealRunner.gd
```

已完成职责：

- CardManager 查找顺序集中，失效 meta 清理保留。
- 局内场景节点路径集中到 bridge。
- 地貌/建筑回合行为执行顺序独立。
- 运行时地貌、外部渲染节点、地图生成、地貌投放和入场动画都离开主脚本。

后续提醒：

- `HexMap` 现在仍是地图 composition root，不应该继续机械拆所有函数。
- 如果再拆 HexMap，优先看跨系统接口是否还能更薄，而不是盯着行数。

## 当前大文件体量观察

最近一次静态统计：

| 文件 | 大约行数 | 处理优先级 |
| --- | ---: | --- |
| `scene/in_scene/hex_map.gd` | 2093 | 已进入维护阶段，只做必要优化。 |
| `scene/in_scene/in_scene.gd` | 1926 | 下一轮首要拆分目标。 |
| `scene/in_scene/DragShapeController.gd` | 1213 | `in_scene.gd` 第一轮稳定后处理。 |
| `scene/in_scene/timeline/timeline_ui.gd` | 967 | 时间轴 UI 表现层后续拆。 |
| `scene/in_scene/rewards/CraftReward.gd` | 939 | 奖励系统后续拆。 |
| `scene/out_scene/out_scene_map_exp.gd` | 853 | 局外地图主控后续拆。 |
| `scene/in_scene/rewards/ShopManager.gd` | 802 | 奖励/商店后续拆。 |
| `scene/in_scene/tile.gd` | 707 | 地块节点脚本后续审查。 |
| `scene/card/custom_card.gd` | 654 | 卡牌表现和数据绑定后续审查。 |
| `scene/in_scene/timeline/TimelineManager.gd` | 517 | 规则核心可继续收口，但不急。 |

## in_scene.gd 当前职责观察

`in_scene.gd` 现在是局内场景的大主控，主要职责包括：

- 创建 CardManager、Hand、Deck、Discard 和 CardFactory。
- 刷新牌堆/弃牌 UI，打开牌堆查看器。
- 处理首回合自动开始、战斗 UI 入场、时间轴入场。
- 控制回合推进、抽牌、弃牌、输入开关。
- 处理卡牌 tooltip 和鼠标 cursor tooltip。
- 处理卡牌目标合法性和 MainBoard hover 适配。
- 处理战斗胜利、失败、结算奖励、奖励按钮和外部奖励场景。
- 构造局内返回局外的 payload。
- 切换场景并处理失败恢复。
- 同步 GlobalClock 的时代和阶段进度。
- 结算阶段回收运行时卡牌到牌堆。

它后续应该收敛为：

```text
局内场景 composition root
```

也就是它可以知道有哪些核心系统存在，但不应该继续亲自实现所有系统内部流程。

## in_scene.gd 推荐拆分顺序

第一轮低风险：

1. `InSceneNodeBridge.gd`：集中节点路径。
2. `InSceneGlobalClockBridge.gd`：集中 GlobalClock 连接和时代同步。
3. `CardPileUiController.gd`：集中牌堆数量、按钮和查看器。

第二轮中风险：

4. `CardSystemBootstrap.gd`：拆 CardManager/Hand/Deck/Discard 初始化。
5. `FirstTurnIntroRunner.gd`：拆首回合等待、时间轴入场和敌人意图初始刷新。
6. `InSceneInputLockController.gd`：拆输入禁用和恢复。

第三轮高风险：

7. `SettlementRewardSceneController.gd`：拆奖励页面打开、缓存和回调。
8. `SettlementDeckReclaimService.gd`：拆结算阶段卡牌回收。
9. `InSceneSceneSwitcher.gd` 和 `CombatReturnPayloadBuilder.gd`：拆切场景和返回局外 payload。
10. `CombatResultController.gd`：拆胜利、失败和结算状态。

每一轮最多处理 1 到 2 个文件，除非新模块必须配套创建。

## 后续所有拆分都遵守的流程

### 先分析

在动代码前，先用文字列清：

```text
目标函数范围：
当前读写的成员变量：
当前触碰的外部节点：
当前触碰的 autoload：
新模块输入：
新模块输出：
保留的旧公共入口：
本批回归路径：
```

### 再列 list

把待拆项按风险排序：

```text
低风险：纯查找、纯展示、纯数据构造。
中风险：初始化流程、输入锁、UI 显隐。
高风险：战斗结算、切场景、跨场景 payload、卡牌生命周期。
```

### 再一步步拆

每批只做一个风险面：

```text
新增模块
-> HexMap 或 InScene 保留旧入口
-> 旧入口转发给新模块
-> 清理本批造成的重复代码
-> 补中文注释
-> 更新当前流程归档
-> 验证
-> commit
```

### 最后更新最终说明

阶段结束后，把本阶段稳定结论写入 `docs/ai-handoff-ultimate-operation-guide.md` 或对应系统的终极说明里。

不要在 `docs/` 里继续新增大量按日期命名的 landing 文档。

## 验证命令

代码改动后运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

只改 Markdown 时运行：

```powershell
git diff --check
```

## 文档清理规则

已经决定：

- `docs/optimization_logs/` 不再作为持续堆放 landing log 的目录。
- 历史 landing log 的要点已经压缩进本文件。
- `docs/` 只保留最新版总结性说明。
- 后续每个系统如果需要最终说明，文件名使用清晰稳定名称，不再按每批日期累加。

推荐命名：

```text
docs/hex-map-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
docs/in-scene-ultimate-operation-guide.md
```

中间记录继续写：

```text
workflow_logs/current-modularization-process.md
```

## 接力提示词

可以把这段给下一位 AI：

```text
请继续 D:/godot/时之钥/时之钥 的模块化解耦。先读取 AGENTS.md、docs/ai-handoff-ultimate-operation-guide.md、docs/hex-map-ultimate-operation-guide.md、workflow_logs/current-modularization-process.md。先分析目标文件，再列待拆清单，最后每批只拆一个风险面。优先处理 scene/in_scene/in_scene.gd，从节点桥接、GlobalClock 桥接或牌堆 UI 控制这种低风险模块开始。新增模块写中文注释，Markdown 用中文自然语言。docs 目录只保留最新版总结性说明，中间流程写 workflow_logs/current-modularization-process.md。每批改完运行 git diff --check 和 Godot headless 检查，并单独 commit。
```

## in_scene.gd 第一批低风险拆分记录

日期：2026-06-05

### 本批目标

本批只处理低风险桥接和 UI 转发，不改卡牌生命周期、回合结算、胜负流程和切场景流程。

目标函数范围：

```text
_ready() 的节点解析入口
_connect_global_clock_progress_signal()
_pull_era_from_global()
_push_era_to_global()
_refresh_combat_cartoon_ui_progress()
_advance_global_phase()
update_counts_and_ui()
_on_deck_button_gui_input()
_on_discard_button_pressed()
_open_deck_pile_viewer()
```

当前读写的成员变量：

```text
deck_button / discard_button / deck_count_label / discard_count_label
shop_button / acquire_reward_button / remove_reward_button / craft_reward_button
lose_button / win_debug_button / combat_victory_debug_button / game_over_ui
hex_map / timecoin_container
end_turn_button / end_combat_button / height_view_toggle_button / cursor_tooltip
timeline_ui / timeline_manager / dim / win / total_enemy_health_bar
combat_victory_banner / combat_cartoon_ui
current_era_value / current_deck_count / current_discard_count
is_processing_deck / current_battle_state
```

当前触碰的外部节点和 autoload：

```text
ui/Main 周边固定 UI 节点
map/HexMap
CartoonUI
combat_victory_banner
TimecoinContainer
GlobalClock
MapState
```

### 新增模块

```text
scene/in_scene/in_scene_modules/bridges/InSceneNodeBridge.gd
scene/in_scene/in_scene_modules/bridges/InSceneGlobalClockBridge.gd
scene/in_scene/in_scene_modules/cards/CardPileUiController.gd
```

模块边界：

- `InSceneNodeBridge.gd` 只集中节点路径查找，不缓存玩法状态，不修改场景树。
- `InSceneGlobalClockBridge.gd` 只集中 `GlobalClock` 信号连接、时代拉取、时代回写和阶段推进后的存档同步。
- `CardPileUiController.gd` 只集中牌堆数量刷新、抽牌堆按钮动作判定、抽牌堆/弃牌堆查看器打开。它不移动卡牌、不洗牌、不决定战斗阶段。

保留的旧公共入口：

```text
update_counts_and_ui()
_on_deck_button_gui_input(event)
_on_discard_button_pressed()
_open_deck_pile_viewer()
_pull_era_from_global()
_push_era_to_global()
_advance_global_phase()
```

这些函数仍由 `in_scene.gd` 暴露，内部转发给新模块，避免影响已有调用方。

### 本批删除或收口的重复点

删除原因：

```text
ui/Main 里分散的 @onready 硬编码节点路径，已经由 InSceneNodeBridge 统一维护。
牌堆计数和查看器打开逻辑，已经由 CardPileUiController 统一维护。
GlobalClock 时代读写和阶段存档同步，已经由 InSceneGlobalClockBridge 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 时，修复过新模块的 warning-as-error 和 class_name 时序问题；最终筛选未再出现 SCRIPT ERROR、Parse Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些在前序文档中已记录，不作为本批新增问题处理。
```

### 下一批建议

下一批仍建议保持低到中风险，不要直接拆胜负或切场景：

1. `CardSystemBootstrap.gd`：拆 `setup_card_system()` 中 CardManager、Hand、Deck、Discard 和 CardFactory 初始化，保留 `manager_instance`、`player_hand`、`deck_pile`、`discard_pile` 等旧变量。
2. 或 `InSceneInputLockController.gd`：拆 `disable_player_inputs()` 与 `enable_player_inputs()`，输入锁范围清晰，验证路径短。
3. 暂缓 `SettlementDeckReclaimService.gd`、`InSceneSceneSwitcher.gd` 和 `CombatResultController.gd`，这些涉及跨场景生命周期和结算状态，等前两批稳定后再动。

## in_scene.gd 第二批三模块拆分记录

日期：2026-06-05

### 本批目标

本批一次拆 3 个低到中风险模块，但继续避开胜负结算、切场景、奖励页关闭和结算阶段卡牌回收。

目标函数范围：

```text
setup_card_system()
disable_player_inputs()
enable_player_inputs()
_schedule_auto_first_turn()
_start_first_turn_after_timeline_ready()
_wait_for_card_system_ready()
_play_timeline_intro_if_visible()
_wait_for_battle_intro_ui()
_is_any_battle_intro_ui_running()
```

当前读写的成员变量：

```text
manager_instance / player_hand / deck_pile / discard_pile
_card_system_ready
_first_turn_started / _first_turn_starting
deck_button / discard_button / end_turn_button
shop_button / acquire_reward_button / remove_reward_button / craft_reward_button
timeline_ui / timeline_manager
combat_cartoon_ui / total_enemy_health_bar
```

当前触碰的外部节点和 autoload：

```text
CardManager / CardFactory / Hand / Pile
GlobalDB.player_deck
HexMap.map_intro_reveal_finished
TimelineUI / TotalEnemyHealthBar / CartoonUI 的入场动画接口
```

### 新增模块

```text
scene/in_scene/in_scene_modules/cards/CardSystemBootstrap.gd
scene/in_scene/in_scene_modules/ui/InSceneInputLockController.gd
scene/in_scene/in_scene_modules/turn/FirstTurnIntroRunner.gd
```

模块边界：

- `CardSystemBootstrap.gd` 只负责创建 `CardManager`、`Hand`、`DeckPile`、`DiscardPile`、设置默认卡牌场景、生成初始牌堆和连接稳定 UI 按钮。它不抽牌、不洗牌、不处理弃牌效果。
- `InSceneInputLockController.gd` 只执行输入锁定/解锁，不决定什么时候锁输入。
- `FirstTurnIntroRunner.gd` 只处理首回合启动前的等待条件和入场 UI 动画，不推进回合规则，不生成敌人意图。

保留的旧公共入口：

```text
setup_card_system()
disable_player_inputs()
enable_player_inputs()
_schedule_auto_first_turn()
_start_first_turn_after_timeline_ready(force_timeline_visible)
_wait_for_card_system_ready()
_play_timeline_intro_if_visible()
_wait_for_battle_intro_ui()
_is_any_battle_intro_ui_running()
```

这些函数仍由 `in_scene.gd` 暴露，内部转发给新模块，避免影响教程导演、信号回调和旧按钮连接。

### 本批删除或收口的重复点

删除原因：

```text
setup_card_system() 中的 CardManager/Hand/Pile 创建和按钮连接已由 CardSystemBootstrap 统一维护。
disable_player_inputs()/enable_player_inputs() 的 UI 状态写入已由 InSceneInputLockController 统一维护。
首回合等待和入场 UI 动画轮询已由 FirstTurnIntroRunner 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批可以继续拆中风险但较独立的 UI 模块：

1. `CursorTooltipController.gd`：拆 `_enhance_cursor_tooltip()`、`set_cursor_tooltip_position()` 和 `_refresh_cursor_tooltip_size()`。
2. `CardTooltipUiAdapter.gd`：拆 `setup_tooltip_ui()`、`show_tooltip()`、`hide_tooltip()` 中与 `CardTooltipPresenter` 的连接。
3. 或 `InSceneUiVisibilityController.gd`：拆 `hide_ui_for_external_scene()`、`restore_ui_after_external_scene()`、`restore_all_ui()`，但这会触碰奖励页和结算阶段，建议单独一批。

仍建议暂缓：

```text
_return_to_out_scene()
_switch_scene_with_data()
_open_settlement_reward_scene()
_reclaim_all_runtime_cards_to_deck()
_on_combat_victory_triggered()
```

## in_scene.gd 第三批三模块拆分记录

日期：2026-06-05

### 本批目标

本批一次拆 3 个低到中风险 UI 模块，继续避开切场景、胜负结算、奖励消费和运行时卡牌回收。
目标函数范围：

```text
_setup_card_tooltip_presenter()
setup_tooltip_ui()
show_tooltip(card)
hide_tooltip(card)
_enhance_cursor_tooltip()
_on_cursor_tooltip_visibility_changed()
set_cursor_tooltip_position(position)
_refresh_cursor_tooltip_size()
hide_ui_for_external_scene()
_set_single_health_bars_visible(is_visible)
_hide_debug_buttons_for_resolution()
_refresh_height_view_toggle_button_after_external_scene()
restore_ui_after_external_scene()
restore_all_ui()
_hide_settlement_buttons()
_show_settlement_buttons()
_hide_combat_phase_ui_for_settlement()
```

当前读写的成员变量：

```text
card_tooltip_presenter / tooltip_config
cursor_tooltip / cursor_tooltip_panel
cursor_tooltip_min_width / cursor_tooltip_max_width / cursor_tooltip_min_height
player_hand / deck_pile / discard_pile
deck_button / discard_button / end_turn_button / end_combat_button / height_view_toggle_button
timeline_ui / hex_map / timecoin_container / total_enemy_health_bar
shop_button / acquire_reward_button / remove_reward_button / craft_reward_button
win_debug_button / combat_victory_debug_button / lose_button
current_battle_state / show_settlement_debug_buttons / _resolution_hides_debug_buttons
```

当前触碰的外部节点和接口：

```text
CardTooltipPresenter
CardManager.current_selected_card
CursorTooltip RichTextLabel
HexMap/BarManager.set_all_health_bars_visible()
TimelineUI.clear_grid_preview()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/ui/CardTooltipUiAdapter.gd
scene/in_scene/in_scene_modules/ui/CursorTooltipController.gd
scene/in_scene/in_scene_modules/ui/InSceneUiVisibilityController.gd
```

模块边界：

- `CardTooltipUiAdapter.gd` 只负责连接 InScene 与共享 `CardTooltipPresenter`，保留旧的 `card_manager` 查找顺序和“选中卡牌不抢 tooltip”规则。
- `CursorTooltipController.gd` 只负责光标提示框的 Panel 包装、定位和尺寸同步，不写提示文本，不参与目标判定。
- `InSceneUiVisibilityController.gd` 只执行局外场景和结算阶段的 UI 显隐，不切换场景，不消费奖励，不改变战斗阶段。

保留的旧公共入口：

```text
setup_tooltip_ui()
show_tooltip(card)
hide_tooltip(card)
set_cursor_tooltip_position(position)
hide_ui_for_external_scene()
restore_ui_after_external_scene()
restore_all_ui()
_hide_debug_buttons_for_resolution()
_hide_settlement_buttons()
_show_settlement_buttons()
_hide_combat_phase_ui_for_settlement()
```

这些函数仍由 `in_scene.gd` 暴露，内部转发给新模块，避免影响卡牌 hover、结算按钮、外部奖励页退出和旧信号回调。

### 本批删除或收口的重复点

删除原因：

```text
卡牌 tooltip presenter 的创建、展示、隐藏连接已由 CardTooltipUiAdapter 统一维护。
光标 tooltip 的外框构建、尺寸刷新和定位已由 CursorTooltipController 统一维护。
局外场景、结算按钮、血条和调试按钮的显隐执行已由 InSceneUiVisibilityController 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议从“结算期卡牌回收”和“场景切换”之间二选一，但不要混拆：

1. `SettlementDeckReclaimService.gd`：拆 `_prepare_deck_button_for_settlement()`、`_snapshot_current_deck_for_settlement()`、`_reclaim_all_runtime_cards_to_deck()` 这一组，但只做卡牌回收到牌堆，不碰胜负触发。
2. 或 `InSceneSceneReturnController.gd`：拆 `_build_combat_return_payload()` 与 `_push_era_to_global()` 周边的 payload 组装，但暂缓真正 `_switch_scene_with_data()`。
3. 继续暂缓 `_on_combat_victory_triggered()`、`_on_defeat_triggered()`、`_open_settlement_reward_scene()`，这些会同时牵动奖励消费、状态迁移和外部场景生命周期。

## in_scene.gd 第四批结算牌堆回收拆分记录

日期：2026-06-05

### 本批目标

本批一次拆 3 个结算牌堆相关模块，只处理“进入结算时把运行时卡牌无动画整理回抽牌堆”这一条链路。
刻意不触碰胜负触发、奖励页打开、奖励消费、局外返回和场景切换。

目标函数范围：

```text
_snapshot_current_deck_for_settlement()
_reclaim_all_runtime_cards_to_deck()
_collect_cards_for_settlement_reclaim()
_get_runtime_card_containers()
_append_runtime_container()
_append_reclaim_card()
_collect_loose_runtime_cards()
_move_cards_to_deck_without_animation()
_silent_add_card_to_deck()
_prepare_card_for_silent_deck_reclaim()
_sync_deck_cards_after_silent_reclaim()
_reset_drag_controller_for_settlement()
_has_runtime_property()
```

当前读写的成员变量：

```text
manager_instance / player_hand / discard_pile / deck_pile
is_processing_deck
deck_button / discard_button
```

当前触碰的外部节点和接口：

```text
GlobalDB.player_deck
MapState.set_saved_deck()
CardManager.deselect_card()
CardContainer._held_cards
Card.card_container / show_front / can_be_interacted_with
DragShapeController 运行时属性
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/SettlementDeckSnapshotService.gd
scene/in_scene/in_scene_modules/settlement/SettlementRuntimeCardCollector.gd
scene/in_scene/in_scene_modules/settlement/SettlementDeckReclaimService.gd
```

模块边界：

- `SettlementDeckSnapshotService.gd` 只保存当前 `GlobalDB.player_deck` 到 `MapState`，不移动卡牌、不刷新 UI。
- `SettlementRuntimeCardCollector.gd` 只从手牌、弃牌堆、抽牌堆、CardManager 容器和当前场景散落卡牌中收集需要回收的运行时卡牌。
- `SettlementDeckReclaimService.gd` 只执行无动画回收、卡牌视觉复位、抽牌堆洗牌和拖拽状态清理，不处理胜负、奖励页或按钮显示。

保留的旧公共入口：

```text
_prepare_deck_button_for_settlement(reclaim_runtime_cards)
_snapshot_current_deck_for_settlement()
_reclaim_all_runtime_cards_to_deck()
```

这些函数仍由 `in_scene.gd` 暴露，内部转发给结算模块。`in_scene.gd` 继续负责 `is_processing_deck`、按钮显隐和 `update_counts_and_ui()`，避免结算服务反向接管主控 UI 状态。

### 本批删除或收口的重复点

删除原因：

```text
运行时卡牌收集逻辑已由 SettlementRuntimeCardCollector 统一维护。
无动画移动、卡牌视觉复位、抽牌堆同步和拖拽状态清理已由 SettlementDeckReclaimService 统一维护。
当前牌组快照写入已由 SettlementDeckSnapshotService 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议转向场景返回链路，但继续避免一次性搬完整切场景生命周期：

1. `InSceneReturnPayloadBuilder.gd`：先拆 `_build_combat_return_payload()`，只组装时代、时间币、牌组快照和房间上下文。
2. `InScenePayloadApplier.gd`：可拆 `_apply_payload_to_new_scene_before_tree()` 与 `apply_external_event()` 的 payload 写入/解析，但不要同时改 HexMap。
3. 暂缓 `_switch_scene_with_data()` 整体搬迁，等 payload 和日志/失败恢复先拆干净后再动 current_scene 生命周期。

## in_scene.gd 第五批场景 payload 拆分记录

日期：2026-06-05

### 本批目标

本批一次拆 3 个场景 payload 相关模块，只处理“数据如何组装、解析和转发”。
刻意不整体搬迁 `_switch_scene_with_data()`，也不改变黑幕过渡、current_scene 替换、旧场景释放和奖励页生命周期。

目标函数范围：

```text
_build_combat_return_payload()
_apply_payload_to_new_scene_before_tree(next_scene, payload)
apply_external_event(payload)
_apply_incoming_payload_to_hex_map()
```

当前读写的成员变量：

```text
incoming_external_payload / incoming_battle_tag / incoming_map_seed
current_era_value
hex_map
```

当前触碰的外部节点和接口：

```text
MapState.get_active_room_context()
GlobalTimecoin.get_timecoins()
GlobalDB.player_deck
next_scene.apply_external_event(payload)
hex_map.apply_external_event(payload_text)
SceneLog.scene_event() / SceneLog.error_event()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd
scene/in_scene/in_scene_modules/scene_flow/InSceneExternalPayloadParser.gd
scene/in_scene/in_scene_modules/scene_flow/InScenePayloadBridge.gd
```

模块边界：

- `InSceneReturnPayloadBuilder.gd` 只组装“局内 -> 局外”的返回 payload，不写 `MapState.pending_room_resolution`，不切场景。
- `InSceneExternalPayloadParser.gd` 只解析局外传入的文本 payload，保留 `battle_tag` 和 `map_seed` 的旧拆分协议。
- `InScenePayloadBridge.gd` 只把 payload 转交给新场景或 `HexMap`，不解析、不创建、不销毁场景节点。

保留的旧公共入口：

```text
_build_combat_return_payload()
_apply_payload_to_new_scene_before_tree(next_scene, payload)
apply_external_event(payload)
_apply_incoming_payload_to_hex_map()
```

这些函数仍由 `in_scene.gd` 暴露，内部转发给 `scene_flow` 模块。教程场景和局外场景仍可沿用旧的 `apply_external_event()` 协议。

### 本批删除或收口的重复点

删除原因：

```text
返回 payload 字段组装已由 InSceneReturnPayloadBuilder 统一维护。
局外传入 payload 的 battle_tag/map_seed 解析已由 InSceneExternalPayloadParser 统一维护。
payload 转发到新场景或 HexMap 的桥接已由 InScenePayloadBridge 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批可以继续沿着场景返回链路向外拆，但仍建议一批只动 3 到 4 个风险面：

1. `InSceneSceneSwitchLoader.gd`：拆 `_load_packed_scene_for_switch()`、`_log_scene_switch_error()`、`_recover_dim_after_failed_switch()`，只处理加载失败和错误记录。
2. 或 `SettlementRewardSceneController.gd`：拆 `_open_settlement_reward_scene()`、`_get_settlement_reward_scene()`、四个奖励按钮入口，但不要同时拆 `_on_external_scene_exit_pressed()` 的奖励消费。
3. 暂缓 `_return_to_out_scene()` 和 `_switch_scene_with_data()` 整体搬迁，等 loader / payload / reward 打开链路都独立后再处理。

## in_scene.gd 第六批结算奖励页打开拆分记录

日期：2026-06-05

### 本批目标

本批只拆结算奖励页“打开”链路，不拆退出回调里的奖励消费。
刻意不触碰 `_on_external_scene_exit_pressed()`、`_consume_settlement_reward_context()`、胜负触发和局外返回。

目标函数范围：

```text
_open_settlement_reward_scene(reward_type, reward_context)
_get_settlement_reward_scene(scene_path)
_on_shop_button_pressed()
_on_acquire_reward_button_pressed()
_on_remove_reward_button_pressed()
_on_craft_reward_button_pressed()
_on_settlement_reward_requested(reward_info)
```

当前读写的成员变量：

```text
_settlement_reward_scene_cache
active_settlement_reward_context
manager_instance
```

当前触碰的外部节点和接口：

```text
reward_scene_close_requested
set_deck_manager(manager_instance)
open_shop()
open()
settlement_reward_context / settlement_reward_committed / settlement_reward_consume_on_exit meta
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/SettlementRewardSceneController.gd
```

模块边界：

- `SettlementRewardSceneController.gd` 只负责加载、缓存、实例化和打开奖励页。
- 它会连接奖励页关闭信号、写入奖励上下文 meta、注入 `deck_manager`，并调用 `open_shop()` 或 `open()`。
- 它不判断战斗阶段、不隐藏主 UI、不保存 `active_settlement_reward_context`、不消费奖励建筑。

保留的旧公共入口：

```text
_open_settlement_reward_scene(reward_type, reward_context)
_get_settlement_reward_scene(scene_path)
```

这些函数仍由 `in_scene.gd` 暴露。主脚本继续负责阶段检查、未知类型/加载失败提前返回、隐藏 UI 和记录 active context，保证失败时不提前隐藏 UI。

### 本批删除或收口的重复点

删除原因：

```text
奖励场景加载缓存已由 SettlementRewardSceneController.get_reward_scene() 统一维护。
奖励页实例化、关闭信号连接、meta 写入、deck_manager 注入和 open/open_shop 调用已由 SettlementRewardSceneController.open_reward_scene() 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议继续沿着场景切换链路拆 loader/错误恢复，或者拆奖励页退出链路，但不要混在一起：

1. `InSceneSceneSwitchLoader.gd`：拆 `_load_packed_scene_for_switch()`、`_log_scene_switch_error()`、`_recover_dim_after_failed_switch()`。
2. 或 `SettlementRewardExitController.gd`：拆 `_on_external_scene_exit_pressed()` 中“读取奖励页 meta、判断是否消费、queue_free、清 active context”的部分，但先不要改 HexMap 的奖励写回。
3. 暂缓 `_switch_scene_with_data()` 完整迁移，等 loader、payload、reward 打开和退出都独立后再处理。

## in_scene.gd 第七批切场加载器拆分记录

日期：2026-06-05

### 本批目标

本批只拆场景切换前的资源加载、失败恢复和错误记录。
刻意不触碰 `_switch_scene_with_data()` 中的新场景实例化、`current_scene` 替换、旧场景释放和 payload 注入。

目标函数范围：

```text
_load_packed_scene_for_switch(path, fail_message)
_recover_dim_after_failed_switch()
_log_scene_switch_error(message, extra)
```

当前读写的成员变量：

```text
dim
```

当前触碰的外部节点和接口：

```text
ResourceLoader.load(path, "PackedScene")
ResourceLoader.exists(path, "PackedScene")
DimMenu.use(1, 1)
SceneLog.error_event()
push_error()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchLoader.gd
```

模块边界：

- `InSceneSceneSwitchLoader.gd` 只负责加载 `PackedScene`、记录加载失败原因、在失败时恢复黑幕。
- 它不实例化新场景、不写 `get_tree().current_scene`，也不释放旧场景。
- `in_scene.gd` 继续保留旧 helper 名称，内部转发给 loader，避免 `_switch_scene_with_data()` 的生命周期逻辑在本批扩大改动。

### 本批删除或收口的重复点

删除原因：

```text
空路径检查、ResourceLoader 加载、resource_exists 诊断和 SceneLog/push_error 记录已由 InSceneSceneSwitchLoader 统一维护。
切场失败时的 DimMenu 黑幕恢复已由 InSceneSceneSwitchLoader 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议从两个方向二选一：

1. `SettlementRewardExitController.gd`：拆 `_on_external_scene_exit_pressed()` 中读取 meta、判断是否消费、隐藏/释放奖励页、清理 active context 的部分；`_consume_settlement_reward_context()` 仍先留在主脚本。
2. `InSceneSceneSwitchExecutor.gd`：在 payload、loader 都拆完后，可以把 `_switch_scene_with_data()` 的实例化、挂树、`current_scene` 替换和旧场景释放整体搬出，但这批风险更高，建议单独做。
3. 暂缓胜负触发函数 `_on_combat_victory_triggered()` 和 `_on_defeat_triggered()`，等奖励退出和切场 executor 稳定后再处理。

## in_scene.gd 第八批结算奖励页退出拆分记录

日期：2026-06-05

### 本批目标

本批只拆结算奖励页退出时的状态读取和页面关闭。
刻意不拆 `_consume_settlement_reward_context()`，也不改 HexMap 的奖励状态写回。

目标函数范围：

```text
_on_external_scene_exit_pressed(scene_instance)
```

当前读写的成员变量：

```text
active_settlement_reward_context
```

当前触碰的外部节点和接口：

```text
scene_instance.get_meta("settlement_reward_context")
scene_instance.get_meta("settlement_reward_committed")
scene_instance.get_meta("settlement_reward_consume_on_exit")
scene_instance.hide()
scene_instance.queue_free()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/SettlementRewardExitController.gd
```

模块边界：

- `SettlementRewardExitController.gd` 只读取奖励页 meta，判断是否需要消费奖励建筑，并关闭奖励页实例。
- 它不调用 `_consume_settlement_reward_context()`，不恢复主 UI，也不改变战斗阶段。
- `in_scene.gd` 继续负责根据返回结果决定是否通知 HexMap、恢复局外按钮和清理 active context。

### 本批删除或收口的重复点

删除原因：

```text
奖励页退出 meta 读取、should_consume_settlement_reward 判断、奖励页 hide/queue_free 已由 SettlementRewardExitController 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议二选一：

1. `InSceneSceneSwitchExecutor.gd`：拆 `_switch_scene_with_data()` 的实例化、payload 注入、挂树、`current_scene` 替换和旧场景释放。payload 与 loader 已经独立，风险比之前低。
2. 或 `SettlementRewardConsumer.gd`：拆 `_consume_settlement_reward_context()`，只负责把已确认使用的奖励建筑写回 HexMap；不要同时拆胜利/失败触发。
3. 暂缓 `_on_defeat_triggered()` 和 `_on_combat_victory_triggered()`，这两个函数仍牵动声音、UI、存档和结算状态。

## in_scene.gd 第九批切场执行器拆分记录

日期：2026-06-05

### 本批目标

本批只拆已经加载好 `PackedScene` 之后的真实切场执行。
前置的资源加载、失败恢复和 payload 解析已经在前面批次独立，本批不再扩大到 `_return_to_out_scene()`、胜负触发或奖励消费。

目标函数范围：

```text
_switch_scene_with_data(path, payload)
```

当前触碰的外部节点和接口：

```text
PackedScene.instantiate()
payload_bridge.apply_to_new_scene_before_tree(next_scene, payload)
get_tree().root.add_child(next_scene)
get_tree().current_scene = next_scene
old_scene.queue_free()
SceneLog.scene_event()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitchExecutor.gd
```

模块边界：

- `InSceneSceneSwitchExecutor.gd` 只执行已经加载好的场景切换。
- 它不加载 `PackedScene`、不恢复黑幕、不解析 payload。
- 它接收 `payload_bridge` 来完成新场景入树前的 payload 注入，避免重复知道 `apply_external_event()` 协议细节。

保留的旧公共入口：

```text
_switch_scene_with_data(path, payload)
```

`in_scene.gd` 仍保留这个入口，负责先调用 loader；如果加载失败，仍按旧逻辑恢复黑幕并返回。

### 本批删除或收口的重复点

删除原因：

```text
新场景实例化、payload 注入、root 挂载、current_scene 替换、旧场景释放已由 InSceneSceneSwitchExecutor 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批建议回到结算奖励消费或胜负流程，但仍保持单一风险面：

1. `SettlementRewardConsumer.gd`：拆 `_consume_settlement_reward_context()`，只负责把已确认使用的奖励建筑写回 HexMap。
2. 或 `CombatResultFlowController.gd`：先拆 `_on_combat_victory_triggered()` 中结算状态切换、隐藏战斗 UI、准备牌堆、清时间轴、显示结算按钮这一组。
3. 继续暂缓 `_on_defeat_triggered()`，它还牵动声音、GameOver UI、存档删除，适合最后单独拆。

## in_scene.gd 第十批结算奖励消费拆分记录

日期：2026-06-05

### 本批目标

本批只拆“奖励建筑已经被确认使用”这件事如何写回 `HexMap`。
不打开奖励页、不关闭奖励页、不恢复 UI，也不动胜负流程。

目标函数范围：

```text
_consume_settlement_reward_context(reward_context)
```

当前读写的成员变量：

```text
active_settlement_reward_context
hex_map
```

当前触碰的外部节点和接口：

```text
reward_context["stack"]
hex_map.mark_settlement_reward_used(stack)
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/SettlementRewardConsumer.gd
```

模块边界：

- `SettlementRewardConsumer.gd` 只负责验证奖励上下文和 `HexMap`，并调用 `mark_settlement_reward_used()`。
- 它不清理 `active_settlement_reward_context`，不恢复 UI，不知道奖励页实例。
- `in_scene.gd` 继续保留 `_consume_settlement_reward_context()` 入口，并负责清理 active context。

### 本批删除或收口的重复点

删除原因：

```text
奖励建筑 stack 校验和 HexMap 标记调用已由 SettlementRewardConsumer 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批可以开始处理结算胜利流程，但仍不要混入失败流程：

1. `CombatVictorySettlementController.gd`：拆 `_on_combat_victory_triggered()` 中“切换 SETTLEMENT、停 BGM、隐藏战斗 UI、准备牌堆、清时间轴、显示结算按钮”这一组。
2. 或先拆 `CombatDefeatFlowController.gd`，但它牵动 GameOver UI 和存档删除，风险略高。
3. `_return_to_out_scene()` 现在已经由 payload / loader / executor 支撑，可以稍后单独瘦身，但不建议和胜负流程同批处理。

## in_scene.gd 第十一批战斗胜利结算编排拆分记录

日期：2026-06-05

### 本批目标

本批只拆“战斗胜利后进入结算期”的场景编排。
不判断胜利条件、不进入最终胜利页、不处理失败结算，也不修改奖励页打开和退出流程。

目标函数范围：

```text
_on_combat_victory_triggered()
```

当前读写的成员变量：

```text
current_battle_state
timeline_manager
hex_map
total_enemy_health_bar
combat_victory_banner
```

当前触碰的外部节点和接口：

```text
SoundManager.stop_bgm()
disable_player_inputs()
timeline_manager.clear_grid()
hex_map.set_tiles_interactive(false)
hex_map.set_visuals_locked(true)
hex_map.update_all_stack_conditional_effects()
hex_map.enter_settlement_reward_mode(self)
combat_victory_banner.play_banner("战斗胜利")
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/CombatVictorySettlementController.gd
```

模块边界：

- `CombatVictorySettlementController.gd` 只负责胜利后进入结算期的动作顺序。
- 它不判断当前是否处于战斗期，状态守卫仍由 `in_scene.gd` 保留。
- 它通过 `Callable` 调回主脚本已有的输入锁、UI 隐藏、牌堆准备和结算按钮显示入口，避免重复知道这些模块内部细节。
- 它只锁定地图并进入结算奖励模式，不消费奖励、不切场。

### 本批删除或收口的重复点

删除原因：

```text
胜利结算中的 BGM 停止、战斗 UI 收口、时间轴清理、地图锁定、血条隐藏、胜利横幅和结算按钮显示，已经由 CombatVictorySettlementController 统一维护。
```

回归检查：

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

已知旧噪声：

```text
TileSet atlas 相关报错仍会在加载场景时大量输出。
ObjectDB / RID / resource 退出提示仍会出现。
这些仍按旧噪声处理。
```

### 下一批建议

下一批可以在结算区继续拆，但建议一次只碰一个入口：

1. `CombatDefeatFlowController.gd`：拆 `_on_defeat_triggered()` 中失败 UI、统计数据和删档动作，但需要特别确认 `Saver.Delete_save(0)` 的触发时机。
2. `GameWinFlowController.gd`：拆 `_on_win_button_button_down()` 中最终胜利页触发和音效，不要和战斗胜利结算混在一起。
3. `_return_to_out_scene()` 可以继续瘦身，但它已经由 payload / loader / executor 分担，优先级低于失败流。
