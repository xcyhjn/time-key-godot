# 下一位 AI 接力当前态与维护 prompt

日期：2026-06-13

## 这份文件的定位

这份文件给下一位接力 AI 快速进入当前项目状态。它不是根基规则，也不替代 `docs/` 下的终极说明。接手者必须先读根基文档，再把本文当作“当前进度、停止点、下一步建议和可复制 prompt”的导航。

项目路径：

```text
D:/godot/时之钥/时之钥
```

## 接手后先读根基文档

按顺序读：

```text
AGENTS.md
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
```

如果仓库里没有实体 `AGENTS.md`，以用户在对话中贴出的 AGENTS 约束为准。当前已确认仓库里没有实体 `AGENTS.md`。

然后按目标文件读取对应维护入口：

```text
workflow_logs/maintenance_guides/README.md
workflow_logs/maintenance_guides/timeline_manager.md
workflow_logs/maintenance_guides/timeline_ui.md
workflow_logs/maintenance_guides/timecoin_ui.md
workflow_logs/maintenance_guides/tile.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
workflow_logs/maintenance_guides/custom_card.md
workflow_logs/maintenance_guides/out_scene_map_exp.md
workflow_logs/maintenance_guides/drag_shape_controller.md
workflow_logs/maintenance_guides/rewards.md
workflow_logs/maintenance_guides/in_scene.md
```

如果环境提供技能文件，做 GDScript 改动前优先读取 `godot-prompter:gdscript-patterns`；写 Markdown 交接或维护说明时优先读取 `docs-write`。本批已读取 `docs-write` 主体，但它引用的共享 style guide 在本机没有找到，因此后续文档以“给接手者快速行动、中文自然语言、少堆空话”为准。

## 用户持续要求汇总

用户的核心要求一直没有变：

```text
先审查，不要直接改代码。
每批只拆 1 个清晰风险面。
一次最多触碰 3 到 4 个模块或风险点。
新增模块必须写中文职责注释，说明“负责什么”和“不负责什么”。
Markdown 使用中文自然语言。
docs/ 目录只保留最新版总结性说明。
中间过程写 workflow_logs/current-modularization-process.md。
每批改完运行 git diff --check、必要 Godot headless 检查、模块覆盖检查。
清理临时日志。
每批单独 commit。
不要回滚、格式化、stage 或提交用户已有改动。
不要为了降行数硬拆 composition root。
```

近几轮用户关注顺序是：

```text
custom_card.gd 拆分
timecoin_ui.gd 拆分
tile.gd 拆分
TimecoinUI 和 EnemyIntentPresentationController tooltip 拆分
时间轴部分继续优化
TimelineManager.gd 继续拆分
当前轮要求：总结前述 prompt 和项目进度，写详细接力 Markdown，并输出给下一位 AI 的接力 prompt
```

## 当前 Git 与工作区状态

最新提交以 `git log --oneline -1` 为准。本批提交主题：

```text
refactor: extract timecoin tween state controller
```

上一批最近提交：

```text
208097e refactor: extract out scene camera limit controller
b055634 refactor: extract enemy intent tooltip position helper
f8ff476 refactor: extract enemy intent status keyword tooltip presenter
a33d06e refactor: extract timeline enemy intent candidate collector
2edfdcf refactor: extract timeline enemy intent target resolver
1b2a367 docs: add next ai modularization handoff
d52ec9a refactor: extract timeline enemy intent priority selector
8ea3b88 refactor: extract timeline intro playback controller
3ea760c refactor: extract timeline visual config reader
fa47777 refactor: extract enemy intent tooltip text builder
99b1876 refactor: extract timecoin feedback animation runner
d8c003b refactor: finish tile intent action data reuse
484f953 refactor: reuse tile intent action data builder
491a422 refactor: extract tile intent action data builder
818a5b8 refactor: extract tile texture state selector
b3d187c refactor: extract tile damage protection rules
```

最近一次确认时，工作区有这些用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

不要回滚、stage 或提交这些文件，除非用户明确要求。本接力文档本身是当前轮新增/整理的 Markdown 文件。

