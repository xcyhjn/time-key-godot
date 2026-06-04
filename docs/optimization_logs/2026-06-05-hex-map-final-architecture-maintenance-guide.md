# HexMap 最终架构与维护说明

日期：2026-06-05

## 这份文档的用途

这份文档给后续维护 `scene/in_scene/hex_map.gd` 和局内地图系统的组员使用。它回答三个问题：

- `HexMap` 现在到底负责什么。
- 局内地图节点和其他系统是怎么互相控制的。
- 后续继续改地图、卡牌目标、地貌、奖励或时间轴时，应该从哪里下手，哪些边界不要再打乱。

本文件是一个独立说明，不依赖前面的每批 landing log。需要快速理解当前局内地图结构时，优先读这份文档。

## 当前审查结论

`hex_map.gd` 的核心地图职责已经拆出了一批稳定模块，整体比最初干净很多。现在比较干净的部分包括：

- 坐标、地形和目标规则已经拆到 rules 模块。
- 地块栈创建已经拆成基础栈工厂、初始地貌挂接服务、初始化收尾服务。
- 地图普通高亮、敌人意图地图表现、结算奖励表现和碰撞输入开关已经拆到 presenter 或 input 模块。
- 高度视图的光柱、标签、整图平铺/恢复和节点同步已经拆到 height_view 模块。
- 运行时地貌注册、外部渲染节点注册、地块升降和地块销毁已经拆成单独服务。

但是，`hex_map.gd` 还不能说已经完全拆干净。它现在仍然是局内地图的 composition root，也就是“地图主控和场景适配器”。它负责把地图数据、地块节点、局内 UI、时间轴、卡牌管理器、血条、奖励状态和全局信号接在一起。

当前更准确的评价是：

- 地图内部的规则、渲染、输入、地块创建和效果编排已经进入可维护状态。
- 与局内场景根节点、CardManager、TimelineSystem、BarManager 和奖励页面之间的耦合仍然存在。
- 后续不应该继续盲目把所有函数都抽走，而应该优先拆这些跨系统适配边界。

当前 `hex_map.gd` 仍有约 2371 行。它还很大，但多数剩余代码已经不是单纯的长函数问题，而是场景主控职责还没完全分层。

## 局内场景节点关系

局内场景入口是：

```text
scene/in_scene/in_scene.tscn
```

和地图相关的主要节点结构如下：

```text
in_scene Control
├─ map Control
│  ├─ HexMap Node2D
│  │  └─ BarManager Node2D
│  └─ Camera2D
└─ ui CanvasLayer
   ├─ TimelineUI Control
   ├─ TimelineSystem Node
   │  ├─ TimelineManager Node
   │  ├─ DragShapeController Node2D
   │  ├─ EnemyIntentManager Node
   │  └─ TimelineVisualizer Node2D
   ├─ Main Control
   ├─ HeightViewToggleButton Button
   ├─ TotalEnemyHealthBar Node2D
   ├─ 奖励按钮和调试按钮
   └─ 胜利、失败、暂停等 UI
```

运行时 `HexMap` 会在自己下面创建一个 `map_root: Node2D`。真实地块 stack 都挂在这个 `map_root` 下，而不是直接挂在 `HexMap` 根节点下。

## HexMap 的核心作用

`HexMap` 是局内战斗地图的主控节点。它的核心职责可以分成七类。

### 1. 接收局外房间上下文

`in_scene.gd` 会通过 `hex_map.apply_external_event(payload_text)` 把局外房间信息传给地图。常见 payload 类似：

```text
battle_normal <map_seed>
battle_elite <map_seed>
boss_stage <map_seed>
```

`HexMap` 会解析房间类型和 seed，然后影响地图生成、地貌池和调试日志。

### 2. 生成地图数据

`HexMap` 持有两份最重要的数据：

- `map_data`：以六边形坐标为 key 的地块数据字典。
- `stack_nodes`：以六边形坐标为 key 的地块节点字典。

`map_data` 是规则数据，包含高度、地形、地貌实例等信息。`stack_nodes` 是运行时节点表，指向 `Area2D` 地块 stack。

### 3. 创建和维护地块 stack

当前地块创建链路已经拆成三层：

- `TileStackFactory.gd`：创建基础 `Area2D`、顶面/侧面 sprite 和碰撞体。
- `TileLandformAttachService.gd`：把初始地貌挂到 stack 下，并收编地貌 sprite。
- `TileStackInitializationService.gd`：写入 metadata、连接输入信号、处理入场 dissolve 和平铺高度标签。

