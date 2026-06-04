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