当前模块统计：

```text
脚本模块：180
默认 Resource 文件：4
总覆盖对象：184
docs/modularized-files-ultimate-operation-guide.md 覆盖缺失：0
```

## 固定开工流程

每批都按这个顺序做：

```text
git status --short
读取根基文档
读取目标文件维护入口
用 rg 输出目标文件函数、变量、信号轮廓
写当前职责、耦合点、待办清单、本批风险面
只选 1 个清晰风险面
修改代码或文档
更新 workflow_logs/current-modularization-process.md
如新增模块，同步更新 docs/modularized-files-ultimate-operation-guide.md、docs/ai-handoff-ultimate-operation-guide.md 和对应 maintenance_guides
运行 git diff --check
运行必要 Godot headless 检查
运行模块覆盖检查
清理临时日志
单独 commit
汇报完成内容、验证结果、当前进度和下一步
```

开工审查需要至少写清楚：

```text
目标文件当前职责
最明显耦合点
待办清单
本批只处理哪个风险面
本批不触碰哪些边界
最小验证路径
```

## 当前项目完成情况

### HexMap 已进入维护阶段

`scene/in_scene/hex_map.gd` 现在是地图 composition root。`hex_map_modules/` 已覆盖生成、地貌投放、目标规则、地块创建、输入、地图表现、高度视图、升降毁灭、运行时注册、结算奖励、回合行为和跨系统桥接。

不要为了降行数继续机械拆 `hex_map.gd`。涉及地图规则时先读 `docs/hex-map-ultimate-operation-guide.md`，新增功能优先进入现有模块分类。

### InScene 已进入维护阶段

`scene/in_scene/in_scene.gd` 已拆出节点桥接、卡牌系统、UI 输入、tooltip、hover、首回合入场、敌人意图刷新、payload、场景切换、结算奖励、胜负流程等模块。剩余主要是 `_ready()`、回合推进、奖励返回和跨系统编排。

继续拆前需要更完整回归路径，不建议直接拆回合流。

### DragShapeController 已接近停止点

已拆节点桥接、拒绝提示、时间轴预览、放置校验、交互锁、放置动画、玩家 `TimelineAction` 创建、时间轴提交、成功卡牌视觉复原和弃牌移动。

不要继续包一层 `collapse(timeline_ui)`；它已经是现有模块单行调用。失败 fallback 同时牵动手牌、tooltip、时间轴和主状态，暂不拆。

### TimelineUI 已进入 UI composition root 阶段

`timeline_ui.gd` 已拆出 16 个 UI 模块和 1 个视觉 Resource：

```text
布局
背景网格
格子交互
顶部锚点布局
网格预览
TimelineManager 查找
视觉配置读取
敌方意图 overlay
行动方格放置动画
行动容器几何
行动方块视觉节点
整体形状视觉层
清理动画残影创建
残影 tween 播放
行动块 hover 状态通知
入场动画编排状态
TimelineVisualConfig 纯视觉参数资源化
```

不要硬拆 `_on_action_placed()`。它仍同时牵动行动容器、方格创建、overlay、hover 信号、intro 动画和整体形状视觉层。

### TimelineManager 已拆出三个敌方意图规则边界

已新增：

```text
scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentCandidateCollector.gd
scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentPrioritySelector.gd
scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentTargetResolver.gd
```

`TimelineEnemyIntentCandidateCollector.gd` 只负责从敌人列表收集本回合可进入时间轴的意图候选，包含协议检查、意图开关、`can_generate_intent(hex_map)` 和 shape 缓存。旧 `_collect_enemy_intent_candidates()` 仍保留并转发。

`TimelineEnemyIntentPrioritySelector.gd` 只负责敌方意图优先级读取、降序优先级列表、同级候选过滤和同级可放置候选选择。`TimelineManager.gd` 仍保留旧入口：

```text
_get_enemy_intent_priority()
_get_sorted_priority_values()
_filter_candidates_by_priority()
_pick_placeable_candidate()
```

