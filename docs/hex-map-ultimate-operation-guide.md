# HexMap 终极操作与维护说明

日期：2026-06-05

## 先读这一段

`scene/in_scene/hex_map.gd` 现在不应该再被理解成“一个什么都写的大脚本”。它更准确的角色是局内地图的主控节点，也就是地图系统的 composition root。

它负责把这些东西接起来：

- 局外传进来的房间类型和地图 seed。
- 地图规则数据 `map_data`。
- 地块节点表 `stack_nodes`。
- 地图生成、地貌投放、地块创建、输入、表现、高度视图、升降、销毁和奖励模块。
- `CardManager`、`TimelineSystem`、`BarManager`、`MainBoard`、`Camera2D` 和局内 UI。

后续维护时，优先把 `HexMap` 当作“调度者”和“场景适配层”。不要再把纯规则、纯表现或跨系统查找直接塞回 `hex_map.gd`。

## 当前文件入口

主文件：

```text
scene/in_scene/hex_map.gd
```

模块目录：

```text
scene/in_scene/hex_map_modules/
```

接力与流程归档：

```text
docs/ai-handoff-ultimate-operation-guide.md
workflow_logs/current-modularization-process.md
```

本文件是更偏操作的手册。需要做具体改动时，优先看本文件；需要理解历史拆分和后续接力方式时，再看接力说明与流程归档。`docs/` 目录不再保留每批 landing log。

## 一句话职责

`HexMap` 负责局内战斗地图的生命周期：

```text
接收局外 payload
-> 生成 map_data
-> 投放初始地貌
-> 创建地块 stack
-> 连接输入和视觉表现
-> 响应卡牌、时间轴、高度视图、奖励和战斗结算
-> 在跨场景状态变化时保持地图可恢复
```

## 核心数据

### map_data 是规则数据

`map_data` 是字典，key 是 `Vector2i` 六边形坐标，value 是单格规则数据。

常见字段：

- `height`：地块高度。
- `terrain`：地形类型，可以是枚举，也可以是 `"nexus_core"` 这类特殊字符串。
- `terrain_type`：渲染和规则读取用的地形标记。
- `landform`：初始或运行时地貌实例。
- `landform_type`：地貌名称或类型标记。
- `tier`：扇形地图使用的层级信息。

维护要求：

- 纯表现模块不要改 `map_data`。
- 地图生成、地貌投放、运行时注册、升降、毁灭和奖励状态可以改 `map_data`。
- 新增字段时，要写清楚字段由谁创建、谁读取、什么时候清理。

### stack_nodes 是节点索引

`stack_nodes` 是字典，key 是 `Vector2i` 坐标，value 是 `Area2D` 地块 stack。

维护要求：

- 创建 stack 后必须写入 `stack_nodes[coord]`。
- 删除 stack 时必须同步 `stack_nodes.erase(coord)`。
- 外部系统只读它，不要直接替换里面的节点。
- 外部要按坐标找实体时，优先用 `HexMap.get_entity_at_hex(coord)`。

### stack metadata 是模块契约

重要 metadata：

- `sprites`：参与高亮、消融、高度视图、入场动画和销毁表现的渲染节点数组。
- `height`：当前节点侧记录的高度。
- `occupant`：当前地貌或敌人建筑实例。
- `collision_node`：地块碰撞节点。
- `visual_state`：普通高亮状态。
- `is_animating`：升降、销毁等动画占用状态。

维护要求：

- 不要随意改这些 key 的名字。
- 新增可高亮、可消融、可跟随高度视图的节点，要通过外部渲染注册流程加入 `sprites`。
- 临时缓存 metadata 要使用清晰前缀，避免和核心 key 冲突。

## 地图启动流程

局内场景初始化后，`HexMap._ready()` 会完成这些事情：

1. 创建 `map_root`。
2. 初始化中立和敌方地貌脚本池。
3. 读取局外 payload。
4. 调用 `build_map_pipeline()`。
5. 绑定高度视图按钮。
6. 刷新地块输入状态。

核心建图流程在 `build_map_pipeline()`：

