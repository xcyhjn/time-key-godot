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

## ShopManager.gd 第三批全局节点查找拆分记录

日期：2026-06-06

### 本批目标

本批只拆商店查找全局单例节点的桥接逻辑。它只负责按 autoload 名称、脚本路径和兼容节点名找到 `GlobalClock` 与 `GlobalTimecoin`；不读取时代值，不消费时间币，也不处理购买、刷新或升级。

目标函数范围：
```text
_find_global_clock()
_find_global_timecoin()
_find_node_with_script_recursive(root, script_name)
```

当前触碰的数据和接口：
```text
owner.has_node()
owner.get_node()
owner.get_tree().root
find_child()
get_script().resource_path
```

### 新增模块

```text
scene/in_scene/rewards/ShopGlobalNodeFinder.gd
```

模块边界：
- `ShopGlobalNodeFinder.gd` 只负责查找商店依赖的全局节点。
- 它不读取 `clock.era`，不调用 `consume_timecoins()`，也不处理商店业务流程。
- `ShopManager.gd` 保留 `_find_global_clock()`、`_find_global_timecoin()` 和 `_find_node_with_script_recursive()` 旧入口，内部转发给新模块，降低调用面变化。

### 本批删除或收口的重复点

删除原因：
```text
GlobalClock 与 GlobalTimecoin 的 autoload 查找、脚本递归查找和兼容命名查找现在由 ShopGlobalNodeFinder 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
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

## in_scene.gd 第十二批失败流和最终胜利页拆分记录

日期：2026-06-05

### 本批目标

本批拆两个相邻但互不混合的结算入口：

```text
_on_defeat_triggered()
_on_win_button_button_down()
```

它们都在结算按钮和全局信号附近，但职责不同：

- 失败流负责 GameOver UI、失败统计和删档。
- 最终胜利页负责最终胜利音效和胜利页触发。

本批不修改战斗胜利进入结算期的流程，不切换局外场景，也不修改奖励页。

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/CombatDefeatFlowController.gd
scene/in_scene/in_scene_modules/settlement/GameWinFlowController.gd
```

模块边界：

- `CombatDefeatFlowController.gd` 只编排失败后的 BGM 停止、调试按钮隐藏、战斗 UI 隐藏、失败统计生成、GameOver UI 启动和 `Saver.Delete_save(0)`。
- `GameWinFlowController.gd` 只编排最终胜利页的 BGM 停止、`game_win` 循环音效和 `win._on_victory_triggered()`。
- `in_scene.gd` 继续保留 `_on_defeat_triggered()`、`_on_win_button_button_down()` 和按钮/信号连接入口。

### 本批删除或收口的重复点

删除原因：

```text
失败统计字典、GameOver 启动、删档和最终胜利页触发不再直接散落在 in_scene.gd 里。
主脚本只负责把当前节点、autoload 和回调交给对应 flow controller。
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

下一批建议处理 `_return_to_out_scene()` 的剩余编排：

1. 新增 `InSceneReturnFlowController.gd`，只负责返回局外前的 dim 显示、入场动画等待、payload 构造调用和 `_switch_scene_with_data()` 调用。
2. 继续保留 `_build_combat_return_payload()` 和 `_switch_scene_with_data()` 旧入口，避免同批触碰 payload、loader、executor 三层。
3. 如果优先更低风险，可以先拆 `_on_external_scene_exit_pressed()` 的退出后恢复编排，但收益比返回局外小。

## in_scene.gd 第十三批返回局外流程编排拆分记录

日期：2026-06-05

### 本批目标

本批只拆“结算结束后返回局外地图”的顺序编排。
不改返回 payload 的字段，不改 PackedScene 加载和实际切场执行，也不改局外场景消费 payload 的协议。

目标函数范围：

```text
_return_to_out_scene()
```

当前触碰的外部节点和接口：

```text
_push_era_to_global()
_build_combat_return_payload()
SceneLog.scene_event()
MapState.set_pending_room_resolution()
disable_player_inputs()
hide_ui_for_external_scene()
SoundManager.stop_looping_sfx()
SoundManager.stop_bgm()
SoundManager.play_bgm_main_menu()
dim.use(0, 0)
_switch_scene_with_data(out_scene_path, return_payload)
```

### 新增模块

```text
scene/in_scene/in_scene_modules/scene_flow/InSceneReturnFlowController.gd
```

模块边界：

- `InSceneReturnFlowController.gd` 只编排返回局外的动作顺序。
- 它通过 `Callable` 调用主脚本已有的时代同步、payload 构造、输入锁、UI 隐藏和切场入口。
- 它不直接构造 payload，不加载 `PackedScene`，不处理切场失败恢复。
- `_build_combat_return_payload()`、`_switch_scene_with_data()`、`InSceneSceneSwitchLoader.gd` 和 `InSceneSceneSwitchExecutor.gd` 的职责保持不变。

### 本批删除或收口的重复点

删除原因：

```text
返回局外前的同步、记录、MapState pending resolution、输入/UI 收口、音频切换、黑幕等待和切场调用，已经由 InSceneReturnFlowController 统一维护。
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

`in_scene.gd` 仍有可拆点，暂时不退出流程：

1. `CardDrawFlowController.gd`：拆 `attempt_draw_cards()` 和 `shuffle_card()`，但要小心 `is_processing_deck`、输入锁和 `process_frame`。
2. `TurnFlowController.gd`：拆 `_on_end_turn_pressed()` / `_start_turn()` 的回合推进编排，但牵动时间轴、敌意图和抽牌，应等抽牌流先稳定。
3. `TargetHoverUiController.gd`：拆 `update_target_selection_hover()` 的 tooltip 文案和定位，风险比回合流低。

## in_scene.gd 第十四批目标选择 hover UI 拆分记录

日期：2026-06-05

### 本批目标

本批只拆卡牌目标选择 hover 时的光标提示表现。
目标合法性仍由 `in_scene.gd` 调用 `HexTargetRules` 判断，避免 UI 模块知道地图规则。

目标函数范围：

```text
update_target_selection_hover(hovered_stack, active_card)
```

当前触碰的外部节点和接口：

```text
cursor_tooltip.text
cursor_tooltip.add_theme_color_override()
cursor_tooltip.show()
set_cursor_tooltip_position()
active_card.card_info["ATK"]
```

### 新增模块

```text
scene/in_scene/in_scene_modules/ui/TargetSelectionHoverUiController.gd
```

模块边界：

- `TargetSelectionHoverUiController.gd` 只负责设置目标 hover tooltip 的位置、文案、颜色和显示。
- 它不调用 `HexTargetRules`，不读取 `hex_map.map_data`，不改地块 shader。
- `in_scene.gd` 保留 `update_target_selection_hover()` 旧接口，供 HexMap 的 `TargetSelectionTooltipAdapter` 继续调用。

### 本批删除或收口的重复点

删除原因：

```text
目标 hover 的 “-伤害值 / 无效果” 文案、红灰颜色和 tooltip 定位已经由 TargetSelectionHoverUiController 统一维护。
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

`in_scene.gd` 仍有可拆点，暂时不退出流程：

1. `CardDrawFlowController.gd`：拆 `attempt_draw_cards()` 和 `shuffle_card()`，需要把 `is_processing_deck` 的读写结果回传给主脚本。
2. `TimelineActionHoverUiController.gd`：拆 `_on_timeline_action_hovered()` 中玩家行动 tooltip 和 pulse shader 表现；敌方意图已由 EnemyIntentPresentationController 接管，拆时要继续保持 ENEMY 早退。
3. `TurnFlowController.gd` 继续暂缓，等抽牌流和时间轴 hover 更薄后再处理。

## in_scene.gd 第十五批抽牌和洗牌流程拆分记录

日期：2026-06-05

### 本批目标

本批只拆抽牌和洗牌的异步动作。
战斗状态判断、抽牌并发锁 `is_processing_deck` 和旧公共入口仍由 `in_scene.gd` 保留。

目标函数范围：

```text
attempt_draw_cards(count)
shuffle_card()
```

当前触碰的外部节点和接口：

```text
player_hand.move_cards()
deck_pile.move_cards()
deck_pile._held_cards.shuffle()
deck_pile.card_face_up = false
deck_pile.update_card_ui()
Signal_Bus.emit_card_drawn()
Signal_Bus.emit_deck_shuffled()
get_tree().process_frame
get_tree().create_timer()
update_counts_and_ui()
disable_player_inputs()
enable_player_inputs()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/cards/CardDrawFlowController.gd
```

模块边界：

- `CardDrawFlowController.gd` 负责抽牌、必要时洗牌、等待帧、等待抽牌间隔、刷新计数和发出抽牌/洗牌信号。
- 它不判断当前是否处于战斗期，不持有 `is_processing_deck`，不处理牌堆按钮输入。
- `in_scene.gd` 继续保留 `attempt_draw_cards()` 和 `shuffle_card()`，并在调用 controller 前后维护并发锁。
- 洗牌仍复刻旧行为：复制弃牌堆数组后由 `deck_pile.move_cards(cards_to_move)` 统一从旧容器转移到抽牌堆。

### 本批删除或收口的重复点

删除原因：

```text
抽牌循环、空抽牌堆时洗牌、抽牌/洗牌信号、抽牌间隔和洗牌延迟已经由 CardDrawFlowController 统一维护。
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

`in_scene.gd` 仍有可拆点，暂时不退出流程：

1. `TimelineActionHoverUiController.gd`：拆 `_on_timeline_action_hovered()` 中玩家行动 tooltip 和 pulse shader 表现。
2. `TurnFlowController.gd`：抽牌流已独立后，可以开始拆 `_on_end_turn_pressed()` / `_start_turn()` 的回合编排，但仍要保守处理胜利中断。
3. `_input(event)` 可以进一步拆键盘调试、右键取消选牌、空地弃牌三个小 handler；这会降低主脚本输入职责，但要逐个拆。

## in_scene.gd 第十六批时间轴行动 hover UI 拆分记录

日期：2026-06-05

### 本批目标

本批只拆时间轴行动 hover 的旧 UI 表现。
敌方意图 hover 已经由 `EnemyIntentPresentationController` 接管，因此继续保持 ENEMY 行动在 `in_scene.gd` 入口早退。

目标函数范围：

```text
_on_timeline_action_hovered(action, is_hovering)
_apply_pulse_shader(target_node, color)
_clear_pulse_shader(target_node)
```

当前触碰的外部节点和接口：

```text
TimelineAction.Type.ENEMY
TimelineAction.Type.PLAYER
action.target_tile
action.source_node
action.action_data["效果"]
pulse_shader.duplicate()
Sprite2D.material
cursor_tooltip.text
cursor_tooltip.add_theme_color_override()
set_cursor_tooltip_position()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/ui/TimelineActionHoverUiController.gd
```

模块边界：

- `TimelineActionHoverUiController.gd` 只处理玩家时间轴行动 hover 的 pulse shader、tooltip 文案、颜色、定位和隐藏。
- 它不处理敌方意图 hover，不生成敌人意图，不修改时间轴数据。
- `in_scene.gd` 继续保留 `_on_timeline_action_hovered()` 作为信号入口，并保留 ENEMY 早退逻辑。

### 本批删除或收口的重复点

删除原因：

```text
玩家时间轴行动 hover 的金色高亮、tooltip 文案和材质清理已经由 TimelineActionHoverUiController 统一维护。
in_scene.gd 不再直接持有 pulse shader 挂载和卸载 helper。
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

继续评估后再决定是否拆：

1. `TurnFlowController.gd` 可以拆回合结束和新回合开始编排，但它同时牵动时间轴 resolve、建筑行为、抽牌、敌方意图和胜利中断，风险高于前面几批。
2. `_input(event)` 可以按键盘调试、右键取消选牌、空地弃牌拆成小 handler，收益中等，风险低于回合流。
3. 如果不继续拆，`in_scene.gd` 已经更接近 composition root，剩余很多函数只是旧公共入口和跨模块编排。

## in_scene.gd 第十七批输入事件拆分记录

日期：2026-06-05

### 本批目标

本批只拆 `MainBoard._input(event)` 中的输入解析。
需要等待一帧的弃牌移动仍留在 `in_scene.gd` 执行，避免输入模块直接持有场景树异步副作用。

目标函数范围：

```text
_input(event)
```

当前拆出的输入类型：

- 键盘 9 调试时间货币。
- 右键取消当前选中卡牌。
- 左键空地释放时识别需要弃掉的卡牌。

### 新增模块

```text
scene/in_scene/in_scene_modules/ui/InSceneInputEventController.gd
```

模块边界：

- `InSceneInputEventController.gd` 负责把原始 `InputEvent` 转成 `"handled"`、`"discard_card"` 或 `"none"`。
- 它可以执行同步的调试加币和右键取消选牌。
- 它不调用 `discard_pile.move_cards()`，不等待 `process_frame`，不刷新牌堆计数。
- `in_scene.gd` 保留 `_input(event)` 入口，并负责弃牌的异步移动、弃牌效果和 UI 计数刷新。

### 本批删除或收口的重复点

删除原因：

```text
键盘调试、右键取消选牌和空地弃牌卡牌识别，不再全部堆在 in_scene.gd 的 _input(event) 中。
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

