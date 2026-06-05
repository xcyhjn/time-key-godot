# AI 接力模块化解耦操作说明

日期：2026-06-06

## 这份文档给后续 AI 使用

这是一份给后续 AI 接力优化项目用的操作说明。它的目标不是解释某一个功能，而是让接手者知道：

- 先读哪些本地文件。
- 如何分析一个大脚本的耦合点。
- 如何列待拆清单。
- 如何按“小批次、低风险、可验证”的方式拆。
- 如何写中文注释和中文 Markdown。
- 如何判断一个文件已经不适合继续机械拆分。

项目路径：

```text
D:/godot/时之钥/时之钥
```

## 当前状态一览

当前模块化工作已经完成两条主线：

| 文件 | 当前状态 | 接下来怎么处理 |
| --- | --- | --- |
| `scene/in_scene/hex_map.gd` | 已完成一轮系统性拆分，主文件仍较大，但已经更接近地图 composition root。 | 不要把规则、表现、节点查找重新塞回主文件。新增地图逻辑优先放入 `hex_map_modules/`。 |
| `scene/in_scene/in_scene.gd` | 已完成 21 批拆分，当前约 1238 行，`in_scene_modules/` 下已有 32 个 `.gd` 模块。 | 低风险小块基本拆完。剩余主要是 `_ready()`、配置组装、回合推进、场景切换 wrapper 和信号回调。继续拆前先建立更完整回归路径。 |
| `scene/in_scene/DragShapeController.gd` | 约 1213 行，下一阶段最值得优先处理。 | 先拆节点查找、拖拽 tooltip、时间轴 hover 预览、放置校验等低风险边界。 |
| `scene/in_scene/timeline/timeline_ui.gd` | 约 967 行，UI 绘制、行动块表现、hover、清理动画混在一起。 | 先拆布局和 grid presenter，再拆行动块视觉与清理动画。 |
| `scene/in_scene/rewards/*.gd` | 奖励页脚本重复较多，CardManager 查找、草稿卡构建、tooltip、飞行动画重复。 | 优先提取共用奖励卡牌展示、卡牌数据提取和 CardManager locator。 |
| `scene/out_scene/out_scene_map_exp.gd` | 局外地图主控约 853 行，房间结算、移动、镜头、切场景耦合。 | 等局内和奖励脚本稳定后，按局外地图流程拆。 |

不要继续优先拆 `addons/dialogic/` 或其他插件目录，除非明确是在改插件行为。插件大文件不计入当前项目解耦优先级。

## 每次接力先读这些文件

按顺序读取：

```text
AGENTS.md
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

如果仓库里暂时没有 `AGENTS.md`，以对话中用户贴出的 `AGENTS.md` 约束为准。

快速查看文档和目标文件：

```powershell
rg --files docs workflow_logs scene/in_scene scene/out_scene
rg -n "^(class_name|extends|signal|@export|@onready|const|var|func) " <目标文件>
```

找当前最大的项目脚本：

```powershell
rg --files -g "*.gd" |
  Where-Object { $_ -notlike "addons/*" } |
  ForEach-Object {
    $count = (Get-Content -Encoding UTF8 -Path $_).Count
    [PSCustomObject]@{ Lines = $count; Path = $_ }
  } |
  Sort-Object Lines -Descending |
  Select-Object -First 30
```

## 固定工作闭环

每个文件都按这个顺序做：

```text
先读文档
-> 分析目标文件职责
-> 列出待拆清单
-> 每批只选 1 个清晰风险面
-> 新增模块写中文职责注释
-> 主文件保留旧公共入口
-> 更新 workflow_logs/current-modularization-process.md
-> 运行 git diff --check 和必要的 Godot headless 检查
-> 单独 commit
-> 重新评估是否还能继续拆
```

不要为了压行数机械搬函数。如果新模块无法用一句话说明职责，或者输入输出需要传一堆隐式状态，说明这一批拆得太大。

## 目标文件分析模板

开始改代码前，先输出一段短分析，至少包含：

```text
1. 目标文件现在承担哪些职责。
2. 文件里最明显的耦合点是什么。
3. 哪些函数只是旧公共入口，暂时不该拆。
4. 哪些函数可以形成独立模块。
5. 每个候选模块读写哪些成员变量。
6. 每个候选模块调用哪些外部节点、autoload 或插件接口。
7. 本轮准备先拆哪 3 到 4 个风险面，为什么这些风险低。
8. 本轮最小验证路径是什么。
```

如果用户要求“继续拆”，也要先做这一步。不要直接打开文件就改。

## 待拆清单写法

待拆清单要按风险排序，而不是按行数排序。

推荐格式：

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `xxx/NodeBridge.gd` | `_ready()` 中的节点查找 | 只集中路径，不改玩法状态。 | 不改场景树。 |
| 2 | `xxx/TooltipController.gd` | tooltip 显示、定位、隐藏 | 纯 UI 表现，输入输出清楚。 | 不改目标判定。 |
| 3 | `xxx/PreviewPresenter.gd` | hover preview | 只处理表现和清理。 | 不写规则数据。 |

每批只选 1 个候选模块，最多触碰 3 到 4 个风险面。跨系统流程，比如“结束回合 -> 时间轴结算 -> 建筑行为 -> 抽牌 -> 敌人意图”，不要一次性拆。

## 新模块写法

优先使用 `RefCounted` 服务模块，除非模块确实需要进场景树。

推荐形态：

```gdscript
class_name XxxController
extends RefCounted


