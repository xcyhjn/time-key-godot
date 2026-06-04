# AI 接力解耦终极操作说明

日期：2026-06-05

## 这份文档给谁看

这份文档给后续继续优化项目的 AI 使用。它不是普通功能说明，而是一份接力手册：打开项目后，先读什么、怎么判断一个大文件是否值得拆、怎么列待办、怎么一批一批落地、怎么写中文文档，以及怎么避免把已经拆干净的模块重新耦合回去。

项目路径：

```text
D:/godot/时之钥/时之钥
```

当前优先目标：

```text
先用 HexMap 的拆分经验，继续处理 scene/in_scene/in_scene.gd。
随后再处理 DragShapeController.gd、timeline_ui.gd、奖励脚本和局外地图主控等仍然偏大的文件。
```

## 接力前先读这些文件

每次接力前，先按顺序读这些本地文件：

```text
AGENTS.md
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

读完后再看目标大文件和它直接引用的节点、信号、模块目录。不要跳过本地文档直接改代码，因为前面已经形成了一套拆分边界和文档规则。

如果需要快速找文件，使用：

```powershell
rg --files docs workflow_logs scene/in_scene
```

如果需要看目标脚本的职责轮廓，使用：

```powershell
rg -n "^(func|signal|@export|@onready|class_name|const|var) " scene/in_scene/in_scene.gd
```

## 本轮已经形成的工作方法

前面的 HexMap 拆分不是一次大重写，而是一个稳定循环：

```text
先分析当前耦合
-> 列出待拆清单
-> 每批只处理一个耦合面
-> 新模块只接管清晰职责
-> HexMap 保留旧公共入口
-> 每批补中文注释和中文文档
-> 运行固定检查
-> 单独 git commit
-> 最后用一份终极说明替代散落的过程文档
```

后续处理 `in_scene.gd` 和其他大文件，也沿用这个节奏。不要为了让行数快速下降而机械搬函数。判断是否值得拆，优先看职责边界是否清楚、调用方是否稳定、验证路径是否可控。

## 文档规则

当前文档规则已经调整为：

- `docs/` 目录只保留最新版、总结性的终极操作与维护说明。
- 中间过程、批次记录、历史列表和阶段复盘放到 `workflow_logs/current-modularization-process.md`。
- 新增或修改的 Markdown 必须用中文自然语言编写。
- 每次大阶段结束后，用最新版总结文档覆盖旧解释，不再在 `docs/optimization_logs/` 里继续堆逐批 landing 文件。
- 如果某一轮需要临时记录细节，先写进 `workflow_logs/current-modularization-process.md`，阶段结束后再浓缩到 `docs/` 下的终极说明。

现在 `docs/` 下保留的核心说明应当是：

```text
docs/hex-map-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
```

## HexMap 已经完成的模块化总结

`scene/in_scene/hex_map.gd` 现在已经从“所有地图逻辑都在一个大脚本里”变成“地图主控 + 多个小模块”的结构。它仍然较大，但大部分内部职责已经有归属。

已经拆出的主要模块包括：

| 模块目录 | 代表文件 | 已接管职责 |
| --- | --- | --- |
| `generation` | `MapGenerationService.gd`、`LandformPlacementService.gd` | 地图规则数据生成、初始地貌投放。 |
| `factory` | `TileStackFactory.gd`、`TileLandformAttachService.gd`、`TileStackInitializationService.gd`、`TileStackRebuildService.gd` | 地块 stack 创建、初始地貌挂接、metadata 收尾、单格重建。 |
| `rules` | `HexCoordRules.gd`、`HexTerrainRules.gd`、`HexTargetRules.gd` | 坐标换算、地形判定、卡牌目标合法性。 |
| `input` | `HexMapInputCoordinator.gd`、`TargetHoverController.gd` | 地块输入开关、hover 状态选择。 |
| `presenters` | `EnemyIntentMapPresenter.gd`、`SettlementRewardPresenter.gd`、`HexMapCollisionPresenter.gd`、`HexMapVisualStatePresenter.gd`、`TargetAoeHoverPresenter.gd` | 地图表现层、高亮、敌人意图、奖励 hover、AOE hover 展示计划。 |
| `height_view` | `HeightViewIndicatorPresenter.gd`、`HeightViewStateSynchronizer.gd`、`HeightViewMapTransitionRunner.gd` | 高度视图的光柱、数字、平铺/3D 状态同步和整图过渡。 |
| `elevation` | `TileElevationService.gd` | 地块升降动画与高度变更编排。 |
| `destruction` | `TileDestructionBatchQueue.gd`、`TileDestructionMutationService.gd` | 地块销毁队列和真实数据/节点移除。 |
| `registrars` | `RuntimeLandformRegistrar.gd`、`ExternalRenderNodeRegistrar.gd` | 运行时地貌注册、外部渲染节点纳入地图表现系统。 |
| `bridges` | `CardManagerLocator.gd`、`HexMapSceneBridge.gd` | CardManager 查找、局内场景节点路径集中管理。 |
| `rewards` | `SettlementRewardController.gd` | 结算奖励资格扫描、奖励状态写回。 |
| `turn` | `TileTurnBehaviorRunner.gd` | 地貌和建筑的回合行为执行顺序。 |
| `ui` | `TargetSelectionTooltipAdapter.gd` | MainBoard 目标选择 tooltip 的旧接口适配。 |
| `timeline/commands/rules` | `TimelineCommandTargetRules.gd` | 时间轴命令最终目标校验。 |

这些拆分的共同原则是：规则模块不改节点，表现模块不改规则数据，桥接模块只找节点不持有玩法状态，主控脚本只负责串联。

## 接下来优先拆 in_scene.gd

`scene/in_scene/in_scene.gd` 当前约 1900 行，是下一轮最值得处理的文件。它现在承担了太多局内场景根职责：

- 卡牌系统创建、牌堆和手牌初始化。
- 牌堆按钮、弃牌按钮、牌堆查看器 UI。
- 首回合自动开始、时间轴入场、敌人意图生成。
- 回合推进、抽牌、弃牌、输入禁用和恢复。
- 卡牌 tooltip、鼠标 tooltip、目标 hover 适配。
- 胜利、失败、结算奖励、商店和外部奖励场景切换。
- 局内 UI 隐藏与恢复。
- 局外返回 payload 构造和场景切换。
- GlobalClock 时代/阶段同步。
- 结算阶段把运行时卡牌回收到牌堆。

`in_scene.gd` 后续应该收敛成“局内场景 composition root”。它可以继续持有关键节点引用，但具体流程要逐步交给服务节点或纯脚本模块。

## in_scene.gd 第一轮待拆列表

### 第一批：场景节点桥接

建议新增：

```text
scene/in_scene/in_scene_modules/bridges/InSceneNodeBridge.gd
```

职责：

- 集中查找 `HexMap`、`TimelineUI`、`TimelineManager`、`HeightViewToggleButton`、`TotalEnemyHealthBar`、`CombatVictoryBanner`、`CartoonUI` 等节点。
- 消除 `in_scene.gd` 中散落的硬编码节点路径。
- 暂时不改场景树结构，只集中路径和必要兜底。

验收：

- `in_scene.gd` 的 `@onready` 路径数量减少。
- 启动局内战斗、时间轴、血条、高度视图按钮、胜利横幅仍能正常找到。

### 第二批：GlobalClock 与时代同步桥接

建议新增：

```text
scene/in_scene/in_scene_modules/bridges/InSceneGlobalClockBridge.gd
```

职责：

- 接管 `_connect_global_clock_progress_signal()`、`_on_global_clock_progress_changed()`、`_pull_era_from_global()`、`_push_era_to_global()` 的纯桥接部分。
- `in_scene.gd` 只保留当前时代值和刷新 UI 的入口。

验收：

- 局内时代值进入、推进、返回局外后仍正确。
- 不引入新的 `Global` 逻辑，不把时代规则复制到局内。

### 第三批：卡牌系统初始化

建议新增：

```text
scene/in_scene/in_scene_modules/cards/CardSystemBootstrap.gd
scene/in_scene/in_scene_modules/cards/CardPileUiController.gd
```

职责：

- `CardSystemBootstrap` 接管 `setup_card_system()` 中的 CardManager、Hand、Deck、Discard、CardFactory 创建和基础连接。
- `CardPileUiController` 接管 `update_counts_and_ui()`、牌堆按钮、弃牌按钮和牌堆查看器打开逻辑。
- `in_scene.gd` 保留 `manager_instance`、`player_hand`、`deck_pile`、`discard_pile` 等稳定公共引用，避免一次性改动太多调用方。

验收：

- 开局抽牌、牌堆数量、弃牌数量、牌堆查看器都不变。
- 左键抽牌 debug 开关行为不变。

### 第四批：首回合与时间轴入场

建议新增：

```text
scene/in_scene/in_scene_modules/turn/FirstTurnIntroRunner.gd
scene/in_scene/in_scene_modules/turn/BattleTurnFlowController.gd
```

职责：

- `FirstTurnIntroRunner` 接管 `_schedule_auto_first_turn()`、`_start_first_turn_after_scene_ready()`、`_wait_for_card_system_ready()`、`_play_timeline_intro_if_visible()`、`_wait_for_battle_intro_ui()`。
- `BattleTurnFlowController` 接管 `start_new_turn()`、`_start_turn()`、`_advance_global_phase()` 中可以独立表达的流程。
- `in_scene.gd` 继续作为最终调度者，负责在关键时刻调用 HexMap、TimelineManager 和 CardManager。

验收：

- 进入战斗后时间轴入场顺序不变。
- 敌人意图生成时机不变。
- 开局自动首回合不提前、不重复。

### 第五批：输入锁与 UI 可见性

建议新增：

```text
scene/in_scene/in_scene_modules/ui/InSceneInputLockController.gd
scene/in_scene/in_scene_modules/ui/InSceneUiVisibilityController.gd
```

职责：

- 输入锁模块接管 `disable_player_inputs()`、`enable_player_inputs()` 中稳定的 UI 和卡牌输入开关。
- UI 可见性模块接管 `hide_ui_for_external_scene()`、`restore_ui_after_external_scene()`、`restore_all_ui()`、`_set_single_health_bars_visible()` 这类外部场景切换时的隐藏恢复。

验收：

- 打开奖励、商店、胜利结算外部界面时，局内 UI 隐藏和恢复不变。
- 拖拽、点击、时间轴输入不会在外部界面打开时误触。

### 第六批：tooltip 与 cursor tooltip

建议新增：

```text
scene/in_scene/in_scene_modules/ui/CursorTooltipController.gd
scene/in_scene/in_scene_modules/ui/CardTooltipUiAdapter.gd
```

职责：

- `CursorTooltipController` 接管 `_enhance_cursor_tooltip()`、`set_cursor_tooltip_position()`、`_refresh_cursor_tooltip_size()`、可见性变化处理。
- `CardTooltipUiAdapter` 接管 `setup_tooltip_ui()`、`show_tooltip()`、`hide_tooltip()` 里和 CardTooltipPresenter 的连接。

验收：

- 卡牌 hover tooltip、目标选择 tooltip、鼠标文字 tooltip 都正常。
- tooltip 不遮挡关键 UI，尺寸不会异常抖动。

### 第七批：奖励和外部奖励场景

建议新增：

```text
scene/in_scene/in_scene_modules/rewards/SettlementRewardSceneController.gd
scene/in_scene/in_scene_modules/rewards/SettlementDeckReclaimService.gd
```

职责：

- `SettlementRewardSceneController` 接管 `_on_settlement_reward_requested()`、`_open_settlement_reward_scene()`、`_get_settlement_reward_scene()`、奖励按钮打开逻辑。
- `SettlementDeckReclaimService` 接管 `_prepare_deck_button_for_settlement()`、`_snapshot_current_deck_for_settlement()`、`_reclaim_all_runtime_cards_to_deck()` 以及相关收集/移动卡牌的纯流程。

验收：

- 战斗胜利后奖励建筑点击、奖励页打开、关闭后奖励 used 状态刷新都不变。
- 结算阶段牌堆按钮显示、运行时卡牌回收、牌堆数量不变。

### 第八批：场景切换和返回局外 payload

建议新增：

```text
scene/in_scene/in_scene_modules/scene_flow/InSceneSceneSwitcher.gd
scene/in_scene/in_scene_modules/scene_flow/CombatReturnPayloadBuilder.gd
```

职责：

- `CombatReturnPayloadBuilder` 接管 `_build_combat_return_payload()`。
- `InSceneSceneSwitcher` 接管 `_switch_scene_with_data()`、`_load_packed_scene_for_switch()`、`_recover_dim_after_failed_switch()`、`_log_scene_switch_error()`、`_apply_payload_to_new_scene_before_tree()` 的通用切场景流程。

验收：

- 局内胜利、失败、普通返回、奖励返回局外 payload 都保持正确。
- Dim 动画和失败恢复不变。

### 第九批：战斗结果与结算状态

建议新增：

```text
scene/in_scene/in_scene_modules/results/CombatResultController.gd
```

职责：

- 接管 `_on_defeat_triggered()`、`_on_combat_victory_triggered()`、`_on_lose_button_pressed()`、debug 胜利按钮、结算阶段 UI 显隐的流程编排。
- 不接管卡牌回收细节，只调用 `SettlementDeckReclaimService`。

验收：

- 失败弹窗、战斗胜利横幅、结算奖励模式、debug 按钮行为不变。

## 其他待拆文件排序

`in_scene.gd` 稳定后，再按下面顺序处理其他大文件：

| 文件 | 当前风险 | 建议拆分方向 |
| --- | --- | --- |
| `scene/in_scene/DragShapeController.gd` | 输入、时间轴 hover、放置校验、tooltip、卡牌归还和效果触发混在一起。 | 拆输入状态、时间轴 hover 预览、放置校验、拒绝提示、放置动画、卡牌归还。 |
| `scene/in_scene/timeline/timeline_ui.gd` | UI 网格、行动块、hover、清理动画、视觉刷新耦合。 | 拆网格渲染、行动块 presenter、hover controller、清理动画 runner。 |
| `scene/in_scene/timeline/TimelineManager.gd` | 时间轴规则核心还可继续收口。 | 保持规则核心，抽命令构造、行动排序、事件通知适配。 |
| `scene/in_scene/rewards/CraftReward.gd` | 奖励 UI、卡牌查找、合成规则和返回流程偏重。 | 拆 CardManager 查找、候选卡牌列表、合成规则、UI 渲染。 |
| `scene/in_scene/rewards/ShopManager.gd` | 商店库存、价格、购买 UI 和场景返回耦合。 | 拆库存生成、购买规则、UI 列表、返回 payload。 |
| `scene/in_scene/rewards/AcquireReward.gd`、`RemoveReward.gd` | 奖励候选、CardManager 查找和 UI 选择重复。 | 共用奖励卡牌选择组件和 CardManager locator。 |
| `scene/out_scene/out_scene_map_exp.gd` | 局外地图移动、tier 推进、镜头、房间结算和切局内耦合。 | 拆 RoomResolutionController、OutSceneSceneSwitcher、ChapterRevealRunner、OutMapMovementController。 |

## 每批拆分前必须先做的分析

每批开始前先输出一个短分析，不要直接改：

```text
1. 当前要拆的函数范围是什么。
2. 这部分现在读写哪些成员变量。
3. 它调用哪些外部节点或 autoload。
4. 新模块的输入和输出是什么。
5. 哪些公共入口必须保持不变。
6. 本批最小验收路径是什么。
```

如果无法用 5 到 8 个明确输入输出说清楚新模块，说明这一批拆得太大，应该缩小范围。

## 新模块写法

优先使用纯 GDScript `RefCounted` 风格服务，除非确实需要进入场景树。

推荐形态：

```gdscript
extends RefCounted