### 退出判断

本批后 `in_scene.gd` 仍保留这些较大的编排入口：

- `_ready()`：局内场景 composition root 初始化。
- `_start_turn()` / `_on_end_turn_pressed()`：回合推进，牵动时间轴、建筑行为、抽牌、敌方意图和胜利中断。
- `discard_all_hand_cards()`：强制结束回合时的手牌回收，和回合流绑定紧密。
- `_open_settlement_reward_scene()` / `_on_external_scene_exit_pressed()`：奖励页打开和退出，已经由多个 settlement 模块分担，剩余是入口编排。
- 多个 `_build_*_config()`：模块配置组装，属于 composition root 职责。

当前可以继续拆的点已经不再是低风险纯表现或桥接模块。
下一步如果继续拆，建议先写专门测试或手动验证回合结束、胜利中断、教程首回合和奖励页返回，再拆 `TurnFlowController.gd`。

## in_scene.gd 第十八批强制弃置手牌拆分记录

日期：2026-06-05

### 本批目标

本批只拆“结束回合时把所有手牌强制丢入弃牌堆”的动作。
不拆 `_on_end_turn_pressed()`，不拆 `_start_turn()`，不改时间轴结算和建筑行为触发顺序。

目标函数范围：

```text
discard_all_hand_cards()
```

当前触碰的外部节点和接口：

```text
player_hand._held_cards.duplicate()
card.force_deselect()
card.change_state(0)
discard_pile.move_cards(cards_to_discard)
get_tree().process_frame
hide_tooltip()
update_counts_and_ui()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/cards/HandDiscardFlowController.gd
```

模块边界：

- `HandDiscardFlowController.gd` 只负责复制手牌数组、复位卡牌状态、隐藏 tooltip、移动到弃牌堆并等待一帧。
- 它不推进回合、不解析时间轴、不发出建筑行为信号。
- `in_scene.gd` 保留 `discard_all_hand_cards()` 旧入口，并在模块完成后刷新牌堆计数。

### 本批删除或收口的重复点

删除原因：

```text
强制弃置全部手牌的 duplicate 安全机制、卡牌状态复位和弃牌堆移动逻辑已经由 HandDiscardFlowController 统一维护。
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

如果继续拆，仍建议暂缓完整 `TurnFlowController.gd`，优先做更小的边界：

1. `EnemyIntentTimelineRefresher.gd`：拆 `_refresh_enemy_intents_on_timeline()`，只负责清时间轴、取 Enemies 组、生成意图和 debug 输出。
2. 或拆 `CombatCartoonUiController.gd`，收口 `_setup_combat_cartoon_ui()` / `_refresh_combat_cartoon_ui_progress()` 的顶部 UI 适配。
3. 完整回合流仍建议等这些小块再瘦一轮后处理。

## in_scene.gd 第十九批敌方意图时间轴刷新拆分记录

日期：2026-06-05

### 本批目标

本批只拆“把当前敌人意图刷新到时间轴”的小流程。
不拆回合推进，不修改敌人意图生成规则，也不处理 hover 表现。

目标函数范围：

```text
_refresh_enemy_intents_on_timeline()
```

当前触碰的外部节点和接口：

```text
timeline_manager.clear_grid()
get_tree().get_nodes_in_group("Enemies")
timeline_manager.generate_enemy_intents(all_enemies)
timeline_manager.debug_print_grid()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/turn/EnemyIntentTimelineRefresher.gd
```

模块边界：

- `EnemyIntentTimelineRefresher.gd` 只负责清空时间轴、读取 Enemies 组、生成敌方意图并输出 debug grid。
- 它不推进回合、不抽牌、不触发建筑行为。
- `in_scene.gd` 保留 `_refresh_enemy_intents_on_timeline()` 旧入口，供 `_start_turn()` 继续调用。

### 本批删除或收口的重复点

删除原因：

```text
敌方意图刷新时间轴的节点组读取和 TimelineManager 调用已经由 EnemyIntentTimelineRefresher 统一维护。
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

如果继续拆，当前还剩一个相对清晰的小块：

1. `CombatCartoonUiController.gd`：拆 `_setup_combat_cartoon_ui()` / `_refresh_combat_cartoon_ui_progress()`，只处理顶部战斗 CartoonUI 与时间轴 reserved space。
2. 完整 `TurnFlowController.gd` 仍暂缓，因为胜利中断、建筑行为、抽牌和敌方意图都在同一条链路里。
3. 如果拆完 CartoonUI 后没有新的低风险小块，就可以退出 `in_scene.gd` 本轮拆解。

## in_scene.gd 第二十批顶部战斗 CartoonUI 拆分记录

日期：2026-06-05

### 本批目标

本批只拆局内顶部 `CombatCartoonUI` 的适配流程。
不拆回合推进，不修改 GlobalClock 推进规则，也不调整时间轴意图生成。

目标函数范围：

```text
_setup_combat_cartoon_ui()
_refresh_combat_cartoon_ui_progress()
```

当前触碰的外部节点和接口：

```text
combat_cartoon_ui.set_progress_labels(current_era_value, phase_value)
combat_cartoon_ui.set_character_index(MapState.chosen_char_index)
combat_cartoon_ui.apply_combat_layout()
combat_cartoon_ui.get_reserved_height()
timeline_ui.set_top_reserved_space(...)
_global_clock_bridge.pull_era()
_global_clock_bridge.get_phase()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/ui/CombatCartoonUiController.gd
```

模块边界：

- `CombatCartoonUiController.gd` 只负责顶部战斗 CartoonUI 的进度文字、角色索引、战斗布局和时间轴预留高度。
- 它只读取 GlobalClock bridge 的当前时代和阶段，不推进 GlobalClock。
- 它不决定战斗阶段、不生成敌人意图、不触发抽牌或弃牌。
- `in_scene.gd` 保留 `_setup_combat_cartoon_ui()` 和 `_refresh_combat_cartoon_ui_progress()` 旧入口，并只负责把返回的 `current_era_value` 写回主状态。

### 本批删除或收口的重复点

删除原因：

```text
顶部 CartoonUI 的进度刷新、角色索引设置、布局应用和时间轴让位已经由 CombatCartoonUiController 统一维护。
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

### 退出判断

本批后，`in_scene.gd` 剩余较明显的逻辑主要是：

- `_ready()` 和多个 `_build_*_config()`：局内场景 composition root 的组装职责。
- `_on_end_turn_pressed()` / `_start_turn()` / `_advance_global_phase()`：回合推进链路，牵动时间轴 resolve、建筑回合行为、抽牌、敌方意图和胜利中断。
- `_open_settlement_reward_scene()` / `_on_external_scene_exit_pressed()`：奖励外部场景入口和退出编排，已有多个 settlement 模块分担，剩余主要是跨模块串联。
- `_switch_scene_with_data()` 及其辅助函数：场景切换底层入口，已经由 loader、executor、payload bridge 分担。

这些剩余点不再属于“低风险小模块”。如果继续拆，需要先为回合结束、胜利中断、结算页返回和场景切换准备更完整的手动或自动回归路径；否则本轮 `in_scene.gd` 拆解可以在这里退出。

## in_scene.gd 第二十一批结算奖励退出收尾拆分记录

日期：2026-06-05

### 本批目标

本批只拆“奖励外部场景退出后的收尾编排”。
不打开奖励页，不修改奖励页提交规则，不改变战斗阶段，也不触碰回合推进。

目标函数范围：

```text
_on_external_scene_exit_pressed(scene_instance)
_consume_settlement_reward_context(reward_context)
```

当前触碰的外部节点和接口：

```text
SettlementRewardExitController.close_reward_scene(scene_instance)
SettlementRewardConsumer.consume(reward_context, hex_map)
restore_ui_after_external_scene()
active_settlement_reward_context.clear()
```

### 新增模块

```text
scene/in_scene/in_scene_modules/settlement/SettlementRewardExitFlowController.gd
```

模块边界：

- `SettlementRewardExitFlowController.gd` 只负责关闭奖励页、按退出结果消费奖励建筑、恢复局内 UI。
- 它不打开奖励页，不设置奖励页 meta，也不决定当前战斗阶段。
- 它不直接知道奖励建筑表现如何刷新；消费事实仍由 `SettlementRewardConsumer.gd` 写回 HexMap。
- `in_scene.gd` 保留 `_on_external_scene_exit_pressed()` 旧入口，并负责清空 `active_settlement_reward_context`。

### 本批删除或收口的重复点

删除原因：

```text
奖励页退出后的 close、consume、restore 三步已经由 SettlementRewardExitFlowController 统一编排。
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

### 退出判断

本批后，`in_scene.gd` 里还能看到的较大入口主要是回合推进、场景切换底层 wrapper、composition root 配置组装，以及奖励页打开的错误提示入口。
这些入口都已经有下层模块承接主要职责，剩余部分多是旧公共 API、信号回调和跨模块串联。

如果继续拆，需要优先建立更完整的回合结束、胜利中断、奖励页返回和场景切换回归路径。
在没有这类回归保护前，不建议继续从 `in_scene.gd` 里机械抽函数。

## AI 接力操作说明更新记录

日期：2026-06-06

### 本批目标

本批不改游戏逻辑，只更新接力说明，让后续 AI 可以沿用 `HexMap` 和 `in_scene.gd` 的拆分经验继续处理其他大文件。

### 更新文件

```text
docs/ai-handoff-ultimate-operation-guide.md
```

### 更新内容

- 总结 `hex_map.gd` 和 `in_scene.gd` 当前模块化状态。
- 明确 `in_scene.gd` 低风险小块已经基本拆完，后续不建议继续机械拆。
- 增加目标文件分析模板、待拆清单模板、每批拆分规则、验证命令和退出标准。
- 列出下一阶段更值得优化解耦的文件：`DragShapeController.gd`、`timeline_ui.gd`、奖励脚本、`out_scene_map_exp.gd`、`tile.gd`、`custom_card.gd` 等。
- 增加可直接复制给下一位 AI 的接力 prompt。

### 回归检查

```text
本批只改 Markdown，运行 git diff --check 即可。
```

## DragShapeController.gd 第一批节点桥接拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `DragShapeController.gd` 里的跨节点查找入口。
不修改拖拽状态机、不修改时间轴 hover preview、不修改放置校验、不修改卡牌弃牌或回手流程。

目标函数范围：

```text
_get_main_board()
_get_card_manager()
_find_discard_pile()
_find_player_hand()
_get_hex_map()
_find_project_node()
```

当前触碰的外部节点和接口：

```text
MainBoard 分组
main.manager_instance
main.discard_pile
main.player_hand
../../map/HexMap
```

### 新增模块

```text
scene/in_scene/drag_modules/DragShapeNodeBridge.gd
```

模块边界：

- `DragShapeNodeBridge.gd` 只负责 DragShapeController 需要的跨节点查找。
- 它不缓存拖拽状态，不修改场景树，也不决定卡牌是否可以放置。
- `DragShapeController.gd` 保留原有 `_get_*` 和 `_find_*` 旧入口，内部转发给 bridge，避免影响现有调用点。

### 本批删除或收口的重复点

删除原因：

