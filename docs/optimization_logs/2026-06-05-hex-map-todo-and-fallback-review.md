# HexMap 待拆清单与兜底审查

日期：2026-06-05

## 当前结论

`hex_map.gd` 已经拆出一批稳定模块：坐标规则、地形规则、目标规则、碰撞输入开关、普通视觉状态、敌人意图地图表现、结算奖励表现、高度视图指示器、平铺/3D 同步、整图平铺/恢复、地图入场、运行时地貌注册、外部渲染节点注册、地块升降编排、地块真实销毁、地块输入协调、目标 hover 控制器、目标 AOE hover 展示计划、基础地块栈工厂和初始地貌挂接服务。

剩下的耦合主要不再是“一个函数特别长”这么简单，而是几个场景根职责还在 `hex_map.gd` 中交叉：地块初始化收尾、MainBoard tooltip 旧接口适配、CardManager 查找、敌人/建筑回合行为、奖励状态、时间轴/敌人意图路径依赖。

## 优先待拆列表

### 已完成第一层：`_update_highlight()` 目标 hover 控制器

完成情况：

- 已新增 `scene/in_scene/hex_map_modules/input/TargetHoverController.gd`。
- `_update_highlight()` 现在只负责调用控制器，并回写 `hovered_stacks` 与 `active_stack`。
- “从 hovered_stacks 选 front_stack”和“active_stack 变化时调哪些回调”已经离开 `hex_map.gd`。
- CardManager 查找和 MainBoard 查找仍由 HexMap 注入，避免本批改变生命周期依赖。

剩余问题：

- `_update_aoe_display()` 仍负责把 AOE 展示计划交给视觉状态 presenter，并把 tooltip 请求翻译回 MainBoard 旧接口。
- CardManager 多路径查找仍留在 `hex_map.gd::get_card_manager()`。

风险：

- 后续继续拆 AOE 表现时，需要回归卡牌 hover、tooltip、遮挡和右键取消。

### 已完成第一层：`_update_aoe_display()` AOE hover 展示计划

完成情况：

- 已新增 `scene/in_scene/hex_map_modules/presenters/TargetAoeHoverPresenter.gd`。
- AOE 范围收集、目标状态选择和 tooltip 请求已经离开 `hex_map.gd`。
- `_update_aoe_display()` 现在只负责应用视觉状态和调用旧 MainBoard tooltip 入口。
- 目标合法性仍由 HexMap 注入，避免本批改变 CardManager 生命周期兜底。

剩余问题：

- MainBoard tooltip 仍是旧直接调用，后续可以拆成 UI controller 或事件。
- CardManager 查找仍在 `_is_stack_valid_target()` 间接路径里。

风险：

- 后续继续拆 tooltip 或 CardManager 查找时，必须回归合法目标、非法目标、范围型效果和无目标卡牌。

### 已完成第一层：`_create_stack_at()` 基础地块栈工厂

完成情况：

- 已新增 `scene/in_scene/hex_map_modules/factory/TileStackFactory.gd`。
- 基础 `Area2D` 容器、顶面/侧面 sprite、基础 shader 参数和碰撞体创建已经离开 `hex_map.gd`。
- `_create_stack_at()` 仍保留旧入口名，并通过 `_build_tile_stack_factory_config()` 传入所有旧导出项和运行时状态。
- 输入信号、高度视图标签和入场 dissolve 收尾仍留在 HexMap。

剩余问题：

- `_create_stack_at()` 仍写入 `sprites/height/occupant` metadata，并连接鼠标输入信号。
- `refresh_tile_visual()` 仍然通过删除旧 stack 再重新 `_create_stack_at()`，属于较粗的局部重绘。

风险：

- 后续继续拆初始化收尾时，需要重点回归地图生成、局部刷新、平铺视图生成、入场动画、血条和碰撞。

### 已完成第一层：地貌挂接与 sprite 收编服务

完成情况：

- 已新增 `scene/in_scene/hex_map_modules/factory/TileLandformAttachService.gd`。
- 初始建图路径中的地貌位置、挂接、`attach_visual()`、地貌 sprite shader 参数、`owner_battle` 和敌人分组已经离开 `hex_map.gd`。
- 本批没有直接复用 `RuntimeLandformRegistrar.gd`，因为初始建图旧逻辑和运行时注册在数据写入、分组和 sprite 扫描范围上存在差异。

剩余问题：

- 初始建图和运行时注册还没有共享底层 sprite 收编工具。
- `_create_stack_at()` 仍负责 metadata、输入信号、高度标签和入场 dissolve 收尾。

风险：

- 后续如果统一初始建图与运行时注册，需要重点回归地貌显示、血条定位、敌人分组、奖励扫描和旧建筑行为。

### 第一优先级：`_create_stack_at()` 初始化收尾服务

当前问题：

- `_create_stack_at()` 仍写入 `sprites/height/occupant` metadata。
- `_create_stack_at()` 仍处理入场 dissolve 初始值、鼠标 hover/input 信号连接和平铺高度标签。

建议拆法：

- 新建 `hex_map_modules/factory/TileStackInitializationService.gd`。
- 第一刀只搬 metadata 写入和输入信号连接，高度标签可以继续留在 HexMap。
- 如果高度标签一起搬，需要把 `_create_height_indicator()` 作为回调注入，不要让初始化服务直接依赖高度视图 presenter。

风险：

- 输入信号连接影响点击、hover、结算奖励 hover、敌人意图 hover 和卡牌 AOE hover，必须完整回归。

