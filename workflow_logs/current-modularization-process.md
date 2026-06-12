# 模块化解耦流程归档

日期：2026-06-05

## 2026-06-13 EnemyIntentPresentationController 引用查找 bridge 拆分

### 读取与轮廓

本批处理 `scene/in_scene/enermy/enemy_intent_presentation_controller.gd` 的引用查找小风险面。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。当前工作区仍有用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不 stage、不提交这些文件。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`。已用 `rg` 输出目标文件的 `class_name`、`extends`、`signal`、`@export`、`@onready`、`const`、`var`、`func` 轮廓，并搜索 `_resolve_references()`、`MainBoard`、`HexMap`、`TimelineManager`、`TimelineUI`、`DragShapeController` 与 hover/重判信号连接。

### 当前职责

`enemy_intent_presentation_controller.gd` 仍是敌人意图表现协调器，负责地图 hover、时间轴 hover、交互阶段门禁、地图与时间轴预览同步、主 tooltip 写入、状态关键词副 tooltip 展示清理，以及 hover 退出、阶段切换和意图重判时的统一清理。

### 耦合点

```text
_resolve_references() 同时查找 MainBoard、HexMap、TimelineManager、TimelineUI 和 DragShapeController。
同一入口还连接 TimelineManager.action_hovered_changed、HexMap.enemy_roster_changed 和 HexMap.tile_topology_changed。
tooltip 文本、tooltip 定位、状态关键词副 tooltip、hover phase 判断和地图/时间轴表现与引用查找相邻，但本批不触碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 引用查找 bridge | `_resolve_references()` 的节点收集与信号接线 | 旧入口可保留，路径和接线顺序清晰 | 执行 |
| 2 | hover phase 判断 | `_is_map_hover_allowed()`、`_is_timeline_hover_allowed()`、`_get_phase()` | 牵动主交互状态和恢复预览 | 不碰 |
| 3 | 地图/时间轴表现同步 | `show_intent_preview()`、`clear_intent_preview()` | 同时驱动 HexMap、TimelineUI 和 tooltip | 不碰 |
| 4 | tooltip 剩余入口 | 主 tooltip 写入、host 选择、source stack 查找 | 已拆三个低风险表现模块，继续硬拆收益低 | 不碰 |

### 本批风险面

本批只处理一个风险面：敌人意图表现控制器的跨系统引用查找与旧信号接线。

涉及的小风险点：

```text
新增 EnemyIntentPresentationReferenceBridge.gd，集中查找 MainBoard、HexMap、TimelineManager、TimelineUI 和 DragShapeController。
旧 _resolve_references() 保留入口，继续先拿 MainBoard、隐藏旧 tooltip，再接收 bridge 返回引用。
hover 与重判信号仍按原顺序连接，避免改变时间轴 hover 和地图拓扑变更后的重判行为。
```

不触碰：

```text
EnemyIntentResolver、EnemyIntentData、TimelineManager 数据结构。
地图/时间轴联动规则、TimelineUI 表现和敌方意图 tooltip 文案。
hover phase 判断、resolve_timeline()、generate_enemy_intents()。
```

### 实现结果

新增：

```text
scene/in_scene/enermy/intent_presentation_modules/bridges/EnemyIntentPresentationReferenceBridge.gd
```

职责：

```text
EnemyIntentPresentationReferenceBridge 只负责为敌人意图表现协调器查找跨系统节点，并按旧入口顺序连接 hover 与重判信号。
它不判断交互阶段，不解析敌人意图，不驱动地图或时间轴表现，也不创建、写入或定位 tooltip。
```

`enemy_intent_presentation_controller.gd` 新增 `EnemyIntentPresentationReferenceBridgeScript` preload、缓存 getter 和旧入口转发。`_resolve_references()` 仍保留主入口，继续在拿到 `MainBoard` 后隐藏旧 tooltip，并把引用结果写回 `hex_map`、`timeline_manager`、`timeline_ui` 和 `drag_shape_controller`。

同步更新：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 179 个，默认 Resource 文件仍为 4 个。EnemyIntentPresentationController 现在有 4 个拆分模块：

```text
EnemyIntentPresentationReferenceBridge.gd
EnemyIntentTooltipTextBuilder.gd
EnemyIntentStatusKeywordTooltipPresenter.gd
EnemyIntentTooltipPositionHelper.gd
```

引用查找和 tooltip 低风险表现面已收口。下一批不要重复拆 reference bridge、tooltip text builder、status keyword presenter 或 tooltip position helper。更稳妥的下一步是 `scene/in_scene/timecoin_ui.gd` 的 `active_tweens` 清理 controller；如果继续 EnemyIntentPresentationController，必须先重新审查剩余函数，不要同批触碰 `EnemyIntentResolver`、`TimelineManager` 或地图/时间轴联动规则。

### 回归检查

已运行：

```text
git diff --check 通过；仅有 workflow_logs/current-modularization-process.md 的既有换行归一化提示，无空白错误。
Godot headless 项目检查通过：EXIT=0；仍有项目既有 ObjectDB/resource 退出噪声。
加载 res://scene/in_scene/in_scene.tscn 通过：EXIT=0；过滤 enemy_intent_presentation_controller 与 intent_presentation_modules 相关输出后没有脚本错误，场景加载仍有项目既有 TileSet atlas 噪声。
模块覆盖检查通过：scripts=179 resources=4 total=183 missing=0。
```

## 2026-06-13 OutScene 镜头限制拆分

### 读取与轮廓

本批处理 `scene/out_scene/out_scene_map_exp.gd` 的镜头限制小风险面。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。当前工作区仍有用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不 stage、不提交这些文件。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/out_scene_map_exp.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`。已用 `rg` 输出目标文件的 `class_name`、`extends`、`signal`、`@export`、`@onready`、`const`、`var`、`func` 轮廓，并搜索 `apply_tier_camera_limit()`、`_apply_sector_camera_limits()`、`Camera2D.limit_*`、`current_tier`、`step_x`、`step_y` 与 `stagger_y` 的耦合边界。

### 当前职责

`out_scene_map_exp.gd` 仍是局外地图 composition root，负责地图生成/恢复、玩家移动、路径坍塌、镜头限制入口、进房、保存、章节推进和场景切换。旧 `apply_tier_camera_limit()` 与 `_apply_sector_camera_limits()` 仍作为主文件入口保留。

### 耦合点

```text
apply_tier_camera_limit() 同时读取 gen.layer_boundaries、step_x、step_y、stagger_y，并直接写 Camera2D.limit_*。
_apply_sector_camera_limits() 根据选角扇区坐标直接裁剪 Camera2D 边界。
镜头动画、镜头锁定、玩家移动、路径坍塌和切场景与镜头限制相邻，但本批不触碰。
房间完成状态回写缺少独立持久化字段，本批不处理。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 镜头限制 controller | `apply_tier_camera_limit()` 与 `_apply_sector_camera_limits()` | 只计算并写入 `Camera2D.limit_*`，旧入口可转发，风险清晰 | 执行 |
| 2 | 房间完成状态持久化 | `MapState`、`Saver`、结算 payload 消费 | 需要新增数据契约，不适合作为顺手拆分 | 暂缓 |
| 3 | 地图移动/路径坍塌 | `_move_to()` 与扇区裁剪流程 | 牵动玩家移动、动画、视觉刷新和进房 | 不碰 |
| 4 | 场景切换 executor | `_switch_scene_with_data()` | 牵动保存、资源加载、挂树和 payload 注入 | 不碰 |

### 本批风险面

本批只处理一个风险面：局外地图镜头边界限制。
涉及的小风险点：

```text
新增 OutSceneCameraLimitController.gd，集中处理层级边界和扇区边界的 Camera2D limit 写入。
out_scene_map_exp.gd 保留 apply_tier_camera_limit() 和 _apply_sector_camera_limits() 旧入口，并只转发参数。
文档同步新增模块、统计和后续停止点，避免下一批重复拆镜头限制。
```

不触碰：

```text
地图移动、路径坍塌、进房、场景切换 executor。
镜头 focus_on_position()、restore_camera()、_is_locked 与 _target_zoom。
房间完成状态、MapState 新字段、Saver 持久化。
tile_data、tile_features、view.tiles 数据契约。
```

### 实现结果

新增：

```text
scene/out_scene/out_scene_modules/OutSceneCameraLimitController.gd
```

职责：

```text
OutSceneCameraLimitController 只负责计算并写入局外地图 Camera2D 的边界限制。
它不移动镜头，不锁定或解锁镜头，不修改地图数据，也不处理玩家移动、进房、保存或场景切换。
```

`out_scene_map_exp.gd` 新增 `OutSceneCameraLimitControllerScript` preload、缓存 getter 和旧入口转发。`apply_tier_camera_limit()` 仍由主文件接收 tier，并把 `gen.layer_boundaries` 与步距参数传给 controller；`_apply_sector_camera_limits()` 仍由选角/恢复流程调用，并把扇区坐标传给 controller。

同步更新：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/maintenance_guides/out_scene_map_exp.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 178 个，默认 Resource 文件仍为 4 个。OutScene 现在有 4 个 `out_scene_modules` 脚本：

```text
RoomResolutionController.gd
ChapterRevealAnimationRunner.gd
OutScenePayloadBridge.gd
OutSceneCameraLimitController.gd
```

下一批不要重复拆镜头限制。如果继续 OutScene，先重新审查剩余函数；房间完成状态必须先设计独立 `MapState` 字段和 Saver 持久化，不要复用 `path_gone`。更稳妥的备选方向是 `enemy_intent_presentation_controller.gd` 的引用查找 bridge，或 `timecoin_ui.gd` 的 `active_tweens` 清理 controller。

### 回归检查

已运行：

```text
git diff --check 通过；仅有 workflow_logs/current-modularization-process.md 的既有换行归一化提示，无空白错误。
Godot headless 项目检查通过：EXIT=0。
加载 res://scene/out_scene/Out_Scene.tscn 通过：EXIT=0。
模块覆盖检查通过：scripts=178 resources=4 total=182 missing=0。
```

## 2026-06-13 EnemyIntentPresentationController 主 tooltip 定位拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/enermy/enemy_intent_presentation_controller.gd`。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。当前工作区仍有用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不 stage、不提交这些文件。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`。已用 `rg` 输出目标文件的 `class_name`、`extends`、`signal`、`@export`、`@onready`、`const`、`var`、`func` 轮廓，并搜索主 tooltip 定位、`set_cursor_tooltip_position()`、副 tooltip 重定位和已有 tooltip presenter/helper 边界。

### 当前职责

`enemy_intent_presentation_controller.gd` 仍是敌人意图表现协调器，负责地图 hover 与时间轴 hover 入口、阶段判断、地图/时间轴高亮、主 tooltip 写入、状态关键词读取、tooltip host 选择、source stack 查找、调用 `MainBoard.set_cursor_tooltip_position()`、副 tooltip 重定位，以及 hover 退出、阶段切换和意图重判时的清理。

### 耦合点

```text
_position_intent_tooltip() 同时做 source stack 查找、fallback 位置、主 tooltip panel size 选择、屏幕边界 clamp、MainBoard.set_cursor_tooltip_position() 调用和副 tooltip 重定位。
主 tooltip 文本已经由 EnemyIntentTooltipTextBuilder.gd 接管，本批不能重复拆。
状态关键词副 tooltip 已经由 EnemyIntentStatusKeywordTooltipPresenter.gd 接管，本批不能重复拆。
引用查找 _resolve_references() 牵动 MainBoard、HexMap、TimelineManager 和 hover 信号连接顺序，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 主 tooltip 定位 helper | `_position_intent_tooltip()` 中的位置计算和 clamp | 纯坐标计算，输入输出清晰，不写节点状态 | 执行 |
| 2 | 引用查找 bridge | `_resolve_references()` | 牵动多节点引用和信号连接顺序 | 暂缓 |

### 本批风险面

本批只处理一个风险面：主 tooltip 屏幕位置计算。

涉及的小风险点：

```text
新增 EnemyIntentTooltipPositionHelper.gd，集中根据锚点、fallback、offset、panel size、screen size 和 margin 计算主 tooltip 位置。
enemy_intent_presentation_controller.gd 保留旧 _position_intent_tooltip() 入口。
旧入口继续负责 source stack 查找、主 tooltip panel size 选择、MainBoard.set_cursor_tooltip_position() 调用和 _position_status_keyword_tooltips()。
```

不触碰：

```text
EnemyIntentResolver 与 EnemyIntentData 字段契约。
TimelineManager 数据结构。
地图高亮与时间轴高亮联动规则。
主 tooltip 文本 builder。
状态关键词副 tooltip presenter。
引用查找和信号连接顺序。
```

### 实现结果

新增：

```text
scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentTooltipPositionHelper.gd
```

职责：

```text
EnemyIntentTooltipPositionHelper 只负责计算敌人意图主 tooltip 的屏幕位置。
它不读取节点树，不写入 MainBoard，不创建或销毁 tooltip，也不处理状态关键词副 tooltip。
```

`enemy_intent_presentation_controller.gd` 新增 helper preload、缓存 getter 和旧入口转发。`_position_intent_tooltip()` 仍保留旧入口，继续收集 source stack、panel size 和 viewport 信息，再把纯位置计算交给 helper。

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 177 个，默认 Resource 文件仍为 4 个。`EnemyIntentPresentationController` 已拆出 3 个 tooltip 表现模块：

```text
EnemyIntentTooltipTextBuilder.gd
EnemyIntentStatusKeywordTooltipPresenter.gd
EnemyIntentTooltipPositionHelper.gd
```

Tooltip 低风险表现面已经基本收口。下一批优先转向 `out_scene_map_exp.gd` 的镜头限制小模块；如果继续 `enemy_intent_presentation_controller.gd`，只谨慎评估引用查找 bridge，不要重复拆 tooltip 三个模块。

### 回归检查

已运行：

```text
git diff --check 通过；仅有 Git 换行归一化提示，无空白错误。
Godot headless 项目检查通过：EXIT=0。
加载 res://scene/in_scene/in_scene.tscn 通过：EXIT=0。
模块覆盖检查通过：scripts=177 resources=4 total=181 missing=0。
```

## 2026-06-13 EnemyIntentPresentationController 状态关键词副 tooltip 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/enermy/enemy_intent_presentation_controller.gd`。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。当前工作区仍有用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不 stage、不提交这些文件。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`。`docs-write` 引用的共享 style guide 在本机缺失，因此本批文档继续按中文自然语言和当前项目文档风格维护。已用 `rg` 输出目标文件的 `class_name`、`extends`、`signal`、`@export`、`@onready`、`const`、`var`、`func` 轮廓，并搜索 tooltip、状态关键词、副 tooltip 和已有 `EnemyIntentTooltipTextBuilder.gd` 的调用边界。

### 当前职责

`enemy_intent_presentation_controller.gd` 仍是敌人意图表现协调器，负责解析地图 hover 与时间轴 hover 入口、判断当前交互阶段、驱动地图高亮、驱动时间轴高亮、写入主 tooltip、读取状态关键词、选择 tooltip host、延迟定位，以及在 hover 退出、阶段切换和意图重判时清理表现。

### 耦合点

```text
_show_intent_tooltip() 仍负责把主 tooltip 文本写到 MainBoard.cursor_tooltip，并触发副 tooltip 重建和延迟定位。
_get_source_status_keywords() 读取敌人状态关键词协议，本批不改变。
_get_status_tooltip_host() 和 _get_cursor_tooltip_panel() 保留主脚本旧语义，本批不改变 host 选择。
_rebuild_status_keyword_tooltips()、_create_status_keyword_panel()、_position_status_keyword_tooltips()、_hide_status_keyword_tooltips() 混合了副 tooltip HBox、Panel 样式、定位和销毁，本批只拆这一块。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 状态关键词副 tooltip presenter | `_rebuild_status_keyword_tooltips()`、`_create_status_keyword_panel()`、`_position_status_keyword_tooltips()`、`_hide_status_keyword_tooltips()` | 纯 UI 表现，输入输出清晰，不改 hover 判定和规则数据 | 执行 |
| 2 | 主 tooltip 定位 helper | `_position_intent_tooltip()` | 只处理坐标、panel size 和屏幕 clamp，但会影响主 tooltip 位置 | 暂缓 |
| 3 | 引用查找 bridge | `_resolve_references()` | 牵动 MainBoard、HexMap、TimelineManager 和 hover 信号连接顺序 | 不碰 |

### 本批风险面

本批只处理一个风险面：状态关键词副 tooltip 的创建、定位和销毁。

涉及的小风险点：

```text
新增 EnemyIntentStatusKeywordTooltipPresenter.gd，集中管理副 tooltip HBox、关键词 Panel 样式、屏幕边界定位和销毁。
enemy_intent_presentation_controller.gd 保留旧 _rebuild_status_keyword_tooltips()、_create_status_keyword_panel()、_position_status_keyword_tooltips() 和 _hide_status_keyword_tooltips() 入口并转发。
关键词列表读取、host 选择和主 tooltip panel 选择仍在 controller 中，避免同批改变来源协议。
```

不触碰：

```text
EnemyIntentResolver 与 EnemyIntentData 字段契约。
TimelineManager 数据结构。
地图高亮与时间轴高亮联动规则。
主 tooltip 文本 builder。
主 tooltip 坐标和屏幕 clamp helper。
```

### 实现结果

新增：

```text
scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentStatusKeywordTooltipPresenter.gd
```

职责：

```text
EnemyIntentStatusKeywordTooltipPresenter 只负责敌人意图状态关键词副 tooltip 的创建、定位和销毁。
它不读取敌人状态，不写入主 tooltip 文本，不判断 hover 阶段，也不驱动地图或时间轴表现。
```

`enemy_intent_presentation_controller.gd` 新增 presenter preload、缓存 getter 和旧入口转发。旧 `_rebuild_status_keyword_tooltips()` 仍先读取关键词并选择 host，再把关键词、host、主 tooltip panel、屏幕大小、间距、宽度和边距传给 presenter。旧 `_create_status_keyword_panel()`、`_position_status_keyword_tooltips()` 与 `_hide_status_keyword_tooltips()` 仍保留，外部行为和调用顺序不变。

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 176 个，默认 Resource 文件仍为 4 个。`EnemyIntentPresentationController` 已拆出 2 个 presenter：

```text
EnemyIntentTooltipTextBuilder.gd
EnemyIntentStatusKeywordTooltipPresenter.gd
```

下一批如果继续 `enemy_intent_presentation_controller.gd`，建议只评估主 tooltip 定位 helper，不要重复拆文本 builder 或状态关键词副 tooltip presenter。备选仍是 `out_scene_map_exp.gd` 的镜头限制小模块。

### 回归检查

已运行：

```text
git diff --check 通过；仅有 Git 换行归一化提示，无空白错误。
Godot headless 项目检查通过：EXIT=0。
加载 res://scene/in_scene/in_scene.tscn 通过：EXIT=0。
模块覆盖检查通过：scripts=176 resources=4 total=180 missing=0。
```

## 2026-06-13 TimelineManager 敌方意图候选收集拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timeline/TimelineManager.gd`。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。当前工作区只剩用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不 stage、不提交这些文件。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/timeline_manager.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`。已用 `rg` 输出 `TimelineManager.gd` 的 `class_name`、`extends`、`signal`、`@export`、`const`、`var`、`func` 轮廓，并搜索敌方意图协议函数、优先级读取、shape 和 action 创建调用。

### 当前职责

`TimelineManager.gd` 仍负责时间轴 `grid` 占用、放置/结算、敌方意图生成主编排、重判、中途移除和 hover 信号。`generate_enemy_intents()` 继续作为组合候选、优先级、目标地块、`TimelineAction` 创建和最终放置的主编排入口。

### 耦合点

```text
_collect_enemy_intent_candidates() 负责敌人实例有效性、协议方法检查、意图展示开关、can_generate_intent(hex_map)、get_intent_shape() 缓存和优先级写入 candidate。
优先级读取已经由 _get_enemy_intent_priority() 转发到 TimelineEnemyIntentPrioritySelector.gd，本批不能重复拆。
目标地块映射已经由 _resolve_intent_target_tile() 转发到 TimelineEnemyIntentTargetResolver.gd，本批不能重复拆。
_apply_intent_priority_to_action() 会写 TimelineAction.action_data，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 敌方意图候选收集 rules | `_collect_enemy_intent_candidates()` | 输入输出清晰，返回候选字典，不改 grid/action/UI | 执行 |
| 2 | action priority 写入 | `_apply_intent_priority_to_action()` | 会碰 `TimelineAction.action_data` 契约 | 暂缓 |
| 3 | 重判与中途移除 | `revalidate_enemy_intents()`、`remove_action_with_fade()` | 牵动 UI 移除动画和 hover 清理 | 不碰 |
| 4 | 生成主编排 | `generate_enemy_intents()` | 仍串联候选、目标、action 创建和最终放置 | 不硬拆 |

### 本批风险面

本批只处理一个风险面：敌方意图候选收集。

涉及的小风险点：

```text
新增 TimelineEnemyIntentCandidateCollector.gd，集中处理敌人协议检查、意图开关、can_generate_intent() 和 shape 缓存。
TimelineManager.gd 保留旧 _collect_enemy_intent_candidates() 入口并转发。
collector 通过 Callable 调旧 _get_enemy_intent_priority()，避免重复拆优先级 selector。
```

不触碰：

```text
grid 数据结构、is_placement_valid()、place_action() 和 find_random_available_spot()。
TimelineEnemyIntentPrioritySelector.gd 和 TimelineEnemyIntentTargetResolver.gd 已拆职责。
TimelineAction.action_data 与 intent_priority 写入。
TimelineUI 表现、敌方意图 tooltip 和 resolve_timeline()。
HexMap.stack_nodes 数据契约。
```

### 实现结果

新增：

```text
scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentCandidateCollector.gd
```

职责：

```text
TimelineEnemyIntentCandidateCollector 负责从敌人列表收集本回合可进入时间轴的意图候选。
它不排序候选，不寻找时间轴放置位置，不创建 TimelineAction，不修改 grid，也不决定目标地块。
```

`TimelineManager.gd` 新增 collector preload、缓存 getter 和旧入口转发。`_collect_enemy_intent_candidates()` 仍保留旧入口，并继续返回包含 `enemy`、`shape` 与 `priority` 的候选字典。优先级读取仍走旧 `_get_enemy_intent_priority()` 入口。

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/timeline_manager.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 175 个，默认 Resource 文件仍为 4 个。`TimelineManager.gd` 已拆出 3 个规则模块：

```text
TimelineEnemyIntentCandidateCollector.gd
TimelineEnemyIntentPrioritySelector.gd
TimelineEnemyIntentTargetResolver.gd
```

敌方意图生成链路里的低风险规则面已经基本收口。下一批建议暂停 `TimelineManager.gd` 的硬拆，转向 `enemy_intent_presentation_controller.gd` 的状态关键词副 tooltip presenter 或主 tooltip 定位 helper，或转向 `out_scene_map_exp.gd` 的镜头限制小模块。

### 回归检查

已运行：

```text
git diff --check 通过；仅有 Git 换行归一化提示，无空白错误。
Godot headless 项目检查通过：EXIT=0。
加载 res://scene/in_scene/in_scene.tscn 通过：EXIT=0。
模块覆盖检查通过：scripts=175 resources=4 total=179 missing=0。
```

## 2026-06-13 TimelineManager 敌方意图目标地块映射拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timeline/TimelineManager.gd`。开工前确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/timeline_manager.md
```

已按要求先读取 `godot-prompter:gdscript-patterns`；写 Markdown 前已读取 `docs-write`，它引用的共享 style guide 在本机缺失，因此本批文档继续按中文自然语言和当前项目文档风格维护。

已用 `rg` 输出 `TimelineManager.gd` 的 `class_name`、`extends`、`signal`、`@export`、`const`、`var`、`func` 轮廓，并搜索敌方意图生成、候选收集、目标坐标、`can_generate_intent()`、`get_intent_shape()` 和 `HexMap.stack_nodes` 的相关调用。

### 当前职责

`TimelineManager.gd` 仍是时间轴规则核心，负责 `grid` 占用、放置校验、行动放置、回合结算、敌方意图生成编排、敌方意图重判、中途移除行动，以及 hover 信号转发。`generate_enemy_intents()` 继续作为组合候选、优先级、目标地块、`TimelineAction` 创建和最终放置的主编排入口。

### 耦合点

```text
_collect_enemy_intent_candidates() 牵动敌人协议、is_intent_preview_enabled()、can_generate_intent()、shape 缓存和优先级读取。
_resolve_intent_target_tile() 只牵动 enemy.get_intent_target_center_coord(hex_map) 与 hex_map.stack_nodes 读取。
_apply_intent_priority_to_action() 会写 TimelineAction.action_data，本批不碰。
generate_enemy_intents() 仍串起候选、优先级、目标、action 创建和 place_action()，本批不整体搬移。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 敌方意图目标地块映射 rules | `_resolve_intent_target_tile()` | 输入输出很小，只读 enemy 与 `HexMap.stack_nodes`，不改 grid/action/UI | 执行 |
| 2 | 敌方意图候选收集 rules | `_collect_enemy_intent_candidates()` | 牵动协议检查、有效性检查、shape 缓存和优先级读取 | 暂缓 |
| 3 | action priority 写入 | `_apply_intent_priority_to_action()` | 会碰 `TimelineAction.action_data` 契约 | 暂缓 |
| 4 | 重判与中途移除 | `revalidate_enemy_intents()`、`remove_action_with_fade()` | 牵动 UI 移除动画和 hover 清理 | 不碰 |

### 本批风险面

本批只处理一个风险面：敌方意图目标中心坐标到地图地块节点的映射。

涉及的小风险点：

```text
新增 TimelineEnemyIntentTargetResolver.gd，集中读取 enemy.get_intent_target_center_coord(hex_map) 和 hex_map.stack_nodes。
TimelineManager.gd 保留旧 _resolve_intent_target_tile() 入口并转发。
同步维护说明、模块总表和接力当前态，避免下一批重复拆目标映射。
```

不触碰：

```text
grid 数据结构、is_placement_valid()、place_action() 和 find_random_available_spot()。
_collect_enemy_intent_candidates() 候选收集。
TimelineAction.action_data 与 intent_priority 写入。
TimelineUI 表现、敌方意图 tooltip 和 resolve_timeline()。
HexMap.stack_nodes 写入、删除或数据契约本身。
```

### 实现结果

新增：

```text
scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentTargetResolver.gd
```

职责：

```text
TimelineEnemyIntentTargetResolver 负责把敌方意图声明的目标中心坐标映射为 HexMap.stack_nodes 里的地块节点。
它不选择目标，不创建 TimelineAction，不修改 HexMap.stack_nodes，也不处理地图或时间轴表现。
```

`TimelineManager.gd` 新增 resolver preload、缓存 getter 和旧入口转发。`_resolve_intent_target_tile()` 仍作为旧私有入口保留，`generate_enemy_intents()` 的调用顺序和 action 创建、优先级写入、最终放置逻辑保持不变。

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/timeline_manager.md
workflow_logs/next-ai-handoff-current-status.md
```

### 当前优化进度与下一步

当前已拆脚本模块更新为 174 个，默认 Resource 文件仍为 4 个。`TimelineManager.gd` 已拆出 2 个规则模块：

```text
TimelineEnemyIntentPrioritySelector.gd
TimelineEnemyIntentTargetResolver.gd
```

下一批如果继续 `TimelineManager.gd`，只建议单独评估敌方意图候选收集。不要重复拆优先级 selector 或目标地块 resolver，不要同批改 `grid`、`place_action()`、`TimelineAction.action_data` 或 UI 表现。

### 回归检查

`git diff --check` 通过；仅提示 `scene/in_scene/timeline/TimelineManager.gd` 与 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

Godot headless 项目检查通过，退出码为 0；错误筛选未出现 `SCRIPT ERROR`、`Parse Error`、`Compile Error`、`Failed to load script`、`Compilation failed`、`Invalid call` 或 `Invalid access`。

加载 `res://scene/in_scene/in_scene.tscn` 通过，退出码为 0；错误筛选未出现 `SCRIPT ERROR`、`Parse Error`、`Compile Error`、`Failed to load script`、`Compilation failed`、`Invalid call` 或 `Invalid access`。

模块覆盖检查通过：

```text
scripts=174 resources=4 total=178 missing=0
```

## 2026-06-13 下一位 AI 接力文档整理

### 读取与轮廓

本批响应用户要求：总结前述 prompt、项目模块化进度和后续维护要求，写成详细接力 Markdown，并输出一份可直接交给下一位 AI 的接力 prompt。本批只处理文档，不改业务 GDScript。

开工前已确认仓库中没有实体 `AGENTS.md`，继续使用当前对话中用户贴出的 AGENTS 约束。已读取：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
workflow_logs/next-ai-handoff-current-status.md
workflow_logs/maintenance_guides/README.md
workflow_logs/maintenance_guides/timeline_manager.md
workflow_logs/maintenance_guides/timecoin_ui.md
workflow_logs/maintenance_guides/tile.md
```

已使用 `rg` 输出当前重点大文件的 `class_name`、`extends`、`signal`、`@export`、`@onready`、`const`、`var`、`func` 轮廓，覆盖：

```text
scene/in_scene/timeline/TimelineManager.gd
scene/in_scene/timeline/timeline_ui.gd
scene/in_scene/timecoin_ui.gd
scene/in_scene/tile.gd
scene/in_scene/enermy/enemy_intent_presentation_controller.gd
scene/out_scene/out_scene_map_exp.gd
scene/card/custom_card.gd
```

### 当前职责

`workflow_logs/next-ai-handoff-current-status.md` 是下一位 AI 的当前态导航文档。它不替代根基操作文档，而是把本轮对话中的连续指令、最新提交、用户已有改动、已完成模块、停止点和下一批建议汇总到一个可复制的接力入口。

### 耦合点

```text
根基规则已经在 docs/ 和 maintenance_guides/ 中维护，接力文档不能变成第二份总手册。
当前工作区还有用户已有改动 default_bus_layout.tres、shaders/color_BG.gdshader、shaders/game_over.gdshader，接力文档必须提醒下一位 AI 不要 stage 或回滚。
模块统计、最新提交和“不要重复拆”的列表需要和 docs/modularized-files-ultimate-operation-guide.md、docs/ai-handoff-ultimate-operation-guide.md 对齐。
下一步建议需要延续当前真实进度，不能把已经判定停止的 TimelineUI、Drag、Rewards、Tile 方向重新推为首选。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 接力文档升级 | `workflow_logs/next-ai-handoff-current-status.md` | 用户明确要求，且该文件已存在为未跟踪接力草稿 | 执行 |
| 2 | 流程日志补记 | `workflow_logs/current-modularization-process.md` | 文档批次也要记录审查、风险面和验证 | 执行 |
| 3 | 根基总结文档 | `docs/*.md` | 本批没有新增模块或状态变化，不应重复改总结文档 | 暂缓 |
| 4 | 业务模块拆分 | GDScript 文件 | 用户本轮要求交接文档，不应顺手拆代码 | 不碰 |

### 本批风险面

本批只处理一个风险面：下一位 AI 接力说明的完整性和可执行性。

涉及的小风险点：

```text
把前述所有用户 prompt 的工作规则和关注方向收束成可执行清单。
同步最新项目进度、模块统计、工作区脏文件、最新提交和验证命令。
给出下一位 AI 可直接复制使用的接力 prompt，并明确首选下一步与禁止重复拆分的边界。
```

不触碰：

```text
任何 GDScript 业务代码。
docs/ 下已有总结性文档。
用户已有 dirty 文件。
模块统计本身和 Resource 内容。
```

### 实现结果

重写并扩展 `workflow_logs/next-ai-handoff-current-status.md`。该文档现在包含：

```text
接手后必须读取的根基文档和维护入口。
前述用户 prompt 的持续工作规则与关注顺序。
当前 Git 提交、用户已有 dirty 文件和模块统计。
HexMap、InScene、Drag、TimelineUI、TimelineManager、TimecoinUI、EnemyIntent、Tile、CustomCard、Rewards、OutScene 的当前完成情况。
当前最推荐下一步：继续 TimelineManager，但只拆敌方意图候选收集或目标地块映射其中一个风险面。
明确不建议继续硬拆的方向。
验证命令、模块覆盖检查脚本和已知 Godot 旧噪声。
可直接复制给下一位 AI 的接力 prompt。
```

本批没有修改 `docs/`，因为没有新增模块或总结状态变化；`docs/` 仍只保留最新版总结性说明。

### 当前优化进度与下一步

接力文档已成为当前流程的入口之一。下一位 AI 应先读根基文档，再读 `workflow_logs/next-ai-handoff-current-status.md`，然后按目标文件维护入口继续。

下一批最推荐继续 `scene/in_scene/timeline/TimelineManager.gd`，但只在敌方意图候选收集或目标地块映射中选择一个风险面。不要重复拆 `TimelineEnemyIntentPrioritySelector.gd`，不要同批改 `grid`、`place_action()`、`TimelineAction.action_data` 或 UI 表现。

### 回归检查

`git diff --check` 通过；仅提示 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：

```text
scripts=173 resources=4 total=177 missing=0
```

本批只改 Markdown，没有运行 Godot headless 场景加载。当前未发现 `godot_*_check.log` 临时日志。

## 2026-06-13 TimelineManager 敌方意图优先级选择规则拆分

### 读取与轮廓

本批处理 `scene/in_scene/timeline/TimelineManager.gd`。开工前 `git status --short` 只显示用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。仓库中没有实体 `AGENTS.md`，继续遵守当前对话中用户贴出的 AGENTS 约束。已读取 `docs/ai-handoff-ultimate-operation-guide.md`、`docs/hex-map-ultimate-operation-guide.md`、`docs/modularized-files-ultimate-operation-guide.md`、`workflow_logs/current-modularization-process.md` 与 `workflow_logs/maintenance_guides/timeline_ui.md`。当前没有单独的 `workflow_logs/maintenance_guides/timeline_manager.md`，本批需要补齐。已用 `rg` 输出 `TimelineManager.gd`、`TimelineIntroAnimator.gd`、`timeline_ui.gd` 和 `timeline/ui_modules` 下的 `class_name`、`signal`、`@export`、`const`、`var`、`func` 轮廓。

### 当前职责

`TimelineManager.gd` 是时间轴规则核心。它负责保存 `grid` 占用、校验/放置行动、回合结算、发射行动放置/结算/清理/hover 信号、生成敌方意图、重判敌方意图有效性，并提供中途移除行动的规则入口。当前 UI 表现已拆到 `timeline_ui.gd` 及 `timeline/ui_modules`，但 `TimelineManager.gd` 内部敌方意图候选收集、优先级排序、同级候选选择、目标映射和 action priority 写入仍集中在一个脚本里。

### 耦合点

```text
generate_enemy_intents() 同时读取当前 HexMap、收集候选、按优先级循环、选择可放置候选、解析目标地块、创建 TimelineAction、写入 priority、调用 place_action() 修改 grid。
_collect_enemy_intent_candidates() 同时做敌人协议检查、意图开关检查、can_generate_intent() 检查、shape 缓存和优先级读取。
_get_enemy_intent_priority()、_get_sorted_priority_values()、_filter_candidates_by_priority() 和 _pick_placeable_candidate() 是纯规则/选择逻辑，适合先拆；但 _pick_placeable_candidate() 仍需要通过 Callable 调用现有寻位逻辑，避免同批复制 grid 校验。
_resolve_intent_target_tile() 读取 HexMap.stack_nodes，和地图数据契约相关，本批不碰。
_apply_intent_priority_to_action() 会写 TimelineAction.action_data，本批不改变 action 数据契约。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 敌方意图优先级/同级选择规则 | `_get_enemy_intent_priority()`、`_get_sorted_priority_values()`、`_filter_candidates_by_priority()`、`_pick_placeable_candidate()` | 纯规则为主，输入输出清晰，只需要传入寻位 Callable | 执行 |
| 2 | 维护说明补充 | 新增 `timeline_manager.md`、更新总结文档 | `TimelineManager.gd` 首个规则拆分需要单独维护入口 | 执行 |
| 3 | 候选收集 | `_collect_enemy_intent_candidates()` | 牵动敌人协议、HexMap 和 shape 缓存 | 暂缓 |
| 4 | 目标地块映射 | `_resolve_intent_target_tile()` | 读取 `hex_map.stack_nodes`，要对齐 HexMap 数据契约 | 暂缓 |
| 5 | action priority 写入 | `_apply_intent_priority_to_action()` | 改 `TimelineAction.action_data`，涉及展示/调试读取 | 暂缓 |

### 本批风险面

本批只处理一个风险面：敌方意图候选的优先级排序与同级可放置选择。

涉及的小风险点：

```text
新增 TimelineEnemyIntentPrioritySelector.gd，读取敌人优先级、返回降序优先级列表、过滤同级候选，并在同级中挑出当前可放置的 candidate/spot。
TimelineManager.gd 保留旧私有入口，内部转发给 selector，减少调用点变化。
_pick_placeable_candidate() 通过 Callable 继续使用现有 find_random_available_spot()，不复制占位校验和 grid 规则。
```

不触碰：

```text
grid 数据结构、is_placement_valid()、place_action() 和 find_random_available_spot()。
generate_enemy_intents() 的 action 创建、place_action() 写入、max_enemy_intents_per_turn 限制和 action_placed 信号。
HexMap.stack_nodes 目标映射。
TimelineAction.action_data 与 intent_priority 写入语义。
TimelineUI、敌方意图 overlay、tooltip 和移除动画。
```

### 实现结果

新增 `scene/in_scene/timeline/manager_modules/rules/TimelineEnemyIntentPrioritySelector.gd`。该模块是 RefCounted rules，中文职责注释明确它只负责敌方意图候选的优先级读取、优先级分层排序和同级可放置候选选择；它不读取或修改 `TimelineManager.grid`，不创建 `TimelineAction`，不调用 `place_action()`，也不决定敌人意图目标地块。

`scene/in_scene/timeline/TimelineManager.gd` 新增 selector preload、缓存和 `_get_enemy_intent_priority_selector()` getter。旧 `_get_enemy_intent_priority()`、`_get_sorted_priority_values()`、`_filter_candidates_by_priority()` 和 `_pick_placeable_candidate()` 入口全部保留并转发给 selector；`_pick_placeable_candidate()` 通过 `Callable(self, "find_random_available_spot")` 继续使用原寻位逻辑，避免复制 grid 校验。

同步更新 `docs/modularized-files-ultimate-operation-guide.md`、`docs/ai-handoff-ultimate-operation-guide.md`，并新增 `workflow_logs/maintenance_guides/timeline_manager.md`。总结文档已记录 `manager_modules/rules/TimelineEnemyIntentPrioritySelector.gd`，维护说明也补上不要重复拆 selector、不要同批改 `grid` / `place_action()` / `TimelineAction.action_data` / UI 表现的停止点。

### 当前优化进度与下一步

当前已拆模块统计更新为 173 个脚本模块和 4 个默认 Resource 文件。`TimelineManager.gd` 从约 517 行降到约 493 行；敌方意图优先级读取、优先级列表、同级过滤和同级可放置选择已经收口到 selector。

下一批如果继续 `TimelineManager.gd`，只建议单独评估敌方意图候选收集或目标地块映射。候选收集牵动敌人协议、`can_generate_intent()` 和 shape 缓存；目标地块映射牵动 `HexMap.stack_nodes` 契约，两者不要同批处理。

### 回归检查

`git diff --check` 通过；仅提示 `scene/in_scene/timeline/TimelineManager.gd` 与 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：`scripts=173 resources=4 total=177 missing=0`。

Godot headless 项目检查通过，无新增脚本解析错误；仍有既有退出时 `ObjectDB instances leaked` 与资源占用提示。

加载 `res://scene/in_scene/in_scene.tscn` 通过，无新增脚本解析错误；仍有既有 TileSet atlas 坐标错误、RID/resource 退出泄漏提示。

