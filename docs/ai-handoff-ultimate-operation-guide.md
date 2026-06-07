# AI 接力模块化解耦操作说明

日期：2026-06-07

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

当前模块化工作已经完成多条主线。当前已拆模块共 `130` 个，`docs/modularized-files-ultimate-operation-guide.md` 对这些模块的覆盖缺失数为 `0`。

| 文件 | 当前状态 | 接下来怎么处理 |
| --- | --- | --- |
| `scene/in_scene/hex_map.gd` | 约 2093 行，`hex_map_modules/` 下已有 31 个模块，已进入地图 composition root 维护阶段。 | 不要为了降行数继续机械拆。新增地图规则、表现或桥接时优先进入现有 `hex_map_modules/` 分类。 |
| `scene/in_scene/in_scene.gd` | 约 1238 行，`in_scene_modules/` 下已有 32 个模块，低风险小块基本拆完。 | 剩余主要是 `_ready()`、配置组装、回合推进、场景切换 wrapper 和信号回调。继续拆前先补更完整回归路径。 |
| `scene/in_scene/DragShapeController.gd` | 约 1074 行，`drag_modules/` 下已有 18 个模块，节点桥接、拒绝提示、时间轴预览、放置校验、场景交互锁、放置动画、玩家行动创建和时间轴提交等已拆。 | 后续只评估成功收尾里的更小边界；不要把卡牌归属变化和 UI 恢复同批合并。 |
| `scene/in_scene/timeline/timeline_ui.gd` | 约 837 行，已拆布局、背景网格、预览样式、TimelineManager 查找、敌方意图 overlay 和放置动画等。 | 可继续评估行动块表现、清理动画和剩余 hover 流程；不要重写时间轴数据规则。 |
| `scene/in_scene/rewards/*.gd` | 奖励页已拆出 49 个模块。Acquire/Remove 已接近页面编排；Shop 生成前依赖检查、商店定价 Resource 和时代权重 Resource 已收口，Craft 结果预览、预览卡 UI、预览 DraftCard 工厂复用、合成结果牌组写入和合成配方 Resource 已完成。 | Craft 预览创建链和确认写入链都不建议继续硬拆；如果继续 Shop，只评估临时牌堆生命周期这类更小边界，否则转向拖拽放置完成流程。 |
| `scene/out_scene/out_scene_map_exp.gd` | 约 853 行，局外地图主控尚未进入系统性拆分。 | 等奖励页、拖拽和时间轴剩余边界稳定后，再按局外地图流程拆。 |

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
docs/modularized-files-ultimate-operation-guide.md
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
| 1 | `scene/in_scene/DragShapeController.gd` | 低风险查找、tooltip、预览、校验、交互锁、放置动画、玩家行动创建和时间轴提交已拆，剩余主要是成功收尾。 | 先评估 `end_dragging_success()` 里的卡牌视觉状态恢复或弃牌归属移动；不要同批改归属变化和 UI 恢复。 |
| 2 | `scene/in_scene/rewards/ShopManager.gd` | `_generate_shop_items()` 仍串联生成锁、时代读取、临时牌堆、选卡、草稿卡创建、异步数据提取和循环编排；生成依赖检查、商店定价和时代权重配置都已拆出。 | 单个商品生成编排不建议硬拆；如果继续 Shop，只评估临时牌堆生命周期等更小边界。 |
| 3 | `scene/in_scene/rewards/CraftReward.gd` | `_refresh_result_preview()` 已拆出结果预览挂载，`_create_preview_card()` 已复用 DraftCard 工厂并拆出预览卡 UI 配置；`_apply_crafting_result_to_deck()` 已拆出移除索引计算和结果写入处理；`CRAFTING_RECIPES` 已资源化为 `CraftRecipeBook`。 | 停止继续硬拆 Craft 页面流程；后续只在新增配方或调整配方资源格式时进入。 |
| 4 | `scene/in_scene/timeline/timeline_ui.gd` | 布局、网格、预览、overlay 和放置动画已拆，剩余行动块表现与清理动画仍有耦合。 | 优先评估行动块视觉或清理动画中的单一 presenter，不动 TimelineManager 数据。 |
| 5 | `scene/out_scene/out_scene_map_exp.gd` | 局外地图初始化、房间结算、tier 推进、章节揭示动画、移动、切场景在一个主控里。 | 等奖励页和拖拽剩余边界稳定后，先拆房间结算 payload 消费，不动地图移动。 |
| 6 | `scene/in_scene/tile.gd` | 地貌规则、状态组件、结算奖励、敌人意图、血量、贴图选择和 timeline shape 混在一个实体脚本里。 | 先拆纯解析或适配小边界，例如 timeline shape 解析，不动实体生命周期。 |
| 7 | `scene/card/custom_card.gd` | 卡牌数据解析、形状解析、选中视觉、tooltip、效果范围和出牌逻辑耦合。 | 先拆形状解析或选中视觉，避免直接改出牌逻辑。 |
| 8 | `scene/in_scene/enermy/enemy_intent_presentation_controller.gd` | 引用查找、hover phase 判断、tooltip、关键词 tooltip、地图和时间轴表现耦合。 | 先拆引用查找或 tooltip presenter，不动地图/时间轴联动规则。 |
| 9 | `scene/in_scene/timeline/TimelineManager.gd` | 时间轴规则核心较集中，但敌人意图候选、排序、落点选择还可拆。 | 等 `timeline_ui.gd` 剩余表现稳定后，再拆敌人意图落点选择服务。 |
| 10 | `scene/in_scene/timecoin_ui.gd` | 全局 Timecoin 查找、数值显示、获得/消耗/警告动画、沙漏 shader 混在 UI 脚本里。 | 先拆全局查找和动画 runner，保持数值来源不变。 |