## 这里写清楚模块职责，以及它不负责什么。

func run(config: Dictionary) -> Dictionary:
    # 这里写清楚主要流程，避免把旧脚本里的隐式依赖藏起来。
    return {}
```

模块函数要有中文注释。注释重点解释“为什么这个模块存在”和“这个函数维护哪些契约”，不要逐行翻译代码。

如果模块需要节点路径，优先做 bridge，不要让业务模块自己 `get_node("../../...")`。

## 兜底代码清理规则

可以砍掉的兜底：

- 已经被新模块统一入口覆盖的重复查找。
- 同一个 signal 重复 connect 的防御性代码，如果已有统一 `_connect_signal_once()` 或 bridge 处理。
- 永远不会再走到的旧 debug fallback。
- 为旧节点路径保留的重复路径，如果场景结构已经由 bridge 统一。

暂时不要砍的兜底：

- `is_instance_valid()`，尤其是跨场景、奖励页、CardManager、TimelineManager 生命周期相关代码。
- 旧插件或外部脚本调用的 `has_method()` 检查，除非已经统一接口。
- meta 清理，尤其是 root/current_scene 中可能残留已释放实例的 CardManager。
- 切场景失败恢复和 Dim 恢复。

每删一段兜底，都要在本批文档里说明：

```text
删除原因：
旧代码保护的场景已经由哪个模块覆盖：
回归检查：
```

## 验证命令

每批代码改动后至少运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

如果本批只改 Markdown，运行 `git diff --check` 即可。

Godot 退出时如果仍出现已知的资源释放噪声，不要把它误判成本批新增错误。重点看是否出现新的脚本解析错误、场景加载失败、空引用错误或资源路径错误。

## 手动回归路径

拆 `in_scene.gd` 时，固定回归：

- 进入局内战斗，地图和 UI 正常出现。
- 首回合自动开始，时间轴入场和敌人意图生成时机正确。
- 抽牌、拖拽卡牌、放入时间轴、结算一回合。
- 打开牌堆和弃牌查看器。
- 卡牌 tooltip、鼠标 tooltip、目标 hover 表现正常。
- 高度视图按钮可以切换，回到普通视图后输入正常。
- 战斗胜利进入结算奖励模式，奖励页面打开和关闭正常。
- 失败流程可以显示失败 UI。
- 返回局外地图后房间状态、时间币、时代阶段、已走路径保持正确。

## Git 和日志规则

每批完成后：

```powershell
git status --short
git diff --check
git add -- <本批文件>
git commit -m "<类型>: <本批清晰描述>"
```

建议 commit 类型：

```text
refactor: extract in scene node bridge
refactor: extract card system bootstrap
docs: refresh ai handoff guide
```

每批不要混入多个系统。比如拆 `in_scene.gd` 的卡牌初始化时，不要同时改 `DragShapeController.gd` 的放置逻辑。

## 给下一位 AI 的最小接力提示词

下面这段可以直接复制给下一位 AI：

```text
请在 D:/godot/时之钥/时之钥 中继续模块化解耦工作。