## 2026-06-13 TimelineUI 入场动画编排 controller 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timeline/timeline_ui.gd` 的时间轴部分。开工前 `git status --short` 只显示用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。仓库中没有实体 `AGENTS.md`，继续遵守当前对话中用户贴出的 AGENTS 约束。已重新读取 `docs/ai-handoff-ultimate-operation-guide.md`、`docs/hex-map-ultimate-operation-guide.md`、`docs/modularized-files-ultimate-operation-guide.md`、`workflow_logs/current-modularization-process.md` 与 `workflow_logs/maintenance_guides/timeline_ui.md`，并用 `rg` 输出 `timeline_ui.gd` 和 `timeline/ui_modules` 下的 `const`、`@export`、`@onready`、`var`、`signal`、`func` 轮廓。

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager`、维护 `action_containers`、创建行动容器、连接 hover 信号、触发敌方意图 overlay、展开/收起、网格预览、清理 UI 和向 `TimelineIntroAnimator` 转发入场动画。已拆出的模块覆盖布局、网格、TimelineManager 查找、视觉配置读取、敌方意图 overlay、行动方格放置动画、行动容器几何、行动方块视觉节点、整体形状视觉层、清理动画残影、残影 tween 和 hover 状态通知。

### 耦合点

```text
_on_action_placed() 仍同时创建容器、计算几何、创建方格、连接 hover、配置敌方意图 overlay，并决定普通逐格动画或整体 intro 动画，不适合整体硬拆。
TimelineIntroAnimator 自身负责实际 tween、格子预处理、行动容器入场和输入恢复；timeline_ui.gd 目前还保存 intro 是否播放/进行中的状态，并直接调用 setup/play/stop/should。
_on_timeline_cleared() 同时清理敌方意图预览、停止 intro tween、清空 action_containers 和释放 shape_layer 子节点。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 入场动画编排 controller | `_setup_timeline_intro_animator()`、`play_intro()`、`is_intro_in_progress()`、`_should_play_action_intro()` 和 action intro 播放/停止转发 | 状态集中，输入输出简单，不改变实际 tween | 执行 |
| 2 | 维护说明补充 | `timeline_ui.md` 与总结文档 | 新增模块必须记录边界 | 执行 |
| 3 | `_on_action_placed()` 行动块生成编排 | 容器、方块、overlay、hover、intro | 耦合强，文档明确不要硬拆 | 暂缓 |
| 4 | 敌方意图 preview 状态 controller | preview action、tween、overlay 清理 | 牵动 hover 和移除动画 | 暂缓 |
| 5 | `clear_ui()` / `_on_timeline_cleared()` 清理编排 | preview、intro、字典、节点释放 | 与多个状态面相连 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimelineUI 入场动画编排状态与 `TimelineIntroAnimator` 调用转发。

涉及的小风险点：

```text
新增 TimelineIntroPlaybackController.gd，只负责记录 intro 是否播放/进行中，并转发 setup、grid intro、action intro、stop 和 should 判断。
timeline_ui.gd 保留旧入口和原有调用时机，只把状态字段与直接调用替换为 controller。
_on_action_placed() 仍创建行动容器、方格、overlay 和 hover 信号，只把 use_action_intro 判断和最终 play_action_intro 转发给新模块。
```

不触碰：

```text
TimelineIntroAnimator.gd 的实际动画参数、tween 曲线和输入恢复逻辑。
TimelineManager 数据结构、敌方意图规则和 action_placed/timeline_cleared 信号。
TimelineVisualConfig 资源字段与读取逻辑。
拖拽放置规则、网格预览、敌方意图 overlay 预览时机和移除动画时序。
```

### 实现结果

新增 `scene/in_scene/timeline/ui_modules/controllers/TimelineIntroPlaybackController.gd`。该模块是 RefCounted controller，中文职责注释明确它只负责在 `TimelineUI` 与 `TimelineIntroAnimator` 之间维护入场动画播放状态，并转发初始化、背景格子入场、行动入场和停止请求；它不创建时间轴行动容器，不播放具体 Tween，不修改 `TimelineManager` 数据，也不决定敌人意图规则。

`scene/in_scene/timeline/timeline_ui.gd` 新增 `TimelineIntroPlaybackControllerScript` preload、`_intro_playback_controller` 缓存和 `_get_intro_playback_controller()` getter。旧 `_setup_timeline_intro_animator()`、`play_intro()`、`is_intro_in_progress()`、`_should_play_action_intro()`、`_on_timeline_cleared()` 中停止 action intro 的入口都保留并转发给 controller。`_on_action_placed()` 仍负责行动容器、方格、overlay、hover 信号和整体形状视觉生成，只把 action intro 判断与播放转发给新模块。

同步更新 `docs/modularized-files-ultimate-operation-guide.md`、`docs/ai-handoff-ultimate-operation-guide.md` 与 `workflow_logs/maintenance_guides/timeline_ui.md`。总结文档已记录 `controllers/TimelineIntroPlaybackController.gd`，并补上“不要重复拆 intro 状态转发、实际 tween 仍归 TimelineIntroAnimator”的停止点。

### 当前优化进度与下一步

当前已拆模块统计更新为 172 个脚本模块和 4 个默认 Resource 文件。TimelineUI 已拆出 16 个 ui_modules 脚本，`timeline_ui.gd` 当前约 800 行。剩余核心仍是 `_on_action_placed()` 的行动容器/方格/overlay/hover 信号生成编排；它仍不建议硬拆。

下一批如果继续时间轴，只建议做明确的小表现配置补充或扩展已有 presenter。不要重复拆 `TimelineVisualConfigReader.gd` 或 `TimelineIntroPlaybackController.gd`，不要同批修改 `TimelineManager` 数据、敌方意图规则和行动块生成编排。

### 回归检查

`git diff --check` 通过；仅提示 `scene/in_scene/timeline/timeline_ui.gd` 与 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：`scripts=172 resources=4 total=176 missing=0`。

Godot headless 项目检查通过，无新增脚本解析错误；仍有既有退出时 `ObjectDB instances leaked` 与资源占用提示。

加载 `res://scene/in_scene/in_scene.tscn` 通过，无新增脚本解析错误；仍有既有 TileSet atlas 坐标错误、RID/resource 退出泄漏提示。

## 2026-06-13 TimelineUI 视觉配置读取 reader 拆分

### 读取与轮廓

本批处理 `scene/in_scene/timeline/timeline_ui.gd`。开工前确认工作区只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。仓库中没有实体 `AGENTS.md`，因此继续遵守当前对话中用户贴出的 AGENTS 约束。随后重新读取接力说明、HexMap 手册、模块总表、`workflow_logs/current-modularization-process.md` 与 `workflow_logs/maintenance_guides/timeline_ui.md`，并用 `rg` 输出 `timeline_ui.gd` 和 `timeline/ui_modules`、`timeline/resources` 下模块的 `class_name`、`extends`、`const`、`@export`、`@onready`、`var`、`signal`、`func`、`visual_config`、`_on_action_placed()`、hover、preview、overlay、remove、tween 等轮廓。

### 当前职责

`timeline_ui.gd` 是时间轴 UI 的 composition root，继续负责连接 `TimelineManager`，维护 `action_containers`，接收 `action_placed` 与 `timeline_cleared` 信号，编排行动容器创建、hover 状态、敌方意图 overlay、放置动画、移除动画、展开/收起、网格预览和 UI 清理。当前已拆出布局、网格、TimelineManager 查找、敌方意图 overlay、行动方块视觉、行动容器几何、行动整体形状视觉、放置动画、移除残影、移除动画、hover 状态和首批 `TimelineVisualConfig` 资源。

### 耦合点

```text
_on_action_placed() 仍同时串联容器创建、几何计算、整体形状视觉、单格 Panel、敌方意图 Overlay、hover 信号和 intro 动画，不适合本批硬拆。
show_enemy_intent_preview()、clear_enemy_intent_preview() 和 animate_action_removal() 牵动 action_containers、hover 状态、overlay 状态和移除动画，不和配置读取同批改。
_get_visual_config_value()、_get_visual_color()、_get_visual_vector2()、_get_visual_float()、_get_visual_int()、_get_visual_bool() 和 _get_enemy_intent_timeline_shader() 只负责从 visual_config 读取静态视觉参数并回退到导出变量。
TimelineVisualConfig.gd 是静态 Resource；读取 helper 不应创建 Resource、不应写回配置、不应读取 TimelineManager 或节点树。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 视觉配置 reader | `_get_visual_*()` 与 `_get_enemy_intent_timeline_shader()` | 纯 Resource 读取和 fallback，输入输出简单 | 执行 |
| 2 | 维护说明补充 | `timeline_ui.md` 与总结文档 | 新增模块必须记录边界 | 执行 |
| 3 | `_on_action_placed()` 行动块生成编排 | 容器、方块、overlay、hover、intro | 多职责但耦合强，文档明确不要硬拆 | 暂缓 |
| 4 | 敌方意图 preview 状态 controller | preview action、tween、overlay 清理 | 牵动 hover 和移除动画 | 暂缓 |
| 5 | 展开/收起状态 controller | `toggle_expand()`、`collapse()` | 会触碰地图交互锁和动画回调 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimelineUI 视觉配置读取与类型兜底。

涉及的小风险点：

```text
新增 TimelineVisualConfigReader.gd，只负责读取 visual_config 上的静态视觉参数并返回 fallback。
timeline_ui.gd 新增 preload、缓存和 reader getter。
保留 _get_visual_*() 旧入口，内部转发给 reader，避免改动既有调用点。
```

不触碰：

```text
TimelineManager 数据结构与 action_placed/timeline_cleared 信号。
_on_action_placed() 行动块生成编排。
敌人意图规则、Overlay 显隐规则和移除动画时序。
TimelineVisualConfig.gd 字段、默认资源内容和导出变量默认值。
网格预览、hover 状态和展开/收起动画时序。
```

### 实现结果

新增 `scene/in_scene/timeline/ui_modules/config/TimelineVisualConfigReader.gd`。该模块是 RefCounted reader，中文职责注释明确它只负责从时间轴视觉配置 Resource 读取静态视觉参数并提供类型兜底；它不创建或修改 Resource，不读取 `TimelineManager`，不创建行动块，也不播放或控制任何 UI 动画。

`scene/in_scene/timeline/timeline_ui.gd` 新增 `TimelineVisualConfigReaderScript` preload、`_visual_config_reader` 缓存和 `_get_visual_config_reader()` getter。旧 `_get_visual_config_value()`、`_get_visual_color()`、`_get_visual_vector2()`、`_get_visual_float()`、`_get_visual_int()`、`_get_visual_bool()` 与 `_get_enemy_intent_timeline_shader()` 入口全部保留，并转发给 reader；现有调用点、`_on_action_placed()`、敌方意图 overlay、移除动画、网格预览和展开/收起流程不变。

同步更新 `docs/modularized-files-ultimate-operation-guide.md`、`docs/ai-handoff-ultimate-operation-guide.md` 与 `workflow_logs/maintenance_guides/timeline_ui.md`。总结文档已记录 `config/TimelineVisualConfigReader.gd`，维护说明也补上“不要重复拆视觉配置读取、不要让 reader 接管行动块生成或动画时机”的停止点。

### 当前优化进度与下一步

当前已拆模块统计更新为 171 个脚本模块和 4 个默认 Resource 文件。TimelineUI 的布局、网格、预览、TimelineManager 查找、视觉配置读取、敌方意图 overlay、放置动画、行动几何、行动块视觉、整体形状视觉、移除残影、移除动画和 hover 状态通知都已拆出。`timeline_ui.gd` 剩余主要是 composition root 编排，尤其 `_on_action_placed()` 仍不建议硬拆。

下一批如果继续时间轴，只考虑新增纯视觉配置字段或扩展已有 presenter；不要重复拆 `TimelineVisualConfigReader.gd`，不要同批修改 `TimelineManager` 数据、敌人意图规则和行动块生成编排。

### 回归检查

`git diff --check` 通过；仅提示 `timeline_ui.gd` 和 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：171 个已拆脚本模块和 4 个默认 Resource 文件都出现在 `docs/modularized-files-ultimate-operation-guide.md`，覆盖缺失为 0。

Godot headless 项目检查可启动并退出，输出仍有既有退出资源占用警告。加载 `res://scene/in_scene/in_scene.tscn` 通过，主场景 ready、地图构建、CardManager 注册和 TimecoinUI 初始化都正常执行；新 `TimelineVisualConfigReader.gd` preload 与 `timeline_ui.gd` 解析没有报错。该场景检查仍输出既有 TileSet atlas 坐标报错和退出资源泄漏警告，本批没有修改 TileSet、地图资源或退出流程。

## 2026-06-13 EnemyIntentPresentationController 主 tooltip 文本 builder 拆分

### 读取与轮廓

本批处理 `scene/in_scene/enermy/enemy_intent_presentation_controller.gd`。开工前确认工作区只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、HexMap 手册、模块总表和 `workflow_logs/current-modularization-process.md`，并用 `rg` 输出 `enemy_intent_presentation_controller.gd` 的 `class_name`、`const`、`@export`、`var`、`func`、主 tooltip、状态关键词副 tooltip、定位和隐藏入口轮廓。当前没有 `workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md`，本批先补单文件维护说明。

### 当前职责

`enemy_intent_presentation_controller.gd` 是敌人意图表现协调器，继续负责收集 `MainBoard`、`HexMap`、`TimelineManager`、`TimelineUI` 和 `DragShapeController` 引用，根据当前交互阶段响应地图 hover 与时间轴 hover，把同一份 `EnemyIntentData` 同步展示到地图、时间轴和 tooltip，并在 hover 退出或阶段切换时统一清理表现。

### 耦合点

```text
_show_intent_tooltip() 当前同时组装主 tooltip 文本、写入 MainBoard.cursor_tooltip、重建状态关键词副 tooltip，并延迟定位主 tooltip。
_get_source_status_lines() 负责从 source_node 拉取建筑状态文本，结果只给主 tooltip 文本使用。
_rebuild_status_keyword_tooltips()、_create_status_keyword_panel()、_position_status_keyword_tooltips() 和 _hide_status_keyword_tooltips() 负责副 tooltip 控件生命周期，和主 tooltip 文本组装不是同一个风险面。
_position_intent_tooltip() 依赖 HexMap stack 坐标、MainBoard tooltip panel 和屏幕边界，不和文本 builder 同批拆。
地图高亮、时间轴高亮、EnemyIntentResolver 数据契约和 TimelineManager 查找不属于本批范围。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 主 tooltip 文本 builder | `intent_data.description`、状态行、无效原因 BBCode | 纯文本组装，输入输出简单 | 执行 |
| 2 | 单文件维护说明 | `workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md` 和索引 | 用户要求大文件有对应维护说明 | 执行 |
| 3 | 状态关键词副 tooltip presenter | HBox、PanelContainer、KeywordTooltipPanel 实例化和定位 | 涉及节点生命周期与 host 选择 | 暂缓 |
| 4 | tooltip 定位 helper | `_position_intent_tooltip()` | 依赖屏幕边界、MainBoard panel 和地图 stack | 暂缓 |
| 5 | 引用查找 bridge | `_resolve_references()` | 会触碰 MainBoard、HexMap、TimelineManager、信号连接 | 暂缓 |

### 本批风险面

本批只处理一个风险面：敌人意图主 tooltip 的文本行组装。

涉及的小风险点：

```text
新增 EnemyIntentTooltipTextBuilder.gd，只负责把 EnemyIntentData 和状态行转成主 tooltip 文本行。
enemy_intent_presentation_controller.gd 新增 preload 与 builder getter。
_show_intent_tooltip() 保留 cursor_tooltip 写入、show、状态关键词副 tooltip 重建和 deferred 定位，只把文本数组构造转发给 builder。
补充 enemy_intent_presentation_controller.gd 单文件维护说明和维护索引。
```

不触碰：

```text
地图与时间轴意图显示规则。
EnemyIntentResolver 和 EnemyIntentData 字段契约。
状态关键词副 tooltip 的节点创建、布局和定位。
主 tooltip 的定位策略、屏幕 clamp 和 MainBoard set_cursor_tooltip_position 调用。
hover phase 判断、revalidate 流程和 TimelineManager 数据结构。
```

### 实现结果

新增 `scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentTooltipTextBuilder.gd`。该模块是 RefCounted builder，中文职责注释明确它只负责组装敌人意图主 tooltip 文本行；它不写入 `MainBoard.cursor_tooltip`、不创建状态关键词副 tooltip、不定位 tooltip，也不判断地图或时间轴 hover 规则。

`scene/in_scene/enermy/enemy_intent_presentation_controller.gd` 新增 `EnemyIntentTooltipTextBuilderScript` preload、`_tooltip_text_builder` 缓存和 `_get_tooltip_text_builder()` getter。`_show_intent_tooltip()` 继续负责校验 `MainBoard.cursor_tooltip`、写入 BBCode、显示主 tooltip、重建状态关键词副 tooltip 和延迟定位；原本的 `intent_data.description`、来源状态行和无效原因 BBCode 组装转交给 builder。

新增 `workflow_logs/maintenance_guides/enemy_intent_presentation_controller.md`，并更新维护索引。同步更新 `docs/modularized-files-ultimate-operation-guide.md` 与 `docs/ai-handoff-ultimate-operation-guide.md`，记录新模块和停止点。

### 当前优化进度与下一步

当前已拆模块统计更新为 170 个脚本模块和 4 个默认 Resource 文件。EnemyIntentPresentationController 的主 tooltip 文本组装已拆出；controller 仍合理保留 hover phase、引用查找、地图/时间轴表现同步、主 tooltip 写入/定位和状态关键词副 tooltip 生命周期。

下一批如果继续该文件，优先二选一：状态关键词副 tooltip presenter，或主 tooltip 定位 helper。不要同批修改 `EnemyIntentResolver`、`EnemyIntentData`、`TimelineManager` 和地图/时间轴联动规则。

### 回归检查

`git diff --check` 通过；仅提示 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：170 个已拆脚本模块和 4 个默认 Resource 文件都出现在 `docs/modularized-files-ultimate-operation-guide.md`，覆盖缺失为 0。

Godot headless 项目检查可启动并退出，输出仍有既有退出资源占用警告。加载 `res://scene/in_scene/in_scene.tscn` 通过，主场景 ready、地图构建、CardManager 注册和 TimecoinUI 初始化都正常执行；新 `EnemyIntentTooltipTextBuilder.gd` preload 与 `EnemyIntentData` 类型解析没有报错。该场景检查仍输出既有 TileSet atlas 坐标报错和退出资源泄漏警告，本批没有修改 TileSet、地图资源或退出流程。

## 2026-06-13 TimecoinUI 获得/消耗/不足动画 runner 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timecoin_ui.gd`。开工前确认工作区仍只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、HexMap 手册、模块总表、`workflow_logs/current-modularization-process.md` 与 `workflow_logs/maintenance_guides/timecoin_ui.md`，并用 `rg` 输出 `timecoin_ui.gd` 与 `enemy_intent_presentation_controller.gd` 中 `class_name`、`extends`、`@export`、`@onready`、`const`、`var`、`func`、`_play_gain_animation()`、`_play_consume_animation()`、`_play_warning_animation()`、`active_tweens`、`original_*`、`_apply_shake_effect()` 和 tooltip 相关函数轮廓。

### 当前职责

`timecoin_ui.gd` 是时间币 UI 的 composition root，继续负责节点引用校验、`GlobalTimecoin` 信号连接、数值显示、动画触发时机、`active_tweens` 清理和完成回调、原始位置/缩放/颜色恢复以及旧 shader 控制入口。当前已拆出 `TimecoinGlobalBridge.gd`、`TimecoinHourglassShaderController.gd` 和 `TimecoinShakeTweenBuilder.gd`；获得、消耗和余额不足三段动画仍在主脚本里直接拼接 tween 片段。

### 耦合点

```text
_play_gain_animation()、_play_consume_animation()、_play_warning_animation() 都会先清理 active_tweens、重置 UI，再创建 Tween、拼接动画、登记 active_tweens 和完成回调。
active_tweens 数组、_cleanup_active_tweens()、_reset_to_original_state()、_remove_tween_from_active() 和完成回调必须继续留在主脚本，避免改变动画冲突管理语义。
TimecoinShakeTweenBuilder.gd 已负责位置抖动片段，新 runner 应复用主脚本旧 _apply_shake_effect() 入口或传入 Callable，不直接接管 shake builder。
数值来源、GlobalTimecoin 信号、amount_label 文本刷新和沙漏 shader 控制都不是本批范围。
EnemyIntentPresentationController tooltip 是下一批候选，不和 Timecoin 动画同批提交。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | Timecoin 动画 runner | 获得/消耗/不足三段 tween 片段构造 | 纯表现，输入为节点、颜色、时长、抖动回调和原始状态 | 执行 |
| 2 | 主脚本旧入口保留 | `_play_gain_animation()` 等 | 保留清理、重置、创建 tween、登记和完成回调 | 执行 |
| 3 | active_tweens 管理 controller | `_cleanup_active_tweens()`、`_reset_to_original_state()` | 会改变冲突管理和完成回调语义 | 暂缓 |
| 4 | EnemyIntent tooltip presenter | 敌人意图 tooltip 文本/定位/关键词面板 | 独立风险面，需要补维护说明后另批处理 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimecoinUI 获得、消耗、余额不足三段动画的 tween 片段构造。

涉及的小风险点：

```text
新增 TimecoinFeedbackAnimationRunner.gd，只负责向传入 Tween 追加 gain/consume/warning 动画片段。
timecoin_ui.gd 新增 preload 与 runner getter。
_play_gain_animation()、_play_consume_animation()、_play_warning_animation() 保留旧入口和完成回调，只把 tween 片段构造转发给 runner。
```

不触碰：

```text
GlobalTimecoin 查找、信号连接和数值刷新。
active_tweens 清理、登记、完成回调和 reset 顺序。
TimecoinHourglassShaderController.gd 与 TimecoinShakeTweenBuilder.gd 的职责。
沙漏 shader 控制旧入口。
EnemyIntentPresentationController tooltip。
```

### 实现结果

新增 `scene/in_scene/timecoin_ui_modules/animation/TimecoinFeedbackAnimationRunner.gd`。该模块是 RefCounted runner，中文职责注释明确它只负责把时间币 UI 的获得、消耗和余额不足反馈动画片段追加到传入 Tween；它不创建 Tween、不管理 `active_tweens`、不连接 `GlobalTimecoin` 信号，也不刷新数值文本或控制沙漏 shader。

`scene/in_scene/timecoin_ui.gd` 新增 `TimecoinFeedbackAnimationRunnerScript` preload、`_feedback_animation_runner` 缓存和 `_get_feedback_animation_runner()` 旧式 getter。`_play_gain_animation()`、`_play_consume_animation()` 与 `_play_warning_animation()` 仍保留清理旧动画、重置状态、创建 Tween、登记 `active_tweens` 与连接完成回调；获得/消耗/不足的 tween 片段构造转交给 runner，并继续通过 `Callable(self, "_apply_shake_effect")` 复用已有 `TimecoinShakeTweenBuilder.gd`。

同步更新 `docs/modularized-files-ultimate-operation-guide.md`、`docs/ai-handoff-ultimate-operation-guide.md` 与 `workflow_logs/maintenance_guides/timecoin_ui.md`。总结文档已记录新 runner，单文件维护说明也补上“不要重复拆反馈动画 runner、不要把 `active_tweens` 塞进表现模块”的停止点。

### 当前优化进度与下一步

当前已拆模块统计更新为 169 个脚本模块和 4 个默认 Resource 文件。TimecoinUI 的全局查找、沙漏 shader、普通抖动片段和反馈动画片段均已拆出；主脚本剩余合理职责是数值显示、信号响应、动画触发入口、`active_tweens` 冲突清理和旧 shader 入口转发。

下一批按用户关注转向 `scene/in_scene/enermy/enemy_intent_presentation_controller.gd` 的 tooltip 拆分，并先补该大文件的维护说明。若继续 TimecoinUI，只单独评估 `active_tweens` 清理 controller，不重复拆获得/消耗/不足动画 runner。

### 回归检查

`git diff --check` 通过；仅提示 `workflow_logs/current-modularization-process.md` 会被 Git 归一化换行。

模块文档覆盖检查通过：169 个已拆脚本模块和 4 个默认 Resource 文件都出现在 `docs/modularized-files-ultimate-operation-guide.md`，覆盖缺失为 0。

Godot headless 项目检查可启动并退出，输出仍有既有退出资源占用警告。加载 `res://scene/in_scene/in_scene.tscn` 通过：TimecoinUI 成功连接 `GlobalTimecoin.timecoin_updated` 与 `GlobalTimecoin.timecoin_insufficient`，完成初始显示刷新和 UI 初始化。该场景检查仍输出既有 TileSet atlas 坐标报错和退出资源泄漏警告，本批没有修改 TileSet、贴图资源或退出流程。

## 2026-06-13 Tile 剩余 action_data 子类收口

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd` 的子类意图数据拆分。开工前确认工作区仍只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、HexMap 手册、模块总表、`workflow_logs/current-modularization-process.md` 与 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `village.gd`、`radar.gd`、`tile.gd` 和 `TileIntentActionDataBuilder.gd` 中 `class_name`、`extends`、`var`、`func`、`get_intent_action()`、`action_data`、`TimelineAction.new()`、`effect_range`、`invalid_reason`、`target_affiliation` 与 `does_intent_include_self()` 的轮廓。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责敌人意图协议旧入口、默认意图工厂接入、状态组件、血量和贴图副作用。`TileIntentActionDataBuilder.gd` 已负责组装标准 `action_data` 字典，当前已接入 `altar.gd`、`iron_mine.gd`、`Animal_husbandry.gd` 与 `center_altar.gd`。`village.gd` 与 `radar.gd` 仍在各自 `get_intent_action()` 中手写同一套标准字段，但它们保留了“位置/目标”语义说明注释。

### 耦合点

```text
village.gd 与 radar.gd 的 get_intent_action() 同时构造 action_data 并调用 TimelineAction.new(...)。
action_data 的 "效果"、"类型"、"位置"、"目标"、"effect_range"、"invalid_reason" 字段被时间轴放置、地图 hover 展示和 EnemyIntentData 解析读取，字段名和存在性不能改变。
两处注释明确 "位置" 是发出者逻辑坐标、"目标" 是 target_tile.position 像素坐标；本批必须保留这段语义说明。
effect_range、invalid_reason、target_affiliation、does_intent_include_self()、can_generate_intent() 和目标中心选择仍由子类自己的接口决定。
radar.gd 还有 locked 受击和死亡联动，village.gd 还有扩张行为；本批不触碰这些行为。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `village.gd` action_data 构建 | 标准字段字典 | 与 builder 输入匹配，保留语义注释即可 | 执行 |
| 2 | `radar.gd` action_data 构建 | 标准字段字典 | 与 builder 输入匹配，保留语义注释即可 | 执行 |
| 3 | 子类 `TimelineAction.new(...)` 工厂统一 | 所有覆盖实现 | 会扩大到颜色、shape、target 和展示契约 | 暂缓 |
| 4 | 子类 picker/死亡/扩张规则 | `tex_picker()`、`die()`、`Behavior()` 等 | 牵动地图规则和状态副作用 | 暂缓 |

### 本批风险面

本批只处理一个风险面：剩余两个 Tile 子类的标准 `action_data` 字典构造入口。

涉及的小风险点：

```text
village.gd 只把原 action_data 字典改为调用 _get_intent_action_data_builder().build_standard_action_data(...)。
radar.gd 只把原 action_data 字典改为调用 _get_intent_action_data_builder().build_standard_action_data(...)。
保留原有字段语义注释，不新增模块，不改 TileIntentActionDataBuilder.gd 的字段输出。
```

不触碰：

```text
TimelineAction.new(...) 参数顺序、颜色、shape 和 target_tile。
get_intent_effect_range()、get_intent_invalid_reason()、can_generate_intent()、target_affiliation 和 includes_self。
village.gd 的扩张逻辑、radar.gd 的 locked/take_damage/die 逻辑。
TileIntentActionFactory.gd、TileTextureStateSelector.gd、TileHealthStateRules.gd、TileDeathExecutionController.gd 与 TileDamageProtectionRules.gd。
血量、死亡、贴图、地图拓扑、时间轴落点和敌人意图排序。
```

### 实现结果

本批没有新增模块，也没有改 `tile.gd` 或 `TileIntentActionDataBuilder.gd`。只把最后两个仍手写标准字典的子类接入现有 builder：

```text
scene/in_scene/enermy/village.gd
scene/in_scene/enermy/radar.gd
```

两处仍保留原来的 `get_intent_action(target_tile)`、长段“位置/目标”语义说明、`TimelineAction.new(...)`、颜色、shape、target_tile 和返回值，只把原字段完全一致的 action_data 字典改为：

```text
_get_intent_action_data_builder().build_standard_action_data(...)
```

字段仍保持：

```text
"效果"
"类型"
"位置"
"目标"
"effect_range"
"invalid_reason"
```

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/tile.md
```

### 当前优化进度与下一步

当前已拆模块统计保持为 168 个脚本模块和 4 个默认 Resource 文件。`TileIntentActionDataBuilder.gd` 当前已接入 `altar.gd`、`iron_mine.gd`、`Animal_husbandry.gd`、`center_altar.gd`、`village.gd` 与 `radar.gd`。Tile 标准 action_data 字典迁移已收口；下一批建议转向 `timecoin_ui.gd` 的获得/消耗/不足动画 runner，或 `enemy_intent_presentation_controller.gd` 的 tooltip 表现拆分。若继续 Tile，只单独重新审查子类 picker 策略，不要同批改 `TimelineAction.new(...)`、目标选择、`effect_range`、`invalid_reason`、死亡、血量、贴图或地图拓扑。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
覆盖检查通过：172 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 168 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 剩余子类 action_data 小批迁移

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd` 的子类意图数据拆分。开工前确认工作区仍只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `Animal_husbandry.gd`、`center_altar.gd`、`tile.gd` 与 `TileIntentActionDataBuilder.gd` 中 `func`、`var`、`const`、`action_data`、`TimelineAction.new()`、`effect_range` 和 `invalid_reason` 的轮廓。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责敌人意图协议的旧入口、默认行动工厂接入、状态组件、血量和贴图副作用。`TileIntentActionDataBuilder.gd` 已存在，只负责组装标准 `action_data` 字典；上一批只接入了 `altar.gd` 与 `iron_mine.gd`。本批审查发现 `Animal_husbandry.gd` 与 `center_altar.gd` 的 `get_intent_action()` 中 action_data 字段结构与已迁移子类完全一致，适合作为继续迁移的小批范围。

### 耦合点

```text
子类 get_intent_action() 同时构造 action_data 并调用 TimelineAction.new(...)。
action_data 的 "效果"、"类型"、"位置"、"目标"、"effect_range"、"invalid_reason" 字段被时间轴放置、地图 hover 展示和 EnemyIntentData 解析读取，字段名和存在性不能改变。
target_tile.position 仍是旧契约，target_tile 为空时仍回落 Vector2.ZERO。
effect_range、invalid_reason、target_affiliation、does_intent_include_self() 和目标选择仍由各子类接口或上游调用决定。
village.gd 与 radar.gd 还有更长的目标/注释上下文，本批不顺手迁移。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `Animal_husbandry.gd` action_data 构建 | 重复标准字段字典 | 与 builder 输入完全匹配，不改变 TimelineAction 构造 | 执行 |
| 2 | `center_altar.gd` action_data 构建 | 重复标准字段字典 | 与 builder 输入完全匹配，不改变目标和颜色 | 执行 |
| 3 | `village.gd` 与 `radar.gd` action_data 构建 | 剩余子类覆盖实现 | 需要单独审查注释、目标规则和合法性说明 | 暂缓 |
| 4 | 所有子类 TimelineAction 工厂统一 | `TimelineAction.new(...)` 调用 | 会扩大到颜色、shape、target 和展示契约 | 暂缓 |

### 本批风险面

本批只处理一个风险面：两个 Tile 子类的标准 `action_data` 字典构造入口。

涉及的小风险点：

```text
Animal_husbandry.gd 只把原 action_data 字典改为调用 _get_intent_action_data_builder().build_standard_action_data(...)。
center_altar.gd 只把原 action_data 字典改为调用 _get_intent_action_data_builder().build_standard_action_data(...)。
不新增模块，不改 tile.gd，不改 TileIntentActionDataBuilder.gd 的字段输出。
```

不触碰：

```text
TimelineAction.new(...) 参数顺序、颜色、shape 和 target_tile。
get_intent_effect_range()、get_intent_invalid_reason()、can_generate_intent()、target_affiliation 和 includes_self。
TileIntentActionFactory.gd、TileTextureStateSelector.gd、TileHealthStateRules.gd、TileDeathExecutionController.gd 与 TileDamageProtectionRules.gd。
血量、死亡、贴图、地图拓扑、时间轴落点和敌人意图排序。
```

### 实现结果

本批没有新增模块，也没有改 `tile.gd` 或 `TileIntentActionDataBuilder.gd`。只把两个子类原本完全展开的标准字典构造改为复用现有 builder：

```text
scene/in_scene/enermy/Animal_husbandry.gd
scene/in_scene/enermy/center_altar.gd
```

两处仍保留原来的 `get_intent_action(target_tile)`、`TimelineAction.new(...)`、颜色、shape、target_tile 和返回值，只把原字段完全一致的 action_data 字典改为：

```text
_get_intent_action_data_builder().build_standard_action_data(...)
```

字段仍保持：

```text
"效果"
"类型"
"位置"
"目标"
"effect_range"
"invalid_reason"
```

同步更新：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/maintenance_guides/tile.md
```

### 当前优化进度与下一步

当前已拆模块统计保持为 168 个脚本模块和 4 个默认 Resource 文件。`TileIntentActionDataBuilder.gd` 当前已接入 `altar.gd`、`iron_mine.gd`、`Animal_husbandry.gd` 与 `center_altar.gd`。下一批如果继续 Tile，只小批重新审查 `village.gd` 或 `radar.gd` 的 action_data 字典是否能迁移；不要同批改 `TimelineAction.new(...)`、目标选择、`effect_range`、`invalid_reason`、死亡、血量、贴图或地图拓扑。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
覆盖检查通过：172 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 168 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 子类意图 action_data 构建规则拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd` 与少量敌方地貌子类。开工前确认工作区仍只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd`、`tile_modules/` 与 `scene/in_scene/enermy/*.gd` 中 `get_intent_action()`、`action_data`、`TimelineAction.new()`、`effect_range`、`invalid_reason`、`target_affiliation` 的轮廓。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责状态组件、血量、结算奖励、敌人意图协议、贴图切换和旧公共入口。默认 `TimelineAction` 构造已经拆到 `TileIntentActionFactory.gd`；多个敌方地貌子类仍在各自 `get_intent_action()` 中重复组装 `"效果"`、`"类型"`、`"位置"`、`"目标"`、`"effect_range"` 和 `"invalid_reason"` 字段。

### 耦合点

```text
子类 get_intent_action() 同时负责 action_data 字典构造和 TimelineAction.new(...)。
action_data 字段被时间轴放置、地图 hover 展示和 EnemyIntentData 解析读取，不能顺手改字段名或增删字段。
target_tile.position 仍是旧契约，当前只移动重复字典构造，不改变目标来源或坐标类型。
effect_range、invalid_reason、can_generate_intent()、target_affiliation 和 includes_self 仍由各子类自己的接口提供，本批不统一规则。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 标准 action_data 构建器 | 子类 `get_intent_action()` 中重复的 action_data 字段 | 纯字典构造，输入输出清楚；不创建 TimelineAction | 执行 |
| 2 | 接入少量重复子类 | `altar.gd`、`iron_mine.gd` 的 action_data 字典 | 两者结构完全一致，能验证继承 getter 和字段保持不变 | 执行 |
| 3 | 继续迁移其他子类 | `Animal_husbandry.gd`、`center_altar.gd`、`village.gd`、`radar.gd` | 文件更多，注释和目标规则差异更大，需后续小批 | 暂缓 |
| 4 | TimelineAction 工厂统一 | 所有子类 `TimelineAction.new(...)` | 会改变更大范围构造入口，牵动时间轴契约 | 暂缓 |

### 本批风险面

本批只处理一个风险面：子类意图 `action_data` 字典构造。

涉及的 4 个小风险点：

```text
新增 TileIntentActionDataBuilder.gd，只组装 action_data 字典。
tile.gd 新增 preload 与 _get_intent_action_data_builder() 缓存 getter，供子类继承使用。
altar.gd 改为通过 builder 构造原字段完全相同的 action_data。
iron_mine.gd 改为通过 builder 构造原字段完全相同的 action_data。
```

不触碰：

```text
TimelineAction.new(...) 参数顺序、颜色、shape 和 target_tile。
get_intent_effect_range()、get_intent_invalid_reason()、can_generate_intent() 与目标选择规则。
TileIntentActionFactory.gd 的默认行动构造。
死亡、血量、贴图、拓扑通知和地图数据。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileIntentActionDataBuilder.gd
```

职责：

```text
TileIntentActionDataBuilder 只负责组装 Tile 敌人意图使用的 action_data 字典。
它不创建 TimelineAction、不选择目标、不判断意图是否合法，也不读取或修改地图、血量、贴图和状态组件。
```

`tile.gd` 新增 `TileIntentActionDataBuilderScript` preload、`_intent_action_data_builder` 缓存和 `_get_intent_action_data_builder()` getter，供继承自 `landform` 的子类复用。`altar.gd` 与 `iron_mine.gd` 的 `get_intent_action()` 仍保留旧入口和原来的 `TimelineAction.new(...)` 参数，只把原字段完全一致的 action_data 字典改为通过 builder 构造：

```text
"效果"
"类型"
"位置"
"目标"
"effect_range"
"invalid_reason"
```

本批没有迁移 `Animal_husbandry.gd`、`center_altar.gd`、`village.gd` 或 `radar.gd`，因为这些文件数量更多，且部分注释和目标规则差异更大，应后续小批处理。

### 当前优化进度与下一步

当前已拆模块统计将更新为 168 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 的子类意图 action_data 字典构造已有第一批 builder；下一批如果继续 Tile，只评估 1 到 2 个剩余子类是否能迁移到 builder，或者停止 Tile 转向其他大文件。不要同批统一 `TimelineAction.new(...)`、目标选择、`effect_range`、`invalid_reason` 或子类 picker 策略。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
覆盖检查通过：172 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 168 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 贴图状态选择规则审查与拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前确认工作区仍只有用户已有的资源与 shader 改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd`、`tile_modules/` 与敌方地貌子类中 `tex_toggle()`、`tex_picker()`、`damaged_tex_picker()`、`get_intent_action()` 的轮廓。