## 各重点文件的第一批低风险拆法

### DragShapeController.gd

已拆低风险边界包括节点桥接、拒绝提示、时间轴预览转发、卡牌形状解析、网格鼠标过滤、放置查询、时间轴 UI 状态、预览清理、网格坐标解析、场景交互锁、卡牌效果预览文本、目标效果预览、玩家 TimelineAction 创建和时间轴提交。

下一批如果继续处理拖拽，优先评估放置成功后的更小收尾边界：

```text
end_dragging_success() 中卡牌视觉状态恢复
或 end_dragging_success() 中弃牌归属移动
```

玩家 TimelineAction 创建已经拆到 `DragPlayerActionFactory.gd`，时间轴提交已经拆到 `DragTimelineActionSubmitter.gd`。继续前先写清楚卡牌视觉恢复、卡牌从手牌到弃牌或时间轴的归属变化、地图输入恢复和 UI 预览清理分别由谁负责。不要把卡牌归属变化和 UI 清理一次性合并到新模块。

### timeline_ui.gd

已拆低风险边界包括展开遮罩表现、背景网格构建、网格交互表现、顶部锚点布局、网格预览样式、TimelineManager 查找、敌方意图 overlay 和行动方格放置动画。

下一批如果继续处理时间轴 UI，优先评估：

```text
行动块视觉 presenter
或 清理动画 runner
```

不要在同一批修改 TimelineManager 数据结构、敌人意图规则和行动块表现。

### 奖励脚本

奖励页已经抽出 CardManager 查找、tooltip、临时牌堆、真实卡牌生成/清理、DraftCard 数据写入、贴图和描述提取、牌组同步、只读牌组来源，以及 Shop、Craft、Remove 的多个页面专属模块。

下一批奖励页优先级：

```text
1. ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小生成边界
2. 如果继续奖励页需要传入过多状态，就停止奖励页，转向 DragShapeController.gd
3. 不要重复拆 Shop 定价和时代权重 Resource
```

`AcquireReward.gd` 和 `RemoveReward.gd` 当前不建议继续硬拆。它们剩余部分主要是页面流程编排、确认提交和关闭逻辑。

### Resource 化候选

Resource 适合承载“可复用、可 Inspector 调参、默认只读”的数据，不适合承载会在一局内频繁变化的运行态状态。当前状态：