`hex_map.gd::_create_stack_at()` 现在主要负责按旧顺序串联这三层。

### 4. 统一地块输入

地块的 `mouse_entered`、`mouse_exited` 和 `input_event` 信号会回到 `HexMap`，然后交给 `HexMapInputCoordinator.gd` 做输入分发。

输入分发会根据当前状态决定下一步：

- 普通卡牌目标 hover。
- 卡牌目标点击。
- 高度视图 hover。
- 结算奖励 hover 或点击。
- 敌人意图地图 hover。

`HexMap` 仍然持有最终状态，比如 `hovered_stacks`、`active_stack`、`selected_stack` 和 `height_view_hovered_stack`。

### 5. 管理地图视觉状态

普通高亮、AOE、遮挡、敌人意图、奖励高亮和高度视图都已经拆出表现模块，但 `HexMap` 仍然负责把它们编排在一起。

主要表现模块如下：

- `HexMapVisualStatePresenter.gd`：普通地块高亮、AOE、遮挡和 shader 状态。
- `EnemyIntentMapPresenter.gd`：敌人意图在地图上的来源和目标表现。
- `SettlementRewardPresenter.gd`：结算奖励高亮和 tooltip。
- `HeightViewIndicatorPresenter.gd`：高度视图光柱和数字标签。
- `HeightViewStateSynchronizer.gd`：单个地块在 3D/平铺视图之间同步。
- `HeightViewMapTransitionRunner.gd`：整图平铺压缩和恢复。

### 6. 处理地块变化

地块高度变化、毁灭、运行时建造和血条同步都通过服务处理：

- `TileElevationService.gd`：地块升降动画、真实高度写入、超限检测。
- `TileDestructionBatchQueue.gd`：把多个毁灭请求合批。
- `TileDestructionMutationService.gd`：执行真实销毁、清理静态库和节点。
- `RuntimeLandformRegistrar.gd`：运行时新增地貌。
- `ExternalRenderNodeRegistrar.gd`：把血条等外部节点注册进地块渲染序列。

### 7. 作为局内系统适配器

这部分是目前还没完全拆干净的地方。`HexMap` 仍然直接或间接连接：

- `CardManager`：读取当前选中卡牌，判断地块是否合法。
- `TimelineSystem/EnemyIntentManager`：转发地图 hover 给敌人意图系统。
- `BarManager`：创建和查找单体血条。
- `TotalEnemyHealthBar`：入场和战斗中刷新总血量。
- `HeightViewToggleButton`：绑定高度视图按钮。
- `Camera2D`：进入结算奖励时关闭/恢复缩放输入。
- `Signal_Bus.step_next`：回合推进时触发建筑行为。
- `in_scene.gd` 的 Main 节点：进入奖励、退出奖励、锁定地图输入、转发局外 payload。

这些耦合现在还集中在 `HexMap` 里。好处是其他小模块不用到处找场景节点；坏处是 `HexMap` 仍然承担较重的场景适配职责。

## 当前模块清单

### rules

- `HexCoordRules.gd`：六边形坐标生成和像素坐标换算。
- `HexTerrainRules.gd`：地形、高度和地貌调试名称。
- `HexTargetRules.gd`：卡牌目标合法性和效果范围。
- `TimelineCommandTargetRules.gd`：时间轴命令执行时的最终目标校验。

### factory

- `TileStackFactory.gd`：基础地块 stack。
- `TileLandformAttachService.gd`：初始建图地貌挂接。
- `TileStackInitializationService.gd`：stack metadata、输入信号、入场 dissolve、高度标签。

### presenters

- `HexMapCollisionPresenter.gd`：碰撞形状和地块输入开关。
- `HexMapVisualStatePresenter.gd`：普通 shader 状态、AOE、遮挡。
- `TargetAoeHoverPresenter.gd`：卡牌 hover 的 AOE 展示计划。
- `EnemyIntentMapPresenter.gd`：敌人意图地图表现。
- `SettlementRewardPresenter.gd`：结算奖励地图表现和 tooltip。

### input

- `HexMapInputCoordinator.gd`：地块 hover/click 输入分发。
- `TargetHoverController.gd`：普通卡牌目标 hover 的状态更新。

### height_view