### 当前职责

`tile.gd` 仍是地貌和建筑实体基类，负责状态组件、血量状态、结算奖励、敌人意图协议、贴图节点挂接和实体生命周期。时间轴 shape 解析、默认敌人意图 action 构造、血量纯规则、Broken 后死亡收尾执行和 protected 受击吸收规则已经拆出；`tex_toggle()` 仍在主脚本里同时判断主状态、选择普通或破损贴图数组，并直接写入 `Sprite2D.texture`。

### 耦合点

```text
tex_toggle() 同时读取 State_Main、landform_tex、landform_damaged_tex 和 tex.texture，并调用 tex_picker()/damaged_tex_picker() 写回 Sprite2D。
Revived()、Captured()、die() 和部分子类死亡流程会调用 tex_toggle()，所以旧入口必须保留。
Animal_husbandry.gd、background.gd、radar.gd、tower.gd 等仍覆写 tex_picker() 或 damaged_tex_picker()，这些是子类多态定制点，本批不统一。
多个敌方地貌子类仍各自覆写 get_intent_action()，但意图 action 数据契约涉及 effect_range、invalid_reason、target_affiliation 和 TimelineAction 展示，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 贴图状态选择规则 | `tex_toggle()` 中普通态、占领态、破损态与贴图数组归属判断 | 纯选择规则，输入输出清楚；主脚本仍写 Sprite2D 并保留子类 picker | 执行 |
| 2 | 子类 picker 策略统一 | 多个敌方地貌覆写 `tex_picker()` 或 `damaged_tex_picker()` | 依赖 `rivet_land`、`Underlings`、地图高度等子类语义，容易扩大风险 | 暂缓 |
| 3 | 子类敌人意图 action 数据统一 | 多个敌方地貌覆写 `get_intent_action()` | 牵动 TimelineAction 数据契约和敌人意图展示规则 | 暂缓 |
| 4 | 贴图节点创建流程 | `_ready()` 中 Sprite2D 创建、位置、血条信号和 owner_battle 交互 | 绑定场景树和血条信号，不适合与贴图选择同批 | 暂缓 |

### 本批风险面

本批只处理一个风险面：`tex_toggle()` 中根据主状态和当前贴图归属选择下一张贴图的纯规则。

涉及的 3 个小风险点：

```text
新增 TileTextureStateSelector.gd，只根据当前贴图和目标贴图数组返回是否需要切换。
tile.gd 新增 preload 与 _get_texture_state_selector() 缓存 getter。
tex_toggle() 旧入口继续存在，仍由 tile.gd 判断状态、调用子类 picker 并写回 tex.texture。
```

不触碰：

```text
死亡收尾 controller、protected 受击规则、血量状态规则和 TimelineAction 构造。
子类 tex_picker()/damaged_tex_picker() 的多态选择逻辑。
get_intent_action() 及敌人意图 action 数据契约。
Sprite2D 创建、血条信号和地图拓扑通知。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileTextureStateSelector.gd
```

职责：

```text
TileTextureStateSelector 只负责根据 Tile 主状态对应的贴图组和当前贴图，判断是否需要切换贴图。
它不修改 Sprite2D、不读取场景树、不加载资源，也不处理死亡、血量、状态组件或子类贴图选择策略。
```

`tile.gd::tex_toggle()` 仍是旧入口。普通态和占领态仍检查 `landform_tex`，破损态仍优先检查 `landform_damaged_tex`，破损贴图缺失时仍回退到普通贴图。新模块只承接原来的 `!landform_tex.has(tex.texture)` 和 `!landform_damaged_tex.has(tex.texture)` 判断，`tex_picker()`、`damaged_tex_picker()` 与 `tex.texture` 写回仍保留在主脚本里，因此子类覆写 picker 的行为不变。

### 当前优化进度与下一步

当前已拆模块统计将更新为 167 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 的贴图数组归属切换判断已经拆出；下一批如果继续 Tile，优先重新审查多个敌方地貌子类的意图 action 数据是否能统一，或停止 Tile 转向其他大文件。不要重复拆死亡收尾 controller、protected 受击规则或贴图归属判断，也不要同批修改子类 picker 策略和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
覆盖检查通过：171 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 167 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile protected 受击规则拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前确认仓库内没有实体 `AGENTS.md` 内容，因此继续遵守当前对话中的 AGENTS 约束。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd` 与 `tile_modules/` 的函数、变量、信号轮廓，重点确认 `take_damage()`、`State_Vice`、`Vice_State_Pool.protected`、`set_health()` 和已拆死亡收尾 controller 的边界。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和旧公共入口。时间轴 shape 解析、默认敌人意图 action 构造、血量纯规则和 Broken 后死亡收尾已经拆出；`take_damage()` 仍在主脚本里同时判断 protected 副状态、清除 protected 位并决定是否扣血。

### 耦合点

```text
take_damage() 当前同时读取 State_Vice、清除 Vice_State_Pool.protected，并决定是否调用 set_health(HP - amount)。
set_health() 后续会触发 Blood_change、State_Update()、死亡收尾和贴图切换，本批不改变这条链路。
altar.gd 会给相邻地貌设置 protected 位；radar.gd 自己覆盖 take_damage() 并叠加 locked 规则，本批不改子类覆盖逻辑。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TileDamageProtectionRules.gd` | `take_damage()` 中 protected 位判断与清除 | 纯位运算规则，输入输出清楚；主脚本继续决定是否扣血 | 执行 |
| 2 | 贴图选择 presenter/rules | `tex_toggle()`、`tex_picker()`、`damaged_tex_picker()` | 牵动主状态与贴图数组兜底，需单独审查 | 暂缓 |
| 3 | 子类敌人意图 action 数据统一 | 多个敌方地貌覆盖 `get_intent_action()` | 牵动 `effect_range`、`invalid_reason`、`target_affiliation` 和 TimelineAction 数据契约 | 暂缓 |
| 4 | radar locked 受击规则统一 | `scene/in_scene/enermy/radar.gd::take_damage()` | 子类额外 locked 语义不同于基础 protected，不应顺手合并 | 暂缓 |

### 本批风险面

本批只处理一个风险面：基础 Tile 受击时 protected 副状态的吸收规则。

涉及的 3 个小风险点：

```text
新增 TileDamageProtectionRules.gd，只根据 State_Vice 和 protected flag 返回是否吸收以及新的 State_Vice。
tile.gd 新增 preload 与 _get_damage_protection_rules() 缓存 getter。
take_damage() 旧入口继续存在，仍由主脚本写回 State_Vice，并在未吸收时调用 set_health(HP - amount)。
```

不触碰：

```text
set_health()、State_Update()、die() 和死亡收尾 controller。
tex_toggle()、tex_picker()、damaged_tex_picker() 的贴图选择规则。
radar.gd 的 locked 覆盖逻辑。
默认或子类 get_intent_action() 的 TimelineAction 数据。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileDamageProtectionRules.gd
```

职责：

```text
TileDamageProtectionRules 只负责判断 Tile 受击时 protected 副状态是否吸收伤害。
它不扣血、不发信号、不播放受击表现，也不处理死亡、贴图切换或子类 locked 规则。
```

`tile.gd::take_damage(amount)` 仍是旧入口，继续由主脚本写回 `State_Vice`，并在 protected 没有吸收伤害时调用 `set_health(HP - amount)`。这保持了受击入口、血量链路、`Blood_change`、死亡收尾和贴图切换的旧调用顺序。

### 当前优化进度与下一步

当前已拆模块统计将更新为 166 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 的 protected 受击吸收规则已经拆出；下一批如果继续 Tile，优先重新审查 `tex_toggle()` 贴图选择边界或多个敌方地貌子类的意图 action 数据统一。不要重复拆死亡执行 controller 或 protected 受击规则，也不要同批修改贴图选择和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：170 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 166 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 死亡收尾执行 controller 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前确认仓库内没有实体 `AGENTS.md` 内容，因此继续遵守当前对话中的 AGENTS 约束。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd` 与 `tile_modules/` 的函数、变量、信号轮廓，重点确认 `set_health()`、`State_Update()`、`die()`、`tex_toggle()`、`get_intent_action()` 与当前已拆 Tile 模块的边界。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和旧公共入口。时间轴 shape 解析、默认敌人意图 action 构造、血量 clamp/damage_rate/主状态判定已经拆出；死亡后的状态组件清理、Broken 贴图切换、`damage_rate = 1.0` 和拓扑信号通知仍集中在 `die()`。

### 耦合点

```text
State_Update() 已经只从 TileHealthStateRules 取下一主状态标签，但仍由主脚本调用 die()/Captured()/Revived() 执行副作用。
die() 同时负责写 State_Main、清理 status_component、切换贴图、写 damage_rate，并通知 owner_battle.tile_topology_changed。
tex_toggle() 内部仍包含主状态到贴图数组的选择规则，死亡收尾不应顺手拆贴图选择。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TileDeathExecutionController.gd` | `die()` 中已经判定 Broken 后的通用收尾 | 可作为小 controller 承接状态组件清理、贴图回调、damage_rate 结果和拓扑通知；不判断血量、不选择贴图规则 | 执行 |
| 2 | 贴图选择 presenter/rules | `tex_toggle()`、`tex_picker()`、`damaged_tex_picker()` | 牵动主状态与贴图数组兜底，和死亡执行相邻但不是同一风险面 | 暂缓 |
| 3 | protected 受击规则 | `take_damage()` | 与 `Vice_State_Pool.protected` 消耗和伤害命令体验绑定，需单独验证 | 暂缓 |
| 4 | 子类敌人意图 action 数据统一 | 多个敌方地貌覆盖 `get_intent_action()` | 牵动 `effect_range`、`invalid_reason`、`target_affiliation` 和 TimelineAction 数据契约 | 暂缓 |

### 本批风险面

本批只处理一个风险面：Tile 已进入 Broken 后的死亡收尾执行。

涉及的 3 个小风险点：

```text
新增 TileDeathExecutionController.gd，只执行 status_component 清理、贴图回调、拓扑信号通知，并返回 damage_rate 结果。
tile.gd 新增 preload 与 _get_death_execution_controller() 缓存 getter。
die() 旧入口继续存在，仍先写 State_Main = Broken，再委托 controller 执行收尾。
```

不触碰：

```text
set_health() 和 State_Update() 的血量判定。
take_damage() 的 protected 消耗。
tex_toggle()、tex_picker()、damaged_tex_picker() 的贴图选择规则。
默认或子类 get_intent_action() 的 TimelineAction 数据。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/controllers/TileDeathExecutionController.gd
```

职责：

```text
TileDeathExecutionController 只负责执行 Tile 已进入 Broken 后的通用死亡收尾。
它不判断血量、不扣血、不释放 tile 节点、不重建地图视觉，也不选择死亡贴图。
```

`tile.gd::die()` 仍是旧入口，继续先写 `State_Main = Main_State_Pool.Broken`，再把状态组件清理、Broken 贴图回调和 `damage_rate` 结果委托给 controller。`damage_rate` 写回后，再由 controller 通知 `owner_battle.tile_topology_changed`，保持外部监听方看到的死亡状态已经完整写回。

### 当前优化进度与下一步

当前已拆模块统计将更新为 165 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 的死亡收尾已经拆出；下一批如果继续 Tile，优先重新审查 `tex_toggle()` 贴图选择边界、`take_damage()` 的 protected 消耗规则，或多个敌方地貌子类的意图 action 数据统一。不要重复拆死亡执行 controller，也不要同批修改贴图选择和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：169 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 165 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 血量状态纯规则拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前确认仓库内没有实体 `AGENTS.md`，因此继续遵守当前对话中的 AGENTS 约束。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd` 的函数、变量、信号轮廓，以及 `State_Update()`、`set_health()`、`damage_rate`、`Capture_rate`、`die()`、`Captured()` 和 `Revived()` 的调用与引用。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和旧公共入口。时间轴 shape 解析、默认敌人意图 action 构造已经拆出；血量链路仍同时包含纯数值判断和副作用执行。

### 耦合点

```text
set_health() 会 clamp HP、调用 State_Update()、发出 Blood_change，并计算 damage_rate。
State_Update() 会根据 HP、Max_Blood 和 Capture_rate 决定 die()/Captured()/Revived()，这些函数会改 State_Main 并触发贴图切换。
die() 额外清理 status_component、把 damage_rate 置 1，并发出 tile_topology_changed。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TileHealthStateRules.gd` | `set_health()` 中的 clamp/damage_rate 与 `State_Update()` 的状态判定 | 纯数值规则，输入输出清楚；主脚本继续执行 die/Captured/Revived 副作用 | 执行 |
| 2 | 死亡执行 controller | `die()` | 牵动状态清理、贴图、拓扑信号和 Broken 占位语义，风险高 | 暂缓 |
| 3 | protected 受击规则 | `take_damage()` | 与 `Vice_State_Pool.protected` 消耗和伤害命令体验绑定，需单独验证 | 暂缓 |

### 本批风险面

本批只处理一个风险面：Tile 血量状态的纯规则计算。

涉及的 3 个小风险点：

```text
新增 TileHealthStateRules.gd，只返回 clamp 后 HP、damage_rate 和下一状态标签。
tile.gd 新增 preload 与 _get_health_state_rules() 缓存 getter。
set_health() 与 State_Update() 旧入口继续存在，仍由主脚本发信号并调用 die()/Captured()/Revived()。
```

不触碰：

```text
take_damage() 的 protected 消耗。
die() 内状态清理、贴图切换、damage_rate=1.0 和 tile_topology_changed。
Captured()、Revived()、tex_toggle() 的贴图行为。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileHealthStateRules.gd
```

职责：

```text
TileHealthStateRules 只负责计算 Tile 血量相关的纯规则结果。
它不发信号、不切换贴图、不清理状态组件，也不通知 HexMap 拓扑变化。
```

`tile.gd::set_health(new_hp)` 仍是旧入口，继续负责写回 `HP`、调用 `State_Update()`、发出 `Blood_change`，并保持旧行为：只有 `Max_Blood > 0` 时才更新 `damage_rate`。

`tile.gd::State_Update()` 仍是旧入口，继续由主脚本调用 `die()`、`Captured()` 或 `Revived()`，因此贴图切换、状态清理和拓扑信号没有进入规则模块。

### 当前优化进度与下一步

已拆模块统计更新为 164 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 当前约 684 行。下一批如果继续 Tile，先重新审查死亡执行 controller 或多个子类意图 action 数据统一；不要同批碰贴图切换、拓扑信号和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：168 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 164 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 默认敌人意图 action factory 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前已确认仓库内没有实体 `AGENTS.md`，因此继续遵守当前对话中的 AGENTS 约束：先审查、保持小步、只做可验证的改动。随后重新读取接力说明、HexMap 手册、模块维护说明和 `workflow_logs/maintenance_guides/tile.md`，并用 `rg` 输出 `tile.gd` 的函数、变量、信号轮廓，以及 `set_health()`、`take_damage()`、`die()`、`get_intent_action()`、`get_intent_shape()` 等相关调用方。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和旧公共入口。时间轴 shape 解析已拆到 `TileTimelineShapeParser.gd`，但 shape 旧入口仍由主脚本写回成员。

### 耦合点

```text
血量链路：set_health()、take_damage()、State_Update() 和 die() 会同时牵动 protected 状态、Blood_change、damage_rate、状态清理、贴图切换和 tile_topology_changed。
默认敌人意图链路：get_intent_action() 只组装 action_data 并创建 TimelineAction，但依赖 landform_name、location、target_tile.position 和 get_intent_shape() 的旧契约。
子类覆盖链路：altar、village、radar 等敌方地貌已有自己的 get_intent_action()，它们补充 effect_range、invalid_reason、target_affiliation 等字段，本批不能改这些子类数据契约。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TileIntentActionFactory.gd` | 默认 `get_intent_action()` 中的 `TimelineAction` 构造 | 输入输出清楚，只搬默认 action_data 和构造，不改目标选择、shape 解析或时间轴落点 | 执行 |
| 2 | 血量状态服务 | `set_health()`、`take_damage()`、`State_Update()`、`die()` | 同时牵动贴图、状态组件和拓扑信号，风险高 | 暂缓 |
| 3 | 子类意图 action 数据统一 | 多个 `enermy/*.gd::get_intent_action()` | 跨多个敌方地貌和 EnemyIntentData 字段，风险高 | 暂缓 |

### 本批风险面

本批只处理一个风险面：Tile 默认敌人意图 `TimelineAction` 构造。

涉及的 3 个小风险点：

```text
新增 TileIntentActionFactory.gd，只负责默认 action_data 与 TimelineAction.new。
tile.gd 新增 preload 与 _get_intent_action_factory() 缓存 getter。
旧 get_intent_action(target_tile) 入口继续存在，只把原有字段和 get_intent_shape() 结果传给 factory。
```

不触碰：

```text
can_generate_intent()、get_intent_target_*()、get_intent_shape() 和 TimelineManager 落点选择。
子类 get_intent_action() 覆盖实现。
血量、死亡、贴图切换、状态清理和 tile_topology_changed。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileIntentActionFactory.gd
```

职责：

```text
TileIntentActionFactory 只负责为 Tile 默认敌人意图创建 TimelineAction。
它不选择目标、不判断意图是否合法、不解析时间轴 shape，也不修改 tile 血量、贴图或地图拓扑。
```

`tile.gd::get_intent_action(target_tile)` 仍是旧入口，内部只把 `self`、`target_tile`、`get_intent_shape()`、`landform_name` 和 `location` 传给 factory。默认 `action_data` 字段保持为 `"效果"`、`"类型"`、`"位置"`、`"目标"`，默认颜色仍是 `Color(0.8, 0.2, 0.2, 0.8)`。

### 当前优化进度与下一步

已拆模块统计更新为 163 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 当前约 674 行。下一批如果继续 Tile，先重新审查血量/死亡状态或多个子类意图 action 数据统一；不要同批碰贴图切换、拓扑信号和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：167 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 163 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
临时 Godot 日志已清理。
```

## 2026-06-12 Tile 时间占位 shape parser 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/tile.gd`。开工前重新用 `rg` 输出 `tile.gd` 的函数、变量、信号轮廓，读取 `CustomCardTimelineShapeParser.gd` 作对照，并搜索 `parse_timeline_shape`、`get_intent_shape`、`set_timeline_shape` 和 `timeline_shape_coords` 的调用方。确认当前 shape 接口主要被敌方地貌脚本、`TimelineManager` 和旧敌人意图管理器读取。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`tile.gd` 仍是地貌/建筑实体基类，负责地貌基础属性、状态组件、结算奖励、敌人意图协议、视觉挂接、血量状态、贴图切换和时间轴 shape 旧入口。

### 耦合点

```text
血量死亡状态会清理 status_component、切换贴图、更新 damage_rate，并发出 tile_topology_changed。
get_intent_action() 会创建 TimelineAction，牵动敌人意图和时间轴数据契约。
_parse_matrix_shape() 只把矩阵字符串转为 timeline_shape_coords 和 timeline_shape_size，是本批最清晰边界。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TileTimelineShapeParser.gd` | `_parse_matrix_shape()` | 纯矩阵解析，不碰实体生命周期、不创建 TimelineAction | 执行 |
| 2 | 血量状态服务 | `set_health()`、`take_damage()`、`die()`、`State_Update()` | 牵动贴图、状态和拓扑信号 | 暂缓 |
| 3 | 敌人意图 action factory | `get_intent_action()` | 牵动 TimelineAction 数据契约 | 暂缓 |

### 本批风险面

本批只处理一个风险面：Tile 的时间占位矩阵 shape 解析。

涉及的 3 个小风险点：

```text
新增 TileTimelineShapeParser.gd，只返回 coords 和 size。
tile.gd 新增 preload 与 _get_timeline_shape_parser() 缓存 getter。
旧 _parse_matrix_shape() 入口继续存在，负责把 parser 结果写回 timeline_shape_coords 和 timeline_shape_size。
```

不触碰：

```text
parse_timeline_shape() 的幂等缓存语义。
get_intent_shape()、get_timeline_shape_size() 和 set_timeline_shape() 的旧入口。
血量、死亡、贴图切换和 tile_topology_changed。
TimelineAction 创建与敌人意图目标规则。
```

### 实现结果

新增：

```text
scene/in_scene/tile_modules/rules/TileTimelineShapeParser.gd
```

职责：

```text
TileTimelineShapeParser 只负责把地貌实体的时间占位矩阵解析为坐标和尺寸。
它不读取场景树、不修改 tile 节点，也不创建 TimelineAction 或判断敌人意图是否合法。
```

`tile.gd::_parse_matrix_shape()` 仍是旧入口，内部转发给 parser 并继续写回原成员变量。

### 当前优化进度与下一步

已拆模块统计更新为 162 个脚本模块和 4 个默认 Resource 文件。`tile.gd` 当前约 678 行。下一批如果继续 Tile，先重新审查血量/死亡状态或敌人意图 action factory；不要同批碰贴图切换、拓扑信号和 TimelineAction 数据契约。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：166 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 162 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
临时 Godot 日志已清理。
```

## 2026-06-12 TimecoinUI 抖动 tween builder 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timecoin_ui.gd`。文档维护入口已在上一批补齐并提交。开工前再次用 `rg` 输出 `timecoin_ui.gd` 与 `timecoin_ui_modules/` 的函数、变量和模块轮廓，并读取现有 `TimecoinGlobalBridge.gd` 与 `TimecoinHourglassShaderController.gd`，确认本批不重复拆 `GlobalTimecoin` 查找或沙漏 shader 控制。

工作区仍存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。

### 当前职责

`timecoin_ui.gd` 仍是时间币 UI 的 composition root，负责节点引用校验、连接 `GlobalTimecoin` 信号、刷新数量文本、播放获得/消耗/不足动画、维护 `active_tweens`、重置原始视觉状态，并保留沙漏 shader 的旧入口。

### 耦合点

```text
_play_gain_animation()、_play_consume_animation() 和 _play_warning_animation() 共享 active_tweens、原始位置、缩放、图标颜色和动画完成回调。
_apply_shake_effect() 只把位置抖动片段追加到传入 Tween，输入输出清楚。
TimecoinHourglassShaderController.gd 只处理 shader 参数，不应该接管普通 tween 动画。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TimecoinShakeTweenBuilder.gd` | `_apply_shake_effect()` | 纯动画片段构造，不改信号、数值、active_tweens 或完成回调 | 执行 |
| 2 | 获得/消耗/不足动画 runner | `_play_gain_animation()`、`_play_consume_animation()`、`_play_warning_animation()` | 共享参数和收尾较多，应另开批次 | 暂缓 |
| 3 | tween 状态清理 controller | `_cleanup_active_tweens()`、`_reset_to_original_state()`、`_remove_tween_from_active()` | 与所有动画共享状态 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimecoinUI 普通动画的位置抖动片段构造。

涉及的 3 个小风险点：

```text
新增 TimecoinShakeTweenBuilder.gd，只负责 append_shake。
timecoin_ui.gd 新增 preload 与 _get_shake_tween_builder() 缓存 getter。
旧 _apply_shake_effect() 入口继续存在，只转发给 builder。
```

不触碰：

```text
GlobalTimecoin 查找、信号连接和 get_timecoins() 数值来源。
三类动画的触发时机、active_tweens 记录和完成回调。
沙漏 shader controller。
```

### 实现结果

新增：

```text
scene/in_scene/timecoin_ui_modules/animation/TimecoinShakeTweenBuilder.gd
```

职责：

```text
TimecoinShakeTweenBuilder 只负责把时间币 UI 的位置抖动片段追加到传入 Tween。
它不创建 Tween、不管理 active_tweens，也不连接时间币信号或修改数值显示。
```

`timecoin_ui.gd::_apply_shake_effect()` 仍是旧入口，内部转发给 builder。获得、消耗和不足动画的调用顺序不变。

### 当前优化进度与下一步

已拆模块统计更新为 161 个脚本模块和 4 个默认 Resource 文件。`timecoin_ui.gd` 当前约 461 行。下一批如果继续 TimecoinUI，可审查获得/消耗/不足动画 runner；也可以转向 `tile.gd` 的 timeline shape parser。两者应分开提交。

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖检查通过：165 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 161 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
临时 Godot 日志已清理。
```

## 2026-06-12 大文件单独维护说明补齐

### 读取与轮廓

本批按接力规则先确认工作区状态、最新提交和 `AGENTS.md`。仓库内仍没有实体 `AGENTS.md`，因此遵守当前对话中用户贴出的约束。工作区存在用户已有改动：

```text
default_bus_layout.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

本批不回滚、不触碰这些文件。随后读取固定文档，并用 `rg` 输出 `timecoin_ui.gd`、`tile.gd`、已有模块目录和文档引用轮廓。

### 当前职责

`docs/` 仍只保留总结性说明；逐批过程仍集中在 `workflow_logs/current-modularization-process.md`。用户指出很多大文件缺少单独维护 md，因此本批新增稳定的单文件维护入口，降低后续接力检索成本。

### 耦合点

```text
docs/ 目录不能重新堆过程日志。
current-modularization-process.md 已经很长，不适合承担单文件维护入口。
大文件维护说明需要能快速说明职责、已拆模块、停止点、下一步和验证入口。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `workflow_logs/maintenance_guides/` | 大文件单独维护说明 | 不违反 docs 总结性规则，便于接力 | 执行 |
| 2 | 总文档索引 | `docs/ai-handoff...` 和 `docs/modularized-files...` | 只加稳定入口说明，不写过程 | 执行 |
| 3 | TimecoinUI 拆分 | `_apply_shake_effect()` 或动画 runner | 代码风险面，另开批次 | 暂缓 |
| 4 | Tile 拆分 | timeline shape 解析 | 代码风险面，另开批次 | 暂缓 |

### 本批风险面

本批只处理文档维护入口补齐。

涉及的风险点：

```text
新增 workflow_logs/maintenance_guides/README.md。
新增 in_scene、DragShapeController、timeline_ui、custom_card、timecoin_ui、tile、out_scene_map_exp、rewards 的单文件维护说明。
更新 docs 中的文档规则和模块总说明索引。
```

不触碰：

```text
GDScript 运行逻辑。
docs/ 目录的过程日志结构。
用户已有 shader 和 bus layout 改动。
```

### 实现结果

新增维护入口：

```text
workflow_logs/maintenance_guides/README.md
workflow_logs/maintenance_guides/in_scene.md
workflow_logs/maintenance_guides/drag_shape_controller.md
workflow_logs/maintenance_guides/timeline_ui.md
workflow_logs/maintenance_guides/custom_card.md
workflow_logs/maintenance_guides/timecoin_ui.md
workflow_logs/maintenance_guides/tile.md
workflow_logs/maintenance_guides/out_scene_map_exp.md
workflow_logs/maintenance_guides/rewards.md
```

同步更新：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

### 当前优化进度与下一步

本批没有新增脚本模块。下一批继续代码拆分时，先处理 `timecoin_ui.gd` 的纯动画抖动 helper，再处理 `tile.gd` 的 timeline shape parser。两者分开提交，避免同批触碰两个运行风险面。

### 回归检查

待运行：

```text
git diff --check
```

## 这份归档的用途

这份文件保存前面 HexMap 解耦过程的中间信息，以及后续继续拆 `in_scene.gd` 和其他大文件时需要遵守的流程。它不是最终说明文档。最终给组员和 AI 快速阅读的文档放在 `docs/` 下，中间过程都集中放在这里。

当前最终说明：

```text
docs/hex-map-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
```

## 2026-06-10 TimecoinUI 沙漏 shader controller 拆分

### 读取与轮廓

本批继续处理 `scene/in_scene/timecoin_ui.gd`。开工前确认工作区干净，最新提交为 `01ab4fd refactor: extract timecoin global bridge`，仓库内仍没有实体 `AGENTS.md`，因此遵守当前对话中用户贴出的 AGENTS 约束。随后重新读取固定文档、模块总说明和上一批 TimecoinUI 记录，并用 `rg` 输出 `timecoin_ui.gd` 与 `timecoin_ui_modules/` 的函数、变量轮廓。补充搜索确认沙漏 shader 入口目前只在 `timecoin_ui.gd` 内定义，仍按旧公共入口保留。

### 当前职责

`timecoin_ui.gd` 仍是时间币 UI 的 composition root，负责校验 UI 节点、连接 `GlobalTimecoin` 信号、刷新数量文本、播放获得/消耗/不足动画、维护活跃 tween 队列、重置视觉状态，并保留沙漏 shader 控制入口。

### 耦合点

```text
_initialize_shader_material()、start_hourglass_shake()、stop_hourglass_shake()、set_shake_intensity() 和 set_shake_frequency() 只读写沙漏图标材质与 shader 参数，是清晰的表现边界。
_play_gain_animation()、_play_consume_animation() 和 _play_warning_animation() 牵动 tween、位置、缩放、颜色、原始状态和动画完成回调，本批不拆。
_cleanup_active_tweens() 与 _reset_to_original_state() 被所有动画共用，暂不移动。
GlobalTimecoin 查找上一批已经进入 TimecoinGlobalBridge.gd，本批不重复处理。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TimecoinHourglassShaderController.gd` | 沙漏 shader 材质初始化与震动参数写入 | 只处理 shader 材质和参数，不碰时间币信号、数值或 tween 动画 | 执行 |
| 2 | 获得/消耗/不足动画 runner | `_play_gain_animation()`、`_play_consume_animation()`、`_play_warning_animation()` | 纯视觉但共享 active_tweens 与收尾回调，需另开小批 | 暂缓 |
| 3 | tween 状态清理 controller | `_cleanup_active_tweens()`、`_reset_to_original_state()`、`_remove_tween_from_active()` | 与所有动画共享状态，暂不单独移动 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimecoinUI 的沙漏 shader 控制。

涉及的 4 个小风险点：

```text
新增 TimecoinHourglassShaderController.gd，只准备或复用 ShaderMaterial，并写入 shake 参数。
timecoin_ui.gd 新增 preload 与 _get_hourglass_shader_controller() 缓存 getter。
旧 _initialize_shader_material()、start_hourglass_shake()、stop_hourglass_shake()、set_shake_intensity()、set_shake_frequency() 入口继续存在并转发给 controller。
hourglass_shader_material 缓存仍保留在 timecoin_ui.gd，避免改变外部调用旧入口时的状态语义。
```

不触碰：

```text
GlobalTimecoin 查找与信号连接。
get_timecoins() 数值来源。
获得/消耗/不足动画。
active_tweens 清理和动画完成回调。
```

### 实现结果

```text
scene/in_scene/timecoin_ui_modules/presenters/TimecoinHourglassShaderController.gd
scene/in_scene/timecoin_ui.gd
```

`TimecoinHourglassShaderController.gd` 是新的 RefCounted presenter/controller，中文职责注释说明它只负责沙漏 shader 材质准备与震动参数写入，不连接时间币信号、不刷新数量文本，也不播放获得、消耗或不足动画。`timecoin_ui.gd` 的旧 shader 入口继续存在，调用顺序保持为旧入口转发。

### 当前优化进度与下一步

```text
已拆模块统计更新为 160 个脚本模块和 4 个默认 Resource 文件。
timecoin_ui.gd 当前约 469 行；本批继续缩小纯表现边界，不追求压行数。
下一批如果继续 TimecoinUI，先重新审查获得/消耗/不足动画 runner；不要同批改数值来源、信号协议和多个动画收尾状态。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
覆盖检查通过：164 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 160 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-10 TimecoinUI 全局时间币查找 bridge 拆分

### 读取与轮廓

本批处理 `scene/in_scene/timecoin_ui.gd`。开工前确认工作区干净，仓库内仍没有实体 `AGENTS.md`，因此遵守当前对话中用户贴出的 AGENTS 约束。随后读取固定文档、模块总说明和 HexMap 手册，并用 `rg` 输出 `timecoin_ui.gd` 的函数、变量和导出参数轮廓。补充搜索确认 `GlobalTimecoin` 的主要接口是 `timecoin_updated`、`timecoin_insufficient` 和 `get_timecoins()`，本批不改变这些协议。

### 当前职责

`timecoin_ui.gd` 仍是时间币 UI 的 composition root，负责校验沙漏图标和数值标签、连接全局时间币信号、刷新数量文本、播放获得/消耗/不足动画、维护活跃 tween 队列、重置视觉状态，以及控制沙漏 shader 的震动参数。

### 耦合点

```text
_get_timecoin_singleton() 同时包含 Autoload、父级、root 子节点和 current_scene 递归查找，是清晰的跨节点 bridge 边界。
_connect_global_signals() 同时连接 GlobalTimecoin 信号并初始同步数值，本批不拆，保持调用旧 _get_timecoin_singleton()。
_play_gain_animation()、_play_consume_animation() 和 _play_warning_animation() 牵动 tween、位置、缩放、颜色和动画收尾，本批不拆。
_cleanup_active_tweens()、_reset_to_original_state() 和 shader 控制牵动动画状态，后续如拆必须单独审查。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TimecoinGlobalBridge.gd` | `_get_timecoin_singleton()` 和 `_find_node_with_script()` | 只查找节点，不连接信号，不改 UI 或时间币数值 | 执行 |
| 2 | 获得/消耗/不足动画 runner | `_play_gain_animation()`、`_play_consume_animation()`、`_play_warning_animation()` | 纯视觉但参数和收尾较多，应另开小批 | 暂缓 |
| 3 | tween 状态清理 controller | `_cleanup_active_tweens()`、`_reset_to_original_state()`、`_remove_tween_from_active()` | 与所有动画共享状态，需先梳理输入输出 | 暂缓 |
| 4 | 沙漏 shader controller | `_initialize_shader_material()`、`start_hourglass_shake()`、`stop_hourglass_shake()` 等 | 纯 shader 参数，适合作为后续小批 | 暂缓 |

### 本批风险面

本批只处理一个风险面：TimecoinUI 的 `GlobalTimecoin` 查找。

涉及的 4 个小风险点：

```text
新增 TimecoinGlobalBridge.gd，保留 Autoload、父级、root 子节点和 current_scene 递归兜底顺序。
timecoin_ui.gd 新增 preload 与 _get_timecoin_global_bridge() 缓存 getter。
旧 _get_timecoin_singleton() 入口继续存在，只转发给 bridge 并保留找不到节点时的 warning。
删除本批造成的 _find_node_with_script() 孤儿函数；递归查找移入 bridge。
```

不触碰：

```text
GlobalTimecoin 信号连接协议。
get_timecoins() 数值来源。
获得/消耗/不足动画。
tween 冲突处理。
沙漏 shader 控制。
```

### 实现结果

```text
scene/in_scene/timecoin_ui_modules/bridges/TimecoinGlobalBridge.gd
scene/in_scene/timecoin_ui.gd
```

`TimecoinGlobalBridge.gd` 是新的 RefCounted bridge，中文职责注释说明它只负责查找 `GlobalTimecoin`，不连接信号、不读写时间币数值、不刷新 UI、不播放动画。`timecoin_ui.gd::_get_timecoin_singleton()` 继续作为旧入口存在，`_connect_global_signals()` 和 `_refresh_display_from_global()` 的调用路径没有改变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 159 个脚本模块和 4 个默认 Resource 文件。
timecoin_ui.gd 当前约 484 行；本批只缩小全局节点查找边界，不追求压行数。
下一批如果继续 TimecoinUI，先重新审查动画 runner 或沙漏 shader controller；不要同批改数值来源、信号协议和 UI 动画。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
覆盖检查通过：163 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 159 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-10 CustomCard 手牌容器查找 bridge 扩展

### 读取与轮廓

本批继续处理 `custom_card.gd`。开工前先确认工作区干净，仓库内仍没有实体 `AGENTS.md`，因此遵守当前对话中用户贴出的 AGENTS 约束：先审查、保持简单、只做可验证的小步。随后重新读取固定文档，并用 `rg` 输出了 `custom_card.gd` 与既有 `custom_card_modules/` 的函数、变量、模块轮廓。补充检查确认 `Hand` 来自 `addons/card-framework/hand.gd` 的 `class_name Hand`，现有 `card_container is Hand` 判断可以安全保留语义。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存和恢复卡牌视觉状态、处理选中/取消选中、请求 tooltip、读取并写回卡牌数据、把卡牌交给拖拽控制器，以及在回手牌时编排 `Hand/Cards` 重挂和扇形布局刷新。

### 耦合点