`TimelineEnemyIntentTargetResolver.gd` 只负责把敌方意图声明的目标中心坐标映射为 `HexMap.stack_nodes` 里的地块节点。旧 `_resolve_intent_target_tile()` 仍保留并转发。

不要重复拆候选收集 collector、优先级 selector 或目标地块 resolver。`generate_enemy_intents()` 仍是主编排入口；当前不建议为了降行数继续硬拆它。

### TimecoinUI 低风险拆分面已基本收口

已拆：

```text
scene/in_scene/timecoin_ui_modules/bridges/TimecoinGlobalBridge.gd
scene/in_scene/timecoin_ui_modules/presenters/TimecoinHourglassShaderController.gd
scene/in_scene/timecoin_ui_modules/animation/TimecoinShakeTweenBuilder.gd
scene/in_scene/timecoin_ui_modules/animation/TimecoinFeedbackAnimationRunner.gd
scene/in_scene/timecoin_ui_modules/animation/TimecoinTweenStateController.gd
```

`timecoin_ui.gd` 仍负责节点引用、`GlobalTimecoin` 信号连接、数值显示、动画触发入口、UI 原始状态恢复和旧 shader 控制入口。

TimecoinUI 的全局查找、沙漏 shader、普通抖动、反馈动画片段和 `active_tweens` 列表维护都已拆出。后续若继续，先重新审查剩余函数，不要重复拆全局查找、沙漏 shader、普通抖动、获得/消耗/不足动画片段或 tween 状态 controller。

### EnemyIntentPresentationController 已拆引用 bridge 与三个 tooltip 表现边界

已新增：

```text
scene/in_scene/enermy/intent_presentation_modules/bridges/EnemyIntentPresentationReferenceBridge.gd
scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentTooltipTextBuilder.gd
scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentStatusKeywordTooltipPresenter.gd
scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentTooltipPositionHelper.gd
```

`EnemyIntentPresentationReferenceBridge.gd` 只查找 `MainBoard`、`HexMap`、`TimelineManager`、`TimelineUI` 和 `DragShapeController`，并按旧入口顺序连接时间轴 hover 与地图重判信号。

`EnemyIntentTooltipTextBuilder.gd` 只组装主 tooltip 文本行。`EnemyIntentStatusKeywordTooltipPresenter.gd` 只创建、定位和销毁状态关键词副 tooltip。

`EnemyIntentTooltipPositionHelper.gd` 只计算主 tooltip 的屏幕位置和边界 clamp。

控制器仍负责 hover phase 判断、地图/时间轴表现、主 tooltip 写入、关键词列表读取、tooltip host 选择、source stack 查找、`MainBoard.set_cursor_tooltip_position()` 调用和延迟定位。引用查找和 tooltip 低风险表现面已经收口；后续如果继续，先重新审查剩余函数，不要重复拆 reference bridge、文本 builder、状态关键词副 tooltip presenter 或主 tooltip 定位 helper。

### Tile 已完成多个低风险边界

已拆：

```text
scene/in_scene/tile_modules/rules/TileTimelineShapeParser.gd
scene/in_scene/tile_modules/rules/TileIntentActionFactory.gd
scene/in_scene/tile_modules/rules/TileIntentActionDataBuilder.gd
scene/in_scene/tile_modules/rules/TileHealthStateRules.gd
scene/in_scene/tile_modules/controllers/TileDeathExecutionController.gd
scene/in_scene/tile_modules/rules/TileDamageProtectionRules.gd
scene/in_scene/tile_modules/rules/TileTextureStateSelector.gd
```

Tile 标准 `action_data` 方向已收口。后续如果继续 Tile，只能单独重新审查子类 picker 策略。不要重复拆死亡收尾、protected 受击、贴图归属判断，也不要同批改 `TimelineAction` 数据契约。

### CustomCard 已拆低风险规则、表现和桥接

已拆：

```text
scene/card/custom_card_modules/bridges/CustomCardNodeBridge.gd
scene/card/custom_card_modules/bridges/CustomCardTooltipBridge.gd
scene/card/custom_card_modules/bridges/CustomCardMapConditionalEffectBridge.gd
scene/card/custom_card_modules/rules/CustomCardDescriptionParser.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
```