## XxxController 只负责某个清晰流程。
## 它不推进回合、不切换场景，也不修改规则数据。


func run(config: Dictionary) -> Dictionary:
	var result: Dictionary = {}
	return result
```

规则：

- 新模块必须写中文职责注释。
- 注释说明“负责什么”和“不负责什么”，不要逐行翻译代码。
- 主文件保留旧公共函数入口，由旧入口调用新模块。
- 业务模块不要自己到处 `get_node("../../...")`，节点路径集中放 bridge。
- 规则模块不改节点，表现模块不改规则数据，桥接模块不持有玩法状态。
- 如果模块要返回主状态，使用 `Dictionary` 返回明确字段，不要悄悄写主脚本成员变量。

## 文档规则

`docs/` 目录只保留最新版、总结性的说明文档。

当前应保留：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
```

中间过程、批次记录、失败尝试、退出判断写到：

```text
workflow_logs/current-modularization-process.md
```

Markdown 必须用中文自然语言。不要在 `docs/optimization_logs/` 继续堆逐批日志。

## 验证命令

每批代码改动后至少运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
```

如果目标文件有明确场景，再加载对应场景。例如局内主场景：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

局外地图可优先检查：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/out_scene/out_scene_map_exp.tscn --quit-after 1 --no-header
```

如果场景路径不确定，先用：

```powershell
rg --files scene | rg "out_scene|timeline|reward|drag|in_scene"
```

Godot 退出时可能仍有旧资源释放噪声。重点看是否出现新的：

```text
SCRIPT ERROR
Parse Error
Compile Error
Failed to load script
Compilation failed
Invalid call
Invalid access
```

如果只改 Markdown，运行 `git diff --check` 即可。

## Git 规则

每批完成后：

```powershell
git status --short
git diff --check
git add -- <本批文件>
git commit -m "<类型>: <本批清晰描述>"
```

规则：

- 每批单独 commit。
- 不混入无关文件。
- 不强行添加 `.uid`。如果 `.uid` 被项目忽略，按旧规则处理。
- 提交前清理临时日志，如 `godot_project_check.log`、`godot_in_scene_check.log`。
- 不要回滚用户已有改动。

## 已完成的 in_scene.gd 拆分总结

`scene/in_scene/in_scene.gd` 已拆出这些模块类型：

| 模块目录 | 代表模块 | 职责 |
| --- | --- | --- |
| `bridges` | `InSceneNodeBridge.gd`、`InSceneGlobalClockBridge.gd` | 节点路径和 GlobalClock 桥接。 |
| `cards` | `CardSystemBootstrap.gd`、`CardPileUiController.gd`、`CardDrawFlowController.gd`、`HandDiscardFlowController.gd` | 卡牌系统初始化、牌堆 UI、抽牌、强制弃牌。 |
| `ui` | `InSceneInputLockController.gd`、`InSceneInputEventController.gd`、`InSceneUiVisibilityController.gd`、`CursorTooltipController.gd`、`CardTooltipUiAdapter.gd`、`TargetSelectionHoverUiController.gd`、`TimelineActionHoverUiController.gd`、`CombatCartoonUiController.gd` | 输入锁、输入事件、UI 显隐、tooltip、hover、顶部战斗 UI。 |
| `turn` | `FirstTurnIntroRunner.gd`、`EnemyIntentTimelineRefresher.gd` | 首回合入场等待和敌人意图刷新。 |
| `scene_flow` | `InSceneReturnPayloadBuilder.gd`、`InSceneExternalPayloadParser.gd`、`InScenePayloadBridge.gd`、`InSceneSceneSwitchLoader.gd`、`InSceneSceneSwitchExecutor.gd`、`InSceneReturnFlowController.gd` | 外部 payload、局外返回、场景切换。 |
| `settlement` | `SettlementRewardSceneController.gd`、`SettlementRewardExitController.gd`、`SettlementRewardExitFlowController.gd`、`SettlementRewardConsumer.gd`、`SettlementDeckSnapshotService.gd`、`SettlementDeckReclaimService.gd`、`CombatVictorySettlementController.gd`、`CombatDefeatFlowController.gd`、`GameWinFlowController.gd` | 结算奖励、奖励页退出、牌堆快照回收、胜负流程。 |