先读取本地文件：
1. AGENTS.md
2. docs/ai-handoff-ultimate-operation-guide.md
3. docs/hex-map-ultimate-operation-guide.md
4. workflow_logs/current-modularization-process.md

然后先分析目标文件，不要直接改代码。目标优先级是：
1. scene/in_scene/in_scene.gd
2. scene/in_scene/DragShapeController.gd
3. scene/in_scene/timeline/timeline_ui.gd
4. scene/in_scene/timeline/TimelineManager.gd
5. scene/in_scene/rewards/*.gd
6. scene/out_scene/out_scene_map_exp.gd

工作方式沿用 HexMap 拆分流程：先列职责和耦合点，再列待拆 list，每批只拆一个风险面；新增模块要有中文注释；Markdown 全部用中文自然语言；docs 目录只保留最新版总结性说明，中间过程写入 workflow_logs/current-modularization-process.md；每批运行 git diff --check 和 Godot headless 检查，最后分组 commit。

本轮优先从 in_scene.gd 开始，先做场景节点桥接或 GlobalClock 桥接这类低风险拆分，不要一上来重写卡牌系统或战斗结算。
```

## 最后的判断原则

如果新模块能让一句话职责变清楚、让 `in_scene.gd` 少知道一个系统的内部细节，并且回归路径可以明确验证，就值得拆。

如果只是把一段代码从 A 文件搬到 B 文件，但输入输出仍然依赖一堆隐式成员变量，那不是解耦，只是换地方堆代码。遇到这种情况，先回到分析清单，缩小本批范围。