```text
清空 map_data
-> MapGenerationService 生成地块规则数据
-> LandformPlacementService 投放初始地貌
-> MapIntroRevealRunner 准备入场动画状态
-> _render_map() 创建所有 stack
-> HexMapCollisionPresenter 刷新碰撞和输入
-> 播放入场动画或直接刷新敌人列表
```

不要在 `build_map_pipeline()` 里继续加新规则。新增规则应该进入对应模块。

## 模块速查

### 生成与投放

```text
hex_map_modules/generation/MapGenerationService.gd
hex_map_modules/generation/LandformPlacementService.gd
```

`MapGenerationService.gd` 负责：

- 地图模式选择。
- 外部 seed 处理。
- 扇形地图坐标采样。
- 圆形地图坐标采样。
- `nexus_core` 写入。
- 单格 `height`、`terrain`、`terrain_type`、`tier` 初始化。

`LandformPlacementService.gd` 负责：

- 清理并重建 `GlobalClock.tile_h_pool`。
- 统计可投放地貌的有效地块。
- 按 `max_landform_ratio`、`Max_Neutral_landform`、`Max_Enemy_landform` 计算配额。
- 先投放中立地貌，再投放敌方地貌。
- 使用探针读取 `landform_rules` 和 `get_possible_coords()`。
- 创建地貌实例，写入 `map_data`，并加入 `Enemies` 或 `Middle` 分组。

### 坐标、地形和目标规则

```text
hex_map_modules/rules/HexCoordRules.gd
hex_map_modules/rules/HexTerrainRules.gd
hex_map_modules/rules/HexTargetRules.gd
hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd
```

使用原则：

- 坐标公式放 `HexCoordRules.gd`。
- 高度和地形映射放 `HexTerrainRules.gd`。
- 玩家 hover 和点击前目标判断放 `HexTargetRules.gd`。
- 时间轴命令执行时的最终目标校验放 `TimelineCommandTargetRules.gd`。

卡牌目标规则要两边同步。不要出现 hover 能放、时间轴执行时失败的情况。

### 地块创建

```text
hex_map_modules/factory/TileStackFactory.gd
hex_map_modules/factory/TileLandformAttachService.gd
hex_map_modules/factory/TileStackInitializationService.gd
hex_map_modules/factory/TileStackRebuildService.gd
```

创建链路：

```text
TileStackFactory
-> TileLandformAttachService
-> TileStackInitializationService
```

职责边界：

- `TileStackFactory.gd` 创建基础 `Area2D`、地块 sprite、碰撞体和碰撞 metadata。
- `TileLandformAttachService.gd` 负责初始地貌挂接和地貌 sprite 收编。
- `TileStackInitializationService.gd` 写 metadata、连接输入信号、处理入场 dissolve 和平铺高度标签。
- `TileStackRebuildService.gd` 当前只承接单格删除重建边界，内部仍是粗粒度重建。

### 输入

```text
hex_map_modules/input/HexMapInputCoordinator.gd
hex_map_modules/input/TargetHoverController.gd
```

输入分工：

- `HexMapInputCoordinator.gd` 处理地块 hover、离开、点击时应该走哪条流程。
- `TargetHoverController.gd` 处理普通卡牌目标选择期间的 hover 状态。

`HexMap` 保留状态变量，比如 `hovered_stacks`、`active_stack`、`selected_stack`、`height_view_hovered_stack`。

### 地图表现

```text
hex_map_modules/presenters/HexMapCollisionPresenter.gd
hex_map_modules/presenters/HexMapVisualStatePresenter.gd
hex_map_modules/presenters/TargetAoeHoverPresenter.gd
hex_map_modules/presenters/EnemyIntentMapPresenter.gd
hex_map_modules/presenters/SettlementRewardPresenter.gd
```

使用原则：

- 碰撞和输入开关放 `HexMapCollisionPresenter.gd`。
- 普通高亮、AOE、遮挡和 shader 状态放 `HexMapVisualStatePresenter.gd`。
- 卡牌 AOE 展示计划放 `TargetAoeHoverPresenter.gd`。
- 敌人意图地图表现放 `EnemyIntentMapPresenter.gd`。
- 结算奖励高亮和 tooltip 放 `SettlementRewardPresenter.gd`。