```text
MainBoard、CardManager、弃牌区、手牌、HexMap 的查找路径已经由 DragShapeNodeBridge 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第二批拒绝提示拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽放置失败时的拒绝提示。
不修改放置合法性判断，不修改卡牌抖动动画，不修改拖拽状态，也不改变 2 秒后隐藏提示的调度方式。

目标函数范围：

```text
_show_reject_tooltip(message)
_update_reject_tooltip_position()
_hide_reject_tooltip()
```

当前触碰的外部节点和接口：

```text
cursor_tooltip.text
cursor_tooltip.size
cursor_tooltip.show()
cursor_tooltip.hide()
cursor_tooltip.global_position
get_global_mouse_position()
get_viewport_rect().size
```

### 新增模块

```text
scene/in_scene/drag_modules/DragRejectTooltipController.gd
```

模块边界：

- `DragRejectTooltipController.gd` 只负责拖拽拒绝提示的文本、位置和显隐。
- 它不判断放置是否合法，不播放卡牌抖动动画，也不修改拖拽状态。
- `DragShapeController.gd` 保留 `_show_reject_tooltip()`、`_update_reject_tooltip_position()` 和 `_hide_reject_tooltip()` 旧入口，由旧入口转发给新模块。

### 本批删除或收口的重复点

删除原因：

```text
拒绝提示的 BBCode 文本、尺寸重置、屏幕边界定位和隐藏逻辑已经由 DragRejectTooltipController 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第三批时间轴预览转发拆分记录

日期：2026-06-06

### 本批目标

本批只拆时间轴拖拽网格预览的转发入口。
不修改鼠标坐标换算，不修改放置合法性判断，不创建 `TimelineAction`，也不统一所有拖拽生命周期里的 preview 清理点。

目标函数范围：

```text
_update_timeline_grid_preview(grid_pos, is_valid)
```

当前触碰的外部节点和接口：

```text
timeline_ui.update_grid_preview(shape_coords, grid_pos, is_valid)
TimelineClearEffectUtil.update_preview(timeline_ui, timeline_manager, shape_coords, grid_pos)
timeline_manager
current_shape_coords
is_timeline_clear_mode
```

### 新增模块

```text
scene/in_scene/drag_modules/DragTimelineGridPreviewPresenter.gd
```

模块边界：

- `DragTimelineGridPreviewPresenter.gd` 只负责把拖拽形状预览转发给时间轴 UI。
- 它不计算鼠标坐标，不判断放置是否合法，也不创建 `TimelineAction`。
- `DragShapeController.gd` 保留 `_update_timeline_grid_preview()` 旧入口，由旧入口传入当前 shape、grid 坐标、合法性和 clear 模式。

### 本批删除或收口的重复点

删除原因：

```text
普通卡牌预览和 clear 卡牌预览的分支转发已经由 DragTimelineGridPreviewPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第四批卡牌形状解析拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽开始时的卡牌时间轴形状读取。
不修改拖拽状态，不修改时间轴展开，不修改放置校验，也不修改 clear 卡牌执行逻辑。

目标函数范围：

```text
start_dragging(card, target_tile) 中的 current_shape_coords 解析
_convert_to_vector2i_array(raw_array)
```

当前触碰的外部节点和接口：

```text
TimelineClearEffectUtil.is_clear_card(card)
TimelineClearEffectUtil.get_clear_shape_coords(card)
card.timeline_shape_coords
card.card_info["shape"]
```

### 新增模块

```text
scene/in_scene/drag_modules/DragCardShapeResolver.gd
```

模块边界：

- `DragCardShapeResolver.gd` 只负责从卡牌数据读取时间轴形状坐标。
- 它不启动拖拽，不修改卡牌节点，也不判断形状是否可以放置。
- `DragShapeController.gd` 保留 `_convert_to_vector2i_array()` 旧入口，由旧入口转发给新模块，避免影响潜在旧调用点。

### 本批删除或收口的重复点

删除原因：

```text
clear 卡牌形状、已解析 timeline_shape_coords 和 card_info.shape 兜底转换已经由 DragCardShapeResolver 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第五批时间轴网格鼠标过滤拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽期间时间轴网格单元格的鼠标过滤开关。
不修改时间轴展开收起，不处理 hover，不修改放置校验，也不改变敌人意图方格 hover 的保留策略。

目标函数范围：

```text
_disable_grid_cells_mouse_filter()
_restore_grid_cells_mouse_filter()
```

当前触碰的外部节点和接口：

```text
timeline_ui.grid_cells
Control.MOUSE_FILTER_IGNORE
Control.MOUSE_FILTER_PASS
```

### 新增模块

```text
scene/in_scene/drag_modules/DragTimelineGridMouseFilterController.gd
```

模块边界：

- `DragTimelineGridMouseFilterController.gd` 只负责时间轴网格单元格的鼠标过滤状态。
- 它不展开或收起时间轴，不处理 hover，也不判断卡牌放置结果。
- `DragShapeController.gd` 保留 `_disable_grid_cells_mouse_filter()` 和 `_restore_grid_cells_mouse_filter()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：

```text
遍历 timeline_ui.grid_cells 并设置 mouse_filter 的两段重复结构已经由 DragTimelineGridMouseFilterController 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第六批放置查询拆分记录

日期：2026-06-06

### 本批目标

本批只拆供外部或时间轴可视化器调用的放置合法性查询入口。
不修改 `try_place_shape()` 的实际放置判定，不创建 `TimelineAction`，不修改时间轴数据，也不触发卡牌效果。

目标函数范围：

```text
_is_placement_valid(grid_pos)
```

当前触碰的外部节点和接口：

```text
timeline_manager.is_placement_valid(current_shape_coords, grid_pos)
TimelineClearEffectUtil.is_origin_in_bounds(...)
timeline_ui.grid_width
timeline_ui.grid_height
```

### 新增模块

```text
scene/in_scene/drag_modules/DragPlacementQueryService.gd
```

模块边界：

- `DragPlacementQueryService.gd` 只负责回答当前拖拽形状在指定时间轴格子是否可用。
- 它不执行放置，不创建 `TimelineAction`，也不修改时间轴或卡牌状态。
- `DragShapeController.gd` 保留 `_is_placement_valid()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：

```text
普通卡牌放置查询和 clear 卡牌边界查询已经由 DragPlacementQueryService 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第七批时间轴 UI 状态拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽期间时间轴 UI 的展开、收起和点击展开开关。
不修改预览格子，不判断放置是否合法，不移动卡牌，也不触发卡牌效果。

目标函数范围：

```text
start_dragging(card, target_tile) 中的时间轴展开与 mouse_filter 设置
_end_dragging() 中的时间轴收起与点击展开禁用
end_dragging_success() 中的时间轴收起
```

当前触碰的外部节点和接口：

```text
timeline_ui.set_allow_click_to_expand(...)
timeline_ui.toggle_expand()
timeline_ui.collapse()
timeline_ui.mouse_filter
timeline_ui.is_expanded
```

### 新增模块

```text
scene/in_scene/drag_modules/DragTimelineUiStateController.gd
```

模块边界：

- `DragTimelineUiStateController.gd` 只负责拖拽期间时间轴 UI 的展开、收起和点击展开开关。
- 它不处理预览格子，不判断放置是否合法，也不移动卡牌。
- `DragShapeController.gd` 仍负责何时进入或退出拖拽模式。

### 本批删除或收口的重复点

删除原因：

```text
拖拽开始、取消拖拽和成功结束拖拽中的时间轴展开/收起调用已经由 DragTimelineUiStateController 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第八批时间轴预览清理拆分记录

日期：2026-06-06

### 本批目标

本批只统一时间轴拖拽预览的清理入口。
不改变清理发生的时机，不修改 clear 卡牌执行，不修改普通卡牌放置流程，也不修改卡牌效果预览清理。

目标函数范围：

```text
_end_dragging() 中的时间轴预览清理
_handle_free_drag(mouse_pos) 中 clear 模式离开时间轴后的预览清理
_execute_timeline_clear(origin_pos) 中确认施放前的预览清理
_finish_placement(grid_pos) 中普通放置成功后的预览清理
force_cancel_drag() 中强制打断时的预览清理
```

当前触碰的外部节点和接口：

```text
TimelineClearEffectUtil.clear_preview(timeline_ui)
timeline_ui.clear_grid_preview()
is_timeline_clear_mode
```

### 更新模块

```text
scene/in_scene/drag_modules/DragTimelineGridPreviewPresenter.gd
```

模块边界：

- `DragTimelineGridPreviewPresenter.gd` 继续只负责时间轴拖拽预览的显示与清理转发。
- 它不决定什么时候清理，不执行 clear 卡牌效果，也不修改卡牌或时间轴数据。
- `DragShapeController.gd` 新增 `_clear_timeline_grid_preview()` 旧内部入口，用于统一转发清理请求。

### 本批删除或收口的重复点

删除原因：

```text
clear 卡牌覆盖层清理和普通 grid preview 清理的重复分支已经由 DragTimelineGridPreviewPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第九批时间轴网格坐标解析拆分记录

日期：2026-06-06

### 本批目标

本批只拆“时间轴本地鼠标位置换算成网格坐标和边界状态”的重复计算。
不判断卡牌形状是否合法，不更新预览，不执行放置，也不改变 hover 和实际放置的后续判定流程。

目标函数范围：

```text
_handle_timeline_hover(mouse_pos) 中的 timeline_ui.grid_background 本地坐标换算
try_place_shape() 中的 timeline_ui.grid_background 本地坐标换算
```

当前触碰的外部节点和接口：

```text
timeline_ui.grid_background.get_local_mouse_position()
timeline_ui.slot_size
timeline_ui.spacing
timeline_ui.grid_width
timeline_ui.grid_height
```

### 新增模块

```text
scene/in_scene/drag_modules/DragTimelineGridCoordinateResolver.gd
```

模块边界：

- `DragTimelineGridCoordinateResolver.gd` 只负责把时间轴本地鼠标位置换算成网格坐标和边界状态。
- 它不判断卡牌形状是否合法，不更新预览，也不执行放置。
- `DragShapeController.gd` 仍负责 hover 合法性、clear 卡牌边界判断、普通卡牌放置判断和拒绝动画。

### 本批删除或收口的重复点

删除原因：

```text
hover 预览和实际放置入口里重复的 slot_size、spacing、grid 坐标和边界计算已经由 DragTimelineGridCoordinateResolver 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十批场景交互锁拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽期间局内地图和时间轴 UI 的交互锁定/恢复。
不处理卡牌状态，不更新预览，不判断放置，也不执行放置动画。

目标函数范围：

```text
start_dragging(card, target_tile) 中的 HexMap 输入锁定和视觉锁定
_restore_mouse_filters()
```

当前触碰的外部节点和接口：

```text
hex_map.set_tiles_interactive(false/true)
hex_map.set_visuals_locked(true/false)
timeline_ui.mouse_filter
```

### 新增模块

```text
scene/in_scene/drag_modules/DragSceneInteractionLockController.gd
```

模块边界：

- `DragSceneInteractionLockController.gd` 只负责拖拽期间局内场景交互的锁定和恢复。
- 它不处理卡牌状态，不更新预览，也不判断或执行放置。
- `DragShapeController.gd` 仍负责决定何时锁定和恢复，并继续单独恢复时间轴网格单元格的 mouse_filter。

### 本批删除或收口的重复点

删除原因：

```text
HexMap 输入/视觉锁定与时间轴 UI mouse_filter 恢复现在由 DragSceneInteractionLockController 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十一批卡牌效果预览文本拆分记录

日期：2026-06-06

### 本批目标

本批只拆从卡牌描述生成拖拽效果预览文案和数值的逻辑。
不显示 tooltip，不查找敌人或血条，也不触发任何实际卡牌效果。

目标函数范围：

```text
_get_card_effect_preview_text()
_get_card_damage_amount()
```

当前触碰的外部节点和接口：

```text
current_card.get_parsed_description()
current_card.raw_description
current_card.card_info["效果"]
```

### 新增模块

```text
scene/in_scene/drag_modules/DragCardEffectPreviewTextResolver.gd
```

模块边界：

- `DragCardEffectPreviewTextResolver.gd` 只负责从卡牌描述生成拖拽效果预览文案和数值。
- 它不显示 tooltip，不查找敌人或血条，也不触发任何实际卡牌效果。
- `DragShapeController.gd` 保留 `_get_card_effect_preview_text()` 和 `_get_card_damage_amount()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：

```text
卡牌描述读取、简单 BBCode 清理、预览文案生成和数值提取已经由 DragCardEffectPreviewTextResolver 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十二批目标效果预览拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽目标效果预览的显示和清理。它只把当前目标地块、伤害数值转交给敌人节点或血条管理器，不生成预览文案，不执行卡牌效果，也不改变拖拽、放置或动画状态。