```text
_get_player_hand_container() 同时处理当前 card_container 优先和 MainBoard.player_hand 兜底查找；这是节点查找边界，可以继续收口到 CustomCardNodeBridge。
_restore_hand_fan_layout() 牵动 Hand.cards_node 重挂、card_container 写回、has_card/add_card/update_card_ui 和全局坐标保持，本批不拆。
return_to_hand() 牵动 tween、fallback 坐标返回和 Hand 布局恢复，本批不拆。
toggle_selection() / force_deselect() 牵动 CardManager、Hand、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不碰。
_enter_state()、apply_stat_modifier() 与 play_card() 仍是多系统编排，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardNodeBridge.gd` 扩展手牌容器解析 | `_get_player_hand_container()` 中的当前容器优先与玩家手牌兜底 | 只集中查找策略，不重挂节点，不刷新布局 | 执行 |
| 2 | `_restore_hand_fan_layout()` | Hand/Cards 重挂和扇形布局刷新 | 同时修改节点树和 Hand 内部状态，暂不拆 | 暂缓 |
| 3 | `_is_another_card_selected()` | CardManager 当前选中查询 | 可作为后续查询小边界，但会靠近选中流程 | 暂缓 |
| 4 | `return_to_hand()` | 回手牌动画和 fallback 收尾 | 牵动视觉、布局和容器状态 | 不拆 |
| 5 | 出牌交接 | `play_card()` / `_play_no_target_timeline_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只处理一个风险面：CustomCard 的玩家手牌容器查找。

涉及的 3 个小风险点：

```text
CustomCardNodeBridge.gd 增加 get_player_hand_container(card_container)，保留 card_container is Hand 优先、否则查找 MainBoard.player_hand 的旧语义。
custom_card.gd 保留旧 _get_player_hand_container() 入口，只改为转发给 bridge 并 cast 为 Hand。
不改变 _restore_hand_fan_layout() 的节点重挂、card_container 写回、has_card/add_card/update_card_ui 调用顺序。
```

不触碰：

```text
Hand 扇形布局刷新规则。
return_to_hand() 的动画和 fallback。
CardManager 选中状态。
tooltip 显隐请求。
DragShapeController.start_dragging() 调用协议。
```

### 实现结果

```text
scene/card/custom_card_modules/bridges/CustomCardNodeBridge.gd
scene/card/custom_card.gd
```

`CustomCardNodeBridge.gd` 沿用既有职责注释，没有新增模块；它现在额外提供 `get_player_hand_container(card_container)`，只负责解析当前手牌容器或兜底玩家手牌。`custom_card.gd::_get_player_hand_container()` 仍作为旧入口存在，回手牌真正的节点重挂和布局刷新仍留在 `_restore_hand_fan_layout()` 中。

### 当前优化进度与下一步

```text
已拆模块统计保持为 158 个脚本模块和 4 个默认 Resource 文件。
CustomCard 仍为 8 个 custom_card_modules 脚本：3 个 bridges、3 个 rules 和 2 个 presenters。
custom_card.gd 当前约 569 行；本批继续缩小节点查找边界，不追求压行数。
下一批如果继续 custom_card.gd，仍需先重新审查剩余函数；不要硬拆 _enter_state()、toggle_selection()、force_deselect()、apply_stat_modifier()、return_to_hand() 或 play_card()。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
覆盖检查通过：162 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 158 个，默认 Resource 文件为 4 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现关键脚本错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard 描述解析规则拆分

### 读取与轮廓

本批继续处理 `custom_card.gd`，先确认工作区干净，再读取固定文档和 Godot 脚本/场景组织相关规则。仓库内仍未发现 `AGENTS.md` 文件，本批遵守当前对话中用户贴出的 AGENTS 约束。随后使用 `rg` 输出了目标文件和既有 CustomCard 模块轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/bridges/CustomCardNodeBridge.gd
scene/card/custom_card_modules/bridges/CustomCardTooltipBridge.gd
scene/card/custom_card_modules/bridges/CustomCardMapConditionalEffectBridge.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
```

上一批已经把 CustomCard 的跨节点查找拆到 `CustomCardNodeBridge.gd`。本批重新审查后，`custom_card.gd` 里仍有一个清晰的纯规则边界：`get_parsed_description()` 中的动态数值替换、关键词高亮和图标替换。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及编排 hover/holding 状态进入。

### 耦合点

```text
get_parsed_description() 同时产出 BBCode 描述和 active_keywords；共享 CardTooltipPresenter 依赖它刷新 active_keywords，本批保留旧入口写回协议。
apply_stat_modifier() 修改 current_stats、播放反馈 tween，并在选中/按压时请求 tooltip 刷新，本批不拆。
_enter_state() 同时适配父类 DraggableState、tween、shader、tooltip 和按压状态，本批不拆。
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不碰。
play_card() 牵动 DragShapeController 和 fallback 即时结算，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardDescriptionParser.gd` | `get_parsed_description()` 中的数值占位符、关键词、图标解析 | 纯文本到 BBCode/关键词列表，不查场景树，不改卡牌状态 | 执行 |
| 2 | `apply_stat_modifier()` | 数值修改、反馈 tween、tooltip 刷新 | 同时修改状态、动画和 UI 刷新，暂不拆 | 暂缓 |
| 3 | `_enter_state()` 视觉状态编排 | hover/holding/idle 分支 | 同时牵动父类状态、tween、shader、tooltip 和 is_pressed | 暂缓 |
| 4 | 选中/取消选中流程 | `toggle_selection()` / `force_deselect()` | 牵动 CardManager、Hand、地图条件效果和 clear 卡 | 暂缓 |
| 5 | 出牌交接 | `play_card()` / `_play_no_target_timeline_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只拆一个风险面：CustomCard 描述文本解析。

涉及的 4 个小风险点：

```text
新增 CustomCardDescriptionParser.gd，只把原始描述、基础/当前数值、关键词库和图标表解析成 BBCode 文本和关键词列表。
custom_card.gd 保留旧 get_parsed_description() 入口，继续写回 active_keywords。
不改变 CardTooltipPresenter 调用 get_parsed_description() 后再读取 active_keywords 的协议。
清理本批造成的 _object_has_property() 孤儿函数；它在 custom_card.gd 中已无其他用途。
```

不触碰：

```text
tooltip 显隐请求。
current_stats 的修改逻辑。
CardManager 选中状态。
Hand 扇形布局刷新规则。
DragShapeController.start_dragging() 调用协议。
```

### 实现结果

```text
scene/card/custom_card_modules/rules/CustomCardDescriptionParser.gd
```

职责：

```text
CustomCardDescriptionParser 只负责把卡牌原始描述解析为 BBCode 文本和关键词列表。
它不负责请求 tooltip、不读取或修改卡牌节点状态，也不处理选中、拖拽或出牌流程。
```

`custom_card.gd` 新增 parser preload 和 `_get_description_parser()` 缓存 getter。旧 `get_parsed_description()` 入口继续存在，并把 parser 返回的关键词列表写回 `active_keywords`；共享 `CardTooltipPresenter` 的描述和关键词读取协议没有变化。

### 当前优化进度与下一步

```text
已拆模块统计更新为 158 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 8 个 custom_card_modules 脚本：3 个 bridges、3 个 rules 模块和 2 个 presenters 模块。
custom_card.gd 当前约 572 行；本批继续缩小纯规则职责边界，不追求压行数。
下一批如果继续 custom_card.gd，先重新审查剩余函数；不要为了降行数硬拆 _enter_state()、toggle_selection()、force_deselect()、apply_stat_modifier()、return_to_hand() 或 play_card()。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现关键脚本错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
覆盖率检查通过：162 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 158 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard 节点查找 bridge 拆分

### 读取与轮廓

本批继续处理 `custom_card.gd`，先确认工作区干净，再读取固定文档和 Godot 脚本/场景组织相关规则。仓库内仍未发现 `AGENTS.md` 文件，本批遵守当前对话中用户贴出的 AGENTS 约束。随后使用 `rg` 输出了目标文件和既有 CustomCard 模块轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
scene/card/custom_card_modules/bridges/CustomCardTooltipBridge.gd
scene/card/custom_card_modules/bridges/CustomCardMapConditionalEffectBridge.gd
```

上一批已经把地图条件效果刷新查找拆到 `CustomCardMapConditionalEffectBridge.gd`。本批重新审查后，`custom_card.gd` 里仍有一个清晰的低风险查找边界：`MainBoard`、`CardManager`、玩家手牌和 `DragShapeController` 的节点定位散在旧入口与 `play_card()` 中。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及编排 hover/holding 状态进入。

### 耦合点

```text
_enter_state() 同时适配父类 DraggableState、tween、shader、tooltip 和按压状态，本批不拆。
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不改变流程。
return_to_hand() 和 _restore_hand_fan_layout() 牵动 Hand/Cards 节点重挂和扇形布局刷新，本批只把 Hand 查找转发出去。
play_card() 负责取消选中、查找 DragShapeController、调用 start_dragging 或 fallback，即使抽查找也继续留在主脚本编排。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardNodeBridge.gd` | `_get_main_board()`、`get_card_manager()`、`_find_player_hand()` 和 `play_card()` 中的拖拽控制器查找 | 只集中路径/群组查找，不改状态和调用顺序 | 执行 |
| 2 | `_enter_state()` 视觉状态编排 | hover/holding/idle 分支 | 同时牵动父类状态、tween、shader、tooltip 和 is_pressed | 暂缓 |
| 3 | 选中/取消选中流程 | `toggle_selection()` / `force_deselect()` | 牵动 CardManager、Hand、地图条件效果和 clear 卡 | 暂缓 |
| 4 | 回手牌流程 | `return_to_hand()` / `_restore_hand_fan_layout()` | 牵动父节点重挂、Hand 布局和兜底 tween | 暂缓 |
| 5 | 出牌交接 | `play_card()` / `_play_no_target_timeline_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆流程 |

### 本批风险面

本批只拆一个风险面：CustomCard 需要的跨节点查找。

涉及的 4 个小风险点：

```text
新增 CustomCardNodeBridge.gd，只负责 MainBoard、CardManager、玩家手牌和 DragShapeController 查找。
custom_card.gd 保留旧 _get_main_board()、get_card_manager() 和 _find_player_hand() 入口，只转发给 bridge。
play_card(target_hex) 仍负责取消选中、调用 start_dragging 或 fallback，只把 DragShapeController 查找交给 bridge。
不修改 toggle_selection()、force_deselect()、return_to_hand() 和 _enter_state() 的编排顺序。
```

不触碰：

```text
CardManager 选中规则。
Hand 扇形布局刷新规则。
clear 卡自动进入时间轴。
DragShapeController.start_dragging() 调用协议。
出牌 fallback 和即时结算。
```

### 实现结果

```text
scene/card/custom_card_modules/bridges/CustomCardNodeBridge.gd
```

职责：

```text
CustomCardNodeBridge 只负责 CustomCard 需要的跨节点查找。
它不负责判断卡牌状态、不修改手牌或地图数据，也不处理选中、拖拽或出牌流程。
```

`custom_card.gd` 新增 bridge preload 和 `_get_node_bridge()` 缓存 getter。旧 `_get_main_board()`、`get_card_manager()`、`_find_player_hand()` 继续存在并转发给 bridge；`play_card(target_hex)` 仍保留出牌交接编排，只通过 bridge 查找 `DragShapeController`。

### 当前优化进度与下一步

```text
已拆模块统计更新为 157 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 7 个 custom_card_modules 脚本：3 个 bridges、2 个 rules 模块和 2 个 presenters 模块。
custom_card.gd 当前约 599 行；本批继续缩小跨节点查找职责边界，不追求压行数。
下一批如果继续 custom_card.gd，先重新审查剩余函数；不要为了降行数硬拆 _enter_state()、toggle_selection()、force_deselect()、return_to_hand() 或 play_card()。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现关键脚本错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
覆盖率检查通过：161 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 157 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard 地图条件效果 bridge 拆分

### 读取与轮廓

本批继续处理 `custom_card.gd`，先确认工作区状态，再读取固定文档。仓库内仍未发现 `AGENTS.md` 文件，本批遵守当前对话中用户贴出的 AGENTS 约束。随后使用 `rg` 输出了目标文件和既有 CustomCard 模块轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
scene/card/custom_card_modules/bridges/CustomCardTooltipBridge.gd
```

上一批已经把 tooltip MainBoard 查找与显隐转发拆到 `CustomCardTooltipBridge.gd`。本批重新审查后，`custom_card.gd` 中剩余最小且清晰的低风险边界是 `_update_map_conditional_effects()` 里的 `MainBoard -> HexMap` 查找和 `update_all_stack_conditional_effects()` 转发。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及编排 hover/holding 状态进入。

### 耦合点

```text
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批只拆其中的地图刷新查找，不改变调用时机。
_enter_state() 同时适配父类 DraggableState、tween、shader、tooltip 和按压状态，本批不拆。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，本批不碰。
get_parsed_description() 读取 GlobalDB 和 active_keywords 生成 tooltip 内容，后续如处理需单独审查，不与地图刷新同批。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardMapConditionalEffectBridge.gd` | `_update_map_conditional_effects()` 的 MainBoard 到 HexMap 查找与刷新转发 | 只查找节点并调用既有刷新方法，不判断卡牌状态，不写地图数据 | 执行 |
| 2 | `_enter_state()` 视觉状态编排 | hover/holding/idle 分支 | 同时牵动父类状态、tween、shader、tooltip 和 is_pressed | 暂缓 |
| 3 | 选中/取消选中流程 | `toggle_selection()` / `force_deselect()` | 牵动 CardManager、Hand、地图条件效果和 clear 卡 | 暂缓 |
| 4 | 出牌交接 | `play_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |
| 5 | tooltip 内容解析 | `get_parsed_description()` | 读取 GlobalDB、关键词和文本替换，影响显示语义 | 暂缓 |

### 本批风险面

本批只拆一个风险面：CustomCard 请求刷新地图条件效果时的 `HexMap` 查找和方法转发。

涉及的 3 个小风险点：

```text
新增 CustomCardMapConditionalEffectBridge.gd，只从 MainBoard 查找 HexMap 并调用 update_all_stack_conditional_effects()。
custom_card.gd 保留旧 _update_map_conditional_effects() 入口，只转发 _get_main_board()。
不修改 toggle_selection() / force_deselect() 中调用 _update_map_conditional_effects() 的顺序。
```

不触碰：

```text
条件效果规则计算。
HexMap 地图数据写入。
CardManager 选中状态。
clear 卡自动进入时间轴。
DragShapeController 交接和出牌 fallback。
```

### 实现结果

```text
scene/card/custom_card_modules/bridges/CustomCardMapConditionalEffectBridge.gd
```

职责：

```text
CustomCardMapConditionalEffectBridge 只负责从 MainBoard 查找 HexMap 并请求刷新地块条件效果。
它不负责判断卡牌状态、不计算条件效果、不修改地图数据，也不处理选中、拖拽或出牌流程。
```

`custom_card.gd` 新增 bridge preload 和 `_get_map_conditional_effect_bridge()` 缓存 getter。旧 `_update_map_conditional_effects()` 入口继续存在，并只把 `_get_main_board()` 的结果转发给 bridge；选中和取消选中里的调用时机没有改变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 156 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 6 个 custom_card_modules 脚本：2 个 bridges、2 个 rules 模块和 2 个 presenters 模块。
custom_card.gd 当前约 601 行；本批继续缩小跨节点查找职责边界，不追求压行数。
下一批如果继续 custom_card.gd，先重新审查剩余函数；不要为了降行数硬拆 _enter_state()、toggle_selection()、force_deselect() 或 play_card()。
```

### 回归检查

```text
git diff --check 通过，仅有既有换行风格提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现关键脚本错误。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现关键脚本错误。
覆盖率检查通过：160 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 156 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard tooltip bridge 拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
```

上一批已经把 hover shader 参数写入拆到 `CustomCardHoverShaderPresenter.gd`。本批重新审查后，`custom_card.gd` 中最小的剩余低风险边界是 `_request_tooltip(should_show)` 里的 `MainBoard` 查找和 `show_tooltip` / `hide_tooltip` 转发。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及编排 hover/holding 状态进入。

### 耦合点

```text
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不碰。
_enter_state() 同时适配父类 DraggableState、tween、shader、tooltip 和按压状态，本批不整段拆。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，本批不碰。
_request_tooltip() 被 hover、holding、数值变化和视觉重置调用；本批只移动 MainBoard 查找与转发，不改变触发时机。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardTooltipBridge.gd` | `_request_tooltip()` 的 MainBoard 查找与显隐转发 | 只查找节点并调用现有 tooltip 方法，不改状态 | 执行 |
| 2 | `_enter_state()` 视觉状态编排 | hover/holding/idle 分支 | 同时牵动父类状态、tween、shader、tooltip 和 is_pressed | 暂缓 |
| 3 | 选中/取消选中流程 | `toggle_selection()` / `force_deselect()` | 牵动 CardManager、Hand、地图条件效果和 clear 卡 | 暂缓 |
| 4 | 出牌交接 | `play_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只拆一个风险面：CustomCard tooltip 的 MainBoard 查找和显隐转发。

涉及的 3 个小风险点：

```text
新增 CustomCardTooltipBridge.gd，只查找 MainBoard 并调用 show_tooltip(card) / hide_tooltip(card)。
custom_card.gd 保留旧 _request_tooltip(should_show) 入口，只转发 self 和 should_show。
不修改 _enter_state()、apply_stat_modifier()、force_reset_visuals() 和 _restore_normal_visuals() 中调用 _request_tooltip() 的时机。
```

不触碰：

```text
tooltip 内容生成。
CardManager 选中状态。
手牌布局恢复。
DragShapeController 交接和出牌 fallback。
```

### 实现结果

```text
scene/card/custom_card_modules/bridges/CustomCardTooltipBridge.gd
```

职责：

```text
CustomCardTooltipBridge 只负责为 CustomCard 查找 MainBoard 并转发 tooltip 显隐请求。
它不负责生成 tooltip 内容、不判断卡牌状态、不创建 tween，也不处理选中、拖拽或出牌流程。
```

`custom_card.gd` 新增 bridge preload 和 `_get_tooltip_bridge()` 缓存 getter。旧 `_request_tooltip(should_show)` 入口继续存在，并只把 `self` 与 `should_show` 转发给 bridge。hover、holding、数值变化和视觉重置中的调用顺序没有改变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 155 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 5 个 custom_card_modules 脚本：1 个 bridge、2 个 rules 模块和 2 个 presenters 模块。
custom_card.gd 当前约 597 行；本批继续缩小跨节点查找职责边界，不追求压行数。
下一批如果继续 custom_card.gd，先重新审查剩余函数；不要为了降行数硬拆 _enter_state()、toggle_selection()、force_deselect() 或 play_card()。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/card/custom_card.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
覆盖率检查通过：159 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 155 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard hover shader presenter 拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
```

上一批已经把选中状态下的逐帧倾斜和阴影跟随拆到 `CustomCardSelectedVisualPresenter.gd`。本批重新审查后，`custom_card.gd` 中更小的剩余表现边界是 `_set_shader(active)`，它只把 `is_hovered` 参数写到卡牌自身材质或正面贴图材质。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及编排 hover/holding 状态进入。

### 耦合点

```text
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不碰。
_enter_state() 同时适配父类 DraggableState、tween、shader、tooltip 和按压状态，本批不整段拆。
_request_tooltip() 查找 MainBoard 并调用 show_tooltip/hide_tooltip，影响 hover、holding、数值变化和视觉重置，本批暂缓。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，本批不碰。
_set_shader(active) 本身只写 shader 参数，可拆成纯表现 presenter。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardHoverShaderPresenter.gd` | `_set_shader(active)` | 只写 shader 参数，不查场景树，不改状态 | 执行 |
| 2 | Tooltip/MainBoard bridge | `_request_tooltip()` / `_get_main_board()` | 查找集中化可行，但影响 hover/选中/数值变化路径 | 暂缓 |
| 3 | `_enter_state()` 视觉状态编排 | hover/holding/idle 分支 | 同时牵动父类状态、tween、tooltip 和 is_pressed | 暂缓 |
| 4 | 出牌交接 | `play_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只拆一个风险面：卡牌 hover shader 参数写入。

涉及的 3 个小风险点：

```text
新增 CustomCardHoverShaderPresenter.gd，只处理 is_hovered shader 参数。
custom_card.gd 保留旧 _set_shader(active) 入口，只转发 material、front_face_texture 和 active。
不修改 _enter_state() 的状态判断、tween、tooltip 调用，也不修改 force_reset_visuals() 的恢复顺序。
```

不触碰：

```text
MainBoard tooltip 显隐。
CardManager 选中状态。
手牌布局恢复。
DragShapeController 交接和出牌 fallback。
```

### 实现结果

```text
scene/card/custom_card_modules/presenters/CustomCardHoverShaderPresenter.gd
```

职责：

```text
CustomCardHoverShaderPresenter 只负责切换卡牌 hover shader 参数。
它不负责判断卡牌状态、不请求 tooltip、不创建 tween，也不处理选中、拖拽或出牌流程。
```

`custom_card.gd` 新增 presenter preload 和 `_get_hover_shader_presenter()` 缓存 getter。旧 `_set_shader(active)` 入口继续存在，并只把 `material`、`front_face_texture` 和 `active` 转发给 presenter。`_enter_state()`、`force_reset_visuals()` 和 `_restore_normal_visuals()` 的调用顺序没有改变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 154 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 4 个 custom_card_modules 脚本：2 个 rules 模块和 2 个 presenters 模块。
custom_card.gd 当前约 594 行；本批继续缩小表现职责边界，不追求压行数。
下一批如果继续 custom_card.gd，优先评估 tooltip 查找桥接；不要同批改 play_card()、CardManager 选中状态、MainBoard tooltip 行为、EffectProcessor 和敌人意图解析。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/card/custom_card.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
覆盖率检查通过：158 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 154 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard 选中跟随视觉 presenter 拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card.tscn
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
```

轮廓确认 `custom_card.gd` 当前仍包含卡牌状态、选中/取消选中、tooltip、数据解析、出牌交接和逐帧选中视觉。已拆出的两个 rules 模块分别承接 `shape` 和 `effect_range` 的纯解析。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及保留选中态视觉的旧入口。

### 耦合点

```text
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip、地图条件效果和 clear 卡自动进入时间轴，本批不碰。
_enter_state() 同时适配父类 DraggableState、shader、tooltip 和按压状态，本批不碰。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，本批不碰。
_process(delta) 中的选中态跟随只读视觉参数和鼠标局部坐标，只写 rotation 与 shadow.position，适合拆成纯表现 presenter。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardSelectedVisualPresenter.gd` | `_process(delta)` 的倾斜和阴影跟随 | 纯逐帧表现，不查场景树，不改选中状态 | 执行 |
| 2 | Tooltip/MainBoard bridge | `_request_tooltip()` / `_get_main_board()` | 查找集中化可行，但影响 hover/选中体验 | 暂缓 |
| 3 | hover shader 小边界 | `_set_shader()` 与 `_enter_state()` 的调用 | 表现可拆，但 `_enter_state()` 仍耦合父类状态 | 暂缓 |
| 4 | 出牌交接 | `play_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只拆一个风险面：卡牌选中状态下的鼠标跟随视觉。

涉及的 3 个小风险点：

```text
新增 CustomCardSelectedVisualPresenter.gd，只计算并写入卡牌 rotation 与 ShadowLayer position。
custom_card.gd 保留旧 _process(delta) 入口，只在 is_selected 为真时转发视觉参数。
不修改 toggle_selection()、force_deselect()、_enter_state()、_request_tooltip()、play_card() 或 DragShapeController。
```

不触碰：

```text
CardManager 选中状态。
MainBoard tooltip 显隐。
手牌扇形布局恢复。
clear 卡自动进入时间轴和正常出牌链路。
```

### 实现结果

```text
scene/card/custom_card_modules/presenters/CustomCardSelectedVisualPresenter.gd
```

职责：

```text
CustomCardSelectedVisualPresenter 只负责卡牌选中状态下的倾斜和阴影跟随表现。
它不负责修改选中状态、请求 tooltip、处理出牌/拖拽，也不调整手牌布局。
```

`custom_card.gd` 新增 presenter preload 和 `_get_selected_visual_presenter()` 缓存 getter。旧 `_process(delta)` 继续先判断 `is_selected`，再把 `self`、`shadow`、`delta` 和选中态视觉参数转发给 presenter，保持导出参数和旧视觉行为不变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 153 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 3 个 custom_card_modules 脚本：2 个 rules 模块和 1 个 presenters 模块。
custom_card.gd 当前约 589 行；本批重点是缩小职责边界，不追求压行数。
下一批如果继续 custom_card.gd，优先评估 tooltip 查找桥接或 hover shader 小边界；不要同批改 play_card()、CardManager 选中状态、MainBoard tooltip 行为、EffectProcessor 和敌人意图解析。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/card/custom_card.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
覆盖率检查通过：157 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 153 个，默认 Resource 文件为 4 个。
临时 Godot 日志已清理。
```

## 2026-06-09 CustomCard 效果范围解析拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
scene/in_scene/hex_map_modules/rules/HexTargetRules.gd
scene/in_scene/effect/effect_processor.gd
scene/in_scene/enermy/enemy_intent_resolver.gd
```

上一批已经把普通时间轴 `shape` 解析拆到 `CustomCardTimelineShapeParser.gd`。本批重新审查后，`custom_card.gd` 里最清晰的剩余规则边界是 `_parse_hex_effect_range()`，它只把 `card_info.effect_range` 解析为 `effect_range_offsets`，再由 `get_absolute_effect_range(center_coord)` 提供给地图 hover 规则读取。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、把解析结果写回卡牌成员、把卡牌移交 DragShapeController，以及维护 selected 状态下的倾斜和阴影跟随视觉。

### 耦合点

```text
_parse_hex_effect_range() 本身是纯解析，但结果被 get_absolute_effect_range() 和 HexTargetRules.get_effect_range_stacks() 读取，用于地图 AOE hover。
EffectProcessor.gd 和 enemy_intent_resolver.gd 里也有相似 effect_range 解析逻辑，但分别服务运行时结算和敌人意图，本批不跨系统统一。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，仍不碰。
toggle_selection() / force_deselect() 牵动 CardManager、手牌布局、tooltip 和地图条件效果，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardEffectRangeParser.gd` | `_parse_hex_effect_range()` | 纯 Variant 到六边形偏移解析，不查场景树，不改目标规则 | 执行 |
| 2 | 选中视觉 presenter | `_process()` 的倾斜和阴影跟随 | 纯表现可拆，但牵动 selected 状态 | 暂缓 |
| 3 | Tooltip/MainBoard bridge | `_request_tooltip()` / `_get_main_board()` | 查找集中化可行，但影响 hover/选中体验 | 暂缓 |
| 4 | 出牌交接 | `play_card()` | 牵动 DragShapeController 和 fallback 即时结算 | 不拆 |

### 本批风险面

本批只拆一个风险面：卡牌六边形效果范围解析。

涉及的 3 个小风险点：

```text
新增 CustomCardEffectRangeParser.gd，只解析 effect_range 数据，不碰地图节点和效果执行。
custom_card.gd 保留旧 _parse_hex_effect_range() 入口，只把解析委托出去并写回 effect_range_offsets。
不修改 get_absolute_effect_range()、HexTargetRules、EffectProcessor、enemy_intent_resolver 或 play_card()。
```

不触碰：

```text
CardManager 选中状态、MainBoard tooltip、DragShapeController 交接。
EffectProcessor 运行时结算范围解析。
敌人意图 effect_range 解析。
```

### 实现结果

```text
scene/card/custom_card_modules/rules/CustomCardEffectRangeParser.gd
```

职责：

```text
CustomCardEffectRangeParser 只负责解析卡牌六边形效果范围偏移。
它不读取地图节点，不执行卡牌效果，也不决定目标是否合法或如何高亮。
```

`custom_card.gd` 新增解析器 preload 和 `_get_effect_range_parser()` 缓存 getter。旧 `_parse_hex_effect_range()` 继续写回 `effect_range_offsets`，保持 `get_absolute_effect_range()` 和 `HexTargetRules.get_effect_range_stacks()` 的读取路径不变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 152 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 2 个 custom_card_modules 规则脚本。
custom_card.gd 从约 597 行降到约 588 行。
下一批如果继续 custom_card.gd，优先评估选中状态视觉 presenter 或 tooltip 查找桥接；不要同批改 play_card()、CardManager 选中状态、MainBoard tooltip、EffectProcessor 和敌人意图解析。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/card/custom_card.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有退出资源占用 warning。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
覆盖率检查通过：156 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 152 个，默认 Resource 文件为 4 个。
```

## 2026-06-09 CustomCard 时间轴形状解析拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/card/custom_card.gd
scene/card/custom_card.tscn
scene/in_scene/drag_modules/rules/DragCardShapeResolver.gd
scene/in_scene/timeline/TimelineClearEffect.gd
scene/in_scene/hex_map_modules/rules/HexTargetRules.gd
```

`custom_card.gd` 当前约 654 行。外部主要通过 `timeline_shape_coords` 读取普通卡牌时间轴形状，通过 `get_absolute_effect_range(center_coord)` 读取六边形影响范围，通过 `play_card(target_hex)` 把选中卡牌交给 DragShapeController。

### 当前职责

`custom_card.gd` 仍是卡牌节点的 composition root，负责适配插件 `Card` 父类状态、保存手牌原始状态、处理选中/取消选中、请求 tooltip、读取 `card_info`、解析时间轴 shape、解析六边形效果范围、把卡牌移交 DragShapeController，以及维护 selected 状态下的倾斜和阴影跟随视觉。

### 耦合点

```text
toggle_selection() / force_deselect() 同时改 CardManager 选中状态、tween、手牌布局、地图条件效果和 clear 卡牌自动进入时间轴。
_enter_state() 与 _process() 同时处理父类 hover 状态、shader、tooltip、倾斜和阴影跟随。
_normalize_and_parse_shape() 是纯数据解析，写入 timeline_shape_coords / timeline_shape_size / timeline_shape_key，后续由 DragShapeController 读取。
play_card() 直接寻找 DragShapeController，失败时走 apply_effect_immediate()，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `CustomCardTimelineShapeParser.gd` | `_normalize_and_parse_shape()` | 纯 Variant 到坐标/尺寸/key 解析，不查场景树，不改出牌流程 | 执行 |
| 2 | `CustomCardEffectRangeParser.gd` | `_parse_hex_effect_range()` | 纯 effect_range 到 offset 数组，但 HexTargetRules 依赖结果 | 暂缓 |
| 3 | 选中视觉 presenter | `toggle_selection()`、`force_deselect()`、`_process()` 部分 | 牵动 CardManager、Hand 布局和父类状态 | 暂缓 |
| 4 | Tooltip/MainBoard bridge | `_get_main_board()`、`_request_tooltip()` | 查找集中化可行 | 暂缓 |

### 本批风险面

本批只拆一个风险面：普通卡牌时间轴形状解析。

涉及的 3 个小风险点：

```text
新增 CustomCardTimelineShapeParser.gd，只解析 shape 数据，不碰节点和放置规则。
custom_card.gd 保留旧 _normalize_and_parse_shape() 入口，只把解析委托出去并写回旧成员。
不修改 card_info["shape"]，不改 DragShapeController、TimelineClearEffect 或 play_card() 流程。
```

不触碰：

```text
CardManager 选中状态、MainBoard tooltip、DragShapeController 交接。
clear 卡牌通过 TimelineClearEffect 的特殊 shape 解析。
六边形 effect_range 解析和 HexTargetRules 读取路径。
```

### 实现结果

```text
scene/card/custom_card_modules/rules/CustomCardTimelineShapeParser.gd
```

职责：

```text
CustomCardTimelineShapeParser 只负责把卡牌时间轴形状数据解析成坐标、尺寸和标准 key。
它不读取场景树，不修改卡牌节点，也不决定卡牌能否放置或如何打出。
```

`custom_card.gd` 新增解析器 preload 和 `_get_timeline_shape_parser()` 缓存 getter。旧 `_normalize_and_parse_shape()` 继续写回 `timeline_shape_coords`、`timeline_shape_size` 和 `timeline_shape_key`，保持 DragShapeController 的读取路径不变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 151 个脚本模块和 4 个默认 Resource 文件。
CustomCard 现在有 1 个 custom_card_modules 规则脚本。
custom_card.gd 从约 654 行降到约 597 行。
下一批如果继续 custom_card.gd，优先评估 _parse_hex_effect_range() 或选中状态视觉 presenter；不要同批改 play_card()、CardManager 选中状态、MainBoard tooltip 和 DragShapeController 交接。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/card/custom_card.gd 的既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有退出资源占用 warning。
Godot 加载 res://scene/card/custom_card.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
覆盖率检查通过：155 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 151 个，默认 Resource 文件为 4 个。
```

## 2026-06-09 TimelineUI 视觉配置 Resource 化

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/in_scene/timeline/timeline_ui.gd
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalAnimator.gd
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd
scene/in_scene/timeline/ui_modules/animation/TimelineBlockPlacementAnimator.gd
scene/in_scene/timeline/ui_modules/bridges/TimelineManagerLocator.gd
scene/in_scene/timeline/ui_modules/controllers/TimelineActionHoverStateController.gd
scene/in_scene/timeline/ui_modules/grid/TimelineGridBuilder.gd
scene/in_scene/timeline/ui_modules/grid/TimelineGridCellInteractionPresenter.gd
scene/in_scene/timeline/ui_modules/grid/TimelineGridPreviewPresenter.gd
scene/in_scene/timeline/ui_modules/layout/TimelineExpandVisualController.gd
scene/in_scene/timeline/ui_modules/layout/TimelineLayoutController.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionBlockPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionGeometryPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionShapeVisualPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd
```

同时检查 `scene/in_scene/in_scene.tscn`，确认当前场景只覆盖了 `TimelineUI.mask_color` 和 `mask_layer`。因此默认视觉资源必须写入这两个场景实际值，避免新资源覆盖旧视觉。

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager`，维护 `action_containers`、`hovered_action`、网格格子和敌方意图预览状态，决定行动容器何时创建、何时播放 intro、何时连接 hover 信号、何时触发移除动画和网格预览。

### 耦合点

```text
_on_action_placed() 同时处理行动容器创建、几何、方块视觉、overlay、hover 信号和 intro 动画，本批不继续硬拆。
slot_size、spacing、grid_width 和 grid_height 仍影响布局契约与 TimelineClearEffect / timeline_visualizer 等外部读取，本批不搬入 Resource。
纯视觉参数集中在网格颜色、展开动画、敌方意图 overlay、移除动画、整体轮廓和遮罩表现上，适合首批资源化。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TimelineVisualConfig` | 网格颜色、展开表现、敌方意图 overlay、移除动画、整体轮廓、遮罩表现 | 纯视觉静态参数，保留旧导出兜底 | 执行 |
| 2 | 行动块生成剩余编排 | `_on_action_placed()` | 牵动创建时机、intro 动画和 hover 信号 | 不拆 |
| 3 | 时间轴布局尺寸契约 | `slot_size`、`spacing`、`grid_width`、`grid_height` | 外部仍会直接读取，搬迁风险较高 | 暂缓 |
| 4 | TimelineManager 规则 | 敌方意图落点、排序、占用 | 数据规则核心 | 不碰 |

### 本批风险面

本批只拆一个风险面：TimelineUI 纯视觉静态参数 Resource 化。

涉及的 3 个小风险点：

```text
新增 TimelineVisualConfig.gd，只保存时间轴 UI 的纯视觉静态参数。
新增 default_timeline_visual_config.tres，并把当前场景已有 mask_color / mask_layer 覆盖值写入默认资源。
timeline_ui.gd 新增 visual_config 资源入口和少量 typed helper，优先读资源、保留旧导出字段兜底。
```

不触碰：

```text
TimelineManager 数据结构、敌人意图规则和行动块生成时机。
_on_action_placed() 的 action_containers 字典、hover 信号连接和 intro 动画触发。
slot_size、spacing、grid_width、grid_height 这类布局/外部契约字段。
```

### 实现结果

```text
scene/in_scene/timeline/resources/TimelineVisualConfig.gd
scene/in_scene/timeline/resources/default_timeline_visual_config.tres
```

职责：

```text
TimelineVisualConfig 只保存时间轴 UI 的纯视觉静态参数。
它不创建行动块，不读写 TimelineManager，也不决定拖拽放置或敌人意图规则。
```

`timeline_ui.gd` 新增 `visual_config` 导出资源和 `_get_visual_*()` helper。旧导出字段仍保留为兜底，避免未挂资源或资源缺字段时改变旧行为。`in_scene.tscn` 的 `TimelineUI` 节点挂上默认视觉资源。

### 当前优化进度与下一步

```text
已拆模块统计更新为 150 个脚本模块和 4 个默认 Resource 文件。
TimelineUI 现在有 14 个 ui_modules 脚本和 1 个视觉配置 Resource 脚本。
timeline_ui.gd 约 807 行；本批不是为了降行数，而是把静态视觉调参收口到资源。
下一批不建议继续硬拆 TimelineUI 行动块生成；可转向 tile.gd 的 timeline shape 解析、custom_card.gd 的形状解析或 timecoin_ui.gd 的全局查找/动画 runner。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/in_scene/timeline/timeline_ui.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
覆盖率检查通过：154 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 150 个，默认 Resource 文件为 4 个。
```

## 2026-06-08 TimelineUI 行动块视觉 presenter 拆分

### 读取与轮廓

本批接续前序半成品改动，先确认工作区已有未提交文件集中在 `timeline_ui.gd`、三个新增 TimelineUI presenter 和相关文档上。仓库内仍未发现 `AGENTS.md`，本批以当前对话中用户贴出的 AGENTS 约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/in_scene/timeline/timeline_ui.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionBlockPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionGeometryPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionShapeVisualPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd
scene/in_scene/timeline/ui_modules/layout/TimelineExpandVisualController.gd
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd
```

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager`，维护 `action_containers` 和当前 hover 状态，决定行动容器何时创建、何时播放 intro、何时连接 hover 信号、何时触发敌方意图预览和移除动画。

### 耦合点

```text
_on_action_placed() 仍是行动容器创建入口，会同时触发行动容器几何、整体形状视觉、单格 Panel、敌方 overlay、hover 信号和 intro 动画。
本批只把其中纯视觉创建细节拆出，不改变创建时机，不改变 TimelineManager 数据，也不改变 hover 信号连接位置。
TimelineEnemyIntentOverlayPresenter 只是增加 config 入口和预览状态写入封装，仍不决定何时进入预览。
TimelineExpandVisualController 和 TimelineActionRemovalGhostBuilder 只是承接前序遗留的缩放/运行时视觉清理小边界，本批不继续扩大它们。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 行动容器几何 presenter | `_on_action_placed()` 中 bounds、容器尺寸位置、方块局部位置 | 纯计算/写 Control 几何，不碰数据规则 | 执行 |
| 2 | 行动方块视觉 presenter | `_on_action_placed()` 中 Panel、StyleBox、EnemyIntentOverlay 子节点 | 只创建视觉节点，不连接 hover | 执行 |
| 3 | 整体形状视觉 presenter | `_get_local_shape_coords()`、`_add_action_shape_visual()` | 只挂 BACKPLATE / OUTLINE 视觉层 | 执行 |
| 4 | TimelineVisualConfig | 多个导出表现参数 | Resource 化另开小批，避免混合风险 | 暂缓 |

### 本批风险面

本批只拆一个风险面：时间轴行动块创建中的纯视觉 presenter。

涉及的 3 个小风险点：

```text
新增 TimelineActionGeometryPresenter.gd，集中计算行动形状边界、容器几何和方块局部位置。
新增 TimelineActionBlockPresenter.gd，集中创建单个 Panel、基础 StyleBox 和 EnemyIntentOverlay 子节点。
新增 TimelineActionShapeVisualPresenter.gd，集中归一化 shape 坐标并创建整体 BACKPLATE / OUTLINE 视觉层。
```

不触碰：

```text
TimelineManager 数据结构和行动放置规则。
_on_action_placed() 的入口时机、action_containers 字典、hover 信号连接和 intro 动画触发。
敌人意图落点规则、地图联动和局内 hover tooltip。
```

### 实现结果

```text
scene/in_scene/timeline/ui_modules/presenters/TimelineActionGeometryPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionBlockPresenter.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineActionShapeVisualPresenter.gd
```

职责：

```text
TimelineActionGeometryPresenter 只负责计算并应用时间轴行动容器的几何信息。
TimelineActionBlockPresenter 只负责创建单个时间轴行动方块的视觉节点。
TimelineActionShapeVisualPresenter 只负责时间轴行动整体形状视觉层。
```

`timeline_ui.gd` 新增三个 presenter preload 和缓存 getter。`_on_action_placed()` 仍保留旧的创建入口和信号连接，但把容器几何、Panel 创建和整体形状视觉层挂载委托给新模块。

### 当前优化进度与下一步

```text
已拆模块统计更新为 149 个脚本模块和 3 个默认 Resource 文件。
TimelineUI 现在有 14 个 ui_modules 脚本，新增 3 个 presenters：TimelineActionGeometryPresenter、TimelineActionBlockPresenter、TimelineActionShapeVisualPresenter。
timeline_ui.gd 从约 786 行降到约 755 行。
下一批如果继续 TimelineUI，只评估 TimelineVisualConfig 这类纯视觉配置；不要硬拆 _on_action_placed() 的剩余创建时机、intro 动画和信号连接。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/in_scene/timeline/timeline_ui.gd、TimelineExpandVisualController.gd、TimelineEnemyIntentOverlayPresenter.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖率检查通过：152 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 149 个，默认 Resource 文件为 3 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
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