Presenter 只处理表现，不写规则数据。

### 高度视图

```text
hex_map_modules/height_view/HeightViewIndicatorPresenter.gd
hex_map_modules/height_view/HeightViewStateSynchronizer.gd
hex_map_modules/height_view/HeightViewMapTransitionRunner.gd
```

职责：

- `HeightViewIndicatorPresenter.gd` 创建和移除光柱、数字标签、hover 浮动。
- `HeightViewStateSynchronizer.gd` 同步单个 stack 在 3D 和平铺视图之间的节点位置。
- `HeightViewMapTransitionRunner.gd` 执行整张地图压缩到平铺视图，或恢复 3D 视图。

高度视图会影响地块 sprite、地貌、血条、奖励 tooltip 和高度标签。改这里必须回归这些联动。

### 升降和毁灭

```text
hex_map_modules/elevation/TileElevationService.gd
hex_map_modules/destruction/TileDestructionBatchQueue.gd
hex_map_modules/destruction/TileDestructionMutationService.gd
```

职责：

- `TileElevationService.gd` 负责升降动画、真实高度写入、高度池同步、超限检测。
- `TileDestructionBatchQueue.gd` 负责合批等待和排队。
- `TileDestructionMutationService.gd` 负责真实删除地块、清理 `map_data`、`stack_nodes` 和静态库引用。

不要绕过这些服务直接改高度或删除节点。

### 注册器

```text
hex_map_modules/registrars/RuntimeLandformRegistrar.gd
hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd
```

职责：

- `RuntimeLandformRegistrar.gd` 处理运行时新增地貌。
- `ExternalRenderNodeRegistrar.gd` 把血条等外部节点加入地块渲染序列。

运行时建造地貌时，不要手动只改 `map_data`。要保证 stack metadata、sprite 收编、视图同步和敌人列表刷新也跟上。

### 奖励和回合行为

```text
hex_map_modules/rewards/SettlementRewardController.gd
hex_map_modules/turn/TileTurnBehaviorRunner.gd
```

职责：

- `SettlementRewardController.gd` 扫描结算奖励资格，构造 `reward_info`，读取和写入 used 状态。
- `TileTurnBehaviorRunner.gd` 处理 `Signal_Bus.step_next` 后的建筑回合行为。

奖励表现不在 controller 里，奖励高亮和 tooltip 在 `SettlementRewardPresenter.gd`。

### 跨系统桥接

```text
hex_map_modules/bridges/CardManagerLocator.gd
hex_map_modules/bridges/HexMapSceneBridge.gd
hex_map_modules/ui/TargetSelectionTooltipAdapter.gd
```

职责：

- `CardManagerLocator.gd` 统一查找 CardManager，并清理失效 metadata。
- `HexMapSceneBridge.gd` 统一查找高度按钮、敌人意图管理器、Camera2D、总血条和 BarManager。
- `TargetSelectionTooltipAdapter.gd` 适配 MainBoard 旧 tooltip 接口。

不要在新模块里新增散落的 `get_node_or_null("../../...")` 查找。

## 稳定公共入口

外部系统优先调用这些入口。

### 局外 payload

```gdscript
hex_map.apply_external_event(payload_text)
```

用途：从局外地图或 in_scene 传入房间类型、seed 和功能标签。

### 地图输入和视觉锁

```gdscript
hex_map.set_tiles_interactive(false)
hex_map.set_visuals_locked(true)
```

用途：时间轴拖拽、结算奖励、动画期间控制地图输入和视觉状态。

### 结算奖励

```gdscript
hex_map.enter_settlement_reward_mode(host_node)
hex_map.exit_settlement_reward_mode()
hex_map.mark_settlement_reward_used(stack)
```

用途：战斗胜利后进入奖励地图模式，奖励页关闭后标记奖励已使用。

### 敌人意图

```gdscript
hex_map.show_enemy_intent_preview(intent_data, source_color, target_color)
hex_map.clear_enemy_intent_preview()
```

用途：时间轴或敌人意图 UI 要在地图上展示来源和目标时使用。

### 运行时地貌和外部渲染节点