- `HeightViewIndicatorPresenter.gd`：光柱和高度数字。
- `HeightViewStateSynchronizer.gd`：单个 stack 的 3D/平铺状态同步。
- `HeightViewMapTransitionRunner.gd`：整张地图压缩到平铺视图或恢复 3D。

### registrars、runners 和 services

- `RuntimeLandformRegistrar.gd`：运行时新增地貌。
- `ExternalRenderNodeRegistrar.gd`：血条等外部渲染节点注册。
- `MapIntroRevealRunner.gd`：地图入场揭示动画。
- `TileElevationService.gd`：地块升降。
- `TileDestructionBatchQueue.gd`：地块毁灭合批队列。
- `TileDestructionMutationService.gd`：地块真实销毁。

## 数据耦合说明

### map_data

`map_data` 是地图规则数据。它至少会包含：

- 地块高度。
- 地形类型。
- 初始地貌或运行时地貌引用。
- 地貌类型标记。

维护要求：

- 不要在表现层 presenter 里直接改 `map_data`。
- 地貌创建、地块升降、毁灭和奖励状态才应该改规则数据。
- 如果新增字段，要在本文件和相关 landing 文档里说明字段来源、读写方和生命周期。

### stack_nodes

`stack_nodes` 是地图节点索引表，key 是六边形坐标，value 是 `Area2D` 地块 stack。

维护要求：

- 新建或删除 stack 时必须同步更新 `stack_nodes`。
- 外部系统可以读取 `stack_nodes`，但不应该直接替换里面的节点。
- 需要按坐标找实体时，优先通过 `HexMap.get_entity_at_hex(coord)` 或受控上下文读取。

### stack metadata

当前重要 metadata 如下：

- `sprites`：参与 shader、高亮、入场、销毁和视图同步的渲染节点列表。
- `height`：当前地块高度。
- `occupant`：当前地貌或敌人建筑实例，可以为空。
- `collision_node`：地块碰撞节点。
- `visual_state`：普通视觉状态。
- `is_animating`：升降或销毁等动画占用状态。
- 高度视图和奖励 tooltip 还会写入各自的辅助 metadata。

维护要求：

- 不要随意改 `sprites`、`height`、`occupant`、`collision_node` 这四个核心 key。
- 如果新增视觉节点需要参与高亮或 dissolve，要通过 `ExternalRenderNodeRegistrar.gd` 或已有注册入口把它加入 `sprites`。
- 如果某个模块只需要临时 tween 或缓存，metadata key 要有清晰前缀，避免和核心 key 冲突。

## 节点和信号耦合说明

### in_scene.gd 控制 HexMap

`in_scene.gd` 是局内主流程控制器。它直接持有 `hex_map = $"../../map/HexMap"`，并负责：

- 转发局外 payload 给 `HexMap.apply_external_event()`。
- 连接 `hex_map.settlement_reward_requested`，打开对应奖励页面。
- 战斗胜利后调用 `set_tiles_interactive(false)`、`set_visuals_locked(true)` 和 `enter_settlement_reward_mode(self)`。
- 奖励页关闭后调用 `mark_settlement_reward_used()`、`exit_settlement_reward_mode()` 等入口。
- 回合开始时调用 `process_turn_start_statuses()`。

维护建议：

- 局内流程状态仍应放在 `in_scene.gd`。
- 地图地块和地图表现状态仍应放在 `HexMap`。
- 奖励页面打开路径属于 `in_scene.gd`，不要让 `HexMap` 直接 preload 奖励场景。

### TimelineSystem 与 HexMap

当前时间轴和地图仍有双向依赖：

- `TimelineManager` 会从当前场景找 `map/HexMap`，用于敌人意图目标解析和重校验。
- `EnemyIntentManager` 接收 `HexMap` 的地图 hover 转发。
- `DragShapeController` 会找 `HexMap`，在拖拽期间关闭地块输入、锁定视觉，并在结束后恢复。

维护建议：

- 时间轴规则和地图目标规则已经开始统一，不要再复制一套目标合法性判断。
- 后续建议引入 `TimelineMapBridge` 或在 `in_scene.gd` 初始化时显式注入 `HexMap`，减少 `get_tree().current_scene.get_node_or_null("map/HexMap")` 这种硬路径。

### CardManager 与 HexMap

`HexMap.get_card_manager()` 现在按顺序查找：