## 奖励页第十三批商店 CardManager 定位复用记录

日期：2026-06-06

### 本批目标

本批只收口 `ShopManager.gd::_try_find_card_manager()` 中重复的 CardManager 查找路径和大量路径级调试输出。商店页面改为复用已有 `RewardCardManagerLocator.gd`，保留 `_try_find_card_manager()` 旧入口和找到后的 `deck_manager` 赋值，不改变商品生成、购买、价格、时代权重或 CardManager 注入方式。

目标函数范围：

```text
ShopManager.gd::_try_find_card_manager()
ShopManager.gd::_get_card_manager_locator()
```

当前触碰的数据和节点：

```text
CardManager
deck_manager
card_manager 元数据
当前场景
父节点链
CardManager 节点名回退
```

### 调整模块

```text
scene/in_scene/rewards/bridges/RewardCardManagerLocator.gd
scene/in_scene/rewards/ShopManager.gd
```

模块边界：

- `RewardCardManagerLocator.gd` 继续只负责奖励页面自动查找 CardManager。
- `ShopManager.gd` 只保留“找到后赋值和输出结果级日志”的页面职责。
- 本批不改 `ShopGlobalNodeFinder.gd`，也不改商店全局时代/时间币节点查找。

### 本批删除或收口的重复点

删除原因：

```text
ShopManager 原本单独维护一套 CardManager 查找路径，包括树根元数据、当前场景元数据、父节点链、节点名回退和遍历查找。
这些查找路径和 AcquireReward/RemoveReward 已使用的 RewardCardManagerLocator 重复。
现在 ShopManager 复用共享 locator，路径级日志收窄为找到/未找到和 card_factory 是否准备。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

ShopManager 的共享依赖定位已经和其他奖励页对齐，`bridges` 目录的职责更统一。商店页面的体积继续下降，路径级调试噪音减少，后续更容易看清真正的商店流程。

当前剩余高价值优化点是 ShopManager 的商品生成流程拆分，以及 `ShopDebugLogger` 是否继续接管价格/购买/依赖查找日志。CraftReward 仍是奖励页中最大的文件，后续可考虑状态流拆分。

### 下一步打算

下一批建议评估 `ShopManager.gd::_generate_shop_items()` 的商品位创建流程。更稳的拆法是只抽“商品位 UI 构建器”，把 DraftCard、价格 Label、VBoxContainer 和映射字典写入分开；不碰时代选卡和临时牌堆数据窃取。

## 奖励页第十四批商店商品位 UI 构建拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `ShopManager.gd::_generate_shop_items()` 中重复占位较大的商品位 UI 包装逻辑。商店仍在原循环中选择卡牌、实例化 DraftCard、执行数据窃取、计算价格、写入 `shop_cards` 和 `card_price_map`，本批只把已准备好的 `shop_card` 包装成 `VBoxContainer + price Label`。

目标函数范围：

```text
ShopManager.gd::_generate_shop_items()
ShopItemSlotPresenter.gd::build_slot(shop_card, price)
```

当前触碰的数据和节点：

```text
shop_card
VBoxContainer
Label
price
card_price_map 的 label / price / container 结构
```

### 新增模块

```text
scene/in_scene/rewards/presenters/ShopItemSlotPresenter.gd
```

模块边界：

- `ShopItemSlotPresenter.gd` 只负责把已准备好的商店 DraftCard 包装成商品位 UI。
- 它不选择卡牌，不读取卡牌数据，也不处理购买或价格消费。
- `ShopManager.gd` 继续负责把商品位加入 `shop_grid`、记录 `shop_cards`、写入 `card_price_map` 和连接购买点击信号。

### 本批删除或收口的重复点

删除原因：

```text
ShopManager 原本在商品生成循环中直接创建 VBoxContainer、配置价格 Label、挂载卡牌和价格标签。
现在商品位 UI 结构由 ShopItemSlotPresenter 统一维护，主循环更专注于“选择卡牌 -> 准备卡牌数据 -> 登记商品”。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

ShopManager 的页面专属拆分继续推进：初始化/生成日志、CardManager 定位和商品位 UI 构建都已经拆出。商品生成主循环仍保留选卡、DraftCard 数据准备和登记逻辑，行为边界稳定。

当前 ShopManager 剩余可拆点主要是商品生成循环中的“单个商品生成编排”、价格显示/刷新日志、购买流程和依赖查找/全局节点访问。下一批需要继续选最小风险面。

### 下一步打算

下一批建议评估 `_generate_shop_items()` 中“单个商品生成编排”：可以考虑抽一个只返回 `shop_card + price + slot_data` 的局部 helper，但它涉及 `await _steal_card_data()`，风险高于本批。更稳的替代方向是先收口 `_clear_shop_items()` 的清理日志和容器清理。

## 奖励页第十五批商店商品清理拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `ShopManager.gd::_clear_shop_items()` 中清空商店商品 UI 和商品记录的逻辑。保留 `_clear_shop_items()` 旧入口，不改变商品生成、购买、价格计算、刷新升级或飞入牌库流程。

目标函数范围：

```text
ShopManager.gd::_clear_shop_items()
ShopItemClearer.gd::clear_items(shop_grid, shop_cards, card_price_map)
ShopDebugLogger.gd::log_physical_cleanup(child_count)
```

当前触碰的数据和节点：

```text
shop_grid
shop_grid.get_children()
shop_cards
card_price_map
queue_free()
```

### 新增模块

```text
scene/in_scene/rewards/presenters/ShopItemClearer.gd
```

模块边界：

- `ShopItemClearer.gd` 只负责清空商店商品 UI 和商品记录。
- 它不生成商品，不计算价格，也不处理购买流程。
- 清理完成后的日志继续交给 `ShopDebugLogger.gd`，保持诊断输出集中。

### 本批删除或收口的重复点

删除原因：

```text
ShopManager 原本直接遍历 shop_grid 子节点、remove_child、queue_free，并清空 shop_cards 与 card_price_map。
现在清理细节由 ShopItemClearer 统一维护，主脚本只保留何时清理的流程入口。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度

ShopManager 的商品生成周边已经拆出调试日志、商品位 UI 构建、商品清理三个小模块。主脚本仍负责核心生成顺序和购买流程，拆分边界稳定。

当前 ShopManager 剩余较大的逻辑块是 `_generate_shop_items()` 的单商品生成编排、购买流程 `_on_shop_card_clicked()`、价格/时代相关提示输出，以及 `_get_cards_by_era()` 的数据源读取。

### 下一步打算

下一批建议谨慎评估 `_generate_shop_items()` 内单个商品生成编排。若要继续拆，建议只抽“创建并准备 shop_card”的 async helper，保留 UI 登记和信号连接在 ShopManager；如果风险偏高，则转向购买流程中的价格验证和卡牌剥离动画前准备。

## 奖励页第十六批 DraftCard 基础工厂拆分记录

日期：2026-06-06

### 本批目标

本批只拆四个奖励页面中重复的轻量 DraftCard 基础初始化逻辑。新模块只负责 `instantiate()`、写入 `card_id` 和 `custom_set_size`；不读取真实卡牌数据，不加入页面容器，不连接点击信号，也不改变商店商品生成、奖励选择、删除或合成流程。

目标函数范围：

```text
AcquireReward.gd::_create_draft_card(card_id, temp_pile)
RemoveReward.gd::_create_deck_card_display(card_id, temp_pile)
CraftReward.gd::_create_selection_card(entry, temp_pile)
ShopManager.gd::_generate_shop_items()
```

当前触碰的数据和节点：

```text
draft_card_scene
card_id
card_display_size
DraftCard.card_id
DraftCard.custom_set_size
```

### 新增模块

```text
scene/in_scene/rewards/factory/RewardDraftCardFactory.gd
```

模块边界：

- `RewardDraftCardFactory.gd` 只负责创建奖励页使用的轻量 DraftCard 并写入基础展示尺寸。
- 它不读取真实卡牌数据，不加入页面容器，也不连接点击信号。
- CraftReward 的 `_create_preview_card()` 使用 `preview_size` 和 tooltip 分支，本批保留原地，不混入基础列表卡工厂。

### 本批删除或收口的重复点

删除原因：

```text
AcquireReward、RemoveReward、CraftReward 和 ShopManager 原本重复维护 DraftCard 实例化、card_id 写入和 custom_set_size 写入。
现在这些基础初始化由 RewardDraftCardFactory 统一维护，页面继续负责数据窃取、容器挂载和交互连接。
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

奖励页通用卡牌创建链路继续收口：真实卡牌生成、DraftCard 数据填充、真实卡牌清理、临时牌堆、卡面读取、描述读取和基础 DraftCard 初始化都已模块化。ShopManager 的单商品生成编排风险进一步降低。

当前剩余可拆点包括 CraftReward 预览卡创建、ShopManager 购买流程、ShopManager 单商品生成 async helper，以及价格/时代日志继续收口。

### 下一步打算

下一批建议优先评估 ShopManager 购买流程 `_on_shop_card_clicked()`：可以先拆“购买前价格验证和时间币消费”或“购买后卡牌剥离准备”之一，不建议一次拆完整购买流程。

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

## ShopManager.gd 第十七批购买价格校验拆分记录

日期：2026-06-06

### 本批目标

本批只拆商店购买卡牌时的价格消费校验。它保留 `_on_shop_card_clicked()` 旧入口，只把“尝试扣除时间币”和“输出购买成败日志”交给新规则模块；不改刷新和升级的时间币消费，不移动卡牌节点，不释放商品槽，也不调整飞入牌库动画。

目标函数范围：

```text
_on_shop_card_clicked(clicked_card) 中的价格验证、购买失败日志和购买成功日志
```

当前触碰的数据和节点：

```text
clicked_card.card_id
price_data.price
Callable(self, "_consume_timecoins")
```

### 新增模块

```text
scene/in_scene/rewards/rules/ShopPurchaseValidator.gd
```

模块边界：

- `ShopPurchaseValidator.gd` 只负责商店购买前的价格消费校验与结果日志。
- 它通过传入的 `Callable` 调用原有时间币消费入口，不直接查找 `global_timecoin`。
- 它不移动卡牌，不修改商店商品列表，也不执行飞入牌库动画。
- `ShopManager.gd` 继续负责编排购买流程、价格标签释放、商品槽清理和飞行动画入口。

### 本批删除或收口的重复点

删除原因：

```text
购买卡牌的时间币消费结果和成败日志现在由 ShopPurchaseValidator 统一维护。
主脚本不再在购买流程中直接拼接价格校验日志，后续可继续拆分商品槽剥离和飞行动画编排。
```

### 回归检查

```text
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买流程已经把价格校验从后续卡牌节点迁移和飞行动画中分离出来。
奖励页模块目录继续保持 animation、bridges、diagnostics、factory、presenters、rules 的归档结构。
```

下一步计划：

```text
继续检查 _on_shop_card_clicked(clicked_card) 中的商品槽剥离逻辑。
下一批优先考虑只拆 price_data.label.queue_free()、slot_container.remove_child(clicked_card)、self.add_child(clicked_card) 和 global_position 恢复这一组“卡牌脱离商品槽准备飞行”的风险面。
暂不触碰 _fly_to_deck_pile(card) 和 _on_fly_to_deck_finished(...) 的牌库同步逻辑。
```

## ShopManager.gd 第十八批购买卡牌脱离商品槽拆分记录

日期：2026-06-06

### 本批目标

本批只拆商店购买成功后卡牌从商品槽脱离并准备飞行动画的 UI 节点处理。它保留 `_on_shop_card_clicked()` 旧入口，只把释放价格标签、卡牌 reparent、保留全局位置和释放空商品槽交给新 presenter；不改购买价格校验，不启动或调整飞行动画，也不处理牌库同步。

目标函数范围：

```text
_on_shop_card_clicked(clicked_card) 中 price_data.label.queue_free()、slot_container.remove_child(clicked_card)、self.add_child(clicked_card)、global_position 恢复和 slot_container.queue_free()
```

当前触碰的数据和节点：

```text
clicked_card
price_data.label
price_data.container
self 作为商店 CanvasLayer
clicked_card.global_position
```

### 新增模块

```text
scene/in_scene/rewards/presenters/ShopPurchaseCardDetachPresenter.gd
```

模块边界：

- `ShopPurchaseCardDetachPresenter.gd` 只负责购买成功后把卡牌从商品槽脱离出来并保持屏幕位置。
- 它不消费时间币，不计算价格，不播放飞行动画，也不写入或同步牌库。
- `ShopManager.gd` 继续负责编排购买流程、调用价格校验、启动飞入牌库动画和处理购买完成后的牌库同步。

### 本批删除或收口的重复点

删除原因：

```text
购买成功后的价格标签释放、卡牌换父节点、全局位置恢复和空商品槽释放现在由 ShopPurchaseCardDetachPresenter 统一维护。
主脚本不再直接操作这一组 UI 节点结构，只保留购买流程的顺序编排。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买流程已经拆出价格校验和购买成功后的商品槽脱离 UI 处理。
购买函数剩余核心职责主要是调用飞行动画入口和购买完成后的牌库同步回调。
```

下一步计划：

```text
下一批优先检查 _fly_to_deck_pile(card) 是否还需要进一步收口参数拼装或保持现状。
如果飞行动画已足够薄，则转向 _on_fly_to_deck_finished(card_id, card) 的商店记录移除与牌库同步边界，仍然每批只拆一个风险面。
```

## 已拆模块终极维护文档批次记录

日期：2026-06-06

### 本批目标

本批不改任何 GDScript 行为，只为当前已经拆出的模块建立最新版总结性维护手册。文档模仿 `docs/hex-map-ultimate-operation-guide.md` 的写法，按模块目录和具体 `.gd` 文件逐一说明用途、维护边界和后续改进方向。

目标文档范围：

```text
docs/modularized-files-ultimate-operation-guide.md
docs/ai-handoff-ultimate-operation-guide.md
docs/hex-map-ultimate-operation-guide.md
```

覆盖模块范围：

```text
scene/in_scene/hex_map_modules/：31 个文件
scene/in_scene/in_scene_modules/：32 个文件
scene/in_scene/drag_modules/：16 个文件
scene/in_scene/rewards/animation|bridges|diagnostics|factory|presenters|rules/：24 个文件
合计：103 个已拆 GDScript 模块
```

### 新增文档

```text
docs/modularized-files-ultimate-operation-guide.md
```

文档边界：

- 它负责汇总每个已拆模块的用途、维护边界和可继续优化方向。
- 它不替代 `docs/hex-map-ultimate-operation-guide.md` 中关于 `map_data`、`stack_nodes` 和 metadata 的细节说明。
- 它不记录中间批次过程；批次过程仍写入 `workflow_logs/current-modularization-process.md`。

### 本批删除或收口的重复点

删除原因：

```text
没有删除文档。本批把“每个拆分文件怎么用、怎么维护、下一步怎么优化”的说明集中成一份最新版总手册，避免 docs 目录出现大量零散批次文档。
```

### 回归检查

```text
覆盖率检查通过：103 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
```

### 当前优化进度与下一步

当前进度：

```text
已拆模块现在有三份核心文档入口：AI 接力总说明、HexMap 专项操作手册、已拆模块总维护手册。
后续 AI 可以先看 docs/modularized-files-ultimate-operation-guide.md 快速定位每个拆分模块的职责边界。
```

下一步计划：

```text
继续回到代码拆分主线时，优先处理 ShopManager.gd 的购买完成边界。
建议下一批只拆 _on_fly_to_deck_finished(card_id, card) 中的商店记录移除，暂不改 RewardDeckSyncBridge.add_card_and_sync(...)。
```

## ShopManager.gd 第十九批购买完成记录移除拆分记录

日期：2026-06-06

### 本批目标

本批只拆购买飞行动画完成后的商店记录移除。它保留 `_on_fly_to_deck_finished(card_id, card)` 旧入口，只把 `shop_cards` 和 `card_price_map` 中已购卡牌的记录清理交给新 presenter；不改 `RewardDeckSyncBridge.add_card_and_sync(...)`，不移动或释放卡牌节点，也不调整飞行动画。

目标函数范围：

```text
_on_fly_to_deck_finished(card_id, card) 中 shop_cards.find(card)、shop_cards.remove_at(idx)、card_price_map.erase(card)
```

当前触碰的数据和节点：

```text
card
shop_cards
card_price_map
```

### 新增模块

```text
scene/in_scene/rewards/presenters/ShopPurchasedItemRecordRemover.gd
```

模块边界：

- `ShopPurchasedItemRecordRemover.gd` 只负责购买完成后从商店记录集合中移除已购卡牌。
- 它不写入牌组，不同步局内抽牌堆，也不释放或移动卡牌节点。
- `ShopManager.gd` 继续负责编排购买完成顺序，先同步牌库，再移除商店记录。

### 本批删除或收口的重复点

删除原因：

```text
购买完成后的 shop_cards/card_price_map 清理从 ShopManager.gd 收口到 ShopPurchasedItemRecordRemover。
主脚本不再直接维护这组记录集合的删除细节，只保留购买完成后的流程顺序。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopPurchasedItemRecordRemover.gd 条目。
商店购买流程链路已更新为 ShopPurchaseValidator -> ShopPurchaseCardDetachPresenter -> RewardCardFlyToDeckAnimator -> RewardDeckSyncBridge -> ShopPurchasedItemRecordRemover。
```

### 回归检查

```text
覆盖率检查通过：104 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买路径已经拆出价格校验、商品槽脱离、飞行动画播放、牌库同步桥接和商店记录移除。
_on_fly_to_deck_finished(card_id, card) 现在只保留购买完成后的流程编排。
```

下一步计划：

```text
下一批优先重新评估 ShopManager.gd 购买路径是否已经足够薄。
如果购买路径不再适合继续拆，则转向 _on_refresh_pressed() 和 _on_upgrade_pressed() 的价格计算、时间币消费和日志边界，每批只拆一个风险面。
```

## ShopManager.gd 第二十批刷新费用结算拆分记录

日期：2026-06-06

### 本批目标

本批只拆刷新商店按钮的费用结算。它保留 `_on_refresh_pressed()` 旧入口，只把刷新费用计算、时间币消费、失败/成功日志和下一次刷新次数结果交给新规则模块；不改升级按钮，不改商品生成，也不改价格标签刷新。

目标函数范围：

```text
_on_refresh_pressed() 中 refresh_cost 计算、_consume_timecoins(refresh_cost)、刷新失败日志、refresh_count += 1、商店刷新成功日志
```

当前触碰的数据和节点：

```text
refresh_base_cost
refresh_count
price_increment
Callable(self, "_consume_timecoins")
```

### 新增模块

```text
scene/in_scene/rewards/rules/ShopRefreshPurchaseProcessor.gd
```

模块边界：

- `ShopRefreshPurchaseProcessor.gd` 只负责商店刷新按钮的费用结算。
- 它不生成商品，不更新价格标签，也不处理时代升级流程。
- `ShopManager.gd` 继续负责刷新成功后的 `_generate_shop_items()` 和 `_update_price_display()`。

### 本批删除或收口的重复点

删除原因：

```text
刷新商店的费用计算、时间币消费和刷新次数递增从 ShopManager.gd 收口到 ShopRefreshPurchaseProcessor。
主脚本不再直接拼刷新费用日志，只接收结算结果并决定是否继续生成商品。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopRefreshPurchaseProcessor.gd 条目。
当前优化方向已更新为继续收口刷新和升级结算。
```

### 回归检查

```text
覆盖率检查通过：105 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
首次加载 res://scene/in_scene/rewards/shop.tscn 时发现 `var result :=` 无法推断类型，已在本批改为 `var result: Dictionary`。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买路径已经基本薄化，刷新按钮也已拆出费用结算。
_on_refresh_pressed() 现在只保留刷新结算结果判断、商品重新生成和价格显示更新。
```

下一步计划：

```text
下一批优先拆 _on_upgrade_pressed() 中的升级费用计算、时间币消费、升级次数/时代偏移更新和日志。
暂不改升级后的 _generate_shop_items() 和 _update_price_display()。
```

## ShopManager.gd 第二十一批升级费用结算拆分记录

日期：2026-06-06

### 本批目标

本批只拆升级时代按钮的费用结算。它保留 `_on_upgrade_pressed()` 旧入口，只把升级费用计算、时间币消费、失败/成功日志、升级次数结果和本地时代偏移结果交给新规则模块；不改升级成功后的商品生成，也不改价格标签刷新。

目标函数范围：

```text
_on_upgrade_pressed() 中 upgrade_cost 计算、_consume_timecoins(upgrade_cost)、升级失败日志、upgrade_count += 1、local_era_offset += 1、时代升级成功日志
```

当前触碰的数据和节点：

```text
upgrade_base_cost
upgrade_count
price_increment
local_era_offset
Callable(self, "_consume_timecoins")
```

### 新增模块

```text
scene/in_scene/rewards/rules/ShopUpgradePurchaseProcessor.gd
```

模块边界：

- `ShopUpgradePurchaseProcessor.gd` 只负责商店升级按钮的费用结算和升级状态结果。
- 它不生成商品，不更新价格标签，也不读取或修改全局时代。
- `ShopManager.gd` 继续负责升级成功后的 `_generate_shop_items()` 和 `_update_price_display()`。

### 本批删除或收口的重复点

删除原因：

```text
升级时代的费用计算、时间币消费、升级次数递增和本地时代偏移递增从 ShopManager.gd 收口到 ShopUpgradePurchaseProcessor。
主脚本不再直接拼升级费用日志，只接收结算结果并决定是否继续免费刷新商店。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopUpgradePurchaseProcessor.gd 条目。
当前优化方向已更新为重新评估 ShopManager.gd 剩余边界。
```

### 回归检查

```text
覆盖率检查通过：106 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买路径、刷新费用结算和升级费用结算都已经拆出。
_on_upgrade_pressed() 现在只保留升级结算结果判断、状态写回、商品重新生成和价格显示更新。
```

下一步计划：

```text
下一批先重新扫描 ShopManager.gd 剩余函数体量和耦合，不直接继续拆。
优先判断 _update_price_display()、_generate_shop_items()、open_shop() 哪个边界还值得拆；如果收益不高，转向奖励页其他大文件如 CraftReward.gd。
```

## ShopManager.gd 第二十二批 CardDataPool 读取桥接拆分记录

日期：2026-06-06

### 本批目标

本批只拆商店按时代读取候选卡牌的桥接逻辑。它保留 `_get_cards_by_era(era)` 旧入口，只把 `CardDataPool.get_instance()`、`get_cards_by_era(era)` 调用、数量日志和旧 fallback 卡牌池交给新 bridge；不改时代权重选择、不改真实卡生成、不改商品槽 UI。

目标函数和轮廓：

```text
ShopManager.gd::CardDataPool preload
ShopManager.gd::_generate_shop_items()
ShopManager.gd::_select_card_by_era_weight(base_era)
ShopManager.gd::_get_cards_by_era(era)
ShopCardPoolBridge.gd::get_cards_by_era(card_data_pool_script, era)
```

当前触碰的数据和节点：

```text
CardDataPool
era
CardDataPool.get_instance()
card_data_pool.get_cards_by_era(era)
fallback era_pools
```

### 新增模块

```text
scene/in_scene/rewards/bridges/ShopCardPoolBridge.gd
```

模块边界：

- `ShopCardPoolBridge.gd` 只负责按时代读取 `CardDataPool` 候选卡牌，并保留旧 fallback。
- 它不选择时代权重，不创建真实卡，不包装商品槽，不更新价格。
- `ShopManager.gd` 继续负责 `_generate_shop_items()` 的完整商品生成编排。

### 本批删除或收口的重复点

删除原因：

```text
CardDataPool 单例读取、候选卡牌数量日志和 fallback 卡牌池从 ShopManager.gd 收口到 ShopCardPoolBridge。
主脚本不再直接知道 CardDataPool 读取细节，只保留旧入口转发。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopCardPoolBridge.gd 条目。
当前优化方向已更新为评估 _update_price_display() 或转向 CraftReward.gd。
```

### 回归检查

```text
覆盖率检查通过：107 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的购买路径、刷新费用结算、升级费用结算和 CardDataPool 读取桥接都已经拆出。
_get_cards_by_era(era) 现在只保留旧入口转发，商品生成主流程仍集中在 _generate_shop_items()。
```

下一步计划：

```text
下一批优先评估 _update_price_display() 是否值得继续薄化。
如果它已经只是价格和标签的短编排，就不要为拆而拆，转向 CraftReward.gd 或其他奖励页大文件。
```

## CraftReward.gd 第六批结果描述内容显示拆分记录

日期：2026-06-06

### 本批目标

本批先按上一批计划评估 `ShopManager.gd::_update_price_display()`，确认它已经通过 `ShopPricingPresenter` 维护刷新/升级费用计算和标签更新，继续拆收益很低。因此本批转向 `CraftReward.gd` 的低风险页面专属边界，只拆合成结果描述内容的读取、显示和隐藏。

目标函数和轮廓：

```text
ShopManager.gd::_update_price_display()
ShopPricingPresenter.gd::calculate_refresh_cost(...)
ShopPricingPresenter.gd::calculate_upgrade_cost(...)
ShopPricingPresenter.gd::update_price_labels(...)
CraftReward.gd::_update_result_description()
CraftReward.gd::_position_result_description_panel()
CraftResultDescriptionContentPresenter.gd::update_result_description(...)
```

当前触碰的数据和节点：

```text
result_preview_card
result_description_panel
result_description_label
get_parsed_description()
raw_description
Callable(self, "_object_has_property")
Callable(self, "_position_result_description_panel")
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftResultDescriptionContentPresenter.gd
```

模块边界：

- `CraftResultDescriptionContentPresenter.gd` 只负责结果描述文本读取、Label 写入、Panel 显示或隐藏。
- 它不配置面板样式，不计算面板位置，不修改合成结果、槽位状态或牌组。
- `CraftReward.gd` 保留 `_update_result_description()` 旧入口，继续把定位交给 `_position_result_description_panel()` 和既有 PositionPresenter。

### 本批删除或收口的重复点

删除原因：

```text
结果预览卡说明文本读取、raw_description 兜底、默认“无效果文本”和面板显示隐藏从 CraftReward.gd 收口到 CraftResultDescriptionContentPresenter。
主脚本不再直接维护结果说明内容显示细节，只保留旧入口转发和定位入口。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftResultDescriptionContentPresenter.gd 条目。
当前优化方向已更新为 CraftReward.gd 页面专属小边界优先。
```

### 回归检查

```text
覆盖率检查通过：108 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的合成结果描述面板已经拆成样式、内容和定位三个 presenter。
_update_result_description() 现在只保留旧入口转发，合成状态和牌组写入未改。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd 的 _refresh_slot_placeholders()、_clear_all_previews()、_clear_slot_preview()、_clear_result_preview()。
如果这些函数适合合并成预览清理/占位符 presenter，就只拆 UI 状态整理；暂不碰 _apply_crafting_result_to_deck() 和 _generate_selection_cards()。
```

## CraftReward.gd 第七批槽位占位符显示拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `CraftReward.gd::_refresh_slot_placeholders()` 的占位符显隐和结果槽文案。它保留旧入口，只把素材槽占位符、结果槽占位符和“无配方/结果槽”文案刷新交给新 presenter；不创建或释放预览卡，不判断配方，不改合成选择状态。

目标函数和轮廓：

```text
CraftReward.gd::_refresh_slot_placeholders()
CraftReward.gd::_clear_all_previews()
CraftReward.gd::_clear_slot_preview(slot_index)
CraftReward.gd::_clear_result_preview()
CraftSlotPlaceholderPresenter.gd::refresh_slot_placeholders(...)
```

当前触碰的数据和节点：

```text
slot1_placeholder
slot2_placeholder
result_placeholder
slot_preview_cards
slot_entries
result_preview_card
SLOT_1
SLOT_2
no_recipe_text
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftSlotPlaceholderPresenter.gd
```

模块边界：

- `CraftSlotPlaceholderPresenter.gd` 只负责合成素材槽和结果槽占位符的显隐与文案。
- 它不创建或释放预览卡，不判断配方，不修改合成选择状态。
- `CraftReward.gd` 保留 `_refresh_slot_placeholders()` 旧入口，继续由页面状态变更点主动调用。

### 本批删除或收口的重复点

删除原因：

```text
素材槽占位符 visible、结果槽 visible 和结果槽文案从 CraftReward.gd 收口到 CraftSlotPlaceholderPresenter。
主脚本不再直接维护占位符显示细节，只保留旧入口转发。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftSlotPlaceholderPresenter.gd 条目。
当前优化方向已更新为继续评估 CraftReward.gd 的预览清理函数。
```

### 回归检查

```text
覆盖率检查通过：109 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的合成结果描述面板和槽位占位符显示都已拆出 presenter。
_refresh_slot_placeholders() 现在只保留旧入口转发，预览卡清理和合成状态未改。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd 的 _clear_all_previews()、_clear_slot_preview() 和 _clear_result_preview()。
如果继续拆，只收口预览卡 queue_free、引用清空和结果 id 清空；暂不碰 _refresh_result_preview() 的配方生成和 _apply_crafting_result_to_deck()。
```

## CraftReward.gd 第八批预览清理拆分记录

日期：2026-06-06

### 本批目标

本批只拆合成页预览卡清理。它保留 `_clear_all_previews()`、`_clear_slot_preview(slot_index)` 和 `_clear_result_preview()` 三个旧入口，只把素材槽预览卡释放、结果预览卡释放、结果预览引用清空和结果 id 清空交给新 presenter；不改 `_refresh_result_preview()` 的配方刷新，不创建预览卡，不改合成结果入库。

目标函数和轮廓：

```text
CraftReward.gd::_clear_all_previews()
CraftReward.gd::_clear_slot_preview(slot_index)
CraftReward.gd::_clear_result_preview()
CraftReward.gd::_refresh_result_preview()
CraftPreviewCleanupPresenter.gd::clear_all_previews(...)
CraftPreviewCleanupPresenter.gd::clear_slot_preview(...)
CraftPreviewCleanupPresenter.gd::clear_result_preview(...)
```

当前触碰的数据和节点：

```text
slot_preview_cards
result_preview_card
current_result_card_id
SLOT_1
SLOT_2
_update_result_description()
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftPreviewCleanupPresenter.gd
```

模块边界：

- `CraftPreviewCleanupPresenter.gd` 只负责释放预览卡节点，并返回结果预览清空状态。
- 它不创建预览卡，不刷新配方，不更新结果描述，也不修改牌组。
- `CraftReward.gd` 保留三个清理旧入口，并继续负责调用 `_update_result_description()`。

### 本批删除或收口的重复点

删除原因：

```text
素材槽预览卡 queue_free、slot_preview_cards 清空、结果预览卡 queue_free、result_preview_card/current_result_card_id 清空从 CraftReward.gd 收口到 CraftPreviewCleanupPresenter。
主脚本不再直接维护预览释放细节，只处理旧入口和结果描述刷新。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftPreviewCleanupPresenter.gd 条目。
当前优化方向已更新为重新扫描 CraftReward.gd 剩余大函数。
```

### 回归检查

```text
覆盖率检查通过：110 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的结果描述、槽位占位符、预览布局和预览清理都已拆出 presenter。
预览清理旧入口仍在页面内，配方刷新、预览创建和合成结果入库未改。
```

下一步计划：

```text
下一批先重新扫描 CraftReward.gd 剩余函数体量和耦合，不直接拆。
优先判断 _build_selection_entries()、_create_selection_card()、_refresh_result_preview()、_apply_crafting_result_to_deck() 哪个还有清晰小边界；如果风险偏高，就转向 RemoveReward.gd 或 ShopManager.gd 的剩余边界。
```

## CraftReward.gd 第九批选择标题文案拆分记录

日期：2026-06-06

### 本批目标

本批先重新扫描 `CraftReward.gd` 剩余大函数。`_build_selection_entries()`、`_create_selection_card()`、`_refresh_result_preview()` 和 `_apply_crafting_result_to_deck()` 已经涉及牌组条目、异步卡牌生成、配方刷新或牌组写入，不适合直接大拆。因此本批只拆低风险的 `_build_selection_title(slot_index)`，把合成选择面板标题文案交给新 presenter。

目标函数和轮廓：

```text
CraftReward.gd::_build_selection_entries(slot_index)
CraftReward.gd::_build_selection_title(slot_index)
CraftReward.gd::_create_selection_card(entry, temp_pile)
CraftReward.gd::_refresh_result_preview()
CraftReward.gd::_apply_crafting_result_to_deck()
CraftSelectionTitlePresenter.gd::build_selection_title(...)
```

当前触碰的数据和节点：

```text
slot_index
slot_entries
SLOT_1
SLOT_2
select_slot_1_text
select_slot_2_text
selection_title_label
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftSelectionTitlePresenter.gd
```

模块边界：

- `CraftSelectionTitlePresenter.gd` 只负责根据当前槽位和已选槽位状态生成标题文案。
- 它不生成选择卡，不排序牌组条目，不修改 pending 选择，也不写合成状态。
- `CraftReward.gd` 保留 `_build_selection_title(slot_index)` 旧入口。

### 本批删除或收口的重复点

删除原因：

```text
当前槽位、另一槽位、重新选择文案、兼容提示文案和默认选择文案从 CraftReward.gd 收口到 CraftSelectionTitlePresenter。
主脚本不再直接拼选择页标题，只在生成选择列表时写入 selection_title_label.text。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftSelectionTitlePresenter.gd 条目。
当前优化方向已更新为评估 _build_selection_entries() 是否能拆纯规则返回状态。
```

### 回归检查

```text
覆盖率检查通过：111 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的结果描述、槽位占位符、预览布局、预览清理和选择标题都已拆出 presenter。
选择条目构建、选择卡创建、结果预览刷新和合成结果入库仍在主脚本内。
```

下一步计划：

```text
下一批优先评估 _build_selection_entries() 是否可以拆成纯规则模块。
如果继续拆，先设计返回值承载 ordered_entries、pending_selected_entry 和 can_close_selection_without_choice，避免新模块直接写主脚本状态。
```

## CraftReward.gd 第十批选择条目构建规则拆分记录

日期：2026-06-06

### 本批目标

本批只拆 `_build_selection_entries(slot_index)` 的纯规则部分。新模块接收当前槽位、牌组 id、两个槽位状态和配方查询入口，返回选择条目、pending 选择建议和是否允许无选择返回；主脚本继续负责把状态写回 `pending_selected_entry`、`can_close_selection_without_choice` 和 `current_deck_entries`。

目标函数和轮廓：

```text
CraftReward.gd::_build_selection_entries(slot_index)
CraftReward.gd::_get_current_deck_card_ids()
CraftReward.gd::_get_recipe_result(card_a_id, card_b_id)
CraftReward.gd::_copy_entry(entry)
CraftSelectionEntryBuilder.gd::build_selection_entries(...)
```

当前触碰的数据和节点：

```text
deck_ids
slot_entries
pending_selected_entry
can_close_selection_without_choice
SLOT_1
SLOT_2
Callable(self, "_get_recipe_result")
```

### 新增模块

```text
scene/in_scene/rewards/rules/CraftSelectionEntryBuilder.gd
```

模块边界：

- `CraftSelectionEntryBuilder.gd` 只负责生成选择条目和状态建议。
- 它不创建卡牌节点，不写主脚本状态，不处理按钮或选择面板 UI。
- `CraftReward.gd` 保留 `_build_selection_entries(slot_index)` 旧入口，并继续负责状态写回。

### 本批删除或收口的重复点

删除原因：

```text
基础条目生成、另一槽位排除、当前槽位置顶/高亮、兼容条目排序、不兼容条目 disabled/dimmed 和无兼容项状态建议从 CraftReward.gd 收口到 CraftSelectionEntryBuilder。
主脚本不再直接维护选择条目构建细节，只接收 entries、pending_selected_entry 和 can_close_selection_without_choice。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftSelectionEntryBuilder.gd 条目。
当前优化方向已更新为重新评估选择卡创建、结果预览刷新和合成结果入库。
```

### 回归检查

```text
覆盖率检查通过：112 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的选择条目构建已拆成纯规则模块，主脚本保留状态写回。
选择卡创建、结果预览刷新和合成结果入库仍在主脚本内。
```

下一步计划：

```text
下一批先重新评估 _create_selection_card() 和 _refresh_result_preview()。
如果继续拆，优先找不涉及 await 或牌组写入的小边界；如果都偏高风险，就转向 RemoveReward.gd 或 ShopManager.gd。
```

## CraftReward.gd 第十一批选择卡状态表现拆分记录

日期：2026-06-06

### 本批目标

本批先评估 `_create_selection_card()` 和 `_refresh_result_preview()`。`_refresh_result_preview()` 仍涉及配方刷新、异步预览卡创建、结果槽挂载和信号连接，暂不拆。本批只拆 `_create_selection_card(entry, temp_pile)` 中同步的选择卡视觉状态和点击入口，不碰 DraftCard 创建、`await _steal_card_data()`、`deck_grid.add_child()` 或 `current_deck_cards`。