```gdscript
hex_map.register_runtime_landform(coord, entity, landform_type)
hex_map.register_extra_render_node(coord, node)
```

用途：卡牌建造、敌人召唤、血条视觉同步。

### 地块高度和实体查询

```gdscript
await hex_map.animate_elevation_change(stack, delta_height)
var entity = hex_map.get_entity_at_hex(coord)
```

用途：卡牌效果、时间轴命令、状态效果。

### 回合开始状态

```gdscript
hex_map.process_turn_start_statuses()
```

用途：回合开始时让地貌或建筑处理状态效果，例如中毒扩散和衰减。

## 常见改动怎么做

### 改地图形状或新增地图模式

优先改：

```text
hex_map_modules/generation/MapGenerationService.gd
hex_map_modules/rules/HexCoordRules.gd
```

操作步骤：

1. 在 `MapGenerationService.gd` 增加模式常量。
2. 在 `generate(config)` 里分发新模式。
3. 如果需要新坐标公式，把纯坐标逻辑放进 `HexCoordRules.gd`。
4. 保持返回 `{"map_data": next_map_data}` 的结构。
5. 在 `hex_map.gd` 的 `_build_map_generation_service_config()` 里补需要的导出变量。

回归重点：

- `build_map_pipeline()` 能正常完成。
- `map_data` key 仍是 `Vector2i`。
- `nexus_core` 如果需要存在，仍写在 `(0, 0)`。
- 地貌投放和地块渲染没有因为新字段缺失报错。

### 改地貌投放规则

优先改：

```text
hex_map_modules/generation/LandformPlacementService.gd
```

操作步骤：

1. 修改配额、候选池或投放顺序时，先确认是否影响 `GlobalClock.tile_h_pool`。
2. 如果新增地貌限制，优先扩展地貌脚本的 `landform_rules`。
3. 不要直接在 HexMap 里写新的投放循环。

回归重点：

- 中立地貌和敌方地貌都能生成。
- 敌方地貌加入 `Enemies` 分组。
- 中立地貌加入 `Middle` 分组。
- 血条、敌人意图、奖励扫描能识别敌方地貌。

### 新增初始地貌脚本

通常要改：

```text
scene/in_scene/enermy/xxx.gd
scene/in_scene/hex_map.gd 中 _ready() 的 neutral_pool 或 enemy_pool
```

地貌脚本至少要确认：

- 能用 `new(coord, owner_battle)` 实例化。
- 有 `landform_name`。
- 有 `landform_rules`。
- 有 `get_possible_coords(coord, map_data)`。
- 有 `Attitude`，并能和 `Attitude_Pool.Enemy` 或 `Attitude_Pool.Middle` 比较。

后续更理想的做法是把 `neutral_pool` 和 `enemy_pool` 移到配置资源，但当前仍在 `HexMap._ready()` 中初始化。

### 新增运行时地貌或建造物

优先走：

```gdscript
hex_map.register_runtime_landform(coord, entity, landform_type)
```

不要只写：

```gdscript
map_data[coord]["landform"] = entity
```

原因是运行时注册还需要同步 stack、sprite、视图状态、敌人列表和可能的视觉特效。

### 新增卡牌目标规则

优先改：

```text
hex_map_modules/rules/HexTargetRules.gd
hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd
```

原则：

- Hover 判断和执行校验要一致。
- 不要把 shader、高亮或 tooltip 写进 rules。
- 如果规则需要实体状态，优先通过上下文传入，不要让 rules 自己找节点。

### 改卡牌 hover AOE 表现

优先改：

```text
hex_map_modules/presenters/TargetAoeHoverPresenter.gd
hex_map_modules/presenters/HexMapVisualStatePresenter.gd
hex_map_modules/ui/TargetSelectionTooltipAdapter.gd
```

分工：

- 范围计划放 `TargetAoeHoverPresenter.gd`。
- 地图 shader 状态放 `HexMapVisualStatePresenter.gd`。
- MainBoard tooltip 旧接口适配放 `TargetSelectionTooltipAdapter.gd`。

### 改敌人意图地图表现

优先改：

```text
hex_map_modules/presenters/EnemyIntentMapPresenter.gd
hex_map_modules/bridges/HexMapSceneBridge.gd
```