- `MainBoard` group 上的 `manager_instance`。
- tree root 的 `card_manager` metadata。
- current_scene 的 `card_manager` metadata。

这段是历史兜底，确实臃肿，但不能直接删除。第二次从局外进入局内时，root 或 current_scene metadata 可能残留已经释放的 CardManager，所以这里还带失效清理。

维护建议：

- `HexMap.get_card_manager()` 已经委托 `CardManagerLocator.gd`，新的地图侧代码不要再复制 CardManager 查找链。
- 奖励脚本、商店脚本或其他 UI 如果后续要收敛，应逐个迁移到 locator。
- 不要直接删 root/current_scene metadata 兜底，除非所有入口都已经统一注入 CardManager。

### BarManager 与 HexMap

`BarManager` 是 `HexMap` 的子节点，负责单体血条。`HexMap` 会在入场、地貌注册、高度视图、升降和毁灭时查找或同步血条。

维护建议：

- 单体血条节点继续放在 `HexMap/BarManager` 下。
- 如果新增血条内部视觉层，需要通过外部渲染节点注册入口加入 stack 的 `sprites`，否则高度视图、遮挡或 dissolve 可能不同步。
- 总血条 `TotalEnemyHealthBar` 是 UI 节点，不属于 `BarManager`，当前由 `HexMap` 通过路径辅助刷新。

### Signal_Bus 与 HexMap

`HexMap` 目前连接 `Signal_Bus.step_next`，在 `_on_step_next(step, behavior)` 里触发建筑行为。

维护建议：

- `_on_step_next()` 已经委托 `TileTurnBehaviorRunner.gd`。
- runner 保留旧顺序：先处理 `iron_mine.Library`，再处理其他地貌。
- 不要在新的建筑脚本里绕过回合 runner 直接找 `HexMap` 改地图。

## HexMap 的使用方式

### 场景里如何放置

局内地图需要保持当前基础结构：

```text
map/HexMap
map/HexMap/BarManager
map/Camera2D
ui/HeightViewToggleButton
ui/TimelineSystem
ui/Main
ui/TotalEnemyHealthBar
```

如果改节点路径，必须同步检查：

- `hex_map.gd` 里对按钮、相机、总血条、敌人意图系统的路径查找。
- `in_scene.gd` 里 `hex_map` 的 `@onready` 路径。
- `TimelineManager.gd`、`DragShapeController.gd` 和 `timeline_ui.gd` 里对地图或 map Control 的查找。

### 外部系统应该调用哪些入口

建议外部系统只调用这些稳定入口：

- `apply_external_event(payload: String)`：传入局外房间上下文。
- `set_tiles_interactive(enabled: bool)`：整体开关地块输入。
- `set_visuals_locked(locked: bool)`：锁定或解锁地图视觉状态。
- `enter_settlement_reward_mode(host_node: Node)`：进入结算奖励地图模式。
- `exit_settlement_reward_mode()`：退出结算奖励地图模式。
- `mark_settlement_reward_used(stack: Area2D)`：奖励页确认后标记建筑已使用。
- `register_runtime_landform(coord: Vector2i, entity: landform, landform_type: String)`：运行时新增地貌。
- `register_extra_render_node(coord: Vector2i, node: Node)`：把外部渲染节点加入地块渲染序列。
- `animate_elevation_change(stack: Area2D, delta_height: int)`：执行地块升降。
- `get_entity_at_hex(coord: Vector2i)`：按坐标读取实体。
- `process_turn_start_statuses()`：回合开始时处理状态效果。
- `show_enemy_intent_preview()` 和 `clear_enemy_intent_preview()`：敌人意图地图预览。

不建议外部系统直接调用 `_create_stack_at()`、`_render_map()`、`_build_*_config()` 或 presenter 内部函数。

### 新增卡牌目标规则时

优先改：

```text
scene/in_scene/hex_map_modules/rules/HexTargetRules.gd
scene/in_scene/hex_map_modules/timeline/commands/rules/TimelineCommandTargetRules.gd
```

维护要求：

- `HexTargetRules.gd` 负责玩家 hover 和点击前反馈。
- `TimelineCommandTargetRules.gd` 负责时间轴命令执行时的最终校验。
- 两边的语义要一致，避免 hover 显示能放，执行时又失败。
- 不要把 shader、tooltip 或 CardManager 查找写进 rules 模块。

### 新增地貌或建筑时

初始建图地貌走：