目标函数和轮廓：

```text
CraftReward.gd::_create_selection_card(entry, temp_pile)
CraftReward.gd::_on_deck_card_clicked(clicked_card, entry)
CraftSelectionCardStatePresenter.gd::apply_selection_card_state(...)
```

当前触碰的数据和节点：

```text
draft_card
entry.selected
entry.dimmed
entry.disabled
incompatible_card_alpha
Callable(self, "_on_deck_card_clicked")
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftSelectionCardStatePresenter.gd
```

模块边界：

- `CraftSelectionCardStatePresenter.gd` 只负责选择列表卡牌的 selected、dimmed、disabled、hover、tooltip 和点击入口。
- 它不创建卡牌，不读取真实卡数据，不写 pending 选择，也不修改牌组。
- `CraftReward.gd` 继续负责异步数据读取、节点挂载和选择点击后的状态变更。

### 本批删除或收口的重复点

删除原因：

```text
选择卡 selected 显示、tooltip 开关、dimmed/hover 状态、disabled mouse_filter 和 card_clicked 连接从 CraftReward.gd 收口到 CraftSelectionCardStatePresenter。
主脚本不再直接维护选择卡状态表现，只传入点击回调和 entry。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftSelectionCardStatePresenter.gd 条目。
当前优化方向已更新为继续评估结果预览刷新和合成结果入库。
```

### 回归检查

```text
覆盖率检查通过：113 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的选择条目构建、选择标题和选择卡状态表现都已拆出。
_create_selection_card() 仍保留 DraftCard 创建、异步数据复制和节点挂载。
```

下一步计划：

```text
下一批优先重新评估 _refresh_result_preview() 是否还有不涉及 await 的小边界。
如果风险仍高，就转向 _apply_crafting_result_to_deck() 的牌组索引计算，或转向 RemoveReward.gd。
```

## CraftReward.gd 第十二批合成结果移除索引规则拆分记录

日期：2026-06-06

### 本批目标

本批先评估 `_refresh_result_preview()` 和 `_apply_crafting_result_to_deck()`。`_refresh_result_preview()` 仍涉及异步预览卡创建和结果槽挂载，暂不拆。本批只拆 `_apply_crafting_result_to_deck()` 中“根据两个合成槽位计算需要倒序移除的牌组索引”，不改 `GlobalDB.player_deck.remove_at()`、追加结果卡或运行时抽牌堆同步。

目标函数和轮廓：

```text
CraftReward.gd::_refresh_result_preview()
CraftReward.gd::_apply_crafting_result_to_deck()
CraftResultDeckIndexResolver.gd::get_remove_indices(slot_entries, slot_1, slot_2)
```

当前触碰的数据和节点：

```text
slot_entries
SLOT_1
SLOT_2
deck_index
remove_indices
```

### 新增模块

```text
scene/in_scene/rewards/rules/CraftResultDeckIndexResolver.gd
```

模块边界：

- `CraftResultDeckIndexResolver.gd` 只负责计算合成结果写回前要移除的倒序索引。
- 它不修改 `GlobalDB`，不添加结果卡，也不同步运行时抽牌堆。
- `CraftReward.gd` 继续负责牌组删除、结果卡追加和 `RewardDeckSyncBridge` 同步。

### 本批删除或收口的重复点

删除原因：

```text
两个槽位 deck_index 收集、排序和 reverse 从 CraftReward.gd 收口到 CraftResultDeckIndexResolver。
主脚本不再直接维护倒序移除索引计算，只处理牌组写入动作。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftResultDeckIndexResolver.gd 条目。
当前优化方向已更新为评估 _refresh_result_preview() 和 GlobalDB 写入是否还值得继续拆。
```

### 回归检查

```text
覆盖率检查通过：114 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的合成结果写回索引计算已拆成纯规则模块。
_apply_crafting_result_to_deck() 仍保留 GlobalDB 删除、结果卡追加和运行时抽牌堆同步。
```

下一步计划：

```text
下一批优先重新评估 CraftReward.gd 是否还适合继续拆。
如果 _refresh_result_preview() 和 GlobalDB 写入都没有足够小的边界，就转向 RemoveReward.gd 或重新扫描 ShopManager.gd 剩余点。
```

## RemoveReward.gd 第一批牌组卡牌选择表现拆分记录

日期：2026-06-06

### 本批目标

本批先从 `CraftReward.gd` 转向 `RemoveReward.gd` 重新扫描。合成页剩余的 `_refresh_result_preview()` 涉及异步预览卡创建，`_apply_crafting_result_to_deck()` 剩余部分涉及真实牌组写入，继续硬拆风险偏高。因此本批只拆删除奖励页的 `_on_deck_card_clicked(clicked_card)`，把牌组卡牌单选、再次点击取消选择、确认按钮和返回按钮状态交给新 presenter。

目标函数和轮廓：

```text
RemoveReward.gd::_on_deck_card_clicked(clicked_card)
RemoveReward.gd::_on_confirm_pressed()
RemoveReward.gd::_remove_card_from_deck(card_id)
RemoveReward.gd::_clear_deck_display()
RemoveDeckCardSelectionPresenter.gd::apply_selection(...)
```

当前触碰的数据和节点：

```text
clicked_card
selected_draft_card
current_deck_cards
btn_confirm
btn_back
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RemoveDeckCardSelectionPresenter.gd
```

模块边界：

- `RemoveDeckCardSelectionPresenter.gd` 只负责删除奖励页牌组卡牌的单选表现和按钮状态。
- 它不删除卡牌，不创建卡牌，也不修改牌组数据。
- `RemoveReward.gd` 保留 `_on_deck_card_clicked(clicked_card)` 旧入口，只接收 presenter 返回的当前选中卡牌。

### 本批删除或收口的重复点

删除原因：

```text
selected_draft_card 判断、所有当前牌组卡 set_selected、确认按钮禁用/启用和返回按钮禁用/启用从 RemoveReward.gd 收口到 RemoveDeckCardSelectionPresenter。
主脚本不再直接维护单选表现，只保留删除页点击入口和后续确认删除流程。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 RemoveDeckCardSelectionPresenter.gd 条目。
当前优化方向已更新为评估 RemoveReward.gd 的 _clear_deck_display()、_on_confirm_pressed() 和 _remove_card_from_deck()。
```

### 回归检查

```text
覆盖率检查通过：115 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
remove_reward.tscn 仍输出既有的 CardManager 手动注入提示；in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
RemoveReward.gd 的牌组卡牌点击单选表现已拆出 presenter。
确认删除动画、实际删除数据写入、显示清理仍在主脚本内。
```

下一步计划：

```text
下一批优先评估 _clear_deck_display()，它只涉及节点释放和状态清空，风险低于 _on_confirm_pressed() 和 _remove_card_from_deck()。
暂不同时拆确认删除动画和 GlobalDB/deck_manager 写入。
```

## RemoveReward.gd 第二批牌组显示清理拆分记录

日期：2026-06-06

### 本批目标

本批继续处理 `RemoveReward.gd`，先用 `rg` 扫描 `_clear_deck_display()`、`_on_confirm_pressed()` 和 `_remove_card_from_deck()`。确认删除动画与牌组写入都涉及真实删除结果，不适合和清屏一起拆。因此本批只拆 `_clear_deck_display()` 的显示节点释放和展示状态清空。

目标函数和轮廓：

```text
RemoveReward.gd::_clear_deck_display()
RemoveReward.gd::open()
RemoveReward.gd::close()
RemoveReward.gd::_generate_deck_display()
RemoveDeckDisplayCleaner.gd::clear_deck_display(...)
```

当前触碰的数据和节点：

```text
current_deck_cards
selected_draft_card
original_deck_card_ids
deck_grid
selected_card_display
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RemoveDeckDisplayCleaner.gd
```

模块边界：

- `RemoveDeckDisplayCleaner.gd` 只负责清空删除奖励页的牌组显示节点和展示状态。
- 它不删除真实牌组数据，不关闭场景，也不生成新的卡牌。
- `RemoveReward.gd` 保留 `_clear_deck_display()` 旧入口，只接收 cleaner 返回的 `selected_draft_card` 清空状态。

### 本批删除或收口的重复点

删除原因：

```text
current_deck_cards queue_free、current_deck_cards 清空、original_deck_card_ids 清空、deck_grid 子节点清空和 selected_card_display 子节点清空从 RemoveReward.gd 收口到 RemoveDeckDisplayCleaner。
主脚本不再直接维护清屏细节，只保留 open/close/generate 调用旧入口。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 RemoveDeckDisplayCleaner.gd 条目。
当前优化方向已更新为继续评估 RemoveReward.gd 的 _remove_card_from_deck() 与 _on_confirm_pressed()。
```

### 回归检查

```text
覆盖率检查通过：116 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
remove_reward.tscn 仍输出既有的 CardManager 手动注入提示；in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
RemoveReward.gd 的牌组卡牌单选表现和牌组显示清理都已拆出。
确认删除动画和实际删除数据写入仍在主脚本内。
```

下一步计划：

```text
下一批优先重新评估 _remove_card_from_deck() 是否能拆成删除规则/同步桥接的小边界。
如果 deck_manager 与 GlobalDB 兼容路径仍不够清楚，就先停止 RemoveReward，转向其他奖励页或重新扫描 ShopManager。
```

## RemoveReward.gd 第三批删牌数据处理拆分记录

日期：2026-06-06

### 本批目标

本批继续处理 `RemoveReward.gd`，先用 `rg` 扫描 `_remove_card_from_deck()`、`_get_current_deck_card_ids()`、`_on_confirm_pressed()`、`RewardDeckSyncBridge.sync_runtime_deck()` 和 `CraftReward.gd::_apply_crafting_result_to_deck()`。确认运行时抽牌堆同步已经由 `RewardDeckSyncBridge` 处理，确认删除动画仍涉及 UI 节点和关闭流程。因此本批只拆 `_remove_card_from_deck(card_id)` 里的删牌数据策略，不改同步桥、不改动画闭包、不改关闭流程。

目标函数和轮廓：

```text
RemoveReward.gd::_remove_card_from_deck(card_id)
RemoveReward.gd::_on_confirm_pressed()
RemoveReward.gd::_get_current_deck_card_ids()
RewardDeckSyncBridge.gd::sync_runtime_deck(owner, deck_manager)
RemoveDeckCardRemovalProcessor.gd::remove_card_from_deck(deck_manager, card_id)
```

当前触碰的数据和接口：

```text
deck_manager.remove_card_from_deck(card_id)
GlobalDB.player_deck.has(card_id)
GlobalDB.player_deck.erase(card_id)
RewardDeckSyncBridge.sync_runtime_deck(self, deck_manager)
```

### 新增模块

```text
scene/in_scene/rewards/rules/RemoveDeckCardRemovalProcessor.gd
```

模块边界：

- `RemoveDeckCardRemovalProcessor.gd` 只负责从牌组数据中移除一张指定卡。
- 它不同步运行时抽牌堆，不关闭奖励页，也不处理删除动画或 UI 节点。
- `RemoveReward.gd` 保留 `_remove_card_from_deck(card_id)` 旧入口，并继续在旧入口里调用 `RewardDeckSyncBridge.sync_runtime_deck(...)`。

### 本批删除或收口的重复点

删除原因：

```text
deck_manager.remove_card_from_deck 优先路径、GlobalDB.player_deck 回退路径、第一张匹配卡删除和删牌日志从 RemoveReward.gd 收口到 RemoveDeckCardRemovalProcessor。
主脚本不再直接维护删牌数据策略，只处理旧入口和运行时抽牌堆同步。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 RemoveDeckCardRemovalProcessor.gd 条目。
当前优化方向已更新为评估 _get_current_deck_card_ids() 和 _on_confirm_pressed()。
```

### 回归检查

```text
覆盖率检查通过：117 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
remove_reward.tscn 仍输出既有的 CardManager 手动注入提示；in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
RemoveReward.gd 的牌组卡牌单选表现、牌组显示清理和删牌数据处理都已拆出。
确认删除动画和当前牌组 ID 读取仍在主脚本内。
```

下一步计划：

```text
下一批优先评估 _get_current_deck_card_ids() 是否值得和 CraftReward.gd 合并成奖励页共用只读牌组来源模块。
如果继续拆 _on_confirm_pressed()，需要先拆动画完成后的 UI 清理，不要同时改真实删牌和关闭流程。
```

## 奖励页共用只读牌组来源拆分记录

日期：2026-06-06

### 本批目标

本批继续按上一批计划评估 `_get_current_deck_card_ids()`。`RemoveReward.gd` 和 `CraftReward.gd` 都有相同的只读牌组来源逻辑：优先调用 `deck_manager.get_deck_card_ids()`，否则读取 `GlobalDB.player_deck.duplicate()`。这块不生成卡牌、不写牌组、不触发动画，适合抽成奖励页共用规则模块。本批不碰 `RemoveReward.gd::_on_confirm_pressed()`，也不碰 `CraftReward.gd::_refresh_result_preview()`。

目标函数和轮廓：

```text
RemoveReward.gd::_get_current_deck_card_ids()
CraftReward.gd::_get_current_deck_card_ids()
RemoveReward.gd::_generate_deck_display()
CraftReward.gd::_build_selection_entries(slot_index)
RewardDeckCardIdProvider.gd::get_current_deck_card_ids(deck_manager)
```

当前触碰的数据和接口：

```text
deck_manager.get_deck_card_ids()
GlobalDB.player_deck.duplicate()
```

### 新增模块

```text
scene/in_scene/rewards/rules/RewardDeckCardIdProvider.gd
```

模块边界：

- `RewardDeckCardIdProvider.gd` 只负责读取当前牌组卡牌 ID 列表。
- 它不修改牌组，不创建卡牌，也不同步运行时抽牌堆。
- `RemoveReward.gd` 和 `CraftReward.gd` 保留 `_get_current_deck_card_ids()` 旧入口，内部转发给 provider。

### 本批删除或收口的重复点

删除原因：

```text
RemoveReward.gd 和 CraftReward.gd 重复的 deck_manager.get_deck_card_ids 优先读取、GlobalDB.player_deck.duplicate 回退读取，收口到 RewardDeckCardIdProvider。
两个页面仍各自决定怎么使用返回的 ID：删除页生成可删卡牌，合成页生成选择条目。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 RewardDeckCardIdProvider.gd 条目。
当前优化方向已更新为 RemoveReward.gd 只剩确认删除动画相关边界，CraftReward.gd 仍剩异步结果预览和牌组写入。
```

### 回归检查

```text
覆盖率检查通过：118 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
remove_reward.tscn 仍输出既有的 CardManager 手动注入提示；in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
奖励页只读牌组来源已共用。
RemoveReward.gd 的牌组卡牌单选表现、牌组显示清理、删牌数据处理都已拆出。
CraftReward.gd 的选择条目读取牌组 ID 入口也已收口。
```

下一步计划：

```text
下一批优先重新扫描 RemoveReward.gd::_on_confirm_pressed()，判断是否只能拆动画完成后的 UI 清理。
如果该函数没有足够小的边界，就停止 RemoveReward，转向 ShopManager 或其他奖励页剩余点。
```

## RemoveReward.gd 第四批确认删除后 UI 清理拆分记录

日期：2026-06-06

### 本批目标

本批继续按上一批计划扫描 `RemoveReward.gd::_on_confirm_pressed()`。该函数同时包含未选中保护、确认日志、按钮禁用、删除 tween、真实删牌、动画完成后的 UI 清理、奖励提交状态和关闭页面。完整拆确认流程会同时碰动画、数据、状态和页面关闭，风险偏大。因此本批只拆 tween 回调里“真实删牌之后、提交关闭之前”的 UI 清理，不碰 `_remove_card_from_deck()`，不碰 `set_meta("settlement_reward_committed", true)`，也不碰 `close()`。

目标函数和轮廓：

```text
RemoveReward.gd::_on_confirm_pressed()
RemoveReward.gd::_remove_card_from_deck(card_id)
RemoveConfirmedCardUiCleaner.gd::cleanup_confirmed_card(...)
```

当前触碰的数据和节点：

```text
selected_draft_card
current_deck_cards
deck_grid
selected_card_display
```

### 新增模块

```text
scene/in_scene/rewards/presenters/RemoveConfirmedCardUiCleaner.gd
```

模块边界：

- `RemoveConfirmedCardUiCleaner.gd` 只负责确认删除动画完成后的卡牌显示清理。
- 它不删除真实牌组数据，不设置奖励提交状态，也不关闭奖励页。
- `RemoveReward.gd` 保留 `_on_confirm_pressed()` 旧入口，并继续在 tween 回调里按原顺序先真实删牌，再调用 UI cleaner，最后提交并关闭。

### 本批删除或收口的重复点

删除原因：

```text
deck_grid.remove_child(selected_draft_card)、selected_draft_card.queue_free()、current_deck_cards.erase(selected_draft_card)、selected_card_display 子节点清空和 selected_draft_card 置空从 RemoveReward.gd 收口到 RemoveConfirmedCardUiCleaner。
主脚本不再直接维护确认删除后的节点清理细节，只保留确认流程编排。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 RemoveConfirmedCardUiCleaner.gd 条目。
当前优化方向已更新为 RemoveReward.gd 剩余确认流程接近页面编排，除非要统一确认动画，否则建议停止 RemoveReward，转向 ShopManager 或 CraftReward。
```

### 回归检查

```text
覆盖率检查通过：119 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/remove_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
remove_reward.tscn 仍输出既有的 CardManager 手动注入提示；in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
RemoveReward.gd 的牌组卡牌单选表现、牌组显示清理、删牌数据处理、只读牌组来源和确认删除后的 UI 清理都已拆出。
_on_confirm_pressed() 仍保留未选中保护、按钮禁用、tween 创建、删牌入口、奖励提交和关闭页面这些页面流程编排。
```

下一步计划：

```text
本批验证通过后，优先停止继续拆 RemoveReward.gd。
下一批转向 ShopManager 或 CraftReward 重新扫描剩余边界；如果没有足够小的风险面，再回到 DragShapeController 的完成流程优化。
```

## ShopManager.gd 商品槽注册拆分记录

日期：2026-06-07

### 本批目标

本批按上一批计划转向 `ShopManager.gd`，先用 `rg` 扫描函数、变量和信号轮廓，再细读 `_generate_shop_items()`。该函数仍把生成锁、清理、依赖检查、时代读取、临时牌堆、选卡、草稿卡创建、数据窃取、定价、商品槽包装、加入网格、记录映射和购买点击绑定串在一起。完整拆单个商品生成会同时碰异步数据窃取和选卡规则，风险偏大。因此本批只拆“已构建商品槽的注册与点击绑定”，不碰选卡、价格、真实卡牌数据和购买流程。

目标函数和轮廓：

```text
ShopManager.gd::_generate_shop_items()
ShopItemSlotPresenter.gd::build_slot(shop_card, price)
ShopItemRegistry.gd::register_item(...)
ShopManager.gd::_on_shop_card_clicked(clicked_card)
```

当前触碰的数据和节点：

```text
shop_grid
shop_cards
card_price_map
shop_card
slot_data
_on_shop_card_clicked
```

### 新增模块

```text
scene/in_scene/rewards/presenters/ShopItemRegistry.gd
```

模块边界：

- `ShopItemRegistry.gd` 只负责注册已构建好的商店商品槽和购买点击信号。
- 它不选择卡牌，不计算价格，也不读取或修改牌组数据。
- `ShopManager.gd` 继续负责商品生成循环、卡牌数据准备、价格计算和购买回调本身。

### 本批删除或收口的重复点

删除原因：

```text
shop_grid.add_child(slot_data["container"])、shop_cards.append(shop_card)、card_price_map[shop_card] = slot_data 和 shop_card.card_clicked.connect(...) 从 ShopManager.gd 收口到 ShopItemRegistry。
主脚本不再直接维护商品槽登记细节，生成循环只保留“生成并注册”的编排。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopItemRegistry.gd 条目。
当前优化方向已更新为 ShopManager 剩余重点只评估单个商品生成编排；如果需要传入过多成员，就停止 ShopManager，转向 CraftReward 或 DragShapeController。
```

### 回归检查

```text
覆盖率检查通过：120 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有既有 LF/CRLF 提示。
Godot 项目 headless 检查退出码为 0，未出现本批脚本解析错误。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 已拆出价格展示、商品清理、商品槽包装、商品槽注册、购买前消费校验、购买卡脱离、购买记录移除、刷新/升级费用处理、CardDataPool 桥接、全局节点查找、tooltip、临时牌堆和真实卡牌数据提取等模块。
_generate_shop_items() 仍保留生成锁、依赖检查、时代读取、临时牌堆生命周期、选卡、草稿卡创建、数据窃取、定价和循环编排。
```

下一步计划：

```text
下一批优先评估 _generate_shop_items() 的单个商品生成编排是否能拆成小模块。
如果单个商品生成模块需要传入 draft_card_factory、temp_pile、deck_manager、价格、UI 注册、异步数据提取等过多状态，就停止 ShopManager，转向 CraftReward 的剩余边界或 DragShapeController 的完成流程。
```

## 接力说明当前状态刷新记录

日期：2026-06-07

### 本批目标

本批只执行文档状态刷新，不改代码。上一轮盘点确认 `docs/ai-handoff-ultimate-operation-guide.md` 里部分行数、优先级表和接力 prompt 仍停留在早期状态，例如还建议从 `DragShapeController.gd` 的节点桥接、时间轴 hover preview、拒绝 tooltip，以及奖励页通用 CardManager/tooltip/数据提取开始。这些边界目前已经完成，继续保留旧建议会误导后续接力。

目标文件：

```text
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

当前只读复核结果：

```text
已拆模块总数：120
docs/modularized-files-ultimate-operation-guide.md 覆盖缺失：0
hex_map.gd：约 2093 行
in_scene.gd：约 1238 行
DragShapeController.gd：约 1054 行
timeline_ui.gd：约 837 行
CraftReward.gd：约 901 行
ShopManager.gd：约 684 行
RemoveReward.gd：约 492 行
AcquireReward.gd：约 480 行
```

### 文档改动

```text
刷新 docs/ai-handoff-ultimate-operation-guide.md 的当前状态一览。
刷新后续优先优化文件表，把 ShopManager、CraftReward、DragShapeController、timeline_ui 和 out_scene_map_exp 的下一阶段目标改成当前真实剩余边界。
刷新 DragShapeController、timeline_ui 和奖励脚本的后续拆分建议，移除已经完成的首批建议。
刷新接力 prompt，避免下一位 AI 重复拆已完成模块。
```

### 回归检查

```text
git diff --check 通过，仅有既有 LF/CRLF 提示。
本批只改 Markdown，未运行 Godot headless。
```

### 当前优化进度与下一步

当前进度：

```text
项目模块化文档体系保持为 docs 下 3 份总结性说明。
docs/modularized-files-ultimate-operation-guide.md 继续作为所有拆分模块的使用维护总手册。
workflow_logs/current-modularization-process.md 继续记录每批过程。
```

下一步计划：

```text
本批验证通过后，下一批优先评估 ShopManager.gd::_generate_shop_items() 的单个商品生成编排。
如果 ShopManager 单商品生成需要传入过多状态，就停止 ShopManager，转向 CraftReward.gd::_refresh_result_preview() 或 DragShapeController.gd 的放置完成流程。
```

## ShopManager.gd 生成依赖检查拆分记录

日期：2026-06-07

### 本批目标

本批只处理 `ShopManager.gd::_generate_shop_items()` 中的生成前依赖检查。上一批建议优先评估单个商品生成编排，但本轮扫描后确认它会同时牵动 `draft_card_factory`、`temp_pile`、`deck_manager`、价格、商品槽注册和异步数据提取，风险面过大，因此不把单个商品生成作为本批拆分目标。

目标文件：

```text
scene/in_scene/rewards/ShopManager.gd
scene/in_scene/rewards/rules/ShopGenerationDependencyGuard.gd
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
ShopManager.gd::_generate_shop_items()
ShopManager.gd::_get_generation_dependency_guard()
ShopGenerationDependencyGuard.gd::check_dependencies(deck_manager)
ShopManager.gd 相关变量：deck_manager、_is_generating、card_price_map、_draft_card_factory、_temp_pile_factory
```

### 当前职责与耦合点

`ShopManager.gd` 当前仍是商店页面 composition root，负责打开商店、生成商品、购买商品、刷新、升级和关闭页面。

`_generate_shop_items()` 的剩余耦合点：

```text
生成锁 _is_generating
旧商品清理和布局日志
生成前依赖检查
商店时代读取
临时牌堆生命周期
按时代权重选卡
DraftCard 创建
异步真实卡数据提取
价格计算
商品槽包装和注册
```

### 待拆清单

```text
P1：CraftReward.gd::_refresh_result_preview() 的异步预览表现边界。
P1：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界；单个商品生成编排暂不硬拆。
P2：DragShapeController.gd 放置完成流程，先写清时间轴行动创建、卡牌归属变化和 UI 恢复边界。
P2：timeline_ui.gd 行动块表现或清理动画。
P2：out_scene_map_exp.gd 房间结算 payload 消费。
```

### 本批风险面

本批只抽出“生成前依赖检查”：

```text
deck_manager 是否存在
deck_manager.card_factory 是否存在
依赖缺失时由 ShopManager 保持原有 push_warning、释放 _is_generating 并 return
```

不触碰：

```text
商品生成循环
临时牌堆创建和释放
异步 _steal_card_data()
价格计算和商品槽注册
刷新、升级和购买流程
```

### 新增模块

```text
scene/in_scene/rewards/rules/ShopGenerationDependencyGuard.gd
```

模块边界：

- `ShopGenerationDependencyGuard.gd` 只负责判断生成前必要依赖是否可用。
- 它返回 `can_generate` 和 `warning`，不直接输出日志，不修改生成锁，也不访问商店 UI。
- `ShopManager.gd` 继续负责失败时 `push_warning`、释放 `_is_generating` 和提前返回。

### 本批删除或收口的重复点

```text
deck_manager / card_factory 的条件判断和提示文案从 ShopManager.gd 收口到 ShopGenerationDependencyGuard.gd。
主脚本保留控制流，避免新模块知道生成锁和页面流程。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 ShopGenerationDependencyGuard.gd 条目。
docs/ai-handoff-ultimate-operation-guide.md 已刷新模块数量、Shop 剩余边界和下一步优先级。
```

### 回归检查

```text
覆盖率检查通过：121 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 已拆出价格展示、商品清理、商品槽包装、商品槽注册、购买前消费校验、购买卡脱离、购买记录移除、刷新/升级费用处理、CardDataPool 桥接、全局节点查找、tooltip、临时牌堆、真实卡牌数据提取和生成依赖检查等模块。
_generate_shop_items() 仍保留生成锁、旧商品清理、时代读取、临时牌堆生命周期、选卡、草稿卡创建、异步数据窃取、定价和商品注册循环。
单个商品生成编排需要传入过多状态，暂不建议继续硬拆。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd::_refresh_result_preview() 的异步预览表现边界。
如果继续看 ShopManager，只评估临时牌堆生命周期等更小边界；若仍需要传入过多成员，就停止 ShopManager。
之后再评估 DragShapeController.gd 的放置完成流程或 timeline_ui.gd 的行动块表现。
```

## CraftReward.gd 结果预览挂载拆分记录

日期：2026-06-07

### 本批目标

本批只处理 `CraftReward.gd::_refresh_result_preview()` 中“已创建结果预览卡之后的挂载和点击绑定”。不拆配方判断，不拆 `_create_preview_card()` 的异步真实卡数据读取，也不改合成结果写入牌组。

目标文件：

```text
scene/in_scene/rewards/CraftReward.gd
scene/in_scene/rewards/presenters/CraftResultPreviewPresenter.gd
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
CraftReward.gd::_refresh_result_preview()
CraftReward.gd::_create_preview_card(card_id, preview_size, tooltip_enabled)
CraftReward.gd::_on_result_card_clicked(_card)
CraftReward.gd 相关变量：result_anchor、result_preview_card、current_result_card_id、slot_entries
CraftResultPreviewPresenter.gd::attach_result_preview(result_anchor, preview_card, click_callback)
```

### 当前职责与耦合点

`CraftReward.gd` 当前仍是合成奖励页的 composition root，负责选择主副卡、生成选择卡、生成槽位预览、计算配方结果、确认合成、写回牌组和关闭奖励页。

`_refresh_result_preview()` 的剩余耦合点：

```text
清理旧结果预览
检查两个槽位是否都有选择
计算配方结果 current_result_card_id
异步创建结果预览卡
结果槽 UI 刷新
结果描述刷新
连接线刷新
```

### 待拆清单

```text
P1：CraftReward.gd::_create_preview_card() 的异步预览创建边界，但它仍牵动临时牌堆、真实卡生成、数据写入和 tooltip 开关，下一批需重新评估。
P1：CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入和同步边界，暂不和预览流程同批处理。
P2：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界。
P2：DragShapeController.gd 放置完成流程。
```

### 本批风险面

本批只抽出结果预览卡挂载：

```text
result_anchor.add_child(preview_card)
preview_card.position = Vector2.ZERO
preview_card.set_selected(true) 兜底调用
preview_card.card_clicked.connect(_on_result_card_clicked) 兜底连接
```

不触碰：

```text
配方判断
预览卡异步创建
临时牌堆创建和释放
结果描述刷新
连接线刷新
确认合成和牌组写入
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftResultPreviewPresenter.gd
```

模块边界：

- `CraftResultPreviewPresenter.gd` 只负责把已创建的结果预览卡挂到结果槽。
- 它不创建预览卡，不读取配方，不更新结果描述或连接线。
- 它返回挂载后的 `Control`，由 `CraftReward.gd` 继续保存 `result_preview_card` 并调度后续刷新。

### 本批删除或收口的重复点

```text
result_anchor.add_child、position 归零、选中态设置和结果卡点击信号连接从 CraftReward.gd 收口到 CraftResultPreviewPresenter.gd。
主脚本保留配方判断、异步预览创建、结果说明和连接线刷新。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftResultPreviewPresenter.gd 条目。
docs/ai-handoff-ultimate-operation-guide.md 已刷新模块数量、Craft 剩余边界和下一步优先级。
```

### 回归检查

```text
覆盖率检查通过：122 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 CraftReward.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
craft_reward.tscn 仍输出退出时 ObjectDB/resource 占用提示，in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 已拆出配方查询、合成移除索引、选择条目构建、选择卡状态、连接线、预览清理、结果预览挂载、结果描述样式/内容/定位、选择标题、槽位占位符、槽位预览布局、奖励页通用卡牌读取模块和只读牌组来源模块。
_refresh_result_preview() 现在保留清理旧结果、槽位检查、配方结果计算、异步预览创建和后续刷新编排。
本批没有触碰 _create_preview_card()、确认合成和牌组写入。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd::_create_preview_card() 的异步预览创建边界。
如果 _create_preview_card() 需要同时传入临时牌堆、真实卡生成、数据写入、tooltip 开关和 deck_manager，就停止 CraftReward 预览创建拆分。
之后可转向 CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入边界，或 DragShapeController.gd 的放置完成流程。
```

## CraftReward.gd 预览卡 UI 配置拆分记录

日期：2026-06-07

### 本批目标

本批评估 `CraftReward.gd::_create_preview_card()` 后，没有直接搬走整条异步创建链。该函数同时负责临时牌堆、真实卡生成、数据写入和临时牌堆释放，整体拆出需要传入过多异步状态。最终只抽出数据写入完成后的预览卡 UI 配置。

目标文件：

```text
scene/in_scene/rewards/CraftReward.gd
scene/in_scene/rewards/presenters/CraftPreviewCardConfigurator.gd
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
CraftReward.gd::_create_preview_card(card_id, preview_size, tooltip_enabled)
CraftReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
CraftReward.gd::_create_temp_pile()
CraftReward.gd 相关变量：draft_card_scene、deck_manager、_temp_pile_factory、_real_card_spawner、_draft_card_data_applier
CraftPreviewCardConfigurator.gd::configure_preview_card(draft_card, preview_size, tooltip_enabled)
```

### 当前职责与耦合点

`CraftReward.gd` 当前仍负责合成奖励页主流程。`_create_preview_card()` 仍串联：

```text
创建临时牌堆
实例化 DraftCard
写入 card_id 和 custom_set_size
await _steal_card_data()
释放临时牌堆
配置预览卡尺寸、位置和 tooltip 鼠标过滤
```

整体拆出会把 `draft_card_scene`、`deck_manager`、临时牌堆、真实卡生成、数据写入和 tooltip 开关一起传入新模块，风险面过大。

### 待拆清单

```text
P1：CraftReward.gd::_create_preview_card() 剩余异步创建链，下一批应优先判断是否停止继续拆。
P1：CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入和同步边界。
P2：DragShapeController.gd 放置完成流程。
P2：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界。
```

### 本批风险面

本批只抽出预览卡 UI 配置：

```text
draft_card.custom_minimum_size = preview_size
draft_card.size = preview_size
draft_card.position = Vector2.ZERO
tooltip_enabled 为 false 时设置 mouse_filter
```

不触碰：

```text
临时牌堆创建和释放
DraftCard 实例化
真实卡生成
异步数据写入
结果预览挂载
确认合成和牌组写入
```

### 新增模块

```text
scene/in_scene/rewards/presenters/CraftPreviewCardConfigurator.gd
```

模块边界：

- `CraftPreviewCardConfigurator.gd` 只负责配置已完成数据写入的预览卡 UI 状态。
- 它不创建预览卡，不读取真实卡数据，也不管理临时牌堆生命周期。
- 它返回配置后的 `Control`，由 `CraftReward.gd::_create_preview_card()` 继续作为旧入口返回。

### 本批删除或收口的重复点

```text
custom_minimum_size、size、position 和 tooltip 鼠标过滤从 CraftReward.gd 收口到 CraftPreviewCardConfigurator.gd。
主脚本保留临时牌堆和异步数据写入编排。
```

### 文档同步

```text
docs/modularized-files-ultimate-operation-guide.md 已补充 CraftPreviewCardConfigurator.gd 条目。
docs/ai-handoff-ultimate-operation-guide.md 已刷新模块数量、Craft 剩余边界和下一步优先级。
```

### 回归检查

```text
覆盖率检查通过：123 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 CraftReward.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
craft_reward.tscn 仍输出退出时 ObjectDB/resource 占用提示，in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 已拆出配方查询、合成移除索引、选择条目构建、选择卡状态、连接线、预览清理、预览卡 UI 配置、结果预览挂载、结果描述样式/内容/定位、选择标题、槽位占位符、槽位预览布局、奖励页通用卡牌读取模块和只读牌组来源模块。
_create_preview_card() 现在只剩临时牌堆创建、DraftCard 实例化、异步真实卡数据写入、临时牌堆释放和调用预览卡 UI 配置模块。
整条 _create_preview_card() 异步链继续拆会传入过多状态，下一批应优先判断是否停止 Craft 预览创建拆分。
```

下一步计划：

```text
下一批优先判断 CraftReward.gd::_create_preview_card() 是否停止继续拆。
如果停止 Craft 预览创建拆分，就转向 CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入边界，或 DragShapeController.gd 的放置完成流程。
如果继续拆 Craft，必须只选临时牌堆生命周期或 DraftCard 实例化其中一个风险面，不要同时改异步数据写入。
```

## CraftReward.gd 预览 DraftCard 工厂复用记录

日期：2026-06-07

### 本批目标

本批继续评估 `CraftReward.gd::_create_preview_card()`。结论是剩余临时牌堆、真实卡生成、数据写入和释放临时牌堆属于同一异步编排，不建议继续硬拆整条链。本批只把 DraftCard 实例化和基础尺寸写入复用到既有 `RewardDraftCardFactory.gd`。

目标文件：

```text
scene/in_scene/rewards/CraftReward.gd
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
CraftReward.gd::_create_preview_card(card_id, preview_size, tooltip_enabled)
CraftReward.gd::_get_draft_card_factory()
RewardDraftCardFactory.gd::create_draft_card(draft_card_scene, card_id, display_size)
CraftReward.gd::_steal_card_data(card_id, draft_card, temp_pile)
CraftReward.gd::_apply_crafting_result_to_deck()
```

### 当前职责与耦合点

`CraftReward.gd::_create_preview_card()` 当前仍保留这些编排：

```text
创建临时牌堆
调用 RewardDraftCardFactory 创建 DraftCard 并写入 card_id/custom_set_size
await _steal_card_data()
释放临时牌堆
调用 CraftPreviewCardConfigurator 配置预览卡 UI 状态
```

剩余异步链如果继续拆，需要同时处理 `temp_pile` 生命周期、`deck_manager.card_factory`、真实卡生成、数据写入和 await 后节点有效性，风险面不再清晰。

### 待拆清单

```text
P1：CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入和同步边界。
P2：DragShapeController.gd 放置完成流程。
P2：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界。
P2：timeline_ui.gd 行动块表现或清理动画。
```

### 本批风险面

本批只复用已有 DraftCard 工厂：

```text
draft_card_scene.instantiate()
draft_card.card_id = card_id
draft_card.custom_set_size = preview_size
```

不触碰：

```text
临时牌堆创建和释放
_steal_card_data() 异步数据写入
预览卡 UI 配置
结果预览挂载
合成确认和牌组写入
```

### 本批删除或收口的重复点

```text
_create_preview_card() 不再直接实例化 DraftCard，也不直接写 card_id/custom_set_size。
这些逻辑与选择列表卡创建一样复用 RewardDraftCardFactory.gd。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新 Craft 下一步为 _apply_crafting_result_to_deck()。
docs/modularized-files-ultimate-operation-guide.md 已注明 _create_preview_card() 剩余异步链不建议继续硬拆。
```

### 回归检查

```text
覆盖率检查通过：123 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 CraftReward.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
craft_reward.tscn 仍输出退出时 ObjectDB/resource 占用提示，in_scene.tscn 仍输出既有 TileSet atlas 噪声，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的 _create_preview_card() 已复用 RewardDraftCardFactory 创建预览 DraftCard，并继续使用 CraftPreviewCardConfigurator 配置预览 UI。
_create_preview_card() 剩余临时牌堆、真实卡生成、异步数据写入和释放临时牌堆是同一条异步编排，不建议继续硬拆。
本批没有新增模块，已拆模块总数保持 123。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd::_apply_crafting_result_to_deck() 的牌组写入边界。
如果写入边界仍会和 RemoveReward 的删牌/同步规则重复或传入过多状态，就停止 CraftReward，转向 DragShapeController.gd 的放置完成流程。
ShopManager.gd 只在需要时评估临时牌堆生命周期，不再回到单商品生成硬拆。
```

## CraftReward.gd 合成结果牌组写入处理记录

日期：2026-06-07

### 本批目标

本批继续评估 `CraftReward.gd::_apply_crafting_result_to_deck()`。上一批已经把合成素材牌的倒序索引计算拆到 `CraftResultDeckIndexResolver.gd`，本批只收口真实牌组数组写入：按索引移除素材牌，并追加合成结果卡。运行时抽牌堆同步和奖励页关闭仍留在主脚本编排。

目标文件：

```text
scene/in_scene/rewards/CraftReward.gd
scene/in_scene/rewards/rules/CraftResultDeckWriteProcessor.gd
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
CraftReward.gd::_apply_crafting_result_to_deck()
CraftReward.gd::_get_result_deck_index_resolver()
CraftReward.gd::_get_result_deck_write_processor()
CraftResultDeckIndexResolver.gd::get_remove_indices(slot_entries, slot_1, slot_2)
CraftResultDeckWriteProcessor.gd::apply_result(player_deck, remove_indices, result_card_id)
RewardDeckSyncBridge.gd::sync_runtime_deck(owner, deck_manager)
```

### 当前职责与耦合点

`CraftReward.gd` 当前仍承担合成奖励页的页面编排：

```text
槽位选择和取消
配方结果刷新
预览卡异步创建
确认按钮动效
牌组写入入口
运行时抽牌堆同步
奖励页提交和关闭
```

最明显的耦合点：

```text
slot_entries 同时记录 UI 槽位、card_id 和原始牌组索引。
GlobalDB.player_deck 是真实持久牌组数据，写入后还需要 RewardDeckSyncBridge 同步局内抽牌堆。
_claim_result_card() 还要处理确认按钮、tooltip、reset、settlement_reward_committed 和 close()。
```

### 待拆清单

```text
P1：CraftReward.gd::CRAFTING_RECIPES 配方表资源化评估。
P1：DragShapeController.gd 放置完成流程的数据流梳理。
P2：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界。
P2：timeline_ui.gd 行动块表现或清理动画。
P2：out_scene_map_exp.gd 房间结算 payload 消费拆分。
```

### 本批风险面

本批只触碰 1 个清晰风险面：

```text
GlobalDB.player_deck 的 remove_at 和 append 写入细节。
```

不触碰：

```text
slot_entries 的生成和 deck_index 记录。
移除索引计算顺序。
RewardDeckSyncBridge 的运行时同步。
确认按钮动画、tooltip、reset 和 close()。
```

### 新增模块

```text
scene/in_scene/rewards/rules/CraftResultDeckWriteProcessor.gd
```

职责：

```text
只负责把合成结果写入传入的牌组数组。
不计算素材牌索引，不同步运行时抽牌堆，也不处理奖励页 UI 或关闭流程。
```

### Resource 化候选分析

本批只分析，不做 Resource 改造。Resource 适合承载可复用、可 Inspector 调参、默认只读的数据；运行态状态和场景节点不适合直接资源化。

适合优先注册成 Resource 的内容：

```text
P1：CraftReward.gd::CRAFTING_RECIPES。
原因：静态配方表，已经由 CraftRecipeResolver.gd 读取，适合拆成 CraftRecipeBook / CraftRecipeEntry。