目标函数范围：
```text
_trigger_enemy_effect_preview(damage_amount)
_clear_effect_preview()
_get_enemy_on_tile(tile)
_get_health_bar_manager()
```

当前触碰的外部节点和接口：
```text
target_enemy.show_card_effect_preview(damage_amount)
target_enemy.clear_card_effect_preview()
HealthBarManager.preview_damage_effect(damage_amount)
HealthBarManager.clear_preview_effect()
get_nodes_in_group("health_bar_manager")
```

### 新增模块

```text
scene/in_scene/drag_modules/DragEffectPreviewPresenter.gd
```

模块边界：
- `DragEffectPreviewPresenter.gd` 只负责拖拽目标效果预览的显示和清理。
- 它保留原有敌人查询占位行为，暂不补充地块到敌人的实际映射。
- `DragShapeController.gd` 保留 `_trigger_enemy_effect_preview()`、`_clear_effect_preview()`、`_get_enemy_on_tile()` 和 `_get_health_bar_manager()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
敌人预览触发、敌人预览清理、血条预览触发、血条预览清理和血条管理器查找现在由 DragEffectPreviewPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第一批展开遮罩表现拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 展开和收起时的 UI 表现面：背景遮罩创建、遮罩淡入淡出、展开时地图鼠标交互过滤。它不处理时间轴行动数据，不创建行动方块，不改变敌人意图 shader，也不参与拖拽放置判断。

目标函数范围：
```text
_create_background_mask()
toggle_expand() 中的背景遮罩和地图交互分支
collapse() 中的背景遮罩分支
```

当前触碰的外部节点和接口：
```text
BackgroundMask
map.mouse_filter
Tween.tween_property()
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineExpandVisualController.gd
```

模块边界：
- `TimelineExpandVisualController.gd` 只负责 TimelineUI 展开/收起时的遮罩和地图交互表现。
- 它不处理时间轴行动数据，不创建行动方块，也不参与拖拽放置判断。
- `timeline_ui.gd` 保留 `_create_background_mask()`、`toggle_expand()` 和 `collapse()` 旧入口，内部转发背景遮罩与地图交互细节。

### 本批删除或收口的重复点

删除原因：
```text
展开和强制收起里重复的 BackgroundMask 淡出隐藏逻辑，以及展开时的 map.mouse_filter 切换，现在由 TimelineExpandVisualController 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第二批背景网格构建拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 的空背景网格初始化：设置 GridContainer 间距、创建空 Panel 格子、绑定格子鼠标信号，并返回 `grid_cells` 字典。它不处理 hover 状态，不更新拖拽预览，不创建时间轴行动方块，也不触碰敌人意图表现。

目标函数范围：
```text
_init_background_grid()
```

当前触碰的外部节点和接口：
```text
GridBackground.columns
GridBackground.add_theme_constant_override()
Panel.gui_input
Panel.mouse_entered
Panel.mouse_exited
grid_cells
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineGridBuilder.gd
```

模块边界：
- `TimelineGridBuilder.gd` 只负责创建 TimelineUI 的空背景格子。
- 它不处理 hover 状态，不更新拖拽预览，也不创建时间轴行动方块。
- `timeline_ui.gd` 保留 `_init_background_grid()` 旧入口，内部转发给新模块并接收新的 `grid_cells` 字典。

### 本批删除或收口的重复点

删除原因：
```text
背景网格创建、默认格子样式、鼠标信号绑定和坐标字典填充现在由 TimelineGridBuilder 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第三批网格格子交互表现拆分记录

日期：2026-06-06

### 本批目标

本批只拆空背景格子的交互表现辅助逻辑：把 `cell_index` 换算成 `grid_pos`，并在鼠标进入/离开时切换空格子的 hover/default 样式。它不发射业务信号，不处理拖拽预览，不读取 `TimelineManager`，也不创建行动方块。

目标函数范围：
```text
_on_grid_cell_gui_input(event, cell_index) 中的 cell_index 到 grid_pos 换算
_on_grid_cell_mouse_entered(cell_index) 中的 hover 样式切换
_on_grid_cell_mouse_exited(cell_index) 中的 default 样式恢复
```

当前触碰的外部节点和接口：
```text
grid_cells
Panel.add_theme_stylebox_override()
StyleBoxFlat.bg_color
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineGridCellInteractionPresenter.gd
```

模块边界：
- `TimelineGridCellInteractionPresenter.gd` 只负责 TimelineUI 空背景格子的坐标换算和 hover 样式。
- 它不发射业务信号，不处理拖拽预览，也不读取 TimelineManager 数据。
- `timeline_ui.gd` 继续负责发射 `grid_cell_clicked`、`grid_cell_right_clicked` 和 `grid_cell_hovered` 信号。

### 本批删除或收口的重复点

删除原因：
```text
三处重复的 cell_index 到 Vector2i 网格坐标换算，以及鼠标进入/离开里重复的 StyleBoxFlat 复制和颜色设置，现在由 TimelineGridCellInteractionPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## CraftReward.gd 第一批合成配方查询拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `CraftReward.gd` 的合成配方查询规则。它只根据两张卡牌 id 和配方表返回结果卡牌 id，不修改牌库，不创建卡牌，也不处理合成界面的选择状态。

目标函数范围：
```text
_get_recipe_result(card_a_id, card_b_id)
```

当前触碰的数据：
```text
CRAFTING_RECIPES
card_a_id
card_b_id
```

### 新增模块

```text
scene/in_scene/rewards/CraftRecipeResolver.gd
```

模块边界：
- `CraftRecipeResolver.gd` 只负责合成配方查询。
- 它不修改牌库，不创建卡牌，也不处理合成界面的选择状态。
- `CraftReward.gd` 保留 `_get_recipe_result()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
正向 key 与反向 key 的配方查询现在由 CraftRecipeResolver 统一维护，后续新增配方查询规则时不用进入主 UI 脚本。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## CraftReward.gd 第二批合成连接线表现拆分记录

日期：2026-06-06

### 本批目标

本批只拆合成面板槽位之间的连接线显示。它只根据主卡槽、副卡槽和结果槽是否已有预览卡，更新 `Line2D.points`；不判断配方，不创建卡牌，也不修改合成选择状态。

目标函数范围：
```text
_update_connection_lines()
```

当前触碰的外部节点和接口：
```text
connection_lines.points
slot1.position / slot1.size
slot2.position / slot2.size
result_slot.position / result_slot.size
```

### 新增模块

```text
scene/in_scene/rewards/CraftConnectionLinePresenter.gd
```

模块边界：
- `CraftConnectionLinePresenter.gd` 只负责合成面板槽位之间的连接线显示。
- 它不判断配方，不创建卡牌，也不修改合成选择状态。
- `CraftReward.gd` 保留 `_update_connection_lines()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
连接线清空、三个槽位中心点计算和 PackedVector2Array 组装现在由 CraftConnectionLinePresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## CraftReward.gd 第三批结果描述面板样式拆分记录

日期：2026-06-06

### 本批目标

本批只拆合成结果描述面板的基础样式配置。它只配置 `PanelContainer` 和 `RichTextLabel` 的样式、尺寸策略和文本颜色；不读取卡牌描述，不计算面板位置，也不改变合成状态。

目标函数范围：
```text
_setup_result_description_panel()
```

当前触碰的外部节点和接口：
```text
result_description_panel
result_description_label
StyleBoxFlat
add_theme_stylebox_override()
add_theme_color_override()
```

### 新增模块

```text
scene/in_scene/rewards/CraftResultDescriptionPanelPresenter.gd
```

模块边界：
- `CraftResultDescriptionPanelPresenter.gd` 只负责合成结果描述面板的基础样式配置。
- 它不读取卡牌描述，不计算面板位置，也不改变合成状态。
- `CraftReward.gd` 保留 `_setup_result_description_panel()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
结果描述面板的 panel 样式、label 尺寸策略、BBCode 开关和默认文字颜色现在由 CraftResultDescriptionPanelPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## ShopManager.gd 第一批商店定价显示拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `ShopManager.gd` 的商店定价与侧边栏价格标签显示。它只计算商品价格、刷新费用、升级费用，并写入价格 Label 文案；不消费时间币，不生成商品，也不处理购买、刷新或升级流程。

目标函数范围：
```text
_calculate_card_price(slot_index)
_update_price_display()
```

当前触碰的数据和节点：
```text
base_price
price_increment
refresh_base_cost
upgrade_base_cost
refresh_count
upgrade_count
label_refresh_cost
label_upgrade_cost
```

### 新增模块

```text
scene/in_scene/rewards/ShopPricingPresenter.gd
```

模块边界：
- `ShopPricingPresenter.gd` 只负责商店价格计算和价格标签显示。
- 它不消费时间币，不生成商品，也不处理购买或刷新升级流程。
- `ShopManager.gd` 保留 `_calculate_card_price()` 和 `_update_price_display()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
商品价格、刷新费用、升级费用和两个价格标签的文案写入现在由 ShopPricingPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## ShopManager.gd 第二批时代权重选择拆分记录

日期：2026-06-06

### 本批目标

本批只拆商店根据权重随机选择目标时代的规则。它只根据基础时代和四个权重返回目标时代；不读取 CardDataPool，不创建卡牌，也不处理商店刷新、升级或购买。

目标函数范围：
```text
_select_card_by_era_weight(base_era) 中的 era_weights 构建、无效时代过滤、总权重计算和随机时代选择
```

当前触碰的数据：
```text
base_era
weight_previous_era
weight_current_era
weight_next_era
weight_next_next_era
```

### 新增模块

```text
scene/in_scene/rewards/ShopEraWeightSelector.gd
```

模块边界：
- `ShopEraWeightSelector.gd` 只负责根据商店权重随机选择目标时代。
- 它不读取 CardDataPool，不创建卡牌，也不处理商店刷新或购买。
- `ShopManager.gd` 仍负责根据选中的时代调用 `_get_cards_by_era()` 并从卡池里随机取卡。

### 本批删除或收口的重复点

删除原因：
```text
时代权重表构建、无效时代过滤、总权重计算和随机时代命中逻辑现在由 ShopEraWeightSelector 统一维护。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Invalid call 或 Invalid access。
```

## CraftReward.gd 第四批槽位预览布局拆分记录

日期：2026-06-06

### 本批目标

本批只拆合成槽位预览锚点的布局辅助逻辑。它只负责给主卡槽、副卡槽和结果槽的 `CardAnchor` 应用边距，并在锚点尺寸未初始化时返回默认卡牌尺寸；不创建预览卡，不读取合成配方，也不修改槽位选择状态。

目标函数范围：
```text
_apply_preview_padding()
_apply_padding_to_anchor(anchor)
_get_anchor_preview_size(anchor)
```

当前触碰的数据和节点：
```text
slot1_anchor
slot2_anchor
result_anchor
slot_preview_padding
card_display_size
```

### 新增模块

```text
scene/in_scene/rewards/CraftSlotPreviewLayoutPresenter.gd
```

模块边界：
- `CraftSlotPreviewLayoutPresenter.gd` 只负责合成槽位预览锚点的边距和尺寸兜底。
- 它不创建预览卡，不读取合成配方，也不修改 `slot_entries`、`slot_preview_cards` 或 `current_result_card_id`。
- `CraftReward.gd` 保留 `_apply_preview_padding()`、`_apply_padding_to_anchor()` 和 `_get_anchor_preview_size()` 旧入口，内部转发给新模块。

### 本批删除或收口的重复点