```text
TileLandformAttachService.gd
```

运行时新增地貌走：

```text
RuntimeLandformRegistrar.gd
```

维护要求：

- 地貌实例要能挂到 stack 下。
- 如果有视觉 sprite，要确保被收编进 stack 的 `sprites`。
- 如果是敌方建筑，需要确认分组、血条和奖励接口是否符合旧流程。
- 如果建筑有回合行为，短期内仍走 `Behavior(step, map_data, null, behavior, rng)`；后续拆 runner 后再统一接口。

### 新增地图表现时

优先判断它属于哪类：

- 普通高亮、AOE、遮挡：放到 `HexMapVisualStatePresenter.gd`。
- 敌人意图：放到 `EnemyIntentMapPresenter.gd`。
- 结算奖励：放到 `SettlementRewardPresenter.gd`。
- 高度视图：放到 `height_view` 模块。
- 入场动画：放到 `MapIntroRevealRunner.gd`。

维护要求：

- Presenter 只写视觉，不判断卡牌规则和奖励资格。
- 可调参数暂时仍放在 `hex_map.gd` 的导出变量里，再通过 `_build_*_config()` 传入模块。
- 不要让 presenter 自己 `get_node()` 找局内 UI。

### 新增地块生命周期逻辑时

先判断是数据变化还是表现变化：

- 只改高度：优先走 `TileElevationService.gd`。
- 真实删除地块：走 `TileDestructionMutationService.gd` 和 `TileDestructionBatchQueue.gd`。
- 局部重绘：目前 `refresh_tile_visual()` 仍是粗粒度删除重建，这是后续待拆点。
- 新增外部视觉节点：走 `ExternalRenderNodeRegistrar.gd`。

## 后续维护优先级

### 已落地第一步：CardManagerLocator

当前状态：`HexMap` 已经通过 `scene/in_scene/hex_map_modules/bridges/CardManagerLocator.gd` 统一查找 CardManager。

已完成：

- `HexMap.get_card_manager()` 保留公共入口，但内部已经委托 locator。
- 查找顺序仍是 `MainBoard.manager_instance`、root meta、current_scene meta。
- root/current_scene metadata 的失效清理已经集中到 locator。

仍待处理：

- 奖励脚本和商店脚本中仍可能存在重复 CardManager 查找链。
- 后续如果要继续收敛，应逐个脚本迁移到 locator，不要一次性删除所有兜底。
- 必须保留失效 metadata 清理，避免二次进局内引用已释放实例。

### 已落地第一步：TileTurnBehaviorRunner

当前状态：`_on_step_next()` 中旧建筑回合行为遍历已经拆到 `scene/in_scene/hex_map_modules/turn/TileTurnBehaviorRunner.gd`。

已完成：

- `HexMap` 继续连接 `Signal_Bus.step_next`，但只把 `step`、`behavior`、`map_data` 和 `rng` 转交给 runner。
- runner 保留旧顺序：先处理 `iron_mine.Library`，再处理其他地貌。
- 旧 `Behavior(step, map_data, null, behavior, rng)` 接口保持不变。

仍待处理：

- 建筑脚本仍使用 `Behavior()` 鸭子类型接口。
- `has_method("Behavior")` 暂时保留，等所有建筑统一契约后再考虑清理。
- 后续必须回归回合推进、建筑行为、敌人意图和地块变化。

### 已落地第一步：SettlementRewardController

当前状态：奖励资格扫描、`reward_info` 构造、used 状态读取和写入已经拆到 `scene/in_scene/hex_map_modules/rewards/SettlementRewardController.gd`。

已完成：

- `SettlementRewardPresenter.gd` 继续只管视觉、tooltip 和 hover 动画。
- `HexMap` 保留奖励模式切换、信号发出和 presenter 刷新。
- `settlement_reward_requested` 信号 payload 保持旧 `reward_info` 结构。

仍待处理：

- 建筑奖励接口仍是鸭子类型，`has_method()` 暂时保留。
- 打开奖励页面仍由 `in_scene.gd` 处理。
- 奖励页内部的 CardManager 查找链后续可以逐步迁移到 `CardManagerLocator.gd`。

### 已落地第一步：TargetSelectionTooltipAdapter

当前状态：`HexMap` 对 MainBoard 旧 tooltip 接口的直接调用已经拆到 `scene/in_scene/hex_map_modules/ui/TargetSelectionTooltipAdapter.gd`。