继续前必须重新审查剩余函数。不要为了行数硬拆 `_enter_state()`、`toggle_selection()`、`force_deselect()`、`return_to_hand()`、`apply_stat_modifier()` 或 `play_card()`。

### Rewards 已接近页面编排

Craft、Shop、Acquire、Remove 已拆出大量通用桥接、presenter、factory、rules 和 Resource。重点完成：

```text
CraftRecipeBook / CraftRecipeEntry / default_craft_recipe_book.tres
ShopPricingConfig / default_shop_pricing_config.tres
ShopEraWeightConfig / default_shop_era_weight_config.tres
Craft 结果预览、预览卡配置、DraftCard 工厂复用、结果写入
Shop 生成依赖检查、商品槽注册、购买/刷新/升级处理、隐藏临时牌堆创建
```

不要继续硬拆 Craft 确认/关闭链或 Shop 单商品生成编排。

### OutScene 已拆结算、章节揭示、payload 注入和镜头限制

已拆：

```text
scene/out_scene/out_scene_modules/RoomResolutionController.gd
scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd
scene/out_scene/out_scene_modules/OutScenePayloadBridge.gd
scene/out_scene/out_scene_modules/OutSceneCameraLimitController.gd
```

房间完成状态回写已经做过数据契约评估：

```text
path_gone 是路径坍塌记录，不是房间完成状态。
active_room_context 和 pending_room_resolution 是跨场景临时桥接，不是长期状态。
tile_data 仍是坐标到房间类型的逻辑地图，不要混入完成状态。
如果实现房间完成/已清空/已领奖，必须新增独立 MapState 字段和 Saver 持久化字段。
```

镜头限制小模块已经完成，`apply_tier_camera_limit()` 与 `_apply_sector_camera_limits()` 旧入口仍保留并转发到 `OutSceneCameraLimitController.gd`。不要重复拆这一面，也不要同批碰地图移动和场景切换 executor。

## 当前最推荐的下一步

### 首选：OutScene 房间完成状态前置设计

目标文件：

```text
scene/out_scene/out_scene_map_exp.gd
```

先读：

```text
workflow_logs/maintenance_guides/out_scene_map_exp.md
```

只评估：

```text
房间完成/已清空/已领奖状态的数据契约，不直接复用 path_gone、active_room_context、pending_room_resolution 或 tile_data
```

不要同批碰：

```text
地图移动
路径坍塌
场景切换 executor
镜头限制
```

### 备选：CustomCard 剩余函数重新审查

目标文件：

```text
scene/card/custom_card.gd
```

继续前先重新审查剩余函数，避免硬拆出牌和选中状态链。不要重复拆 node bridge、description parser、timeline shape parser、effect range parser、selected follow presenter、hover shader presenter、tooltip bridge 或 map conditional bridge。

### 备选：EnemyIntentPresentationController 剩余函数重新审查

目标文件：

```text
scene/in_scene/enermy/enemy_intent_presentation_controller.gd
```

引用查找 bridge 和三个 tooltip 模块已经完成。后续如果继续，只能先重新审查剩余函数；不要重复拆：

```text
EnemyIntentPresentationReferenceBridge.gd
EnemyIntentTooltipTextBuilder.gd
EnemyIntentStatusKeywordTooltipPresenter.gd
EnemyIntentTooltipPositionHelper.gd
```

不要同批碰 `EnemyIntentResolver`、`TimelineManager` 或地图/时间轴联动规则。

### OutScene 继续前先重新审查

`out_scene_map_exp.gd` 已经完成镜头限制拆分。如果继续，只能先重新审查剩余函数；房间完成状态必须先设计独立 `MapState` 字段和 Saver 持久化，不要复用 `path_gone`，也不要同批碰地图移动和场景切换 executor。

### 暂不建议继续硬拆 TimelineManager

```text
scene/in_scene/timeline/TimelineManager.gd
```