删除原因：
```text
三个槽位锚点的 padding 写入和预览尺寸兜底现在由 CraftSlotPreviewLayoutPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 CRLF/LF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## CraftReward.gd 第五批结果描述定位拆分记录

日期：2026-06-06

### 本批目标

本批只拆合成结果描述面板的尺寸计算和屏幕内定位。它保留 `_position_result_description_panel()` 作为旧入口，只把重置尺寸、等待布局帧、计算面板宽高和左右/底部防溢出逻辑交给新模块；不写入描述文本，不配置面板样式，也不修改合成结果或槽位状态。

目标函数范围：
```text
_position_result_description_panel()
```

当前触碰的数据和节点：
```text
result_description_panel
result_description_label
result_preview_card
result_slot
result_tooltip_offset_x
result_tooltip_offset_y
result_tooltip_max_width
get_viewport().get_visible_rect().size
```

### 新增模块

```text
scene/in_scene/rewards/CraftResultDescriptionPositionPresenter.gd
```

模块边界：
- `CraftResultDescriptionPositionPresenter.gd` 只负责合成结果描述面板的尺寸计算和屏幕内定位。
- 它不写入描述文本，不配置面板样式，也不读取或修改 `current_result_card_id`。
- `CraftReward.gd` 保留 `_position_result_description_panel()` 旧入口，并继续以 `await` 等待新模块完成两帧布局测量。

### 本批删除或收口的重复点

删除原因：
```text
结果描述面板的尺寸重置、文本最小宽度测量、面板宽高计算和屏幕边界修正现在由 CraftResultDescriptionPositionPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 CRLF/LF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第四批顶部锚点布局拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 的顶部锚点布局逻辑。它只根据网格宽高、格子尺寸、间距和顶部预留空间计算 TimelineUI 的锚点偏移、缩放中心和 `GridBackground` 对齐；不创建行动块，不处理展开动画，也不读取 TimelineManager 数据。

目标函数范围：
```text
_apply_anchor_layout()
set_top_reserved_space(px) 仍保留旧入口并继续调用 _apply_anchor_layout()
```

当前触碰的数据和节点：
```text
grid_background
grid_width
grid_height
slot_size
spacing
margin_top_preset
top_reserved_space
offset_left / offset_right / offset_top / offset_bottom
pivot_offset
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineLayoutController.gd
```

模块边界：
- `TimelineLayoutController.gd` 只负责 TimelineUI 的顶部锚点布局和背景网格对齐。
- 它不创建行动块，不处理展开动画，也不读取 TimelineManager 数据。
- `timeline_ui.gd` 保留 `_apply_anchor_layout()` 旧入口，原有 `_ready()`、展开/收起回调和 `set_top_reserved_space()` 仍通过旧入口触发布局刷新。

### 本批删除或收口的重复点

删除原因：
```text
TimelineUI 锚点预设、物理宽高计算、offset 写入、pivot 设置和 GridBackground 对齐现在由 TimelineLayoutController 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第五批网格预览样式拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 的空背景格子拖拽预览样式。它根据拖拽形状、原点、边界和敌方意图占用情况给 `grid_cells` 写入蓝色、红色或红色边框预览，并负责清理预览样式；不修改 TimelineManager 数据，不创建行动块，也不处理卡牌放置规则。

目标函数范围：
```text
update_grid_preview(shape_coords, origin_pos, is_valid)
clear_grid_preview()
```

当前触碰的数据和节点：
```text
grid_cells
timeline_manager.grid
grid_width
grid_height
grid_cell_default_color
create_tween()
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineGridPreviewPresenter.gd
```

模块边界：
- `TimelineGridPreviewPresenter.gd` 只负责 TimelineUI 空背景格子的拖拽预览样式。
- 它不修改 TimelineManager 数据，不创建行动块，也不处理卡牌放置规则。
- `timeline_ui.gd` 保留 `update_grid_preview()` 和 `clear_grid_preview()` 旧入口，供 DragShapeController 和 TimelineClearEffect 继续调用。

### 本批删除或收口的重复点

删除原因：
```text
覆盖格子计算、边界判断、敌方意图重叠检测、预览颜色写入、缩放 tween 和清理默认样式现在由 TimelineGridPreviewPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第六批 TimelineManager 查找拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 查找 `TimelineManager` 的桥接逻辑。它保留 `_find_timeline_manager()` 旧入口，只把按分组、节点名、父节点链和 managers 分组兜底查找的逻辑交给新模块；不读取时间轴数据，不连接信号，也不创建或移除行动块。

目标函数范围：
```text
_find_timeline_manager()
```

当前触碰的数据和接口：
```text
get_tree()
get_nodes_in_group("TimelineManager")
find_child("TimelineManager", true, false)
get_parent()
get_nodes_in_group("managers")
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineManagerLocator.gd
```

模块边界：
- `TimelineManagerLocator.gd` 只负责为 TimelineUI 查找 TimelineManager 节点。
- 它不读取 TimelineManager 的 grid，不连接 action_placed 或 timeline_cleared，也不创建行动块。
- `timeline_ui.gd` 保留 `_find_timeline_manager()` 旧入口，`_ready()` 中的信号连接仍由主脚本负责。

### 本批删除或收口的重复点

删除原因：
```text
TimelineManager 的分组查找、节点名查找、父节点链查找和 managers 分组兜底查找现在由 TimelineManagerLocator 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第七批敌方意图 Overlay 表现拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `timeline_ui.gd` 中敌方意图 `EnemyIntentOverlay` 的材质创建、条纹 shader 参数写入和显示/隐藏。它不决定何时预览，不创建行动块，不修改 TimelineManager 数据，也不处理移除动画。

目标函数范围：
```text
_on_action_placed(action) 中的敌方意图 overlay material 创建
_set_enemy_intent_overlay_visible(container, visible, color)
_configure_enemy_intent_timeline_material(material)
```

当前触碰的数据和节点：
```text
enemy_intent_timeline_shader
enemy_intent_pulse_speed
enemy_intent_pulse_min_alpha
enemy_intent_pulse_max_alpha
enemy_intent_stripe_color
enemy_intent_stripe_speed
enemy_intent_stripe_density
enemy_intent_stripe_width
enemy_intent_stripe_strength
EnemyIntentOverlay
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineEnemyIntentOverlayPresenter.gd
```

模块边界：
- `TimelineEnemyIntentOverlayPresenter.gd` 只负责时间轴敌方意图 Overlay 的材质和显示状态。
- 它不创建行动块，不修改 TimelineManager 数据，也不决定何时进入或退出预览。
- `timeline_ui.gd` 保留 `_set_enemy_intent_overlay_visible()` 和 `_configure_enemy_intent_timeline_material()` 旧入口，并继续负责预览状态字段。

### 本批删除或收口的重复点

删除原因：
```text
敌方意图 overlay 的 ShaderMaterial 创建、pulse 参数写入、条纹参数刷新和批量显示/隐藏现在由 TimelineEnemyIntentOverlayPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## timeline_ui.gd 第八批行动方格放置动画拆分记录

日期：2026-06-06

### 本批目标

本批只拆单个时间轴行动方格的放置入场动画。它保留 `_animate_block_placement()` 旧入口，只把复制独立 StyleBox、设置透明初始色、补间到目标色和缩放弹入动画交给新模块；不创建行动容器，不修改 TimelineManager 数据，也不处理敌方意图入场动画。

目标函数范围：
```text
_animate_block_placement(block, target_color)
```

当前触碰的数据和节点：
```text
block.get_theme_stylebox("panel")
StyleBoxFlat.duplicate()
block.add_theme_stylebox_override("panel", style)
style.bg_color
style.border_color
create_tween()
block.scale
```

### 新增模块

```text
scene/in_scene/timeline/ui_modules/TimelineBlockPlacementAnimator.gd
```

模块边界：
- `TimelineBlockPlacementAnimator.gd` 只负责单个时间轴行动方格的放置入场动画。
- 它不创建行动容器，不修改 TimelineManager 数据，也不处理敌方意图入场动画。
- `timeline_ui.gd` 保留 `_animate_block_placement()` 旧入口，并通过 `Callable` 传入 `create_tween()`。

### 本批删除或收口的重复点

删除原因：
```text
行动方格放置时的样式副本创建、透明初始色、背景色 tween 和缩放 tween 现在由 TimelineBlockPlacementAnimator 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 的错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十三批放置校验收口记录

日期：2026-06-06

### 本批目标

本批只收口拖拽时间轴放置校验的调用路径。它让时间轴 hover 预览和鼠标确认放置都走已有的 `DragPlacementQueryService`，不修改拖拽状态机，不创建 `TimelineAction`，也不改放置动画或卡牌回手流程。

目标函数范围：
```text
_handle_timeline_hover(mouse_pos)
try_place_shape()
_is_placement_valid(grid_pos)
DragPlacementQueryService.is_placement_valid(...)
```

当前触碰的数据和节点：
```text
timeline_ui
timeline_manager
current_shape_coords
is_timeline_clear_mode
hover_grid_pos / origin_pos
TimelineClearEffectUtil
```

### 调整内容

```text
scene/in_scene/DragShapeController.gd
scene/in_scene/drag_modules/DragPlacementQueryService.gd
```

模块边界：
- `DragPlacementQueryService.gd` 继续只回答“指定格子是否可放置”，不执行放置，不创建行动，也不修改时间轴或卡牌状态。
- `DragShapeController.gd` 保留 `_is_placement_valid()` 旧入口，并让 hover 预览与确认放置都通过旧入口转发给服务。
- clear 类即时卡牌的边界判断不再被 `timeline_manager` 有效性提前拦截，保持旧逻辑只依赖时间轴网格尺寸。

### 本批删除或收口的重复点

删除原因：
```text
hover 预览和确认放置里直接调用 TimelineClearEffectUtil / timeline_manager.is_placement_valid 的判断，现在统一收口到 DragPlacementQueryService。
这样后续修改拖拽放置规则时，只需要维护一个查询入口。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## 奖励页第三批临时牌堆工厂拆分记录

日期：2026-06-06

### 本批目标

本批只拆四个奖励页面重复的临时幽灵牌堆创建逻辑。保留各页面原有 `_create_temp_pile()` 入口，只把空 `deck_manager` 检查、`pile.tscn` 实例化和挂到 `deck_manager` 子节点这三步收口到统一工厂；不改变卡牌数据生成、奖励选择、商店购买、合成结果或移除流程。

目标函数范围：

```text
AcquireReward.gd::_create_temp_pile()
RemoveReward.gd::_create_temp_pile()
CraftReward.gd::_create_temp_pile()
ShopManager.gd::_create_temp_pile()
```

当前触碰的数据和节点：

```text
deck_manager
res://addons/card-framework/pile.tscn
临时 Pile 子节点
```

### 新增模块

```text
scene/in_scene/rewards/factory/RewardTempPileFactory.gd
```

模块边界：

- `RewardTempPileFactory.gd` 只负责为奖励页面创建临时幽灵牌堆。
- 它不生成卡牌，不读取卡牌数据，也不决定奖励页面的选择、确认、购买或合成流程。
- 四个奖励页继续保留旧 `_create_temp_pile()` 入口，后续调用点无需同时迁移，降低本批风险面。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 deck_manager 空值检查、pile.tscn 实例化和 add_child 挂载逻辑。
现在这些重复创建步骤由 RewardTempPileFactory 统一维护，各页面只传入自己的 deck_manager 和日志标签。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P0 已完成：`.obsidian/` 已加入忽略，`in_scene.tscn` 中调试按钮可见性修改已提交。

文件夹归档已完成：`drag_modules`、`timeline/ui_modules` 和 `rewards` 下已拆出的模块已按 animation、bridges、coordinates、factory、grid、layout、presenters、rules、ui 等职责归档，整体结构开始对齐 hex map 的分类方式。

P1 奖励页拆分已推进三批：已完成奖励页 CardManager 定位器、Tooltip 适配器、临时牌堆工厂拆分。当前四个奖励页仍保留旧入口，外部行为应保持不变。

### 下一步打算

下一批优先评估奖励页重复的卡牌数据与卡面素材提取逻辑。候选风险面是 `_steal_card_data()`、`_extract_front_texture()` 和 `_extract_card_description()`，其中 `_steal_card_data()` 涉及真实卡牌实例、异步等待和临时牌堆清理，风险更高；更稳妥的下一批可能先拆 `_extract_front_texture()` 与描述提取，继续维持一次只碰一个清晰风险面。

## 奖励页第四批卡牌描述提取拆分记录

日期：2026-06-06

### 本批目标

本批只拆四个奖励页面重复的卡牌效果文本读取逻辑。保留各页面原有 `_extract_card_description(real_card)` 入口，只把 `setup_card_data()` 后等待一帧、优先读取 `get_parsed_description()`、回退读取 `raw_description`、最终读取 `card_info["效果"]` 的顺序收口到统一提取器；不改变卡牌实例创建、贴图提取、关键词复制、临时牌堆清理或奖励确认流程。