已完成：

- `TargetAoeHoverPresenter.gd` 只生成展示计划。
- `HexMap` 只把 `main_board` 和 `display_plan` 交给 adapter。
- MainBoard 旧接口 `update_target_selection_hover(center_stack, card)` 暂时保持不变。

仍待处理：

- 要回归卡牌 hover 合法/非法目标、范围型效果和无目标卡牌。
- 如果后续 MainBoard tooltip 改成信号或专用 UI controller，应优先替换 adapter 内部实现，而不是再改 `HexMap`。

### 第五优先级：refresh_tile_visual 局部重绘

目标：减少当前“删除旧 stack 后完整 `_create_stack_at()`”的粗粒度流程。

预期收益：

- 运行时地貌变化和局部刷新更可控。
- 降低 stack metadata、血条和高度视图缓存被重建影响的风险。

注意：

- 只有在地块创建三层服务稳定后再动。
- 改前要先写清楚哪些 metadata 要保留，哪些节点可以重建。

## 维护规则

### 规则一：模块只消费快照，不反向查找 HexMap

已经拆出的模块大多通过 `_build_*_config()` 接收配置和回调。后续继续保持这个方式。

不要在新模块里写：

```gdscript
get_tree().current_scene.get_node_or_null("map/HexMap")
```

如果模块需要地图状态，就让 `HexMap` 通过 config 传进去。

### 规则二：表现层不写规则数据

Presenter 可以写 shader、tooltip、可见性和 tween，但不应该修改 `map_data` 的规则字段。

如果一个需求同时涉及规则和表现，先拆成两段：

- 规则服务改数据。
- Presenter 根据数据结果刷新表现。

### 规则三：核心 metadata 需要稳定

`sprites`、`height`、`occupant`、`collision_node` 是当前很多模块共享的契约。改这些 key 会影响高亮、目标规则、奖励、血条、高度视图、敌人意图和地块销毁。

如果确实要改，必须同步改所有读取方，并补一份迁移说明。

### 规则四：跨系统引用优先由 in_scene.gd 注入

`HexMap`、`TimelineSystem`、`MainBoard`、`CardManager`、`BarManager` 之间的硬路径是后续主要债务。新增跨系统功能时，优先考虑：

- 在 `in_scene.gd` 初始化时连接信号。
- 通过导出 `NodePath` 注入依赖。
- 通过专门 bridge 或 locator 统一查找。

不要再新增散落的硬编码路径。

### 规则五：不要把兜底一次性砍光

当前项目经历过跨场景切换、二次进入局内、奖励页打开关闭等生命周期问题。很多 `is_instance_valid()`、metadata 清理和 `has_method()` 现在看起来啰嗦，但不能直接删。

可以删除兜底的条件是：

- 相关接口已经统一成明确契约。
- 所有调用方已经迁移。
- 至少跑过启动、局内、奖励和回到局外路径。

## 验证方式

每次改 `HexMap` 或它的模块，至少跑：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

当前已知噪声包括 TileSet atlas 报错、ObjectDB/resource 退出提示。只要退出码为 0，且没有新增脚本解析错误、preload 错误或空引用崩溃，就可以继续进入手动回归。

手动回归优先走：

- 启动局内战斗，确认地图生成。
- hover 和点击合法/非法卡牌目标。
- 拖拽卡牌到时间轴，确认地图输入锁定和恢复。
- 敌人意图 hover，确认地图来源和目标表现。
- 高度视图切换、hover 光柱和高度数字。
- 地块升降到超限并销毁。
- 战斗胜利后进入结算奖励模式，hover 和点击奖励建筑。
- 奖励页关闭后回到局内，确认奖励建筑状态已刷新。

## 最后结论

`HexMap` 现在已经不再是“所有逻辑都堆在一个脚本里”的状态。地块创建、目标规则、表现、输入、高度视图、升降、销毁和注册等核心能力已经拆出独立模块。

但 `HexMap` 仍然是局内地图的主控节点。它还承担跨系统适配职责，所以后续维护重点不是继续机械拆小函数，而是把 CardManager、TimelineSystem、奖励状态、回合行为和 UI tooltip 这些跨系统边界逐步变成明确接口。

一句话维护原则：

让 `HexMap` 保持“地图主控”，让小模块保持“单一职责”，让跨系统依赖通过 `in_scene.gd`、locator、bridge 或信号明确注入。