`in_scene.gd` 当前剩余主要是 composition root 和跨模块编排。不要继续从它里面硬拆回合流，除非先补回合结束、胜利中断、奖励返回、场景切换的回归路径。

## 后续优先优化文件

下面是当前更值得继续模块化的项目文件。行数只是参考，优先级结合了耦合程度和拆分收益。

| 优先级 | 文件 | 当前问题 | 首批建议 |
| --- | --- | --- | --- |
| 1 | `scene/in_scene/DragShapeController.gd` | 拖拽状态、节点查找、时间轴 hover、tooltip、放置校验、拒绝动画、放置动画和卡牌归还混在一起。 | 先拆 `DragShapeNodeBridge.gd`、`DragTimelineHoverController.gd`、`DragRejectTooltipController.gd`。 |
| 2 | `scene/in_scene/timeline/timeline_ui.gd` | 时间轴布局、背景格子、行动块视觉、hover、敌方意图 preview、清理动画、grid preview 混在一个 UI 脚本里。 | 先拆 `TimelineLayoutController.gd`、`TimelineGridPresenter.gd`、`TimelineGridPreviewPresenter.gd`。 |
| 3 | `scene/in_scene/rewards/CraftReward.gd` | 合成规则、牌组选择、预览卡生成、结果描述、tooltip、CardManager 查找都在奖励页里。 | 先拆 `CraftRecipeRules.gd`、`RewardCardDataExtractor.gd`、`CraftSlotPreviewPresenter.gd`。 |
| 4 | `scene/in_scene/rewards/ShopManager.gd` | 商店库存生成、时代权重、价格、购买动画、tooltip、CardManager 查找耦合。 | 先拆 `ShopInventoryGenerator.gd`、`ShopPricingService.gd`、`RewardCardDataExtractor.gd`。 |
| 5 | `scene/in_scene/rewards/AcquireReward.gd` 和 `RemoveReward.gd` | 与商店和合成页重复 CardManager 查找、临时 pile、草稿卡生成、tooltip。 | 先抽共用 `RewardCardFactoryAdapter.gd` 和 `RewardTooltipAdapter.gd`。 |
| 6 | `scene/out_scene/out_scene_map_exp.gd` | 局外地图初始化、房间结算、tier 推进、章节揭示动画、移动、切场景在一个主控里。 | 先拆 `OutSceneGlobalStateBridge.gd`、`RoomResolutionController.gd`、`OutSceneSceneSwitchController.gd`。 |
| 7 | `scene/in_scene/tile.gd` | 地貌规则、状态组件、结算奖励、敌人意图、血量、贴图选择和 timeline shape 混在一个实体脚本里。 | 先拆 `TileTimelineShapeParser.gd`、`TileSettlementRewardAdapter.gd`、`TileIntentAdapter.gd`。 |
| 8 | `scene/card/custom_card.gd` | 卡牌数据解析、形状解析、选中视觉、tooltip、效果范围和出牌逻辑耦合。 | 先拆 `CardShapeParser.gd`、`CardSelectionVisualController.gd`、`CardEffectRangeService.gd`。 |
| 9 | `scene/in_scene/enermy/enemy_intent_presentation_controller.gd` | 引用查找、hover phase 判断、tooltip、关键词 tooltip、地图和时间轴表现耦合。 | 先拆 `EnemyIntentReferenceBridge.gd`、`EnemyIntentTooltipPresenter.gd`、`EnemyIntentHoverPhaseGuard.gd`。 |
| 10 | `scene/in_scene/timeline/TimelineManager.gd` | 时间轴规则核心较集中，但敌人意图候选、排序、落点选择还可拆。 | 等 `timeline_ui.gd` 稳定后，再拆 `EnemyIntentPlacementService.gd`。 |
| 11 | `scene/in_scene/timecoin_ui.gd` | 全局 Timecoin 查找、数值显示、获得/消耗/警告动画、沙漏 shader 混在 UI 脚本里。 | 先拆 `TimecoinGlobalBridge.gd` 和 `TimecoinAnimationRunner.gd`。 |

## 各重点文件的第一批低风险拆法

### DragShapeController.gd

第一批只建议拆节点查找和引用解析：

```text
scene/in_scene/drag_modules/DragShapeNodeBridge.gd
```

接管：

- `_get_main_board()`
- `_get_card_manager()`
- `_find_discard_pile()`
- `_find_player_hand()`
- `_get_hex_map()`
- `_find_project_node()`

不要在第一批动拖拽状态机、放置校验或动画。