原则：

- 来源、目标、颜色、波纹等地图表现放 presenter。
- 查找 `EnemyIntentManager` 的路径放 bridge。
- 不要让敌人意图模块直接改卡牌目标规则。

### 改结算奖励

状态逻辑改：

```text
hex_map_modules/rewards/SettlementRewardController.gd
```

表现逻辑改：

```text
hex_map_modules/presenters/SettlementRewardPresenter.gd
```

流程入口仍在：

```text
hex_map.gd
in_scene.gd
```

原则：

- `HexMap` 发出 `settlement_reward_requested(reward_info)`。
- 打开奖励页由 `in_scene.gd` 负责。
- 奖励建筑 used 状态由 controller 写入。
- tooltip 和高亮由 presenter 处理。

### 改高度视图

优先改：

```text
hex_map_modules/height_view/
```

回归重点：

- 切换平铺视图。
- 恢复 3D 视图。
- 地貌、血条和奖励 tooltip 跟随。
- hover 光柱和数字标签。
- 地块升降后高度标签刷新。

### 改地块升降

优先改：

```text
hex_map_modules/elevation/TileElevationService.gd
```

注意：

- 高度变化要同步 `map_data`、stack metadata 和 `GlobalClock.tile_h_pool`。
- 超过 `max_height` 或低于 `min_height` 会触发毁灭。
- 外部血条如果不是 stack 子节点，需要单独跟随。

### 改地块毁灭

优先改：

```text
hex_map_modules/destruction/TileDestructionBatchQueue.gd
hex_map_modules/destruction/TileDestructionMutationService.gd
```

不要直接 `queue_free()` stack。真实毁灭要同步：

- `map_data`
- `stack_nodes`
- `GlobalClock.tile_h_pool`
- 静态库引用
- 敌人列表
- 拓扑变化信号

### 改血条同步

优先看：

```text
HexMap/BarManager
hex_map_modules/registrars/ExternalRenderNodeRegistrar.gd
hex_map_modules/bridges/HexMapSceneBridge.gd
```

原则：

- 单体血条仍在 `HexMap/BarManager` 下。
- 血条视觉需要加入 `sprites` 时，走 `register_extra_render_node()` 或 registrar。
- `HealthBar_<instance_id>` 命名规则集中在 `HexMapSceneBridge.gd`。

### 改场景节点路径

优先改：

```text
hex_map_modules/bridges/HexMapSceneBridge.gd
```

不要到处搜索替换 `../../ui/...`。现在路径集中在 bridge。

## 导出变量怎么理解

`HexMap` 的导出变量仍然保留在 Inspector 上。它们没有被搬进 Resource，原因是当前策划调参仍依赖这个面板。

主要分组：

- **Assets**：基础地块纹理、材质、标签背景。
- **地块入场动画**：地图揭示动画时长、波纹、锁交互、隐藏血条等。
- **敌人意图地图表现**：来源高亮、目标波纹、材质和透明度。
- **Grid Generation Settings**：地图模式、扇形/圆形半径、seed、高度范围。
- **Grid Spacing & Hitbox**：六边形间距、地块缩放、碰撞形状。
- **高度视图配置**：光柱、数字、hover 动画、点击高亮。
- **局外收获配置**：奖励绑定死亡/存活建筑、tooltip 样式、高亮颜色。
- **地形系统**：高度到地形映射、地形纹理、地貌密度。
- **地块生成配额**：中立和敌方地貌上限。
- **地形升降动画**：升降时长、震动、移动距离。
- **高度限制设置**：最大/最小高度，超限后毁灭。
- **地块消失动画**：毁灭合批和消融参数。

如果未来要减少 Inspector 复杂度，建议按这些分组拆成 Resource 配置，而不是零散搬变量。

## 信号说明

`HexMap` 暴露这些重要信号：

- `enemy_roster_changed`：敌人列表变化，总血条和敌人意图系统需要刷新。
- `tile_topology_changed`：地块拓扑变化，时间轴和目标规则可能需要重新判断。
- `settlement_reward_requested(reward_info)`：玩家点击可领取奖励建筑。
- `map_intro_reveal_finished`：地图入场动画结束。
- `CreateBar(...)`：旧血条创建信号，仍保留兼容。