P1：ShopManager.gd 的 base_price、price_increment、refresh_base_cost、upgrade_base_cost、时代权重。
原因：都是调参型策略数据，可以拆成 ShopPricingConfig / ShopEraWeightConfig。

P2：timeline_ui.gd 的网格、行动块、敌方意图 overlay 和清理动画表现参数。
原因：大量导出表现参数集中在 UI 层，适合 TimelineVisualConfig，但需要先避免和 TimelineManager 数据规则混在一起。

P2：hex_map.gd 的高度视图、敌方意图地图表现、入场动画、地貌投放配额。
原因：参数已经比较多，但 HexMap 是大 composition root，建议拆成多个小 Resource，而不是一个巨型 GameConfig。

P2：DragShapeController.gd 的拖拽动画、吸附速度、旋转/缩放时长和拒绝提示样式。
原因：表现调参数据多于规则数据，适合 DragPlacementVisualConfig。
```

暂不适合资源化的内容：

```text
GlobalDB.player_deck、map_data、stack_nodes、slot_entries、current_result_card_id。
临时牌堆、真实卡节点、DraftCard 节点、Tween、场景树查询结果。
需要 get_tree()、输入、动画回调或跨场景同步的行为。
```

下一批如果做 Resource，最稳目标是 `CRAFTING_RECIPES`。它是静态数据，改造面小，且可以保留旧字典 fallback，便于验证。

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 124，并加入 Resource 化候选总结。
docs/modularized-files-ultimate-operation-guide.md 已加入 CraftResultDeckWriteProcessor.gd 条目。
```

### 回归检查

```text
覆盖率检查通过：124 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 CraftReward.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
in_scene.tscn 仍输出退出时 RID/ObjectDB/resource 占用提示，不作为本批新增问题处理。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的 _apply_crafting_result_to_deck() 已拆出结果牌组写入处理，主脚本只保留移除索引解析、运行时抽牌堆同步和页面收尾编排。
CraftReward.gd 的预览创建链和确认写入链都不建议继续硬拆。
本批新增 1 个模块，已拆模块总数从 123 增至 124。
```

下一步计划：

```text
下一批优先评估 CraftReward.gd::CRAFTING_RECIPES 的 Resource 化，目标是 CraftRecipeBook / CraftRecipeEntry。
如果配方表资源化需要牵动场景、卡牌数据池或保存系统，就停止 Resource 改造，转向 DragShapeController.gd 的放置完成流程。
ShopManager.gd 后续只评估临时牌堆生命周期或定价/时代权重配置资源化，不再硬拆单商品生成编排。
```

## CraftReward.gd 合成配方资源化记录

日期：2026-06-07

### 本批目标

本批继续评估 `CraftReward.gd::CRAFTING_RECIPES`。结论是它是静态配方数据，适合注册成 Resource；本批只做配方表资源化，不触碰合成选择、预览异步链、牌组写入、运行时抽牌堆同步或奖励页关闭。

目标文件：

```text
scene/in_scene/rewards/CraftReward.gd
scene/in_scene/rewards/rules/CraftRecipeResolver.gd
scene/in_scene/rewards/resources/CraftRecipeBook.gd
scene/in_scene/rewards/resources/CraftRecipeEntry.gd
scene/in_scene/rewards/resources/default_craft_recipe_book.tres
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

`rg` 轮廓：

```text
CraftReward.gd::CRAFTING_RECIPES
CraftReward.gd::craft_recipe_book
CraftReward.gd::_get_recipe_result(card_a_id, card_b_id)
CraftRecipeResolver.gd::get_recipe_result(card_a_id, card_b_id, recipe_book, fallback_recipes)
CraftRecipeBook.gd::get_recipe_result(card_a_id, card_b_id)
CraftRecipeEntry.gd::card_a_id / card_b_id / result_card_id
```

### 当前职责与耦合点

`CraftReward.gd` 当前仍负责合成页 UI 编排：

```text
素材槽选择
配方结果刷新
预览卡异步创建
确认按钮与关闭
牌组写入入口
```

本批耦合点：

```text
_get_recipe_result() 会被选择列表过滤和结果预览刷新共同调用。
CraftRecipeResolver.gd 原本只接收 Dictionary。
新 Resource 脚本首次进入 Godot 时，class_name 全局类缓存可能尚未刷新。
```

### 待拆清单

```text
P1：ShopManager.gd 定价、刷新/升级费用和时代权重资源化评估。
P1：DragShapeController.gd 放置完成流程的数据流梳理。
P2：ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期等更小边界。
P2：timeline_ui.gd 行动块表现或清理动画。
P2：out_scene_map_exp.gd 房间结算 payload 消费拆分。
```

### 本批风险面

本批只触碰 1 个清晰风险面：

```text
合成配方静态数据来源从硬编码字典迁移为 CraftRecipeBook Resource。
```

涉及的 3 个小风险点：

```text
默认配方资源能被 CraftReward.gd preload。
CraftRecipeResolver.gd 优先读取 Resource，资源缺失时回退 CRAFTING_RECIPES。
Resource 脚本内部避免直接依赖新 class_name 类型，防止首次 headless 解析失败。
```

不触碰：

```text
slot_entries 和 current_result_card_id。
_refresh_result_preview() 异步预览创建。
_apply_crafting_result_to_deck() 牌组写入和同步。
Craft 关闭流程。
```

### 新增资源与模块

```text
scene/in_scene/rewards/resources/CraftRecipeBook.gd
scene/in_scene/rewards/resources/CraftRecipeEntry.gd
scene/in_scene/rewards/resources/default_craft_recipe_book.tres
```

职责：

```text
CraftRecipeBook.gd 只保存配方表并提供只读查询。
CraftRecipeEntry.gd 只记录单条配方的主卡、副卡和结果卡。
default_craft_recipe_book.tres 是默认配方数据资产，不处理逻辑。
```

### 本批实现注意

```text
CraftRecipeBook.gd 保留 class_name，方便编辑器识别。
CraftRecipeBook.gd 内部 recipes 使用 Array[Resource]，字段读取用 Resource.get()。
CraftReward.gd 的 craft_recipe_book 导出类型使用 Resource。
这样可以避免新 Resource 首次进入项目时，Godot 全局类缓存尚未刷新导致 Parse Error。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 126，并把下一优先级切到 Shop 配置资源化或 Drag 放置流程。
docs/modularized-files-ultimate-operation-guide.md 已新增 rewards/resources 分类和 CraftRecipeBook、CraftRecipeEntry、default_craft_recipe_book.tres 条目。
```

### 回归检查

```text
覆盖率检查通过：126 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 CraftReward.gd、CraftRecipeResolver.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/craft_reward.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
首次验证时发现新 Resource 脚本直接引用新 class_name 会导致 Craft 场景 Parse Error；本批已改为导出 Resource 和 Array[Resource]，并使用 Resource.get() 读取字段。
```

### 当前优化进度与下一步

当前进度：

```text
CraftReward.gd 的 CRAFTING_RECIPES 已资源化为 default_craft_recipe_book.tres。
CraftRecipeResolver.gd 优先读取 CraftRecipeBook，资源缺失时回退旧 CRAFTING_RECIPES 字典。
CraftReward.gd 的预览创建链、确认写入链和配方数据来源都已收口，不建议继续围绕 Craft 页面硬拆。
本批新增 2 个 Resource 脚本模块，已拆模块总数从 124 增至 126。
```

下一步计划：

```text
下一批优先评估 ShopManager.gd 的定价、刷新/升级费用和时代权重资源化。
如果 Shop 配置资源化需要牵动商品生成异步链，就停止 Shop，转向 DragShapeController.gd 的放置完成流程。
DragShapeController.gd 评估时先写清时间轴行动创建、卡牌归属变化和 UI 恢复边界，不要同批修改卡牌归属和 UI 清理。
```

## ShopManager.gd 商店定价资源化记录

日期：2026-06-07

### 本批目标

本批继续处理 `ShopManager.gd` 的 Resource 化候选，只拆“商店静态定价参数”这一处风险面。范围包括基础价格、商品位价格步进、刷新基础费用、升级基础费用和费用增量；不碰时代权重、商品生成异步链、购买流程、刷新/升级后的生成流程或临时牌堆生命周期。

### rg 轮廓

```text
scene/in_scene/rewards/ShopManager.gd
@export var base_price
@export var price_increment
@export var refresh_base_cost
@export var upgrade_base_cost
@export var pricing_config
func _on_refresh_pressed()
func _on_upgrade_pressed()
func _calculate_card_price(slot_index)
func _update_price_display()
func _get_base_price()
func _get_slot_price_step()
func _get_price_increment()
func _get_refresh_base_cost()
func _get_upgrade_base_cost()
func _get_pricing_config_value(property_name, fallback_value)

scene/in_scene/rewards/presenters/ShopPricingPresenter.gd
func calculate_card_price(slot_index, base_price, slot_price_step)
func calculate_refresh_cost(refresh_base_cost, refresh_count, price_increment)
func calculate_upgrade_cost(upgrade_base_cost, upgrade_count, price_increment)
func update_price_labels(...)

scene/in_scene/rewards/resources/ShopPricingConfig.gd
class_name ShopPricingConfig
extends Resource
@export var base_price
@export var slot_price_step
@export var price_increment
@export var refresh_base_cost
@export var upgrade_base_cost
```

### 当前职责与耦合点

`ShopManager.gd` 仍是商店页 composition root，负责编排打开商店、生成商品、购买、刷新、升级、关闭和调试输出。前面批次已经拆出价格展示、购买校验、刷新/升级费用结算、CardDataPool 桥接、商品槽注册、生成依赖检查等模块。

定价耦合点原本有三类：

```text
ShopManager.gd 的导出定价值。
ShopPricingPresenter.gd 内部的商品位价格步进硬编码 5。
刷新/升级处理器和价格标签刷新都直接读取 ShopManager 的导出值。
```

本批只把这些静态数值收口为 `ShopPricingConfig`。`refresh_count`、`upgrade_count` 和 `local_era_offset` 仍是运行态状态，不适合写入 Resource。

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `ShopPricingConfig.gd` | `_calculate_card_price()`、`_update_price_display()`、`_on_refresh_pressed()`、`_on_upgrade_pressed()` 的静态定价参数读取 | 只迁移只读调参数据，保留旧导出值 fallback | 不改时间币消费、刷新/升级结果、商品生成 |
| 2 | `ShopEraWeightConfig.gd` | `_select_card_by_era_weight()` 使用的四个时代权重 | 同样是静态调参数据，但会影响选卡概率 | 本批不碰时代权重 |
| 3 | 临时牌堆生命周期 | `_generate_shop_items()` 的 temp_pile 创建和销毁 | 可能形成小边界 | 本批不碰异步真实卡生成和数据提取 |

### 本批风险面

本批风险面：商店经济定价静态参数资源化。

涉及的 3 个小风险点：

```text
ShopManager.gd 新增 pricing_config Resource，并通过 getter 读取资源值。
ShopPricingPresenter.gd 的商品位价格步进从硬编码 5 改为调用方传入。
default_shop_pricing_config.tres 保存与旧行为一致的默认数值。
```

不触碰：

```text
时代权重 weight_current_era / weight_previous_era / weight_next_era / weight_next_next_era。
_generate_shop_items() 的选卡、临时牌堆、DraftCard 创建和异步数据提取。
_on_shop_card_clicked() 购买流程和飞入牌库流程。
refresh_count、upgrade_count、local_era_offset 等运行态计数。
```

### 新增资源与模块

```text
scene/in_scene/rewards/resources/ShopPricingConfig.gd
scene/in_scene/rewards/resources/default_shop_pricing_config.tres
```

职责：

```text
ShopPricingConfig.gd 只保存商店经济定价的静态参数。
default_shop_pricing_config.tres 是默认商店定价数据资产，不处理逻辑。
```

### 本批实现注意

```text
ShopManager.gd 的 pricing_config 导出类型使用 Resource，避免新 class_name 缓存未刷新造成解析风险。
旧的 base_price、price_increment、refresh_base_cost、upgrade_base_cost 暂时保留为 fallback，降低场景资源缺失时的风险。
slot_price_step 没有旧导出字段，因此 getter 使用旧硬编码值 5 作为 fallback，保持当前商品位定价行为不变。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 127，并把 Shop 下一步从“定价和时代权重”改为“时代权重”。
docs/modularized-files-ultimate-operation-guide.md 已新增 ShopPricingConfig.gd 和 default_shop_pricing_config.tres 条目。
```

### 回归检查

```text
覆盖率检查通过：127 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 ShopPricingPresenter.gd 和 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的商店静态定价数据已资源化为 default_shop_pricing_config.tres。
ShopPricingPresenter.gd 不再保留商品位价格步进硬编码。
ShopManager.gd 的旧导出定价值仍保留为 fallback，不建议下一批重复处理定价。
已拆模块总数从 126 增至 127。
```

下一步计划：

```text
下一批优先评估 ShopManager.gd 的时代权重资源化，目标是 ShopEraWeightConfig。
如果时代权重资源化需要牵动 _generate_shop_items() 的异步生成链，就停止 ShopManager，转向 DragShapeController.gd 放置完成流程。
DragShapeController.gd 评估时先写清时间轴行动创建、卡牌归属变化和 UI 恢复边界。
```

## ShopManager.gd 商店时代权重资源化记录

日期：2026-06-07

### 本批目标

本批继续处理 `ShopManager.gd` 的 Resource 化候选，只拆“商店时代权重静态参数”这一处风险面。范围包括当前时代、前一个时代、下一个时代和下两个时代的权重；不碰时代读取、随机选择算法、卡池读取、商品生成异步链、临时牌堆生命周期或购买流程。

### rg 轮廓

```text
scene/in_scene/rewards/ShopManager.gd
@export var weight_current_era
@export var weight_previous_era
@export var weight_next_era
@export var weight_next_next_era
@export var era_weight_config
func _select_card_by_era_weight(base_era)
func _get_weight_current_era()
func _get_weight_previous_era()
func _get_weight_next_era()
func _get_weight_next_next_era()
func _get_era_weight_config_value(property_name, fallback_value)

scene/in_scene/rewards/rules/ShopEraWeightSelector.gd
func select_era(base_era, weight_previous_era, weight_current_era, weight_next_era, weight_next_next_era)

scene/in_scene/rewards/resources/ShopEraWeightConfig.gd
class_name ShopEraWeightConfig
extends Resource
@export var weight_current_era
@export var weight_previous_era
@export var weight_next_era
@export var weight_next_next_era
```

### 当前职责与耦合点

`ShopManager.gd` 仍是商店页 composition root。前面批次已经拆出价格展示、购买路径、刷新/升级费用结算、CardDataPool 读取、商品槽注册、生成依赖检查和静态定价 Resource。

时代权重耦合点原本有两类：

```text
ShopManager.gd 直接导出四个时代权重。
_select_card_by_era_weight() 直接把这些导出值传入 ShopEraWeightSelector。
```

本批只把这些静态权重收口为 `ShopEraWeightConfig`。`base_era`、随机命中的 `selected_era`、卡池读取结果和最终卡牌 ID 仍是运行态流程，不适合写入 Resource。

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `ShopEraWeightConfig.gd` | `_select_card_by_era_weight()` 的四个静态权重读取 | 只迁移只读调参数据，保留旧导出值 fallback | 不改随机选择算法、不改卡池读取、不改商品生成 |
| 2 | 临时牌堆生命周期 | `_generate_shop_items()` 的 temp_pile 创建和销毁 | 可能形成更小边界 | 本批不碰异步真实卡生成和数据提取 |
| 3 | Drag 放置完成流程 | `_finish_placement()` 或等价流程 | 如果 Shop 继续变重，转向拖拽更合理 | 本批不碰拖拽 |

### 本批风险面

本批风险面：商店时代权重静态参数资源化。

涉及的 3 个小风险点：

```text
ShopManager.gd 新增 era_weight_config Resource，并通过 getter 读取资源值。
_select_card_by_era_weight() 保持旧入口，只把传给 ShopEraWeightSelector 的参数来源改为 getter。
default_shop_era_weight_config.tres 保存与旧行为一致的默认权重。
```

不触碰：

```text
ShopEraWeightSelector.gd 的随机选择算法。
_get_shop_era() 和 _get_global_era() 的时代读取。
_get_cards_by_era() 的 CardDataPool 读取。
_generate_shop_items() 的临时牌堆、DraftCard 创建和异步数据提取。
```

### 新增资源与模块

```text
scene/in_scene/rewards/resources/ShopEraWeightConfig.gd
scene/in_scene/rewards/resources/default_shop_era_weight_config.tres
```

职责：

```text
ShopEraWeightConfig.gd 只保存商店按时代选卡的静态权重参数。
default_shop_era_weight_config.tres 是默认商店时代权重数据资产，不处理逻辑。
```

### 本批实现注意

```text
ShopManager.gd 的 era_weight_config 导出类型使用 Resource，避免新 class_name 缓存未刷新造成解析风险。
旧的四个 weight_* 导出值暂时保留为 fallback，降低场景资源缺失时的风险。
ShopEraWeightSelector.gd 已经是纯规则模块，本批不修改它，避免改变随机命中行为。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 128，并把下一优先级切到 Drag 放置完成流程或 Shop 临时牌堆生命周期。
docs/modularized-files-ultimate-operation-guide.md 已新增 ShopEraWeightConfig.gd 和 default_shop_era_weight_config.tres 条目。
```

### 回归检查

```text
覆盖率检查通过：128 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有 CRLF/LF 提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
ShopManager.gd 的商店静态定价数据和时代权重数据都已资源化。
ShopManager.gd 的旧导出定价值与权重值仍保留为 fallback，不建议下一批重复处理 Shop 配置资源化。
已拆模块总数从 127 增至 128。
```

下一步计划：

```text
下一批优先评估 DragShapeController.gd 的放置完成流程。
如果用户希望继续看 ShopManager.gd，只评估 _generate_shop_items() 的临时牌堆生命周期；如果需要传入过多成员，就停止 ShopManager。
DragShapeController.gd 评估时先写清时间轴行动创建、卡牌归属变化和 UI 恢复边界。
```

## DragShapeController.gd 第二十批玩家行动创建拆分记录

日期：2026-06-07

### 本批目标

本批转向 `DragShapeController.gd` 的放置完成流程，但只拆最小边界：“玩家 TimelineAction 创建”。不拆 `timeline_manager.place_action()`，不改成功/失败分支，不改卡牌进入弃牌区，也不改 UI 预览清理。

### rg 轮廓

```text
scene/in_scene/DragShapeController.gd
const DragPlayerActionFactoryScript
var _player_action_factory
func _get_player_action_factory()
func _finish_placement(grid_pos)
func end_dragging_success()

scene/in_scene/drag_modules/rules/DragPlayerActionFactory.gd
func create_action(card, target_tile, shape_coords)

scene/in_scene/timeline/TimelineAction.gd
class_name TimelineAction
func _init(p_type, p_source, p_target, p_coords, p_color, p_data)
```

### 当前职责与耦合点

`DragShapeController.gd` 仍是拖拽系统 composition root，负责编排开始拖拽、时间轴 hover、放置校验、拒绝动画、放置动画、确认放置、clear 即时卡牌、成功后弃牌和 UI 收尾。

`_finish_placement()` 的耦合点包括：

```text
恢复地图和时间轴鼠标过滤。
创建玩家 TimelineAction。
调用 timeline_manager.place_action(action, grid_pos)。
发射 player_action_placed。
失败时播放拒绝动画并释放 is_placing。
成功后触发卡牌效果、清理预览、进入 end_dragging_success()。
```

本批只拆其中“创建玩家 TimelineAction”这一步，避免同批触碰时间轴写入和卡牌归属变化。

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `DragPlayerActionFactory.gd` | `_finish_placement()` 中 `TimelineAction.new(...)` | 只创建数据对象，不调用 `place_action`，不改卡牌归属 | 不改成功/失败流程、不改弃牌 |
| 2 | 时间轴提交模块 | `timeline_manager.place_action()` 与 `player_action_placed.emit(action)` | 边界可能清楚，但会触碰写入结果 | 本批不碰 |
| 3 | 成功收尾视觉清理 | `end_dragging_success()` 的 shader/状态恢复 | 状态细节多，需另批分析 | 本批不碰 |
| 4 | 弃牌归属移动 | `end_dragging_success()` 中 discard fallback | 涉及 CardManager/牌堆效果 | 本批不碰 |

### 本批风险面

本批风险面：玩家 `TimelineAction` 创建。

涉及的 3 个小风险点：

```text
新增 DragPlayerActionFactory.gd，集中创建 TimelineAction.Type.PLAYER。
DragShapeController.gd 增加 preload、缓存 getter 和 _ready() 初始化。
_finish_placement() 保留原流程，只把 TimelineAction.new(...) 替换为工厂调用。
```

不触碰：

```text
timeline_manager.place_action(action, grid_pos)。
player_action_placed.emit(action) 的触发时机。
_play_reject_animation() 失败回退。
_trigger_card_effect()、_clear_timeline_grid_preview() 和 end_dragging_success()。
current_card 移入弃牌区或返回手牌。
```

### 新增模块

```text
scene/in_scene/drag_modules/rules/DragPlayerActionFactory.gd
```

职责：

```text
DragPlayerActionFactory.gd 只负责从当前拖拽上下文创建玩家 TimelineAction。
它不调用 TimelineManager，不发射信号，也不移动卡牌到弃牌区。
```

### 本批实现注意

```text
工厂仍直接读取 card.card_info，保持与旧 TimelineAction.new(...) 参数一致。
shape_coords 仍由 TimelineAction 自己 duplicate，工厂不额外复制，避免行为差异。
DragShapeController.gd 仍决定何时提交时间轴、何时播放失败动画和何时进入成功收尾。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 129，并把 Drag 下一步切到时间轴提交边界。
docs/modularized-files-ultimate-operation-guide.md 已新增 DragPlayerActionFactory.gd 条目。
```

### 回归检查

```text
覆盖率检查通过：129 个已拆模块路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0。
git diff --check 通过，仅有 DragShapeController.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## 2026-06-07 OutScene 房间结算 payload 控制器拆分

### 读取与轮廓

本批继续处理 `scene/out_scene/out_scene_map_exp.gd`，先用 `rg` 输出了变量、导出项和函数轮廓。目标文件当前仍包含：

```text
_ready()
apply_external_event(payload)
_consume_pending_room_resolution()
_handle_room_resolution_payload(payload)
_should_advance_tier_from_boss_payload(payload)
_advance_tier_from_boss_resolution(_payload)
_get_reveal_coords_between_radii(previous_radius, next_radius)
_prepare_chapter_reveal_tiles(coords_list)
_execute_chapter_reveal_animation(coords_list, s)
_move_to(target)
_enter_room_logic(target)
_switch_scene_with_data(path, data)
```

### 当前职责

`out_scene_map_exp.gd` 是局外地图的 composition root，负责地图初始化/恢复、角色选路、玩家移动、进房切场景、房间结算返回、boss 后章节推进、章节揭示动画、镜头限制和局外 UI 进度刷新。

本批后，`RoomResolutionController.gd` 只负责局外房间结算 payload 的读取、解析和章节推进判断。它不播放章节揭示动画，不移动玩家，不切换场景，也不直接写入 `current_tier`。

### 耦合点

```text
pending_external_event 与 MapState.pending_room_resolution 是两条结算返回来源。
boss 推进判断需要读取 payload.room_context、battle_tag、tile_data、gen.layer_boundaries 和 current_tier。
真正推进章节仍会触发 current_tier、MapState.current_tier、apply_tier_camera_limit()、章节揭示动画和 _save_to_global()，这些仍留在主脚本。
地图移动、镜头动画和切场景都与玩家操作链强耦合，本批不碰。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `RoomResolutionController.gd` | `_consume_pending_room_resolution()`、`_should_advance_tier_from_boss_payload()`、`_advance_tier_from_boss_resolution()` 的纯计划部分 | payload 来源、boss 判定和 tier 半径计划是纯逻辑，边界清晰 | 执行 |
| 2 | 房间完成状态回写 controller | `_handle_room_resolution_payload()` 预留挂点 | 需要先确认完成/清空/奖励领取状态写到哪里 | 后续评估 |
| 3 | 章节揭示动画 runner | `_prepare_chapter_reveal_tiles()`、`_execute_chapter_reveal_animation()` | 可以拆，但牵动 camera、view.tiles、随机动画参数 | 暂停 |
| 4 | 地图移动/进房流程 | `_move_to()`、`_enter_room_logic()` | 同时牵动玩家、镜头、确认 UI、MapState 和切场景 | 不拆 |

### 本批风险面

本批风险面：局外房间结算 payload 控制器。

涉及的 3 个小风险点：

```text
新增 RoomResolutionController.gd，统一读取 pending_external_event 与 MapState.pending_room_resolution。
把 boss 房判定、room_hex 解析和 hex 距离判断移入新模块。
把 tier 推进的 previous_radius、next_tier、next_radius 计划移入新模块，真实推进和动画仍由 out_scene_map_exp.gd 执行。
```

不触碰：

```text
_move_to() 的玩家移动、确认 UI、镜头跟随和路径崩塌动画。
_enter_room_logic() 的 active_room_context 写入与切场景。
章节揭示动画的 tween、camera.focus_on_position() 和 view.tiles 状态。
MapState 的字段结构和 InScene 返回 payload 构造。
```

### 实现结果

```text
scene/out_scene/out_scene_modules/RoomResolutionController.gd
```

职责：

```text
RoomResolutionController 只负责局外房间结算 payload 的读取、解析和章节推进判断。
它不播放章节揭示动画，不移动玩家，不切换场景，也不直接写入 current_tier。
```

`out_scene_map_exp.gd` 新增 `RoomResolutionControllerScript` preload 和 `_get_room_resolution_controller()` 缓存入口。旧的 `_consume_pending_room_resolution()`、`_should_advance_tier_from_boss_payload()` 和 `_advance_tier_from_boss_resolution()` 继续保留原调用顺序，但纯判断委托给新模块。

### 当前优化进度与下一步

```text
已拆模块统计更新为 142 个脚本模块和 3 个默认 Resource 文件。
OutScene 现在有 1 个 out_scene_modules 脚本。
out_scene_map_exp.gd 从约 853 行降到约 830 行。
下一批如果继续局外地图，先评估“房间完成状态回写”是否能独立成纯规则/小 controller。
如果房间完成状态回写需要同时改 tile_data、tile_features、view.tiles、存档和切场返回，就暂停，改评估章节揭示动画 runner。
```

### 回归检查

```text
覆盖率检查通过：145 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 142 个，默认 Resource 文件为 3 个。
git diff --check 通过。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/out_scene/Out_Scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；场景加载仍输出既有 TileSetAtlasSource atlas tile 资源错误，本批未改 TileSet。
```

## 2026-06-07 OutScene 切场前 payload bridge 拆分

### 读取与轮廓

本批继续处理 `scene/out_scene/out_scene_map_exp.gd`。启动前已重新读取固定文档，并用 `rg` 输出目标文件、`MapState` 和切场 payload 相关轮廓。`MapState.path_gone` 是选角色后路径崩塌记录，不是“房间已完成”状态；因此本批不复用它做房间完成回写，避免制造错误语义。

### 当前职责

`out_scene_map_exp.gd` 仍是局外地图 composition root，负责地图初始化、玩家移动、切场景和生命周期编排。前面已经拆出房间结算 payload 控制器和章节揭示动画 runner；本批后，切场前 payload 注入由 `OutScenePayloadBridge.gd` 承接。

### 耦合点

```text
_switch_scene_with_data() 同时处理 PackedScene 加载、保存、实例化、payload 注入、挂树、current_scene 替换和 queue_free()。
payload 注入只需要知道 HexMap、MainBoard、根节点 apply_external_event 和旧 received_text 兜底路径。
ResourceLoader、_save_to_global()、get_tree().root.add_child()、current_scene 和 queue_free() 仍留在主脚本。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `OutScenePayloadBridge.gd` | `_apply_payload_before_scene_enters_tree()` | 只写 payload，不创建/销毁场景 | 执行 |
| 2 | 房间完成状态数据契约 | `_handle_room_resolution_payload()` 预留挂点 | 缺少既有状态字段，不直接写代码 | 暂缓 |
| 3 | 切场景 executor | `_switch_scene_with_data()` | 会牵动挂树、current_scene、queue_free 和失败恢复 | 不拆 |
| 4 | 地图移动确认流程 | `_move_to()` | 牵动玩家、镜头、确认 UI、路径崩塌和进房 | 不拆 |

### 本批风险面

本批风险面：切场前 payload 注入桥接。

涉及的 3 个小风险点：

```text
新增 OutScenePayloadBridge.gd，负责把 payload 写给 HexMap 和 MainBoard。
保留根节点 apply_external_event 兜底。
保留旧 Main/Node2D.received_text 兜底，通过主脚本传入属性检查 Callable。
```

不触碰：

```text
PackedScene 加载和失败恢复。
_save_to_global()。
新场景挂树、current_scene 替换和 queue_free()。
房间完成状态、tile_data、tile_features 和 MapState.path_gone 语义。
```

### 实现结果

```text
scene/out_scene/out_scene_modules/OutScenePayloadBridge.gd
```

职责：

```text
OutScenePayloadBridge 只负责在局外切入新场景前写入外部 payload。
它不加载场景，不挂树，不释放旧场景，也不解析 payload 内容。
```

`out_scene_map_exp.gd` 新增 `OutScenePayloadBridgeScript` preload 和 `_get_payload_bridge()` 缓存入口。旧 `_apply_payload_before_scene_enters_tree()` 保留，内部转发给新模块。

### 当前优化进度与下一步

```text
已拆模块统计更新为 144 个脚本模块和 3 个默认 Resource 文件。
OutScene 现在有 3 个 out_scene_modules 脚本：RoomResolutionController、ChapterRevealAnimationRunner、OutScenePayloadBridge。
out_scene_map_exp.gd 从约 805 行降到约 798 行。
下一批如果继续 OutScene，优先只做房间完成状态回写的数据契约设计，不直接写状态。
如果继续拆代码，切场景 executor 和地图移动流程都偏高风险；可转向镜头限制小模块或暂停 OutScene。
```

### 回归检查

```text
覆盖率检查通过：147 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 144 个，默认 Resource 文件为 3 个。
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/out_scene/Out_Scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
DragShapeController.gd 的放置完成流程已拆出玩家 TimelineAction 创建。
_finish_placement() 剩余时间轴提交、失败回退、效果触发、预览清理和成功收尾仍在主脚本中编排。
已拆模块总数从 128 增至 129。
```

下一步计划：

```text
下一批如果继续 Drag，优先评估 timeline_manager.place_action(action, grid_pos) 与 player_action_placed.emit(action) 是否能形成小提交模块。
如果提交模块需要吞掉失败动画、效果触发、预览清理和弃牌收尾，就停止 DragShapeController。
另一个可选方向是 ShopManager.gd 的临时牌堆生命周期，但不要回到 Shop 定价或时代权重 Resource。
```

## DragShapeController.gd 第二十一批时间轴提交拆分记录

日期：2026-06-07

### 本批目标

本批继续 `DragShapeController.gd` 的放置完成流程，但只拆最小边界：“已创建玩家 TimelineAction 后提交给 TimelineManager，并在成功时触发旧的玩家行动放置信号”。不拆失败动画，不拆卡牌效果，不拆预览清理，不拆成功收尾，也不移动卡牌到弃牌区。

### rg 轮廓

```text
scene/in_scene/DragShapeController.gd
const DragTimelineActionSubmitterScript
var _timeline_action_submitter
func _get_timeline_action_submitter()
func _finish_placement(grid_pos: Vector2i)
func _emit_player_action_placed(action: TimelineAction)

scene/in_scene/drag_modules/timeline/DragTimelineActionSubmitter.gd
func submit_action(timeline_manager, action, grid_pos, success_callback)

scene/in_scene/timeline/TimelineManager.gd
signal action_placed(action: TimelineAction)
func place_action(action: TimelineAction, origin: Vector2i) -> bool
```

### 当前职责

`DragShapeController.gd` 仍是拖拽系统 composition root，负责编排开始拖拽、hover、放置校验、拒绝动画、放置动画、确认放置、即时 clear 卡牌、效果触发、预览清理、成功后弃牌和 UI 收尾。

本批后，`DragTimelineActionSubmitter.gd` 只承担一个窄职责：调用 `TimelineManager.place_action(action, grid_pos)`，并在提交成功时调用主脚本传入的成功回调。它不创建 `TimelineAction`，不判断失败表现，也不处理成功收尾。

### 当前耦合点

```text
_finish_placement() 仍依赖 current_card、current_target_tile、current_shape_coords。
TimelineManager.place_action() 是实际写入时间轴的唯一入口。
player_action_placed 仍由 DragShapeController.gd 发射，保持旧对外信号位置。
成功后仍由 DragShapeController.gd 触发卡牌效果、清理时间轴预览并调用 end_dragging_success()。
失败后仍由 DragShapeController.gd 播放拒绝动画并释放 is_placing。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `DragTimelineActionSubmitter.gd` | `_finish_placement()` 中 `timeline_manager.place_action()` 与成功信号回调 | 只包装提交结果和成功回调，不接管失败/成功收尾 | 本批执行 |
| 2 | 成功收尾视觉清理 | `end_dragging_success()` 的卡牌 shader、scale、modulate、rotation、mouse_filter 恢复 | 可形成纯卡牌视觉状态恢复模块 | 本批不碰 |
| 3 | 弃牌归属移动 | `end_dragging_success()` 中 discard fallback 与手牌移除 | 可能形成牌堆移动小模块 | 本批不碰 |
| 4 | 成功后 UI 恢复 | `end_dragging_success()` 中 scene interaction 和 drag state 清理 | 涉及主控状态较多 | 本批不碰 |

### 本批风险面

本批风险面：时间轴提交和成功信号回调。

涉及的 3 个小风险点：

```text
新增 DragTimelineActionSubmitter.gd，集中调用 TimelineManager.place_action()。
DragShapeController.gd 增加 preload、缓存 getter 和 _ready() 初始化。
_finish_placement() 保留原失败与成功分支，只把提交和成功信号触发交给 submitter 回调。
```

不触碰：

```text
DragPlayerActionFactory.gd 的行动创建。
_play_reject_animation() 失败回退。
_trigger_card_effect()、_clear_timeline_grid_preview() 和 end_dragging_success()。
current_card 移入弃牌区或返回手牌。
TimelineManager.place_action() 内部规则。
```

### 新增模块

```text
scene/in_scene/drag_modules/timeline/DragTimelineActionSubmitter.gd
```

职责：

```text
DragTimelineActionSubmitter.gd 只负责把已创建的 TimelineAction 提交给 TimelineManager。
它不创建行动，不播放失败动画，不清理预览，也不移动卡牌到弃牌区。
```

### 本批实现注意

```text
submit_action() 在 timeline_manager 无效时返回 false，主脚本沿用失败动画分支。
submit_action() 只在 placement_success 为 true 且回调有效时调用 success_callback。
_emit_player_action_placed() 让 player_action_placed 仍由 DragShapeController.gd 发射，避免新模块直接拥有主脚本信号。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 130，并把 Drag 下一步切到成功收尾边界。
docs/modularized-files-ultimate-operation-guide.md 已新增 DragTimelineActionSubmitter.gd 条目。
```

### 回归检查

```text
覆盖率检查通过：133 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 130 个。
git diff --check 通过，仅有 DragShapeController.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
DragShapeController.gd 的放置完成流程已拆出玩家 TimelineAction 创建和时间轴提交。
_finish_placement() 剩余失败回退、效果触发、预览清理和成功收尾仍在主脚本中编排。
已拆模块总数从 129 增至 130。
```

下一步计划：

```text
下一批如果继续 Drag，优先评估 end_dragging_success() 中卡牌视觉状态恢复是否能形成纯视觉状态模块。
如果视觉恢复和弃牌归属、手牌同步或场景交互恢复纠缠过深，就停止拆该边界。
另一个可选方向是 ShopManager.gd 的临时牌堆生命周期，但不要回到 Shop 定价或时代权重 Resource。
```

## DragShapeController.gd 第二十二批成功卡牌视觉复原拆分记录

日期：2026-06-07

### 本批目标

本批继续 `DragShapeController.gd` 的放置成功收尾，但只拆最小边界：“成功放置后恢复卡牌自身视觉状态”。不拆 `drag_ended` 信号，不拆弃牌归属移动，不拆弃牌效果，不拆时间轴收起，也不改失败分支。

### rg 轮廓

```text
scene/in_scene/DragShapeController.gd
const DragSuccessCardVisualRestorerScript
var _success_card_visual_restorer
func _get_success_card_visual_restorer()
func end_dragging_success()