目标函数范围：

```text
AcquireReward.gd::_extract_card_description(real_card)
RemoveReward.gd::_extract_card_description(real_card)
CraftReward.gd::_extract_card_description(real_card)
ShopManager.gd::_extract_card_description(real_card)
```

当前触碰的数据和节点：

```text
real_card
setup_card_data()
get_parsed_description()
raw_description
card_info["效果"]
owner.get_tree().process_frame
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RewardCardDescriptionExtractor.gd
```

模块边界：

- `RewardCardDescriptionExtractor.gd` 只负责从真实卡牌节点读取奖励页展示用效果文本。
- 它不创建真实卡牌，不复制关键词，不修改 DraftCard，也不参与 Tooltip、飞入牌库、商店购买、合成或移除流程。
- 四个奖励页继续保留旧 `_extract_card_description(real_card)` 入口，调用点暂不迁移，避免把描述读取和 `_steal_card_data()` 的异步流程混在同一批。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 setup_card_data 后等待一帧、解析描述优先级、raw_description 回退和 card_info["效果"] 回退逻辑。
现在这些重复读取步骤由 RewardCardDescriptionExtractor 统一维护，各页面只传入真实卡牌节点和自身 owner。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页拆分已推进四批：CardManager 定位器、Tooltip 适配器、临时牌堆工厂、卡牌描述提取器已经完成。四个奖励页现在仍保留旧方法入口，页面外部行为和调用结构保持稳定。

当前仍未拆的高重复区域主要是 `_extract_front_texture()`、`_steal_card_data()`、飞入牌库动画、ShopManager 的价格与生成流程细节，以及合成页结果描述面板内部的少量 UI 描述读取逻辑。

### 下一步打算

下一批建议继续选择低到中风险的 `_extract_front_texture()`。它会触碰 `front_face_texture`、`preloaded_cards`、`card_info["front_image"]` 和 `card_asset_dir`，但仍可以保持旧入口转发，不碰真实卡牌创建与临时牌堆生命周期。`_steal_card_data()` 暂时排在后面，因为它同时包含 card_factory 创建、等待真实卡牌 ready、关键词复制、描述和贴图赋值，是更大的风险面。

## 奖励页第五批卡面贴图提取拆分记录

日期：2026-06-06

### 本批目标

本批只拆四个奖励页面重复的卡面贴图读取逻辑。保留各页面原有 `_extract_front_texture(real_card, card_id)` 入口，只把直接读取 `FrontFace/TextureRect`、读取 `front_face_texture`、等待一帧后再次读取、从 `preloaded_cards` 缓存回退、最后通过 `card_info["front_image"]` 与 `card_asset_dir` 加载资源这几步收口到统一提取器；不改变真实卡牌创建、关键词复制、描述读取、DraftCard 赋值、临时牌堆清理或飞入牌库流程。

目标函数范围：

```text
AcquireReward.gd::_extract_front_texture(real_card, card_id)
RemoveReward.gd::_extract_front_texture(real_card, card_id)
CraftReward.gd::_extract_front_texture(real_card, card_id)
ShopManager.gd::_extract_front_texture(real_card, card_id)
```

当前触碰的数据和节点：

```text
real_card
FrontFace/TextureRect
front_face_texture
deck_manager.card_factory
preloaded_cards
card_info["front_image"]
card_asset_dir
owner.get_tree().process_frame
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RewardCardTextureExtractor.gd
```

模块边界：

- `RewardCardTextureExtractor.gd` 只负责从真实卡牌节点或卡牌工厂缓存中读取奖励页卡面贴图。
- 它不创建真实卡牌，不修改 DraftCard，不复制关键词，也不参与奖励确认、商店购买、合成、移除或飞入动画。
- 四个奖励页继续保留旧 `_extract_front_texture(real_card, card_id)` 入口，调用点保持不变。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 FrontFace/TextureRect 读取、front_face_texture 回退、等待一帧后二次读取、preloaded_cards 缓存读取和 front_image 路径加载。
现在这些重复读取步骤由 RewardCardTextureExtractor 统一维护，各页面只传入真实卡牌节点、card_id、deck_manager 和自身 owner。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页共享读取链路继续收口：CardManager 定位、Tooltip 适配、临时牌堆创建、卡牌描述提取、卡面贴图提取都已完成。四个奖励页的高重复 `_extract_card_description()` 与 `_extract_front_texture()` 已变成旧入口转发，页面主体更接近“流程编排”职责。

当前仍未拆的主要风险面是 `_steal_card_data()`、飞入牌库动画、奖励页页面状态清理与 ShopManager 商品生成/价格刷新细节。`_object_has_property()` 仍被关键词、时代、贴图和合成结果描述读取使用，暂时保留。

### 下一步打算

下一批建议开始评估 `_steal_card_data()`，但不要一次把四页全部逻辑改成大模块。更稳妥的路径是先提取“真实卡牌创建与等待 ready”的小助手，保留关键词、描述、贴图赋值和临时牌堆清理由原页面处理；如果这一步验证稳定，再继续提取 DraftCard 数据填充器。

## 奖励页第六批真实卡牌生成助手拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `_steal_card_data()` 里“让 card_factory 真实生产一张牌、等待一帧、从临时牌堆取回刚创建真实卡牌”的小片段。四个奖励页面继续保留 `_steal_card_data(card_id, draft_card, temp_pile)` 主入口，描述读取、关键词复制、贴图赋值、从临时牌堆移除真实卡牌和 `queue_free()` 清理都仍留在原页面中。

目标函数范围：

```text
AcquireReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
RemoveReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
CraftReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
ShopManager.gd::_steal_card_data(card_id, draft_card, temp_pile)
```

当前触碰的数据和节点：

```text
deck_manager.card_factory
card_factory.create_card(card_id, temp_pile)
temp_pile._held_cards
owner.get_tree().process_frame
```

### 新增模块

```text
scene/in_scene/rewards/factory/RewardRealCardSpawner.gd
```

模块边界：

- `RewardRealCardSpawner.gd` 只负责把 `card_factory` 生成的真实卡牌临时放入奖励页幽灵牌堆，并返回刚创建的真实卡牌节点。
- 它不复制描述、关键词或贴图，不修改 DraftCard，也不负责从临时牌堆移除真实卡牌。
- 四个奖励页面保留各自的依赖检查和错误提示风格，避免本批顺手统一日志或改变失败分支表现。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 card_factory.create_card、等待 process_frame、检查 temp_pile 是否还有效、从 temp_pile._held_cards 取最后一张真实卡牌。
现在真实卡牌生成与取回由 RewardRealCardSpawner 统一维护，页面只在 _steal_card_data 中继续编排后续数据填充和清理。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页拆分已经覆盖共享定位、Tooltip、临时牌堆创建、描述读取、贴图读取和真实卡牌生成取回。`_steal_card_data()` 仍在四个页面中，但内部最敏感的真实卡牌创建片段已经有独立模块承接，后续可以更安全地继续拆数据填充。

当前未拆完的奖励页重复点主要剩下关键词复制、DraftCard 数据填充顺序、临时真实卡牌清理和奖励页飞入牌库动画。ShopManager 仍有商品生成、价格刷新和调试日志偏重的问题。

### 下一步打算

下一批建议评估“真实卡牌清理”或“DraftCard 数据填充”二选一。更稳的选择是先抽取 `RewardRealCardCleaner`，只收口 `temp_pile.remove_card(real_card)` 与 `real_card.queue_free()`；如果直接抽 DraftCard 数据填充器，会同时碰描述、关键词、贴图和赋值顺序，风险更高。

## 奖励页第七批真实卡牌清理拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `_steal_card_data()` 末尾重复的真实卡牌清理逻辑。四个奖励页面继续保留 `_steal_card_data(card_id, draft_card, temp_pile)` 主入口，真实卡牌生成、描述读取、关键词复制、贴图读取和 DraftCard 赋值顺序都不在本批改变。

目标函数范围：

```text
AcquireReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
RemoveReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
CraftReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
ShopManager.gd::_steal_card_data(card_id, draft_card, temp_pile)
```

当前触碰的数据和节点：

```text
temp_pile
real_card
temp_pile.remove_card(real_card)
real_card.queue_free()
```

### 新增模块

```text
scene/in_scene/rewards/factory/RewardRealCardCleaner.gd
```

模块边界：

- `RewardRealCardCleaner.gd` 只负责从奖励页临时幽灵牌堆移除真实卡牌并释放节点。
- 它不读取卡牌数据，不修改 DraftCard，不判断奖励状态，也不管理临时牌堆自身的生命周期。
- 四个奖励页面只把末尾清理两行替换为旧流程中的清理调用，数据填充仍留在页面内。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 temp_pile.remove_card(real_card) 与 real_card.queue_free()。
现在真实卡牌清理由 RewardRealCardCleaner 统一维护，同时在清理前用 is_instance_valid 做更稳的局部保护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页 `_steal_card_data()` 已经拆出真实卡牌生成取回和真实卡牌清理两个低风险模块。再加上前面完成的描述、贴图、Tooltip、CardManager 定位和临时牌堆创建，四个奖励页中可共享的基础设施已经大部分归档到 bridges、factory、presenters。

当前仍未拆的核心重复点是 DraftCard 数据填充顺序本身：`raw_description` 赋值、`active_keywords` 复制、`texture` 赋值，以及页面级飞入牌库动画。这个点虽然重复，但一次抽取会跨越多个已拆模块调用，建议下一批先做详细分析再决定是否拆。

### 下一步打算

下一步先检查文件管理：`scene/in_scene/rewards` 根目录下仍有一些已归档脚本遗留的 `.gd.uid` 文件，例如早期移动到 presenters、rules、bridges 后留下的 UID 文件。需要确认这些 UID 是否被 Godot 仍引用，若只是旧位置遗留文件，再单独做一批“文件管理清理”提交；不要和 DraftCard 数据填充拆分混在同一批。

## 奖励页第八批 DraftCard 数据填充拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `_steal_card_data()` 中重复的 DraftCard 数据填充顺序。四个奖励页面继续保留 `_steal_card_data(card_id, draft_card, temp_pile)` 主入口，真实卡牌生成、失败分支提示、临时真实卡牌清理和奖励页选择流程都不在本批改变。

目标函数范围：

```text
AcquireReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
RemoveReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
CraftReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
ShopManager.gd::_steal_card_data(card_id, draft_card, temp_pile)
```

当前触碰的数据和节点：

```text
draft_card.raw_description
draft_card.active_keywords
draft_card.texture
real_card.active_keywords
_extract_card_description(real_card)
_extract_front_texture(real_card, card_id)
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RewardDraftCardDataApplier.gd
```

模块边界：

- `RewardDraftCardDataApplier.gd` 只负责把真实卡牌读取到的数据写入奖励页 DraftCard。
- 它不创建真实卡牌，不清理临时牌堆，不判断奖励页是否可确认，也不处理商店购买、合成或移除流程。
- 描述与贴图读取仍通过页面旧入口传入，避免本批同时改动前面已经拆出的读取模块。

### 本批删除或收口的重复点

删除原因：