新增功能时，优先通过信号通知外部系统，不要让模块互相硬找节点。

## 不要做的事

### 不要把模块重新耦合回 HexMap

不要在 presenter、rules、generation service 里写：

```gdscript
get_tree().current_scene.get_node_or_null("map/HexMap")
```

模块需要数据时，让 `HexMap` 通过 `_build_*_config()` 传进去。

### 不要在表现层写规则数据

Presenter 可以改 shader、tooltip、可见性和 tween，但不要改 `map_data`。

### 不要一次性删除兜底

项目经历过二次进局内、奖励页打开关闭、跨场景返回等生命周期问题。很多 `is_instance_valid()`、`has_method()` 和 metadata 清理现在看起来啰嗦，但不能随手删。

可以删除兜底的条件：

- 所有调用方都迁移到统一接口。
- 场景生命周期已经验证。
- 同一批只删一个接口的兜底。
- 删除后跑固定回归路径。

### 不要绕过公共入口

外部系统不要直接调用 `_create_stack_at()`、`_render_map()`、`_build_*_config()` 或 presenter 内部函数。

## 当前还值得优化的方向

### 地貌池配置资源

`neutral_pool` 和 `enemy_pool` 仍在 `HexMap._ready()` 里硬编码。后续可以做：

```text
LandformPoolProvider
或 Resource: LandformPoolConfig
```

这样不同房间、时代或 Boss 层可以用不同地貌池。

### 导出变量拆 Resource

Inspector 变量很多。可以按功能拆：

- `MapGenerationConfig`
- `EnemyIntentVisualConfig`
- `HeightViewConfig`
- `SettlementRewardVisualConfig`
- `TileDestructionConfig`

这不是优先事项。要等玩法稳定后再做。

### 单格重建真正局部化

`TileStackRebuildService.gd` 当前仍是删除旧 stack 后完整重建。真正优化前要先明确哪些东西要保留：

- `sprites`
- `height`
- `occupant`
- `collision_node`
- 高度视图缓存
- 奖励 tooltip
- 血条绑定

不要直接做对象池替换整个 stack。可以先从 tooltip、overlay、label 这类纯表现节点做复用。

### Timeline 和地图依赖注入

时间轴、拖拽控制器和地图仍有一些跨路径依赖。更好的方向是让 `in_scene.gd` 在初始化时注入依赖，而不是各自找当前场景。

## 固定回归清单

每次改 HexMap 或它的模块，至少跑：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归重点：

- 局内地图能生成。
- 中立和敌方地貌能投放。
- 敌方血条能生成。
- 选卡后 hover 合法/非法地块表现正确。
- AOE 卡牌范围显示正确。
- 右键取消选卡正常。
- 拖拽卡牌到时间轴时地图输入锁定和恢复正常。
- 敌人意图 hover 能高亮来源和目标。
- 高度视图切换、hover 光柱和数字标签正常。
- 地块升降、超限毁灭、拓扑信号正常。
- 战斗胜利后进入奖励模式，奖励建筑 hover、点击、已使用状态正常。
- 二次进入局内不保留已释放 CardManager 或旧地图状态。

已知旧噪声：

- TileSet atlas 报错。
- ObjectDB / RID / resource 退出提示。

这些不是 HexMap 当前拆分引入的问题。除非本批改动正好触碰 TileSet 或资源生命周期，否则不要把它们和本批逻辑问题混在一起。

## 最后的维护原则

`HexMap` 现在还大，但它的“大”主要来自主控和场景适配职责。后续不要为了降低行数继续机械拆函数。

更好的判断标准是：

- 纯规则能不能离开节点脚本。
- 纯表现能不能离开规则数据。
- 跨系统查找能不能集中到 bridge 或由 `in_scene.gd` 注入。
- 旧兜底能不能在接口统一后小批删除。
- 每次改动能不能用固定路径验证。

一句话：让 `HexMap` 继续做地图主控，但不要让它重新变成规则、表现、UI、时间轴和资源管理的混合仓库。