已拆：

```text
候选收集 collector
优先级 selector
目标地块 resolver
```

继续前先重新审查剩余函数，不要重复拆：

```text
_collect_enemy_intent_candidates()
_get_enemy_intent_priority()
_pick_placeable_candidate()
_resolve_intent_target_tile()
generate_enemy_intents()
```

## 当前不建议继续的方向

```text
不要硬拆 timeline_ui.gd::_on_action_placed()
不要继续包 DragShapeController 的成功收尾或 fallback 收尾
不要硬拆 Craft/Shop 奖励页确认、关闭或单商品生成编排
不要继续 Tile 已完成的 action_data、死亡、protected、贴图选择方向
不要重复拆 TimecoinUI 的反馈动画 runner、active_tweens controller、GlobalTimecoin 查找、shader controller
不要把运行态对象、节点、Tween、tile_data、path_gone 或真实卡节点注册成 Resource
不要把 out_scene 的 path_gone 复用成房间完成状态
```

## 验证命令

文档批次至少运行：

```powershell
git diff --check
```

代码批次至少运行：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
```

局内相关改动加载：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

局外相关改动加载：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/out_scene/Out_Scene.tscn --quit-after 1 --no-header
```

模块覆盖检查：

```powershell
$moduleDirs = @(
  'scene/in_scene/hex_map_modules',
  'scene/in_scene/in_scene_modules',
  'scene/in_scene/drag_modules',
  'scene/in_scene/timeline/ui_modules',
  'scene/in_scene/timeline/manager_modules',
  'scene/in_scene/timeline/resources',
  'scene/in_scene/rewards/animation',
  'scene/in_scene/rewards/bridges',
  'scene/in_scene/rewards/diagnostics',
  'scene/in_scene/rewards/factory',
  'scene/in_scene/rewards/presenters',
  'scene/in_scene/rewards/rules',
  'scene/in_scene/rewards/resources',
  'scene/card/custom_card_modules',
  'scene/in_scene/timecoin_ui_modules',
  'scene/in_scene/enermy/intent_presentation_modules',
  'scene/in_scene/tile_modules',
  'scene/out_scene/out_scene_modules'
)
$files = foreach ($dir in $moduleDirs) {
  if (Test-Path $dir) {
    Get-ChildItem -Path $dir -Recurse -File | Where-Object { $_.Extension -in '.gd', '.tres' }
  }
}
$doc = Get-Content docs/modularized-files-ultimate-operation-guide.md -Raw -Encoding UTF8
$missing = @()
foreach ($file in $files) {
  $rel = $file.FullName.Substring((Get-Location).Path.Length + 1).Replace([char]92, [char]47)
  if ($doc -notmatch [regex]::Escape($rel)) {
    $missing += $rel
  }
}
$scriptCount = ($files | Where-Object { $_.Extension -eq '.gd' }).Count
$resourceCount = ($files | Where-Object { $_.Extension -eq '.tres' }).Count
$totalCount = $files.Count
"scripts=$scriptCount resources=$resourceCount total=$totalCount missing=$($missing.Count)"
$missing | Sort-Object
```

当前预期输出：

```text
scripts=180 resources=4 total=184 missing=0
```

如果新增模块，同步更新统计和文档后，新的统计可以增加，但 `missing` 必须仍为 0。

当前已知 Godot 旧噪声：

```text
ObjectDB instances leaked at exit
resources still in use at exit
TileSet atlas has no tile / Cannot create tile
RID allocations leaked at exit
```

重点检查是否出现新的：

```text
SCRIPT ERROR
Parse Error
Compile Error
Failed to load script
Compilation failed
Invalid call
Invalid access
```

## 给下一位 AI 的可复制接力 prompt