```text
四个奖励页原本重复维护 raw_description 赋值、active_keywords 复制和 texture 赋值。
现在 DraftCard 数据填充顺序由 RewardDraftCardDataApplier 统一维护，页面只保留真实卡牌生命周期和奖励流程编排。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页 `_steal_card_data()` 现在已经拆成真实卡牌生成、DraftCard 数据填充、真实卡牌清理三段共享模块，四个页面里的重复数据窃取逻辑显著减少。奖励页基础模块已集中在 `bridges`、`factory`、`presenters`、`rules`，文件归档状态继续对齐 hex map 的分类模式。

本轮还清理了 `scene/in_scene/rewards` 根目录下 12 个被 `.gitignore` 忽略的本地 `.gd.uid` 遗留文件；这些文件不在 git 跟踪中，因此不产生提交。

### 下一步打算

下一批建议重新扫描奖励页剩余重复点。候选方向有两个：其一是奖励页飞入牌库动画，它仍可能在 AcquireReward 与 ShopManager 之间重复；其二是 ShopManager 商品生成与价格刷新流程，属于更偏页面专属的拆分。优先级上先查飞入动画是否同形，若风险面清晰再拆。

## 奖励页第九批飞入牌库视觉动画拆分记录

日期：2026-06-06

### 本批目标

本批只拆获取奖励页和商店页重复的卡牌飞入牌库视觉动画。保留 `_fly_to_deck_pile(card)` 旧入口，只把红色拖影、飞行 tween、缩放、旋转、拖影计时器和动画结束时的视觉节点清理交给统一动画 runner；不改变购买扣费、加入牌组、同步抽牌堆、商店列表移除或奖励页关闭流程。

目标函数范围：

```text
AcquireReward.gd::_fly_to_deck_pile(card)
ShopManager.gd::_fly_to_deck_pile(card)
```

当前触碰的数据和节点：

```text
card.global_position
card.scale
card.rotation
Line2D 拖影
Timer 拖影采样
trail_color
trail_width
fly_duration
动画完成回调
```

### 新增模块

```text
scene/in_scene/rewards/animation/RewardCardFlyToDeckAnimator.gd
```

模块边界：

- `RewardCardFlyToDeckAnimator.gd` 只负责奖励页卡牌飞入牌库的视觉动画。
- 它不写入牌组数据，不同步抽牌堆，也不决定奖励页关闭或商店状态。
- `AcquireReward.gd` 与 `ShopManager.gd` 继续保留各自的完成回调，页面专属的入库、同步、关闭和商店列表移除逻辑不在本批迁移。

### 本批删除或收口的重复点

删除原因：

```text
AcquireReward 和 ShopManager 原本重复维护 Line2D 拖影、Curve 宽度曲线、飞行 tween、拖影 Timer 和视觉清理。
现在这些视觉动画步骤由 RewardCardFlyToDeckAnimator 统一维护，页面只负责给出目标位置和动画完成后的业务回调。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页共享模块继续完善：奖励卡牌数据窃取链路、卡牌飞入牌库视觉动画都已经模块化。奖励页目录新增 `animation` 分类，和此前的 `bridges`、`factory`、`presenters`、`rules` 形成更完整的文件归档。

目前 AcquireReward 和 ShopManager 的飞入动画视觉重复已收口，但入库同步逻辑仍各自保留。该同步逻辑虽然重复，但涉及奖励页关闭和商店状态移除差异，下一批需要先分析是否能只抽“入库同步”这一小块。

### 下一步打算

下一批优先评估 `add_card_to_deck` 与 `sync_runtime_deck_from_global` 的重复同步逻辑。如果两页完全一致，可以只抽 `RewardDeckSyncBridge`，让页面继续处理关闭和商店列表移除；如果有隐性差异，就转向 ShopManager 内部商品生成流程拆分。

## 奖励页第十批新增卡牌入库同步拆分记录

日期：2026-06-06

### 本批目标

本批只拆获取奖励页和商店页在飞入动画完成后重复的“新增卡牌写入 deck_manager 并同步局内抽牌堆”逻辑。保留两个页面各自的 `_on_fly_to_deck_finished(...)` 入口，奖励页关闭、领取标记、商店列表移除和价格映射清理仍留在原页面中。

目标函数范围：

```text
AcquireReward.gd::_on_fly_to_deck_finished(card_id)
ShopManager.gd::_on_fly_to_deck_finished(card_id, card)
```

当前触碰的数据和节点：

```text
deck_manager.add_card_to_deck(card_id)
MainBoard
main.deck_pile
deck_manager.sync_runtime_deck_from_global(main.deck_pile)
main.update_counts_and_ui()
```

### 新增模块

```text
scene/in_scene/rewards/bridges/RewardDeckSyncBridge.gd
```

模块边界：

- `RewardDeckSyncBridge.gd` 只负责奖励页把新增卡牌写入 `deck_manager` 并同步局内抽牌堆。
- 它不关闭奖励页，不修改商店列表，也不处理移除或合成的牌组变更。
- RemoveReward 与 CraftReward 里也有运行时抽牌堆同步，但它们属于移除/合成后的牌组变更，本批不混入。

### 本批删除或收口的重复点

删除原因：

```text
AcquireReward 和 ShopManager 原本重复维护 add_card_to_deck、MainBoard 查找、sync_runtime_deck_from_global、update_counts_and_ui 和成功日志。
现在新增卡牌入库同步由 RewardDeckSyncBridge 统一维护，页面只负责动画完成后的页面专属收尾。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页新增卡牌流程已经拆成：飞入视觉动画、入库同步、页面专属收尾三段。AcquireReward 和 ShopManager 的重复主体进一步减少，`bridges` 目录现在承担 CardManager 定位、商店全局节点查找和奖励牌组同步三类跨节点桥接职责。

当前仍保留在页面内的同步逻辑主要是 RemoveReward 和 CraftReward 的“牌组变更后同步运行时抽牌堆”。它和新增卡牌入库不同，不应直接复用本批 bridge，下一步需要单独看是否能抽一个更泛用的 runtime deck sync helper。

### 下一步打算

下一批建议评估 RemoveReward 与 CraftReward 的 `sync_runtime_deck_from_global` 片段，目标只拆“已修改 GlobalDB/player_deck 后刷新运行时抽牌堆和 UI”的同步 helper，不碰移除卡牌和合成配方写入。

## 奖励页第十一批运行时抽牌堆同步拆分记录

日期：2026-06-06

### 本批目标

本批只拆删除奖励页和合成奖励页在牌组数据已经变更后重复的运行时抽牌堆同步逻辑。保留 RemoveReward 的移除策略和 CraftReward 的合成结果写入策略，只把 `MainBoard` 查找、`sync_runtime_deck_from_global(main.deck_pile)` 和 `update_counts_and_ui()` 收口到已有 `RewardDeckSyncBridge`。

目标函数范围：

```text
RemoveReward.gd::_remove_card_from_deck(card_id)
CraftReward.gd::_apply_crafting_result_to_deck()
RewardDeckSyncBridge.gd::sync_runtime_deck(owner, deck_manager)
```

当前触碰的数据和节点：

```text
MainBoard
main.deck_pile
deck_manager.sync_runtime_deck_from_global(main.deck_pile)
main.update_counts_and_ui()
```

### 调整模块

```text
scene/in_scene/rewards/bridges/RewardDeckSyncBridge.gd
```

模块边界：

- `RewardDeckSyncBridge.gd` 继续只负责奖励页和局内抽牌堆之间的同步桥接。
- 新增 `sync_runtime_deck(owner, deck_manager)` 只刷新运行时抽牌堆和 UI，不修改 `GlobalDB.player_deck`。
- RemoveReward 和 CraftReward 仍各自决定如何移除、添加或替换牌组数据。

### 本批删除或收口的重复点

删除原因：

```text
RemoveReward 和 CraftReward 原本重复维护 MainBoard 查找、deck_pile 判空、sync_runtime_deck_from_global 和 update_counts_and_ui。
现在运行时抽牌堆刷新由 RewardDeckSyncBridge.sync_runtime_deck 统一维护，页面只负责自己的牌组数据变更。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页的牌组同步桥接已经覆盖新增卡牌、删除卡牌和合成结果三类流程。页面本身保留业务决策，bridge 只负责跨节点同步，边界比较清楚。

当前奖励页剩余可拆点开始偏向页面专属逻辑：ShopManager 的商品生成、刷新/升级价格显示，RemoveReward 的删除选择 UI，CraftReward 的合成状态和结果面板刷新。共享基础设施拆分已经接近一个阶段性收口点。

### 下一步打算

下一步先做全局扫描，重新列 P1/P2 优先级：确认奖励页是否还值得继续拆，还是该转向 ShopManager 页面专属流程、CraftReward 状态机化，或回到 in_scene 其他模块的文件管理与耦合点优化。

## 奖励页第十二批商店调试日志收口记录

日期：2026-06-06

### 本批目标

本批只收口 `ShopManager.gd` 中初始化和商品生成阶段的调试输出。默认仍然打印同样的日志内容，保持运行行为和排查信息不变；不改变商品生成、时代权重、价格计算、购买扣费、刷新升级或 CardManager 查找逻辑。

目标函数范围：

```text
ShopManager.gd::_ready()
ShopManager.gd::_generate_shop_items()
```

当前触碰的数据和节点：

```text
shop_slots_count
shop_columns
shop_grid.columns
base_price
price_increment
refresh_base_cost
upgrade_base_cost
label_refresh_cost
label_upgrade_cost
shop_grid.get_child_count()
current_era
local_era_offset
shop_cards.size()
```

### 新增模块

```text
scene/in_scene/rewards/diagnostics/ShopDebugLogger.gd
```

模块边界：

- `ShopDebugLogger.gd` 只负责商店页面初始化和商品生成阶段的调试输出。
- 它不计算价格，不生成商品，也不改变商店流程。
- 本批只替换初始化/生成阶段的调试日志；购买提示、警告、价格更新日志和 CardManager 查找日志暂时保留在 ShopManager 中。

### 本批删除或收口的重复点

删除原因：

```text
ShopManager 的初始化和商品生成调试输出原本散落在流程中，增加了主流程阅读噪音。
现在这些格式化输出由 ShopDebugLogger 统一维护，后续若要降噪或加开关，可以只改 diagnostics 模块。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

P1 奖励页共享模块已经基本完成，ShopManager 开始进入页面专属清理阶段。当前已新增 `diagnostics` 分类，用来承接不会改变游戏行为的调试与诊断输出。

ShopManager 仍然是奖励页中最大的文件，剩余优化点主要包括商品生成流程、CardManager 查找日志、价格刷新日志和购买流程拆分。下一批应继续选择一个小风险面，避免把商店核心生成和购买逻辑混在一起。

### 下一步打算

下一批建议评估 `ShopManager.gd::_try_find_card_manager()` 的日志和查找流程。它当前有大量查找路径调试输出，可以先只收口到诊断模块或已有 `ShopGlobalNodeFinder` 风格的 bridge；不建议同时改商品生成。

## in_scene 已拆模块文件夹归档记录

日期：2026-06-06

### 本批目标

本批只整理已经拆出的模块目录，让 `scene/in_scene` 下的非 HexMap 模块更接近 `hex_map_modules/` 的职责分层。它只移动文件并更新 `preload()` 路径，不改模块内部逻辑，不改变主脚本公共入口，也不继续拆新职责。

### 归档后的目录职责

```text
scene/in_scene/drag_modules/bridges      拖拽系统节点桥接
scene/in_scene/drag_modules/rules        拖拽形状、放置校验和文本解析规则
scene/in_scene/drag_modules/coordinates  拖拽时间轴坐标和放置目标坐标计算
scene/in_scene/drag_modules/presenters   拖拽视觉表现与预览
scene/in_scene/drag_modules/ui           拖拽期间 UI 和交互开关
scene/in_scene/drag_modules/animation    拖拽拒绝动画与放置动画

scene/in_scene/timeline/ui_modules/bridges     TimelineUI 外部引用定位
scene/in_scene/timeline/ui_modules/layout      TimelineUI 布局与展开收起表现
scene/in_scene/timeline/ui_modules/grid        TimelineUI 网格构建、输入和预览
scene/in_scene/timeline/ui_modules/presenters  TimelineUI 敌人意图覆盖表现
scene/in_scene/timeline/ui_modules/animation   TimelineUI 行动块动画