第二批再拆时间轴 hover preview：

```text
scene/in_scene/drag_modules/DragTimelineHoverController.gd
```

接管 `_handle_timeline_hover()` 中的鼠标转 grid、合法性查询、preview 更新。不要创建 `TimelineAction`。

第三批可拆拒绝 tooltip：

```text
scene/in_scene/drag_modules/DragRejectTooltipController.gd
```

接管 `_show_reject_tooltip()`、`_update_reject_tooltip_position()`、`_hide_reject_tooltip()`。

### timeline_ui.gd

第一批优先拆布局：

```text
scene/in_scene/timeline/ui_modules/TimelineLayoutController.gd
```

接管 `_apply_anchor_layout()`、`set_top_reserved_space()` 中纯布局计算。不要动行动块容器和动画。

第二批拆背景格子创建：

```text
scene/in_scene/timeline/ui_modules/TimelineGridPresenter.gd
```

接管 `_init_background_grid()` 和单格 signal 绑定。保留 `grid_cell_clicked` 等旧 signal。

第三批拆 grid preview：

```text
scene/in_scene/timeline/ui_modules/TimelineGridPreviewPresenter.gd
```

接管 `update_grid_preview()`、`clear_grid_preview()`。

### 奖励脚本

奖励脚本不要单文件单独发明三套工具。先抽共用模块：

```text
scene/in_scene/rewards/reward_modules/RewardCardManagerLocator.gd
scene/in_scene/rewards/reward_modules/RewardCardDataExtractor.gd
scene/in_scene/rewards/reward_modules/RewardTooltipAdapter.gd
```

第一批优先抽 `RewardCardDataExtractor.gd`，因为 `AcquireReward.gd`、`RemoveReward.gd`、`CraftReward.gd`、`ShopManager.gd` 都有临时 pile、偷取真实卡牌数据、提取贴图和描述的重复逻辑。

### out_scene_map_exp.gd

第一批不要动地图移动。先拆局外返回 payload 消费：

```text
scene/out_scene/out_scene_modules/RoomResolutionController.gd
```

接管：

- `_consume_pending_room_resolution()`
- `_handle_room_resolution_payload()`
- `_should_advance_tier_from_boss_payload()`
- `_advance_tier_from_boss_resolution()` 的纯判断部分

移动动画、镜头限制、切场景留在主文件，等第二批再拆。

## 退出一个文件的标准

满足下面任一条件，就停止当前文件，告诉用户“这个文件本轮不建议继续拆”：

- 剩余函数主要是 signal 回调、旧公共 API 或 config builder。
- 新模块需要传入 10 个以上互相耦合的成员变量才跑得动。
- 要拆的流程跨越多个系统，而且没有明确回归路径。
- 继续拆只能减少行数，不能让职责更清楚。
- Godot headless 无法覆盖风险点，需要先补手动回归或测试脚本。

退出不是失败。一个主控文件收敛成 composition root 后，就应该停止。

## 给下一位 AI 的接力 prompt

可以把下面整段复制给下一位 AI：

```text
请在 D:/godot/时之钥/时之钥 中继续项目模块化解耦。

先读取本地文件：
1. AGENTS.md
2. docs/ai-handoff-ultimate-operation-guide.md
3. docs/hex-map-ultimate-operation-guide.md
4. workflow_logs/current-modularization-process.md

如果 AGENTS.md 不存在，以当前对话里用户贴出的 AGENTS 约束为准。

先分析目标文件，再列待拆清单，最后每批只拆 1 个清晰风险面，最多触碰 3 到 4 个风险点。不要直接改代码。

当前 in_scene.gd 已完成 21 批拆分，低风险小块基本拆完，不要继续机械拆它。下一阶段优先处理：
1. scene/in_scene/DragShapeController.gd
2. scene/in_scene/timeline/timeline_ui.gd
3. scene/in_scene/rewards/CraftReward.gd
4. scene/in_scene/rewards/ShopManager.gd
5. scene/in_scene/rewards/AcquireReward.gd 和 RemoveReward.gd
6. scene/out_scene/out_scene_map_exp.gd

工作方式：
- 先用 rg 输出目标文件函数、变量、信号轮廓。
- 写出当前职责、耦合点、待拆 list 和本批风险面。
- 新增模块写中文职责注释。
- Markdown 用中文自然语言。
- docs 目录只保留最新版总结性说明。
- 中间流程写 workflow_logs/current-modularization-process.md。
- 每批改完运行 git diff --check 和必要的 Godot headless 检查。
- 清理临时日志。
- 每批单独 commit。

优先从 DragShapeController.gd 的低风险模块开始，例如节点桥接、时间轴 hover preview 或拒绝 tooltip。不要一开始重写拖拽状态机、放置动画或卡牌效果触发。
```