### 第二优先级：CardManager 查找服务

当前问题：

- `hex_map.gd::get_card_manager()` 同时走 MainBoard、root metadata、current_scene metadata。
- 奖励脚本里也存在多套 CardManager 自动查找逻辑，风格不统一。

建议拆法：

- 新建通用 `CardManagerLocator.gd`，统一 root/current_scene/MainBoard 查找和失效 meta 清理。
- 先让 HexMap 使用，再逐步替换奖励脚本中的重复查找。

风险：

- CardManager 生命周期跨局内、奖励、商店，不能一次性删所有 fallback。

### 第二优先级：敌人和建筑回合行为处理

当前问题：

- `_on_step_next()` 直接遍历 `iron_mine.Library`，再遍历 `map_data`，并调用旧 `Behavior()`。
- 它把旧建筑库、地图数据和回合事件绑定在 HexMap 中。

建议拆法：

- 新建 `hex_map_modules/turn/TileTurnBehaviorRunner.gd`。
- 把“先处理 iron_mine，再处理其他地貌”的旧顺序保留，只搬运遍历和调用。

风险：

- 旧建筑行为可能依赖顺序，必须保留 iron_mine 优先。

### 第二优先级：结算奖励状态 controller

当前问题：

- 结算奖励 presenter 已拆，但奖励资格扫描、reward_info 构造、used 状态写入还在 HexMap。

建议拆法：

- 新建 `hex_map_modules/rewards/SettlementRewardController.gd`。
- Presenter 继续只管视觉，Controller 管状态扫描和 reward_info。

风险：

- 影响胜利后奖励入口、局外回流和奖励使用状态。

### 第三优先级：`refresh_tile_visual()` 局部重绘

当前问题：

- 当前做法是删除旧 stack 再重新 `_create_stack_at()`，简单但较粗。
- 后续如果 `TileStackFactory` 成熟，可以让它变成更明确的“重建单格视觉”服务。

建议拆法：

- 等 `TileStackFactory` 完成后再动。

## 臃肿代码审查

### 明显臃肿但暂不建议本轮删除

- `hex_map.gd::get_card_manager()`：有多条查找路径，确实臃肿，但这是跨场景生命周期的真实历史债。建议先抽 locator，再逐步删重复路径。
- `DragShapeController.gd` 中大量 `has_method()`：很多是为了兼容 timeline UI、卡牌视觉和旧拖拽接口。可以按接口分批清理，不建议一口气删除。
- 奖励脚本中的 CardManager 自动查找：`AcquireReward.gd`、`RemoveReward.gd`、`CraftReward.gd`、`ShopManager.gd` 都有相似查找链。应该统一到 locator，而不是在每个脚本里单独删。
- `custom_card.gd::apply_effect_immediate()`：它现在是 DragShapeController 缺失时的最终回退。正常战斗流程应该不走它，但要先确认所有测试路径都有 DragShapeController，再考虑删除。

### 看起来可以删除，但需要先统一契约

- `HexMapInputCoordinator.gd` 中 `active_card.has_method("play_card")`：当前 `CardManager.current_selected_card` 类型是 `Node`，而实际战斗卡牌是 `CustomCard`。如果把 CardManager 的选中卡牌类型约束为 `CustomCard` 或统一出牌接口，就可以删掉这个兜底。
- `hex_map.gd::_handle_enemy_intent_stack_hover()` 中 `has_method("handle_map_stack_hover")`：如果 `EnemyIntentManager` 路径和接口固定，可以删。但目前路径查找仍是软依赖，建议等 TimelineSystem 依赖拆分后处理。
- `hex_map.gd::_is_valid_settlement_reward_landform()` 中多段 `has_method()`：如果所有敌方可奖励建筑都继承统一奖励接口，可以删。当前建筑脚本仍是多态鸭子类型，建议先保留。
- `TimelineManager.gd` 中敌人意图相关 `has_method()`：敌人意图接口已经较多，适合抽 `EnemyIntentProvider` 契约后再删。

### 应该保留的防护

- `is_instance_valid()`：Godot 节点常被 `queue_free()` 延迟释放，跨帧、Tween、信号回调里必须保留。
- `get_tree().current_scene` 和 root meta 的失效清理：第二次进入局内时曾经出现释放实例残留，这类防护不能直接删。
- `TileDestructionBatchQueue.gd` 的 metadata 等待：它避免时间轴在地块销毁 VFX 未结束时继续结算，应保留。
- 平铺视图缓存中的类型检查：高度视图会同时处理 `Node2D` 和 `Control`，不能因为看起来啰嗦就删。

## 下一步建议顺序

1. 拆 `_create_stack_at()` 的初始化收尾：metadata、输入信号、入场 dissolve 和高度标签。
2. 抽 `CardManagerLocator.gd`，统一 HexMap 和奖励脚本的查找路径。
3. 抽 `_on_step_next()` 的建筑回合行为 runner。
4. 拆 MainBoard tooltip 旧接口适配，让 HexMap 最终只发目标 hover 事件。
5. 最后再清理 `has_method` 兜底，每次只删一个契约已经统一的接口。

## 验证要求

每一批继续按固定命令验证：

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

手动回归优先走：

- 选中卡牌 hover 地块。
- 点击合法和非法目标。
- 平铺高度视图 hover。
- 敌人意图 hover。
- 结算奖励 hover 和点击。
- 地块升降到超限并销毁。