scene/in_scene/rewards/bridges      奖励页外部节点查找桥接
scene/in_scene/rewards/rules        奖励页规则和权重选择
scene/in_scene/rewards/presenters   奖励页 UI 表现模块
```

### 本批触碰范围

```text
DragShapeController.gd 的 drag_modules preload 路径
timeline_ui.gd 的 timeline/ui_modules preload 路径
CraftReward.gd 和 ShopManager.gd 的奖励辅助模块 preload 路径
已拆辅助模块的文件位置
```

### 暂不处理

```text
in_scene_modules/ 已经有 bridges/cards/ui/turn/settlement/scene_flow 分层，本批不移动。
奖励页主脚本 AcquireReward.gd、RemoveReward.gd、CraftReward.gd、ShopManager.gd 仍留在 rewards 根目录。
后续 P1 新增共用模块时，直接放入 rewards/bridges、rewards/rules 或 rewards/presenters。
```

### 回归检查

```text
旧 flat preload 路径检查通过，未发现已移动辅助模块仍被旧路径引用。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call、Invalid access 或 hides a global script class。
Godot 加载奖励页 craft_reward、shop、acquire_reward、remove_reward 场景退出码均为 0，错误筛选未出现脚本解析、编译或旧全局类缓存冲突。
```

## 奖励页第一批 CardManager 查找桥接拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `AcquireReward.gd` 和 `RemoveReward.gd` 中重复的 CardManager 自动查找逻辑。它保留两个页面原有 `_try_find_card_manager()` 入口，只把 root metadata、current_scene metadata、父节点链和节点名回退查找交给共用桥接模块；不生成奖励卡牌，不改确认获取或删除流程，也不移动卡牌到牌组。

目标函数范围：

```text
AcquireReward.gd::_try_find_card_manager()
RemoveReward.gd::_try_find_card_manager()
```

当前触碰的数据和节点：

```text
deck_manager
get_tree().root
get_tree().current_scene
card_manager metadata
CardManager 节点名回退查找
```

### 新增模块

```text
scene/in_scene/rewards/bridges/RewardCardManagerLocator.gd
```

模块边界：

- `RewardCardManagerLocator.gd` 只负责奖励页面自动查找 CardManager。
- 它不生成奖励卡牌，不修改牌组，也不处理奖励确认或退出流程。
- `AcquireReward.gd` 和 `RemoveReward.gd` 继续保留旧查找入口，并负责把查找结果写入自身 `deck_manager`。

### 本批删除或收口的重复点

删除原因：

```text
AcquireReward 和 RemoveReward 里重复的四段 CardManager 查找现在统一交给 RewardCardManagerLocator。
后续奖励页如果继续接入 CardManager，可以复用同一个桥接模块，而不是继续复制父链和全树查找逻辑。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/acquire_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现脚本解析或编译类错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现脚本解析或编译类错误。
```

## 奖励页第二批 Tooltip 适配拆分记录

日期：2026-06-06

### 本批目标

本批只拆四个奖励页面重复的卡牌 Tooltip 初始化、显示和隐藏逻辑。它保留 `AcquireReward.gd`、`RemoveReward.gd`、`CraftReward.gd` 和 `ShopManager.gd` 原有 `show_tooltip()` / `hide_tooltip()` 入口，只把通用 presenter 创建、统一显示参数和隐藏调用交给新模块；不判断奖励是否可领取，不读取或修改牌组，也不创建奖励卡牌。

目标函数范围：

```text
_setup_tooltip_presenter()
show_tooltip(card)
hide_tooltip(card)
```

当前触碰的数据和节点：

```text
tooltip_presenter
tooltip_config
CardTooltipPresenter
奖励页自身 CanvasLayer
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RewardTooltipAdapter.gd
```

模块边界：

- `RewardTooltipAdapter.gd` 只负责奖励页卡牌 Tooltip 的初始化、显示和隐藏。
- 它不判断奖励是否可领取，不读取或修改牌组，也不创建奖励卡牌。
- 四个奖励页继续保留旧 Tooltip 入口，外部 DraftCard / CustomCard 调用协议不变。

### 当前优化进度

```text
P0 已完成：docs/.obsidian 已忽略，CombatVictoryDebugButton 默认隐藏已提交。
in_scene 已拆模块归档已完成：drag/timeline/rewards 的辅助模块已经按职责目录整理。
P1 奖励页共用能力已完成两批：CardManager locator 和 RewardTooltipAdapter。
奖励页仍剩余重复点：临时牌堆创建、卡牌数据窃取、front texture/description 提取、飞入动画。
```

### 下一步打算

```text
下一批优先拆 RewardTempPileFactory，只收口 Acquire/Remove/Shop/Craft 中重复的 _create_temp_pile()。
暂不碰 _steal_card_data()，因为它涉及 await、真实卡牌实例、贴图和描述提取，风险更高。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 acquire_reward、remove_reward、craft_reward、shop 四个奖励页退出码均为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现脚本解析或编译类错误。
```

## DragShapeController.gd 第十四批拒绝动画拆分记录

日期：2026-06-06

### 本批目标

本批只拆拖拽放置失败时的拒绝动画。它保留 `_play_reject_animation()` 旧入口，只把卡牌横向抖动、拒绝提示显示和 2 秒后隐藏提示交给新模块；不判断放置合法性，不结束拖拽，不创建 `TimelineAction`，也不移动卡牌到手牌或弃牌区。

目标函数范围：
```text
_play_reject_animation()
```

当前触碰的数据和节点：
```text
current_card
cursor_tooltip
create_tween()
get_tree().create_timer()
_show_reject_tooltip(message)
_hide_reject_tooltip()
```

### 新增模块

```text
scene/in_scene/drag_modules/DragRejectAnimationRunner.gd
```

模块边界：
- `DragRejectAnimationRunner.gd` 只负责拖拽放置失败时的卡牌抖动动画和提示隐藏计时。
- 它不判断是否可放置，不结束拖拽，也不修改时间轴、卡牌归属或回合状态。
- `DragShapeController.gd` 保留 `_play_reject_animation()` 旧入口，并通过 `Callable` 传入提示、tween 和 timer 的创建方式。

### 本批删除或收口的重复点

删除原因：
```text
拒绝动画的卡牌横向抖动、提示显示和延迟隐藏现在由 DragRejectAnimationRunner 统一维护。
主脚本仍决定什么时候拒绝放置，新模块只执行表现。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十五批自由拖拽表现拆分记录

日期：2026-06-06

### 本批目标

本批只拆普通拖拽时卡牌跟随鼠标的表现逻辑。它保留 `_handle_free_drag()` 旧入口，只把非 clear 模式下的卡牌位置计算、位置写入和拖拽 shader 状态恢复交给新模块；不处理 clear 模式预览清理，不判断放置合法性，也不修改时间轴、手牌或弃牌区。

目标函数范围：
```text
_handle_free_drag(mouse_pos) 的普通拖拽分支
```

当前触碰的数据和节点：
```text
current_card
drag_offset
mouse_pos
card.global_position
card.material.is_invalid
card.material.drag_visual_state
```

### 新增模块

```text
scene/in_scene/drag_modules/DragFreeDragPresenter.gd
```

模块边界：
- `DragFreeDragPresenter.gd` 只负责普通拖拽时让卡牌自由跟随鼠标并恢复拖拽视觉状态。
- 它不处理 clear 模式预览，不判断放置合法性，也不修改时间轴、手牌或弃牌区。
- `DragShapeController.gd` 保留 `_handle_free_drag()` 旧入口，并继续负责 clear 模式离开时间轴后的预览清理。

### 本批删除或收口的重复点

删除原因：
```text
普通自由拖拽的中心点到左上角换算、global_position 写入和无效放置 shader 状态清理现在由 DragFreeDragPresenter 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十六批放置前视觉状态拆分记录

日期：2026-06-06

### 本批目标

本批只拆卡牌进入放置动画前的视觉状态准备。它保留 `_play_placement_animation()` 旧入口，只把禁用当前卡牌鼠标输入、清除无效放置 shader 标记交给新模块；不计算目标格位置，不播放飞行动画，不执行时间轴放置，也不改变卡牌归属。

目标函数范围：
```text
_play_placement_animation(grid_pos) 中的卡牌状态准备分支
```

当前触碰的数据和节点：
```text
current_card.mouse_filter
current_card.material.is_invalid
current_card.material.drag_visual_state
```

### 新增模块

```text
scene/in_scene/drag_modules/DragPlacementVisualStatePreparer.gd
```

模块边界：
- `DragPlacementVisualStatePreparer.gd` 只负责卡牌进入放置动画前的视觉状态准备。
- 它不计算目标格位置，不播放飞行动画，也不执行时间轴放置或卡牌归属变更。
- `DragShapeController.gd` 继续负责放置动画的目标点计算、场景交互锁定和实际放置收尾。

### 本批删除或收口的重复点

删除原因：
```text
放置动画前的鼠标输入禁用和拖拽 shader 状态清理现在由 DragPlacementVisualStatePreparer 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十七批放置目标位置解析拆分记录

日期：2026-06-06

### 本批目标

本批只拆卡牌放置动画的目标位置计算。它保留 `_play_placement_animation()` 旧入口，只把时间轴目标格中心点、`float_offset` 和目标卡牌缩放下的左上角位置计算交给新模块；不锁定交互，不播放 tween，不连接完成回调，也不执行时间轴放置。

目标函数范围：
```text
_play_placement_animation(grid_pos) 中的 card_top_left 计算分支
```

当前触碰的数据和节点：
```text
timeline_ui.grid_cells
timeline_ui.scale
current_card.get_size()
float_offset
get_global_mouse_position()
grid_pos
```

### 新增模块

```text
scene/in_scene/drag_modules/DragPlacementTargetResolver.gd
```

模块边界：
- `DragPlacementTargetResolver.gd` 只负责计算卡牌放置动画的目标左上角位置。
- 它不锁定交互，不播放动画，也不执行时间轴放置或卡牌归属变更。
- `DragShapeController.gd` 继续负责创建 tween、设置动画参数和连接 `_finish_placement()`。

### 本批删除或收口的重复点

删除原因：
```text
时间轴格子中心点读取、视觉尺寸换算、兜底鼠标位置和目标卡牌缩放下的左上角计算现在由 DragPlacementTargetResolver 统一维护。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十八批放置动画交互锁收口记录

日期：2026-06-06

### 本批目标

本批只收口放置动画期间的局内交互锁。它保留 `_play_placement_animation()` 旧入口，只把临时禁用 HexMap 和 TimelineUI 鼠标交互的逻辑交给已有 `DragSceneInteractionLockController`；不改拖拽开始锁、不改恢复路径、不播放动画，也不执行时间轴放置。

目标函数范围：
```text
_play_placement_animation(grid_pos) 中的 HexMap / TimelineUI mouse_filter 写入
DragSceneInteractionLockController.lock_for_placement_animation(...)
```

当前触碰的数据和节点：
```text
_get_hex_map()
timeline_ui
hex_map.mouse_filter
timeline_ui.mouse_filter
```

### 调整模块

```text
scene/in_scene/drag_modules/DragSceneInteractionLockController.gd
scene/in_scene/DragShapeController.gd
```

模块边界：
- `DragSceneInteractionLockController.gd` 继续只负责拖拽和放置动画期间的交互开关。
- 它不处理卡牌视觉状态、不更新预览，也不判断或执行放置。
- `DragShapeController.gd` 继续负责决定何时进入放置动画，并保留旧 `_play_placement_animation()` 入口。

### 本批删除或收口的重复点

删除原因：
```text
放置动画期间对 HexMap 和 TimelineUI 的 mouse_filter 写入现在由 DragSceneInteractionLockController 统一维护。
主脚本不再散落直接写交互锁状态。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## DragShapeController.gd 第十九批放置动画播放拆分记录

日期：2026-06-06

### 本批目标

本批只拆卡牌放置动画的 tween 播放表现。它保留 `_play_placement_animation()` 旧入口，只把卡牌飞向时间轴格子、缩放、透明度变化和完成回调连接交给新模块；不改变放置校验，不创建 `TimelineAction`，不调用 `timeline_manager.place_action()`，也不处理卡牌进入弃牌区或返回手牌。

目标函数范围：

```text
_play_placement_animation(grid_pos) 中的 create_tween、三条 tween_property 和 finished 回调连接
```

当前触碰的数据和节点：

```text
current_card
card_top_left
create_tween()
_finish_placement.bind(grid_pos)
```

### 新增模块

```text
scene/in_scene/drag_modules/DragPlacementAnimationRunner.gd
```

模块边界：

- `DragPlacementAnimationRunner.gd` 只负责播放卡牌飞向时间轴格子的放置动画。
- 它不判断放置是否合法，不执行时间轴放置，也不改变卡牌归属或拖拽状态。
- `DragShapeController.gd` 继续负责进入放置动画前的状态准备、交互锁、目标位置计算和动画结束后的实际放置流程。

### 本批删除或收口的重复点

删除原因：

```text
放置动画的目标位置、缩放、透明度 tween 和完成信号连接现在由 DragPlacementAnimationRunner 统一维护。
主脚本不再直接拼装放置动画 tween，只保留“何时播放”和“播放完做什么”的编排职责。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```