scene/in_scene/drag_modules/presenters/DragSuccessCardVisualRestorer.gd
func restore(card: Control) -> void
func _object_has_property(target: Object, property_name: StringName) -> bool
```

### 当前职责

`DragShapeController.gd` 仍是拖拽系统 composition root，负责编排拖拽结束状态、`drag_ended` 信号、卡牌放入弃牌区、弃牌效果、时间轴收起和主状态清理。

本批后，`DragSuccessCardVisualRestorer.gd` 只承担一个窄职责：成功放置后把当前卡牌自身视觉状态恢复到可进入弃牌区的状态，包括拖拽 shader、透明度、材质、缩放和鼠标过滤。它不处理牌堆、不处理时间轴，也不发射信号。

### 当前耦合点

```text
end_dragging_success() 仍决定 is_dragging/is_placing 的最终状态。
drag_ended 仍由 DragShapeController.gd 发射，保持旧对外信号位置。
弃牌区查找、move_cards()、_handle_discard_effects() 仍在 DragShapeController.gd。
时间轴 collapse 仍由 DragShapeController.gd 调用 DragTimelineUiStateController。
视觉复原模块只接收 current_card，不读取 controller 其他成员。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `DragSuccessCardVisualRestorer.gd` | `end_dragging_success()` 中卡牌 shader、材质、透明度、scale、mouse_filter 恢复 | 只处理卡牌自身表现，不接触牌堆和时间轴 | 本批执行 |
| 2 | 弃牌归属移动 | `end_dragging_success()` 中 `_find_discard_pile()`、`move_cards()`、弃牌效果和 fallback | 可形成牌堆移动小模块，但涉及 MainBoard 旧效果 | 本批不碰 |
| 3 | 成功后 UI 收尾 | `end_dragging_success()` 中时间轴 collapse 与主状态清理 | 主控状态较多，需另批分析 | 本批不碰 |
| 4 | `_return_card_to_hand()` 复原 fallback | 回退到手牌时的 card 状态复原和 hand.move_cards() | 与弃牌失败路径相关 | 本批不碰 |

### 本批风险面

本批风险面：成功放置后的卡牌自身视觉复原。

涉及的 3 个小风险点：

```text
新增 DragSuccessCardVisualRestorer.gd，集中恢复卡牌自身视觉状态。
DragShapeController.gd 增加 preload、缓存 getter 和 _ready() 初始化。
end_dragging_success() 保留旧信号、弃牌和时间轴收起，只把视觉复原块替换为 restore(current_card)。
```

不触碰：

```text
drag_ended.emit(current_card, true) 的触发时机。
_find_discard_pile()、move_cards()、_handle_discard_effects() 和 _return_card_to_hand()。
_get_timeline_ui_state_controller().collapse(timeline_ui)。
失败动画、效果触发和时间轴提交。
```

### 新增模块

```text
scene/in_scene/drag_modules/presenters/DragSuccessCardVisualRestorer.gd
```

职责：

```text
DragSuccessCardVisualRestorer.gd 只负责成功放置后恢复卡牌自身视觉状态。
它不发射拖拽信号，不移动卡牌到弃牌区，也不收起时间轴。
```

### 本批实现注意

```text
force_reset_visuals()、_set_shader(false) 和 set_card_transparency(1.0) 的调用顺序保持和旧逻辑一致。
card_current_state 仍设置为 0，保持旧 CustomCardState.IDLE 语义。
original_material 和 front_face_texture 的恢复仍带属性检查，避免假设所有卡牌都有这些字段。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 131，并把 Drag 下一步切到弃牌归属移动边界。
docs/modularized-files-ultimate-operation-guide.md 已新增 DragSuccessCardVisualRestorer.gd 条目。
```

### 回归检查

```text
覆盖率检查通过：134 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 131 个。
git diff --check 通过，仅有 DragShapeController.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
DragShapeController.gd 的放置完成流程已拆出玩家 TimelineAction 创建、时间轴提交和成功卡牌视觉复原。
end_dragging_success() 剩余 drag_ended 信号、弃牌归属移动、弃牌效果和时间轴收起仍在主脚本中编排。
已拆模块总数从 130 增至 131。
```

下一步计划：

```text
下一批如果继续 Drag，优先评估 end_dragging_success() 中弃牌归属移动是否能形成小模块。
如果弃牌移动需要同时接管 MainBoard 旧效果、手牌 fallback、时间轴收起和主状态清理，就停止拆该边界。
另一个可选方向是 ShopManager.gd 的临时牌堆生命周期，但不要回到 Shop 定价或时代权重 Resource。
```

## DragShapeController.gd 第二十三批成功弃牌移动拆分记录

日期：2026-06-07

### 本批目标

本批继续 `DragShapeController.gd` 的放置成功收尾，但只拆最小边界：“成功放置后把卡牌移入弃牌区并触发旧弃牌效果”。不拆回手牌 fallback，不拆时间轴收起，不拆主状态清理，也不改 `drag_ended` 信号。

### rg 轮廓

```text
scene/in_scene/DragShapeController.gd
const DragSuccessDiscardMoverScript
var _success_discard_mover
func _get_success_discard_mover()
func end_dragging_success()
func _return_card_to_hand(card: Node)

scene/in_scene/drag_modules/cards/DragSuccessDiscardMover.gd
func move_to_discard(card: Control, discard_pile: Node, main_board: Node) -> bool

scene/in_scene/drag_modules/bridges/DragShapeNodeBridge.gd
func get_main_board() -> Node
func find_discard_pile() -> Node
func find_player_hand() -> Node
```

### 当前职责

`DragShapeController.gd` 仍是拖拽系统 composition root，负责编排拖拽结束状态、`drag_ended` 信号、成功视觉复原、弃牌移动失败后的回手牌 fallback、时间轴收起和主状态清理。

本批后，`DragSuccessDiscardMover.gd` 只承担一个窄职责：当主脚本传入有效卡牌、弃牌区和 MainBoard 时，调用弃牌区 `move_cards([card])`，并延迟触发旧的 `_handle_discard_effects(card)`。它不查找任何节点，不处理失败回手牌，也不收起时间轴。

### 当前耦合点

```text
end_dragging_success() 仍负责查找 discard_pile_node 和 main_board。
DragSuccessDiscardMover.gd 只返回 bool，失败时由 DragShapeController.gd 调用 _return_card_to_hand(current_card)。
_return_card_to_hand() 仍负责手牌查找、回手牌、时间轴 fallback 展开、tooltip 隐藏和状态清空。
时间轴 collapse 仍由 DragShapeController.gd 调用 DragTimelineUiStateController。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 低风险原因 | 暂不触碰 |
| --- | --- | --- | --- | --- |
| 1 | `DragSuccessDiscardMover.gd` | `end_dragging_success()` 中 `move_cards()` 与 `_handle_discard_effects()` | 只接收已查好的节点，只返回成功/失败 | 本批执行 |
| 2 | 成功后 UI/时间轴收起 | `end_dragging_success()` 中 `collapse(timeline_ui)` | 已有 `DragTimelineUiStateController`，但单行 wrapper 收益很低 | 本批不碰 |
| 3 | `_return_card_to_hand()` fallback 收尾 | 回手牌、timeline toggle、tooltip、状态清空 | 触碰手牌、UI 和主状态，风险更高 | 本批不碰 |
| 4 | 判断停止 Drag 成功收尾拆分 | 剩余代码接近 composition root 编排 | 继续拆可能只降低行数 | 本批不碰 |

### 本批风险面

本批风险面：成功放置后的弃牌区移动和旧弃牌效果触发。

涉及的 3 个小风险点：

```text
新增 DragSuccessDiscardMover.gd，集中调用 discard_pile.move_cards([card])。
DragShapeController.gd 增加 preload、缓存 getter 和 _ready() 初始化。
end_dragging_success() 保留旧节点查找和 fallback，只把弃牌移动块替换为 move_to_discard(...)。
```

不触碰：

```text
drag_ended.emit(current_card, true) 的触发时机。
DragSuccessCardVisualRestorer.gd 的视觉复原。
_return_card_to_hand() 的回手牌 fallback 和状态清空。
_get_timeline_ui_state_controller().collapse(timeline_ui)。
失败动画、效果触发、时间轴提交和 TimelineManager。
```

### 新增模块

```text
scene/in_scene/drag_modules/cards/DragSuccessDiscardMover.gd
```

职责：

```text
DragSuccessDiscardMover.gd 只负责成功放置后把卡牌移入弃牌区并触发旧弃牌效果。
它不查找节点，不处理回手牌 fallback，也不收起时间轴。
```

### 本批实现注意

```text
move_to_discard() 在 card、discard_pile 或 move_cards() 无效时返回 false，让主脚本沿用旧 fallback。
旧的 MainBoard._handle_discard_effects(card) 仍使用 call_deferred，保持原触发时机。
main_board 无效不会阻止卡牌进入弃牌区，只跳过旧弃牌效果，保持旧逻辑。
```

### 文档同步

```text
docs/ai-handoff-ultimate-operation-guide.md 已更新模块数为 132，并把 Drag 下一步切到成功后 UI/时间轴收起或停止拆分评估。
docs/modularized-files-ultimate-operation-guide.md 已新增 DragSuccessDiscardMover.gd 条目。
```

### 回归检查

```text
覆盖率检查通过：135 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 132 个。
git diff --check 通过，仅有 DragShapeController.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
DragShapeController.gd 的放置完成流程已拆出玩家 TimelineAction 创建、时间轴提交、成功卡牌视觉复原和成功弃牌移动。
end_dragging_success() 剩余 drag_ended 信号、弃牌失败 fallback 和时间轴收起仍在主脚本中编排。
已拆模块总数从 131 增至 132。
```

下一步计划：

```text
下一批如果继续 Drag，优先评估 end_dragging_success() 中成功后 UI/时间轴收起是否值得拆。
如果只剩 collapse(timeline_ui) 这种单行 wrapper，就停止 DragShapeController 的成功收尾拆分。
另一个可选方向是 ShopManager.gd 的临时牌堆生命周期，但不要回到 Shop 定价或时代权重 Resource。
```

## ShopManager.gd 临时牌堆隐藏创建收口记录

### 本批启动前约束

本批继续遵守当前对话中的 AGENTS 约束：先分析目标文件，再列待拆清单，最后每批只拆 1 个清晰风险面，最多触碰 3 到 4 个风险点。仓库内未发现 `AGENTS.md` 时，以用户贴出的约束为准。

本批先评估上批留下的两个候选：

```text
DragShapeController.gd 的成功后 UI/时间轴收起。
ShopManager.gd::_generate_shop_items() 的临时牌堆生命周期。
```

### rg 轮廓摘要

```text
scene/in_scene/DragShapeController.gd
end_dragging_success()
_return_card_to_hand(card)
_get_timeline_ui_state_controller().collapse(timeline_ui)

scene/in_scene/rewards/ShopManager.gd
signal reward_scene_close_requested(scene_instance)
var _is_generating
var _temp_pile_factory
func _generate_shop_items()
func _steal_card_data(card_id, draft_card, temp_pile)
func _create_temp_pile()

scene/in_scene/rewards/factory/RewardTempPileFactory.gd
const PILE_SCENE
func create_temp_pile(deck_manager, owner_label)
```

### 当前职责

`DragShapeController.gd` 仍是拖拽放置流程的 composition root，负责编排拖拽结束信号、成功视觉恢复、弃牌移动、失败 fallback、时间轴收起和主状态清理。

`ShopManager.gd` 仍是商店页 composition root，负责编排商店打开、商品生成锁、旧商品清理、生成依赖检查、时代读取、选卡、DraftCard 创建、真实卡异步数据提取、定价、商品槽注册、购买、刷新、升级和退出。

`RewardTempPileFactory.gd` 是奖励页共享 factory，负责创建临时幽灵牌堆。

### 耦合点

```text
Drag 的 collapse(timeline_ui) 已经由 DragTimelineUiStateController 承担；继续包一层只会制造单行 wrapper。
Drag 的 _return_card_to_hand() 同时触碰 hand、timeline_ui、tooltip、current_target_tile、current_shape_coords、current_card 和 is_timeline_clear_mode。
Shop 的单个商品生成会牵动 draft_card_factory、temp_pile、deck_manager、真实卡异步生成、DraftCard 数据写入、价格和 UI 注册。
Shop 的 temp_pile.visible = false 是生成前的基础可见性设置，能形成更小边界。
```

### 待拆清单

| 优先级 | 候选 | 范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 临时牌堆隐藏创建 | `_generate_shop_items()` 中创建 temp_pile 后立刻 `visible = false` | 只影响创建后基础状态，不碰异步生成和释放 | 执行 |
| 2 | 临时牌堆释放生命周期 | `_generate_shop_items()` 尾部 `temp_pile.queue_free()` | 与异步生成循环绑定，若提前抽出会牵动异常路径 | 暂停 |
| 3 | 单个商品生成 | 选卡、DraftCard、数据提取、定价、注册 | 需要传入过多状态 | 不拆 |
| 4 | Drag 成功后 UI 收起 | `collapse(timeline_ui)` | 已经是现有模块单行调用 | 停止 |
| 5 | Drag 回手牌 fallback | `_return_card_to_hand()` | 同时触碰手牌、时间轴、tooltip 和主状态 | 停止 |

### 本批风险面

本批风险面：把 Shop 临时牌堆“创建后隐藏”收口到 `RewardTempPileFactory.gd`。

涉及的 3 个小风险点：

```text
RewardTempPileFactory.gd 新增 create_hidden_temp_pile()，复用旧 create_temp_pile()。
ShopManager.gd 的 _create_temp_pile() 改为调用 create_hidden_temp_pile()。
_generate_shop_items() 移除散落的 temp_pile.visible = false，不改变生成循环、异步数据提取或 queue_free()。
```

不触碰：

```text
_steal_card_data() 的异步真实卡生成与 DraftCard 数据写入。
_generate_shop_items() 的商品循环、选卡、价格和 UI 注册。
Shop 购买、刷新、升级和退出流程。
Acquire/Remove/Craft 的旧 create_temp_pile() 调用语义。
```

### 实现结果

```text
RewardTempPileFactory.gd 增加 create_hidden_temp_pile(deck_manager, owner_label)。
ShopManager.gd 继续保留旧 _create_temp_pile() wrapper，但内部改用隐藏创建入口。
docs/ai-handoff-ultimate-operation-guide.md 刷新当前完成度、优先级和停止点。
docs/modularized-files-ultimate-operation-guide.md 更新 RewardTempPileFactory 入口和后续优化方向。
```

### 当前优化进度与下一步

```text
DragShapeController.gd 成功收尾本轮确认停止继续硬拆：collapse 已是单行模块调用，fallback 收尾耦合过多。
ShopManager.gd 的隐藏临时牌堆创建已收口，单个商品生成和释放生命周期暂不硬拆。
下一批优先评估 timeline_ui.gd 的行动块视觉 presenter 或清理动画 runner。
另一个优先方向是 out_scene_map_exp.gd 的房间结算 payload 消费拆分，不动地图移动。
```

## ShopManager.gd 临时牌堆隐藏创建最终验证记录

### 回归检查

```text
覆盖率检查通过：135 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 132 个，默认 Resource 文件为 3 个。
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/rewards/shop.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

### 当前优化进度与下一步

当前进度：

```text
已拆脚本模块仍为 132 个，默认 Resource 文件仍为 3 个；本批没有新增模块，只扩展了 RewardTempPileFactory 的隐藏创建入口。
ShopManager.gd 的隐藏临时牌堆创建已从 _generate_shop_items() 收口到 RewardTempPileFactory.create_hidden_temp_pile()。
DragShapeController.gd 成功收尾本轮确认停止继续硬拆：collapse 已是单行模块调用，fallback 收尾耦合过多。
ShopManager.gd 的单个商品生成和临时牌堆释放生命周期暂不硬拆。
```

下一步计划：

```text
下一批优先评估 timeline_ui.gd 的行动块视觉 presenter 或清理动画 runner。
另一个优先方向是 out_scene_map_exp.gd 的房间结算 payload 消费拆分，不动地图移动。
如果继续 Resource 化，转向 timeline/drag/hex_map 的纯视觉调参配置；不要把临时牌堆、真实卡节点或 DraftCard 节点注册成 Resource。
```

## timeline_ui.gd 第九批清理动画残影创建拆分记录

### 本批启动前约束

本批继续遵守当前对话中的 AGENTS 约束：先分析目标文件，再列待拆清单，最后每批只拆 1 个清晰风险面，最多触碰 3 到 4 个风险点。仓库内未发现 `AGENTS.md`，以用户贴出的约束为准。

### rg 轮廓摘要

```text
scene/in_scene/timeline/timeline_ui.gd
const TimelineBlockPlacementAnimatorScript
var action_containers
var hovered_action
var current_enemy_intent_preview_action
func _on_action_placed(action)
func animate_action_removal(action, reason)
func _create_action_removal_ghost(source_container, action_id, reason)
func _strip_action_container_runtime_effects(node)

scene/in_scene/timeline/TimelineActionShapeVisual.gd
func clone_visual()

scene/in_scene/timeline/ui_modules/animation/TimelineBlockPlacementAnimator.gd
func animate_block_placement(block, target_color, tween_factory)
```

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager` 信号、维护行动容器字典、创建行动容器、处理 hover 状态、敌方意图预览、移除动画编排和 UI 清理。

本批后，`TimelineActionRemovalGhostBuilder.gd` 只承担一个窄职责：从即将移除的行动容器复制一份干净残影。它不启动 tween，不修改 `TimelineManager` 数据，不维护 `action_containers`，也不清理原容器。

### 耦合点

```text
_on_action_placed() 同时处理容器几何、Panel 创建、EnemyIntentOverlay、intro 动画和 hover 信号，暂不硬拆。
animate_action_removal() 同时处理 preview 清理、hover 信号复位、容器字典移除、残影动画和释放，仍留在主脚本编排。
_create_action_removal_ghost() 只读取原容器的几何、StyleBox 和 TimelineActionShapeVisual 纯绘制参数，输入输出清楚。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `TimelineActionRemovalGhostBuilder.gd` | `_create_action_removal_ghost()` | 纯视觉节点复制，不碰时间轴数据和状态字典 | 执行 |
| 2 | 清理动画 tween runner | `animate_action_removal()` 的 tween 段 | 可拆，但会和 ghost 生命周期绑定 | 暂停 |
| 3 | 行动块 hover presenter | `_on_block_hovered()` / `_on_block_exited()` | 边界较小，但涉及 TimelineManager 信号和 action 类型 | 后续评估 |
| 4 | 行动块生成 builder | `_on_action_placed()` | 牵动容器、格子、overlay、intro 和 hover | 不拆 |

### 本批风险面

本批风险面：清理动画残影创建。

涉及的 3 个小风险点：

```text
新增 TimelineActionRemovalGhostBuilder.gd，复制 Control/Panel/TimelineActionShapeVisual 的纯视觉状态。
timeline_ui.gd 增加 preload、缓存 getter 和旧 _create_action_removal_ghost() 转发。
docs 和 workflow 更新模块统计，把 timeline/ui_modules 纳入已拆模块覆盖。
```

不触碰：

```text
TimelineManager 数据结构和 remove_action_with_fade()。
animate_action_removal() 的 hover 信号复位、action_containers.erase() 和 tween 顺序。
EnemyIntentOverlay 的材质创建与显示逻辑。
_on_action_placed() 的行动块生成流程。
```

### 实现结果

```text
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd
```

职责：

```text
TimelineActionRemovalGhostBuilder.gd 只负责从即将移除的时间轴行动容器生成清理动画残影。
它不启动 Tween，不修改 TimelineManager 数据，不维护 action_containers，也不清理原行动容器。
```

`timeline_ui.gd` 保留 `_create_action_removal_ghost()` 旧入口，内部转发给新模块。残影创建之后的运行时效果卸载、原容器释放、字典清理和 tween 动画仍由 `timeline_ui.gd` 编排。

### 当前优化进度与下一步

```text
TimelineUI 已拆出 9 个 ui_modules 脚本：布局 2、网格 3、桥接 1、presenter 1、animation 2。
已拆脚本模块统计口径修正为包含 timeline/ui_modules，本批后为 141 个脚本模块和 3 个默认 Resource 文件。
timeline_ui.gd 从约 837 行降到约 804 行。
下一批优先转向 out_scene_map_exp.gd 的房间结算 payload 消费拆分。
如果继续 timeline_ui.gd，只评估 hover 信号/表现或清理动画 tween runner，不硬拆 _on_action_placed()。
```

### 回归检查

```text
覆盖率检查通过：144 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 141 个，默认 Resource 文件为 3 个。
git diff --check 通过，仅有 timeline_ui.gd 和 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
```

## 2026-06-07 最新状态：OutScene 房间结算 payload 控制器已完成

这段是当前文件的最新尾部状态，供下一批继续时优先读取。

```text
已拆模块统计：142 个脚本模块 + 3 个默认 Resource 文件。
docs/modularized-files-ultimate-operation-guide.md 覆盖缺失：0。
out_scene_map_exp.gd 已拆出房间结算 payload 读取、boss 推进判断、room_hex 解析和 tier 推进计划。
地图移动、镜头限制、章节揭示动画执行和切场景仍留在 out_scene_map_exp.gd。
下一批优先评估房间完成状态回写；如果需要同时改 tile_data、tile_features、view.tiles、存档和切场返回，就暂停，改评估章节揭示动画 runner。
```

## 2026-06-07 OutScene 章节揭示动画 runner 拆分

### 读取与轮廓

本批继续处理 `scene/out_scene/out_scene_map_exp.gd`。启动前已重新读取固定文档，并用 `rg` 输出目标文件轮廓。房间完成状态回写经过搜索后没有发现既有“已完成/已清空/已领奖”的局外状态字段，当前只有 `tile_data`、`tile_features`、`view.tiles` 和 `MapState` 保存。

### 当前职责

`out_scene_map_exp.gd` 仍是局外地图 composition root。上一批已把结算 payload 读取、boss 推进判断和 tier 推进计划拆到 `RoomResolutionController.gd`；本批后，章节揭示地块的初始视觉状态、升起 tween 和收尾 tween 由 `ChapterRevealAnimationRunner.gd` 执行。

### 耦合点

```text
房间完成状态回写会牵动 tile_data、tile_features、view.tiles、MapState 保存和房间返回 payload，目前缺少明确数据契约，暂缓。
章节揭示动画只读写 view.tiles 中的 Sprite2D，依赖 _hex_to_pixel()、随机延迟、动画参数和 create_tween()。
镜头聚焦、current_tier 推进、MapState.current_tier、apply_tier_camera_limit() 和 _save_to_global() 仍留在 out_scene_map_exp.gd。
```

### 待拆清单

| 优先级 | 候选模块 | 当前函数范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | `ChapterRevealAnimationRunner.gd` | `_prepare_chapter_reveal_tiles()`、`_execute_chapter_reveal_animation()` | 纯动画执行，不决定揭示范围，不改 tier 和保存 | 执行 |
| 2 | 房间完成状态回写 controller | `_handle_room_resolution_payload()` 预留挂点 | 缺少既有状态字段，容易变成玩法设计 | 暂缓 |
| 3 | 地图移动确认流程 | `_move_to()` | 牵动玩家、镜头、确认 UI、路径崩塌和进房 | 不拆 |
| 4 | 切场景 payload 注入 | `_switch_scene_with_data()`、`_apply_payload_before_scene_enters_tree()` | 可评估，但优先级低于状态契约 | 后续 |

### 本批风险面

本批风险面：章节揭示动画 runner。

涉及的 3 个小风险点：

```text
新增 ChapterRevealAnimationRunner.gd，负责新章节地块的初始位置、透明度、旋转和缩放。
把升起 tween 和收尾 tween 从 out_scene_map_exp.gd 移入新模块。
out_scene_map_exp.gd 保留旧 _prepare_chapter_reveal_tiles() 和 _execute_chapter_reveal_animation() 入口，转发给新模块。
```

不触碰：

```text
current_tier 推进和 MapState.current_tier 写入。
camera.focus_on_position() 的镜头聚焦。
_get_reveal_coords_between_radii() 的揭示范围判断。
地图移动、路径崩塌、进房和切场景。
```

### 实现结果

```text
scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd
```

职责：

```text
ChapterRevealAnimationRunner 只负责局外新章节地块的揭示动画。
它不推进 current_tier，不移动镜头，不保存 MapState，也不决定哪些地块需要揭示。
```

`out_scene_map_exp.gd` 新增 `ChapterRevealAnimationRunnerScript` preload、缓存 getter 和动画配置 getter。旧入口仍保留，调用顺序不变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 143 个脚本模块和 3 个默认 Resource 文件。
OutScene 现在有 2 个 out_scene_modules 脚本：RoomResolutionController、ChapterRevealAnimationRunner。
out_scene_map_exp.gd 从约 830 行降到约 805 行。
下一批如果继续 OutScene，优先只做房间完成状态回写的数据契约评估，不直接写新状态。
如果要继续拆代码，优先评估切场景 payload 注入或镜头限制小模块；地图移动流程暂不拆。
```

### 回归检查

```text
覆盖率检查通过：146 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 143 个，默认 Resource 文件为 3 个。
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access。
Godot 加载 res://scene/out_scene/Out_Scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；场景加载仍输出既有 TileSetAtlasSource atlas tile 资源错误，本批未改 TileSet。
```

## 2026-06-07 OutScene 房间完成状态回写数据契约评估

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内未发现 `AGENTS.md`，本批以当前对话中用户贴出的约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/out_scene/out_scene_map_exp.gd
scene/out_scene/out_scene_modules/RoomResolutionController.gd
scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd
scene/out_scene/out_scene_modules/OutScenePayloadBridge.gd
scene/global/map_data.gd
scene/global/Saver.gd
scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd
scene/in_scene/in_scene_modules/scene_flow/InSceneReturnFlowController.gd
```

本批只审查局外房间完成状态的数据契约，不改 GDScript 运行逻辑。

### 当前职责

`out_scene_map_exp.gd` 仍是局外地图 composition root，负责地图生成/恢复、玩家移动、进房、切场、保存和章节推进。`RoomResolutionController.gd` 负责消费局内返回 payload、解析 `room_context.room_hex`、判断 boss 房是否推进章节和构造 tier 推进计划。`MapState.active_room_context` 只记录当前正在进入的房间，`MapState.pending_room_resolution` 只暂存一次局内返回局外的结算 payload。

### 耦合点

```text
path_gone 只在局外移动/选角后的路径坍塌流程里追加，语义不是房间完成状态。
active_room_context 是进房上下文，pending_room_resolution 是一次性返回桥接，二者都不适合作为持久完成状态。
tile_data 当前是坐标到房间类型整数的逻辑地图；如果直接混入完成状态，会影响 map_renderer.gd、移动判断和存档恢复。
真正实现房间完成状态会同时牵动 MapState、Saver、out_scene_map_exp.gd 的结算消费入口，以及可能的局外视觉刷新。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 房间完成状态数据契约 | 现有保存字段与返回 payload 语义 | 需要先确认独立字段，不复用 `path_gone` | 执行文档评估 |
| 2 | 房间完成状态实现 | `MapState`、`Saver`、`out_scene_map_exp.gd`、视图刷新 | 会触碰持久化和视觉规则 | 暂不写代码 |
| 3 | 房间完成视觉表达 | `map_renderer.gd` 或局外 tile sprite 状态 | 需要先有状态字段 | 暂不处理 |
| 4 | 奖励/事件房差异化结算 | payload 分流与房间类型规则 | 玩法契约未定 | 暂不处理 |

### 本批风险面

本批风险面：房间完成状态回写的数据契约评估。

涉及的 3 个小风险点：

```text
确认 path_gone 不适合复用为房间完成状态。
确认 active_room_context 和 pending_room_resolution 只适合做跨场景桥接。
确认后续如实现，需要新增独立 MapState 字段和 Saver 持久化字段。
```

不触碰：

```text
tile_data、tile_features 和 view.tiles 的运行时结构。
地图移动、路径坍塌、进房和切场景。
current_tier 推进、镜头限制和章节揭示动画。
```

### 实现结果

本批没有新增脚本模块，也没有修改 GDScript。只更新总结文档和当前流程归档：

```text
docs/ai-handoff-ultimate-operation-guide.md
docs/modularized-files-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

契约结论：

```text
房间完成状态必须使用独立字段，不复用 path_gone。
临时 payload 和 active room context 只能作为跨场景桥接，不能作为长期状态。
后续如实现，建议单独小批新增 MapState 房间状态字典、Saver 保存/读取和局外结算消费入口。
```

### 当前优化进度与下一步

```text
已拆模块统计保持为 144 个脚本模块和 3 个默认 Resource 文件。
docs/modularized-files-ultimate-operation-guide.md 覆盖缺失目标仍为 0；本批没有新增模块路径。
下一批如果继续 OutScene，可在确认字段命名和视觉语义后，单独实现房间完成状态字段与存档；不要同批改移动、镜头和切场。
如果继续代码拆分，优先评估 timeline_ui.gd 的 hover 表现边界或清理动画 tween runner。
```

### 回归检查

```text
git diff --check 通过，仅有 workflow_logs/current-modularization-process.md 的既有换行提示。
覆盖率检查通过：147 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 144 个，默认 Resource 文件为 3 个。
本批只改 Markdown，没有运行 Godot headless 场景加载。
```

## 2026-06-07 TimelineUI 清理动画 tween runner 拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内未发现 `AGENTS.md`，本批以当前对话中用户贴出的约束为准。随后用 `rg` 输出了以下目标轮廓：

```text
scene/in_scene/timeline/timeline_ui.gd
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalGhostBuilder.gd
scene/in_scene/timeline/ui_modules/animation/TimelineBlockPlacementAnimator.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd
scene/in_scene/timeline/ui_modules/grid/TimelineGridPreviewPresenter.gd
scene/in_scene/timeline/ui_modules/grid/TimelineGridCellInteractionPresenter.gd
```

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager`，维护 `action_containers` 和 `hovered_action`，创建/移除行动容器，触发敌方意图 overlay、放置动画、清理残影动画和网格交互转发。

### 耦合点

```text
_on_action_placed() 同时创建行动容器、格子 Panel、overlay、intro 动画和 hover 信号，暂不硬拆。
_on_block_hovered() / _on_block_exited() 会同步 hovered_action 与 TimelineManager.action_hovered_changed，影响地图和敌方意图联动，本批不动。
animate_action_removal() 的残影创建和 tween 播放相邻，但 tween 播放只依赖 ghost、颜色、位移、缩放、时长和 tween factory，可作为纯表现 runner 拆出。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | 清理动画 tween runner | `animate_action_removal()` 残影播放与释放 | 纯表现边界，风险小 | 执行 |
| 2 | hover 信号/表现边界 | `_on_block_hovered()`、`_on_block_exited()` | 会碰行动 hover 和地图联动 | 暂缓 |
| 3 | 行动块生成 presenter | `_on_action_placed()` | 牵动容器、格子、overlay、intro、信号 | 不拆 |
| 4 | TimelineVisualConfig | 多个表现参数 | Resource 化会碰 class_name 缓存和导出参数 | 不同批处理 |

### 本批风险面

本批只拆一个风险面：时间轴行动移除残影的 tween 播放。

涉及的 3 个小风险点：

```text
新增 TimelineActionRemovalAnimator.gd，只负责播放残影淡出、下落、缩放并释放残影。
timeline_ui.gd 保留动画触发时机和残影创建，只把 tween 播放委托出去。
不修改 TimelineManager 数据结构、敌人意图规则、行动块生成或 hover 信号语义。
```

不触碰：

```text
_on_action_placed() 的行动块生成流程。
_on_block_hovered() / _on_block_exited() 的 hover 联动。
TimelineManager 的行动数据与敌方意图规则。
```

### 实现结果

```text
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalAnimator.gd
```

职责：

```text
TimelineActionRemovalAnimator 只负责播放时间轴行动残影的移除动画。
它不创建残影，不修改 TimelineManager 数据，不维护 action_containers，也不处理原行动容器释放。
```

`timeline_ui.gd` 新增 `TimelineActionRemovalAnimatorScript` preload、缓存 getter 和委托调用。`animate_action_removal()` 仍决定何时创建残影，也仍使用原有表现参数。

### 当前优化进度与下一步

```text
已拆模块统计更新为 145 个脚本模块和 3 个默认 Resource 文件。
TimelineUI 现在有 10 个 ui_modules 脚本：布局、背景网格、格子交互、网格预览、TimelineManager 查找、敌方意图 overlay、行动方格放置动画、清理动画残影创建、清理动画 tween 播放和展开表现。
下一批如果继续 TimelineUI，只评估行动块 hover 信号/表现边界；不要硬拆 _on_action_placed()。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/in_scene/timeline/timeline_ui.gd 的既有 LF/CRLF 提示。
覆盖率检查通过：148 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 145 个，默认 Resource 文件为 3 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍有既有退出资源占用 warning。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
```

## 2026-06-07 TimelineUI 行动块 hover 状态通知拆分

### 读取与轮廓

本批按接力规则先确认工作区干净，再读取固定文档。仓库内未发现 `AGENTS.md`，本批以当前对话中用户贴出的约束为准。用户明确要求先不要解耦局外，因此本批只处理局内时间轴。随后用 `rg` 输出了以下目标轮廓：

```text
scene/in_scene/timeline/timeline_ui.gd
scene/in_scene/timeline/TimelineManager.gd
scene/in_scene/in_scene.gd
scene/in_scene/in_scene_modules/ui/TimelineActionHoverUiController.gd
scene/in_scene/timeline/ui_modules/animation/TimelineActionRemovalAnimator.gd
scene/in_scene/timeline/ui_modules/presenters/TimelineEnemyIntentOverlayPresenter.gd
```

### 当前职责

`timeline_ui.gd` 仍是时间轴 UI 的 composition root，负责连接 `TimelineManager`，维护 `action_containers` 和当前 `hovered_action`，创建行动容器，触发敌方意图 overlay、放置动画、移除动画和网格交互转发。

### 耦合点

```text
_on_block_hovered() / _on_block_exited() 同时更新 timeline_ui.gd 的 hovered_action，并通过 TimelineManager.action_hovered_changed 通知局内 UI 与敌方意图控制器。
animate_action_removal() 在移除当前 hover action 前也要发出 action_hovered_changed(false)，避免外部高亮残留。
_on_action_placed() 仍同时创建容器、格子 Panel、overlay、intro 动画和信号连接，本批不碰。
```

### 待办清单

| 优先级 | 候选事项 | 当前范围 | 判断 | 本批处理 |
| --- | --- | --- | --- | --- |
| 1 | hover 状态通知 controller | `_on_block_hovered()`、`_on_block_exited()`、移除时清 hover | 只处理本地状态和旧信号通知 | 执行 |
| 2 | 行动块视觉 presenter | `_on_action_placed()` 内样式与方格配置 | 仍牵动 overlay、intro 和信号连接 | 暂缓 |
| 3 | TimelineVisualConfig | 时间轴视觉参数 | Resource 化要小批处理 | 暂缓 |
| 4 | TimelineManager 敌方意图规则 | `TimelineManager.gd` | 数据规则核心，风险高 | 不碰 |

### 本批风险面

本批只拆一个风险面：时间轴行动块 hover 的本地状态切换和旧信号通知。

涉及的 3 个小风险点：

```text
新增 TimelineActionHoverStateController.gd，负责 enter_hover、exit_hover 和移除当前 hover action 时的清理通知。
timeline_ui.gd 保留 hovered_action 成员和旧入口，只把状态转换和 action_hovered_changed 发射委托出去。
不修改 TimelineManager.hovered_action，不展示 tooltip，不处理敌方意图预览，也不改行动块生成。
```

不触碰：

```text
_on_action_placed() 的行动块生成流程。
TimelineManager 的网格数据、敌方意图排布和 hover 存储。
EnemyIntentPresentationController 与 InScene hover UI 的外部响应。
```

### 实现结果

```text
scene/in_scene/timeline/ui_modules/controllers/TimelineActionHoverStateController.gd
```

职责：

```text
TimelineActionHoverStateController 只负责时间轴行动方块 hover 的本地状态切换和旧信号通知。
它不创建行动方块，不展示 tooltip，不处理敌方意图预览，也不修改 TimelineManager 的网格数据。
```

`timeline_ui.gd` 新增 `TimelineActionHoverStateControllerScript` preload、缓存 getter 和三个委托调用。`_on_block_hovered()`、`_on_block_exited()` 和 `animate_action_removal()` 的旧公共入口与旧信号语义保持不变。

### 当前优化进度与下一步

```text
已拆模块统计更新为 146 个脚本模块和 3 个默认 Resource 文件。
TimelineUI 现在有 11 个 ui_modules 脚本，新增 controllers/TimelineActionHoverStateController.gd。
timeline_ui.gd 从约 800 行降到约 786 行。
下一批如果继续 TimelineUI，只评估行动块视觉 presenter 或 TimelineVisualConfig 这类纯视觉配置；不要硬拆 _on_action_placed() 的完整生成编排。
```

### 回归检查

```text
git diff --check 通过，仅有 scene/in_scene/timeline/timeline_ui.gd 的既有 LF/CRLF 提示。
覆盖率检查通过：149 个已拆脚本和资源路径都出现在 docs/modularized-files-ultimate-operation-guide.md，缺失数为 0；其中脚本模块为 146 个，默认 Resource 文件为 3 个。
Godot 项目 headless 检查退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍有既有退出资源占用 warning。
Godot 加载 res://scene/in_scene/in_scene.tscn 退出码为 0，错误筛选未出现 SCRIPT ERROR、Parse Error、Compile Error、Failed to load script、Compilation failed、Invalid call 或 Invalid access；仍输出既有 TileSetAtlasSource atlas tile 资源错误和退出资源占用 warning，本批未改 TileSet。
```