```text
已完成：CraftReward.gd::CRAFTING_RECIPES -> CraftRecipeBook / CraftRecipeEntry / default_craft_recipe_book.tres
已完成：ShopManager.gd 的 base_price、slot_price_step、price_increment、refresh_base_cost、upgrade_base_cost -> ShopPricingConfig / default_shop_pricing_config.tres
已完成：ShopManager.gd 的 weight_current_era、weight_previous_era、weight_next_era、weight_next_next_era -> ShopEraWeightConfig / default_shop_era_weight_config.tres
P2：timeline_ui.gd 的网格、行动块、敌方意图 overlay 表现参数 -> TimelineVisualConfig
P2：hex_map.gd 的高度视图、敌方意图地图表现、入场动画和地貌投放配额 -> 独立小配置 Resource
P2：DragShapeController.gd 的拖拽动画、吸附参数和拒绝提示样式 -> DragPlacementVisualConfig
P3：tutorial_config.gd 当前已经是配置脚本，后续可继续保持 Resource 风格，但不要把教程运行进度写入其中
```

不要直接资源化：

```text
GlobalDB.player_deck、map_data、stack_nodes、slot_entries、current_result_card_id
临时牌堆、真实卡节点、DraftCard 节点、Tween、场景树查询结果
需要 get_tree()、输入、动画回调或跨场景同步的行为
```

下一批如果继续 Resource 化，Shop 的定价和时代权重都已经完成，不要重复拆 `ShopPricingConfig` 或 `ShopEraWeightConfig`。后续 Resource 候选应转向 timeline/drag/hex_map 的纯视觉或调参配置；注意新 Resource 脚本之间不要在首次解析时互相依赖新 `class_name` 类型，内部字段优先用 `Resource` 或基础类型，避免 Godot 全局类缓存未刷新时报解析错误。

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

当前已拆模块共 130 个，docs/modularized-files-ultimate-operation-guide.md 覆盖缺失为 0。不要继续机械拆 hex_map.gd、in_scene.gd、AcquireReward.gd、RemoveReward.gd 或 CraftReward.gd 的确认关闭链。下一阶段优先处理：
1. scene/in_scene/DragShapeController.gd 的放置完成流程评估
2. scene/in_scene/rewards/ShopManager.gd 的 _generate_shop_items() 临时牌堆生命周期等更小边界评估
3. scene/in_scene/timeline/timeline_ui.gd 的行动块表现或清理动画评估
4. scene/out_scene/out_scene_map_exp.gd 的房间结算 payload 消费拆分

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

优先评估 DragShapeController.gd 放置成功收尾中的卡牌视觉状态恢复或弃牌归属移动，或先确认 ShopManager.gd 的临时牌堆生命周期是否还能形成小边界。DragShapeController.gd 的玩家 TimelineAction 创建已拆到 DragPlayerActionFactory.gd，时间轴提交已拆到 DragTimelineActionSubmitter.gd，下一批不要重复拆行动创建或时间轴提交。CraftReward.gd::_refresh_result_preview() 的结果预览挂载已经拆到 CraftResultPreviewPresenter.gd，预览卡尺寸/位置/tooltip 鼠标过滤已经拆到 CraftPreviewCardConfigurator.gd，_create_preview_card() 已复用 RewardDraftCardFactory 且剩余异步链不建议继续硬拆，_apply_crafting_result_to_deck() 已拆出索引计算和结果写入处理，CRAFTING_RECIPES 已资源化，剩余同步/关闭属于页面编排。ShopManager.gd 的商店定价已资源化为 ShopPricingConfig，时代权重已资源化为 ShopEraWeightConfig；_generate_shop_items() 的单个商品生成编排已经判断会传入 draft_card_factory、temp_pile、deck_manager、UI 注册和异步数据提取等过多状态，不建议硬拆。不要重复拆已经完成的节点桥接、tooltip、CardManager 查找、临时牌堆、DraftCard 数据写入、生成依赖检查、只读牌组来源、Craft 预览工厂复用、Craft 结果写入模块、Craft 配方资源、Shop 定价资源、Shop 时代权重资源、Drag 玩家行动创建和 Drag 时间轴提交。
```