```text
请在 D:/godot/时之钥/时之钥 继续项目模块化解耦。

必须先审查，不要直接改代码。先读取并遵守：
1. AGENTS.md；如果不存在，以本 prompt 内约束和当前对话中用户贴出的 AGENTS 约束为准。
2. docs/ai-handoff-ultimate-operation-guide.md
3. docs/hex-map-ultimate-operation-guide.md
4. docs/modularized-files-ultimate-operation-guide.md
5. workflow_logs/current-modularization-process.md
6. workflow_logs/next-ai-handoff-current-status.md
7. 目标文件对应的 workflow_logs/maintenance_guides/*.md

如果环境提供技能文件，做 GDScript 改动前先读 godot-prompter:gdscript-patterns；写 Markdown 交接或维护说明时先读 docs-write。

当前工作区预计有用户已有改动：
- default_bus_layout.tres
- shaders/color_BG.gdshader
- shaders/game_over.gdshader
不要回滚、stage、格式化或提交这些文件，除非我明确要求。

当前模块化进度：
- 最新提交：本批提交为 `refactor: extract timecoin tween state controller`；哈希以 `git log --oneline -1` 为准。
- 当前已拆脚本模块 180 个，另有 4 个默认 Resource 文件。
- docs/modularized-files-ultimate-operation-guide.md 覆盖缺失应为 0。
- docs/ 目录只保留总结性说明；中间过程写 workflow_logs/current-modularization-process.md。

固定工作规则：
- 先用 rg 输出目标文件函数、变量、信号轮廓。
- 先写当前职责、耦合点、待办清单、本批风险面，再开始修改。
- 每批只拆 1 个清晰风险面；一次最多触碰 3 到 4 个模块或风险点。
- 新增模块必须写中文职责注释，说明“负责什么”和“不负责什么”。
- Markdown 用中文自然语言。
- 主文件保留旧入口，新模块只接管清晰小职责。
- 不要为了降行数硬拆 composition root。
- 不要回滚用户已有改动。
- 每批改完运行 git diff --check、必要 Godot headless 检查、模块覆盖检查。
- 清理临时日志。
- 每批单独 commit。

推荐下一步：
- out_scene_map_exp.gd：若继续必须先重新审查剩余函数；房间完成状态需要独立数据契约，不复用 path_gone，不同批碰地图移动和场景切换 executor。
- custom_card.gd：只重新审查剩余函数，避免硬拆出牌和选中状态链。
- timecoin_ui.gd：低风险拆分面已基本收口；若继续先重新审查剩余函数，不重复拆 GlobalTimecoin 查找、shader controller、反馈动画 runner 或 active_tweens controller。
- enemy_intent_presentation_controller.gd：引用查找 bridge 和 tooltip 三个模块已完成；若继续先重新审查剩余函数，不重复拆 reference bridge、tooltip text builder、status keyword presenter 或 tooltip position helper。

如果重新评估 TimelineManager：
- 先读 workflow_logs/maintenance_guides/timeline_manager.md。
- 不要重复拆 TimelineEnemyIntentCandidateCollector.gd。
- 不要重复拆 TimelineEnemyIntentPrioritySelector.gd。
- 不要重复拆 TimelineEnemyIntentTargetResolver.gd。
- 不要同批修改 grid、is_placement_valid()、place_action()、find_random_available_spot()。
- 不要同批改 TimelineAction.action_data、TimelineUI 表现、敌方意图 tooltip 或 resolve_timeline()。
- generate_enemy_intents() 仍作为组合候选、目标、action 创建和最终放置的主编排入口，当前不建议为了降行数硬拆。

不要继续硬拆：
- timeline_ui.gd 的 _on_action_placed()
- DragShapeController 成功收尾和 fallback 收尾
- Craft/Shop 奖励页确认、关闭或单商品生成编排
- Tile 已完成的 action_data、死亡、protected、贴图选择方向
- TimecoinUI 的反馈动画 runner、active_tweens controller、GlobalTimecoin 查找、shader controller

验证要求：
- git diff --check
- Godot headless 项目检查
- 若改局内模块，加载 res://scene/in_scene/in_scene.tscn
- 若改局外模块，加载 res://scene/out_scene/Out_Scene.tscn
- 模块覆盖检查应输出 scripts=180 resources=4 total=184 missing=0；若新增模块则同步更新统计和 docs，missing 仍必须为 0。
```
