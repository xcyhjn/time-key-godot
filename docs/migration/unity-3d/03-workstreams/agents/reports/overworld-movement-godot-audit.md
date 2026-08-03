# Wave 03P Godot 局外移动源语义审计

> 审计角色：只读 Godot source auditor
> 审计日期：2026-08-03
> 写入边界：本报告是本 Agent 唯一写入；未启动 Godot、Unity、Blender、测试、Build 或 Git 写操作。

## 1. 范围与方法

已逐行读取以下必需输入：

- `scene/out_scene/Out_Scene.tscn`
- `scene/out_scene/out_scene_map_exp.gd`
- `scene/out_scene/map_generator.gd`
- `scene/out_scene/map_renderer.gd`
- `scene/out_scene/camera_2d_outscene.gd`
- `scene/out_scene/point.gd`
- `scene/out_scene/HexUtils.gd`
- `scene/out_scene/out_scene_modules/ChapterRevealAnimationRunner.gd`
- `scene/out_scene/out_scene_modules/OutSceneCameraLimitController.gd`
- `scene/out_scene/out_scene_modules/OutScenePayloadBridge.gd`
- `scene/out_scene/out_scene_modules/RoomResolutionController.gd`
- `scene/global/map_data.gd`
- `scene/global/Saver.gd`
- 局内返回 payload、scene switch 和教程派生调用点
- Wave 03P Prompt 与 gap analysis

只读命令包括 `Get-Content` 分段逐行读取、`rg -n` 符号/调用点搜索、`rg --files` 资源引用搜索和 `Test-Path` 缺失引用检查。未以 README 代替源码。首次对 `Out_Scene.tscn` 的组合正则因 PowerShell 引号解析失败，随后拆成单引号只读查询并完成检查；失败命令没有产生写入。

## 2. 已验证的源事实

### 2.1 地图坐标、六方向和节点类型

1. 地图使用轴向 `(q, r)` 坐标。默认像素投影是 `x = q * 272`、`y = r * 304 + q * 152`：导出默认值在 `out_scene_map_exp.gd:14-17`，运行时转换在 `out_scene_map_exp.gd:453-454`，独立工具实现同一公式于 `HexUtils.gd:6-7`。
2. 六边距离为 `(abs(dq) + abs(dr) + abs(dq + dr)) / 2`：`map_generator.gd:139-140` 与 `HexUtils.gd:3-4`。主控制器和结算模块使用等价的到原点位移公式：`out_scene_map_exp.gd:449-450`、`RoomResolutionController.gd:104-105`。
3. 生成器的正式六方向完整且互异：`(-1,1), (-1,0), (0,-1), (1,-1), (1,0), (0,1)`，见 `map_generator.gd:18-19`。半径 1 的六个格按该数组索引赋为 `CHAR_1` 至 `CHAR_6`，见 `map_generator.gd:26-39`。
4. `TileType` 的源值为 `MOUNTAIN=-1, VOID=0, NORMAL=1, ELITE=2, EVENT=3, BOSS=4, START=5, CHAR_1..CHAR_6=6..11`，见 `map_generator.gd:15`。
5. 默认总半径为 12，章节边界为 `[4,8,12]`；边界环被生成成 Boss，中心是 Start，见 `map_generator.gd:5-7,26-45`。生成器会对每个正式方向检查通向半径 12 的正值路径，并在必要时把非正值格挖成 Normal，见 `map_generator.gd:56-107`。
6. `Out_Scene.tscn:928-948` 保存了 MapGenerator/MapRenderer 及所有地图纹理引用；Scene 仅覆盖生成权重为 Normal 50、Elite 20、Event 20，坐标间距、移动时长和相机参数继续使用脚本默认值。

### 2.2 `HEX_DIRS` 异常不能复制

`out_scene_map_exp.gd:71-78` 另有一份 `HEX_DIRS`：

```text
(-1,0), (1,-1), (0,-1), (-1,0), (-1,1), (0,1)
```

它重复 `(-1,0)`、缺少 `(1,0)`，顺序也不等于 `MapGenerator.hex_directions`。它不参与普通点击邻接判定；只在 `_ready()` 的 `MapState.is_initialized && !MapState.loaded` 路径中以 `HEX_DIRS[chosen_char_index - 1]` 裁剪扇区和设置相机边界，见 `out_scene_map_exp.gd:143-189`。新选择角色的正常确认路径则直接使用实际 `target` 坐标，见 `out_scene_map_exp.gd:537-568`。

因此该常量是继续游戏/恢复路径的源异常，而不是 Unity 六方向契约。特别是 `chosen_char_index - 1` 还让索引 0 取数组末项；六个角色里只有索引 1 恰好与生成器对应方向相同。Unity 必须以生成器的六个互异方向和距离 1 测试为准，同时保留此差异记录。

### 2.3 点击合法性和可见状态

1. 根控制器只在 `is_moving == false` 时处理输入，见 `out_scene_map_exp.gd:473-474`。
2. 只处理左键抬起；若相机报告实际拖拽则直接退出，见 `out_scene_map_exp.gd:475-479`。
3. 目标由本地鼠标位置经 cube rounding 转回轴向坐标，见 `out_scene_map_exp.gd:649-666`。
4. 点击触发移动的全部源条件只有：目标 key 存在、tile type `> 0`、目标与 `player_hex` 的距离等于 1，见 `out_scene_map_exp.gd:479-483`。所以 VOID、MOUNTAIN 不可走；Start、普通/精英/事件/Boss、角色格均可走。源码没有“只能向外”、visited、settled 或禁止回到已走格的规则。
5. `MapRenderer.draw_map()` 完全跳过 VOID，因此 VOID 没有 Sprite；Mountain 仍会绘制，见 `map_renderer.gd:44-57`。
6. `MapRenderer.update_visuals()` 的状态是纯派生显示：当前格 `Color(1.2,1.2,1.2)`；距离 1 且 type `> 0` 为 `Color(0.6,1.2,0.6)`；距离 1 且 type `<= 0` 为灰 `Color(0.5,0.5,0.5)`；其余可见格为白色，见 `map_renderer.gd:94-114`。由于 VOID 没有 Sprite，灰色邻格实际主要是 Mountain。
7. 到原点距离超过当前 tier 边界的节点直接隐藏，见 `out_scene_map_exp.gd:668-672`、`map_renderer.gd:101-107`。源码没有命名为 `locked` 的节点状态；Wave 03P 可把“章节外隐藏”和“不可通行”显式拆成 `Locked`/`Blocked`，但这是目标契约，不是原版字段。

### 2.4 current / available / visited / settled 的准确含义

| 名称 | Godot 源事实 |
| --- | --- |
| current | 唯一逻辑位置是局部 `player_hex`，初始为 `(0,0)`，加载时来自 `MapState.player_hex`；Player Sprite 和 Camera 恢复到该坐标，见 `out_scene_map_exp.gd:56,228-233,263-290`。 |
| available | 没有持久字段。显示上等价于“当前格距离 1 且 type > 0 且在可见 tier 内”；点击再次独立计算相同距离/type 条件。 |
| locked | 没有持久字段。章节边界外节点隐藏；type `<=0` 不可走。二者不应在 typed contract 中混为一类。 |
| visited | 游戏玩法没有 visited 集合。`map_generator.gd:65-82` 的 `visited` 只用于生成期 BFS。`MapState.path_gone` 存在并持久化，但当前只在角色选择确认时追加一次 target，见 `out_scene_map_exp.gd:567-568`；普通移动不追加，也不参与合法性或渲染。 |
| settled | 没有房间 settled 字段或节点结算集合。`MapState.ui_settled` 只控制时钟 UI 是否直接吸附到 UI，见 `map_data.gd:14`、`out_scene_map_exp.gd:282-284`，不能解释成房间已结算。 |

### 2.5 普通移动时序

普通相邻正值格的源码顺序如下：

1. `_move_to()` 立即将 `is_moving=true`，缓存旧像素位置/旧坐标，计算目标像素，见 `out_scene_map_exp.gd:485-492`。
2. Player 使用 `TRANS_SINE/EASE_OUT` 移动 `move_duration * Global.get_anim_speed()`；默认 `0.3s`，并等待 Tween 完成，见 `out_scene_map_exp.gd:20,494-498`。全局倍率为慢 `1.5`、中 `1.0`、快 `0.5`，见 `scene/global/global.gd:34-39`。
3. 非首次角色选择分支在 Player Tween 完成后才把 `player_hex=target`，然后创建 Camera position 的 `0.3 * speed` Tween，见 `out_scene_map_exp.gd:580-583`。
4. Camera follow 没有显式 transition/ease，且没有 `await`。随后立即 `is_moving=false`、刷新视觉，见 `out_scene_map_exp.gd:582-586`。
5. 若 type 为 1..4，再 `await _enter_room_logic(target)`，见 `out_scene_map_exp.gd:588-589`。

这说明评估文档中的“同时启动 0.3 秒镜头跟随”不是当前源的严格时序：镜头 Tween 是玩家 0.3 秒完成后才启动，而且输入锁在镜头 Tween 完成前已释放。

### 2.6 角色选择的确认与取消

首次走到 type `>=6` 的角色格会在玩家预移动完成后把 Camera 以 `0.5 * speed` 正弦缓入缓出聚焦到目标，发出 `Global.choose(type)`，并等待 confirm/cancel，见 `out_scene_map_exp.gd:500-515`。

- confirm：设置角色、播放缩放/闪白，裁掉所选扇区外 tile，设置扇区相机边界，然后提交 `player_hex=target` 并仅在这里追加 `MapState.path_gone`，见 `out_scene_map_exp.gd:516-568`。
- cancel：先用 `0.4 * speed` 恢复 Camera，再用 `move_duration * speed`、`TRANS_SINE/EASE_IN_OUT` 把 Player 返回旧像素位置；`player_hex` 恢复旧值、释放 `is_moving`、刷新视觉并返回，见 `out_scene_map_exp.gd:569-579`。

这是当前唯一明确的移动取消/回滚。普通地图移动没有用户取消接口，也没有 Tween 中断恢复处理。

### 2.7 输入锁、拖拽、滚轮、键盘与 Camera 边界

1. Camera 有独立 `_is_locked`；锁定时 `_process` 和 `_unhandled_input` 都退出，见 `camera_2d_outscene.gd:13-16,25-34`。该锁只由 focus/restore 流程直接维护，不等同于主控制器 `is_moving`。
2. 左键按下开始候选拖拽；鼠标到按下点距离严格 `> 5.0px` 才把 `is_actually_dragging=true`，见 `camera_2d_outscene.gd:42-55`。恰好 5px 不算拖拽。
3. 无论是否超过 5px，每个按住左键的 MouseMotion 都会实际平移 Camera，因为 `position -= event.relative * drag_sensitivity / zoom.x` 位于阈值分支之外，见 `camera_2d_outscene.gd:53-56`。因此小于等于 5px 的移动可能既平移镜头又仍被当成点击。
4. 左键抬起后 Camera 延迟一帧才清 `is_actually_dragging`，目的是让主控制器读取，见 `camera_2d_outscene.gd:47-52`。
5. 滚轮每步把目标 zoom 乘 `1.1` 或除 `1.1`，逐轴 clamp 到 `[0.5,1.0]`；实际 zoom 每帧以 `zoom_smoothness * delta`（默认 10）向目标 lerp，见 `camera_2d_outscene.gd:3-11,25-31,35-41,82-85`。
6. 键盘从 `ui_left/right/up/down` 读取轴，以默认 `1500 * delta` 平移，不按 zoom 缩放，见 `camera_2d_outscene.gd:7,87-92`。
7. Scene 保存 `position_smoothing_enabled=true`、`position_smoothing_speed=10`，见 `Out_Scene.tscn:953-956`。
8. tier 边界公式见 `OutSceneCameraLimitController.gd:7-32`：`w=radius*step_x+300`、`h=radius*(step_y+stagger_y)+300`；left/right 再各扩 1000，top 加 500，bottom 减 500，并启用 smooth limits。默认 tier 0/radius 4 得到 left/right `-2388/2388`、top/bottom `-1624/1624`。
9. 角色扇区边界仅按所选六方向覆写部分现有边，margin 为 1400，见 `OutSceneCameraLimitController.gd:35-53`；它没有先重置全部四边。

### 2.8 房间进入、保存与切场失败

对 Normal/Elite/Boss/Event（type 1..4）：

1. `player_hex` 已在 Player Tween 完成后提交，主移动锁也已释放，然后才进入 `_enter_room_logic()`。
2. `_enter_room_logic()` 先等待 `switch_delay * speed`，默认 `0.5s`，见 `out_scene_map_exp.gd:674-676`。
3. room payload 分别为 `battle_normal`、`battle_elite`、`boss_stage`、`event_stage`，末尾追加 `map_seed`；Event 切向 `res://event.tscn`，其余切向战斗 Scene，见 `out_scene_map_exp.gd:677-686`。
4. 延迟结束后才写 `MapState.active_room_context`，字段为 `source_scene, room_hex, room_type, room_data, map_seed, current_tier`，见 `out_scene_map_exp.gd:695-705`。
5. dim 过渡完成后调用 `_switch_scene_with_data()`。只有目标 PackedScene 成功加载后才执行 `_save_to_global()`；保存 seed、tile data/features、`player_hex`、tier、角色和 UI 状态，并调用 slot 0 存档，见 `out_scene_map_exp.gd:707-720,724-743`。
6. `Saver.Save_game()` 实际持久化 `player_hex`、角色、seed、tier、初始化状态、tile data/features、`path_gone` 和 deck，见 `Saver.gd:32-58`。`active_room_context` 与 `pending_room_resolution` 不写磁盘。
7. PackedScene 加载失败时只退黑幕并返回，不调用 `_save_to_global()`，见 `out_scene_map_exp.gd:724-730,747-766`。此时局部 `player_hex` 已是目标、`active_room_context` 已写入，但 MapState/磁盘位置仍可能是旧值。
8. 仓库检查确认默认 `combat_scene` 存在，但默认 `event_scene = res://event.tscn` 不存在；仅找到 Dialogic 编辑器事件 Scene，没有正式 `event.tscn`。因此 Event 节点会走上述已知加载失败路径，除非运行时/Inspector 另行覆盖（`Out_Scene.tscn` 没有覆盖）。

### 2.9 返回 outcome 和房间身份

1. 局内结算按钮只在 `SETTLEMENT` 状态触发返回，见 `in_scene.gd:794-823`。
2. 返回 payload 固定包含 `transition_type=return_from_combat`、`combat_result=completed`、`battle_state=settlement`、`battle_tag`、`map_seed`、`era`、`timecoins`、deck snapshot、`room_context` 和 `clear_active_room_context=true`，见 `InSceneReturnPayloadBuilder.gd:9-28`。
3. `room_context` 是进入房间时 `MapState.active_room_context` 的副本，所以当前唯一房间身份是 `room_hex` 加类型/seed/tier；没有稳定 `MapNodeId`、launch id、outcome id 或 sequence，见 `InSceneReturnPayloadBuilder.gd:31-38`。
4. 返回编排先把 payload 存到 `MapState.pending_room_resolution`，随后才禁用输入、隐藏 UI、过渡和切场，见 `InSceneReturnFlowController.gd:9-23,41-43`。
5. OutScene ready 后，显式注入的 Dictionary 优先，并清空 pending；否则 peek 后 consume 一次 pending，见 `RoomResolutionController.gd:8-27` 与 `out_scene_map_exp.gd:319-325`。这避免同一次正常返回同时走显式与 fallback 两条路径，但不是带 identity/sequence 的通用幂等契约。
6. 普通/精英/事件返回只刷新全局进度文字并默认清空 active context；源码注释明确把“定位节点、标记完成/清空/领取”留到未来，见 `out_scene_map_exp.gd:328-350`。当前不会写 visited/settled。
7. Boss 只有在 `combat_result=completed`、payload 能识别为 Boss、目标坐标仍是 Boss tile、且恰好位于当前 tier 边界时才推进下一 tier，见 `RoomResolutionController.gd:34-61`。推进先提交 `current_tier`，再更新相机/播放新环揭示，并在完成后保存，见 `out_scene_map_exp.gd:378-413`。
8. 该返回路径没有 Defeat outcome；失败走独立 GameOver 流程。仓库中也没有对普通房间重复 outcome replay 的显式 node-settlement guard。

## 3. 推断（不是直接源字段）

1. 因为主移动锁在创建 Camera follow 后立即释放，玩家可能在 Camera 仍 Tween 时发起下一步移动；这是由时序推出，未运行窗口验证。
2. 因为 `_enter_room_logic()` 的 0.5 秒等待发生在 `is_moving=false` 之后，等待期间可能再次点击并启动另一 `_move_to()`/room timer；源码没有 request token 或 transition lock。是否在实际输入分发中稳定复现需要窗口测试。
3. 小于等于 5px 的拖动会改变 Camera 但不会置 `is_actually_dragging`，因此抬起可能继续触发格子点击；具体目标坐标取决于 Control/Camera 坐标变换和事件顺序，需要视觉/交互测试。
4. 返回 Scene 加载失败时 pending resolution 已保存、玩家输入/UI 已禁用/隐藏；已读失败恢复只退黑幕，没有恢复输入/UI。实际旧 Scene 是否仍可操作需要运行验证。
5. Boss 重复 payload 在 tier 已推进后通常会因“旧 Boss 不在新边界”而拒绝再次推进，但这只是数据条件的偶然保护，不等于正式幂等 identity。

## 4. 未解决行为与源矛盾

1. 未启动 Godot，故 5px 边界、事件传播顺序、Camera follow 与输入重入均未做真实窗口确认；Gate C 前必须补起点/移动中/到达/非法/拖拽/进房前帧。
2. 普通 Tween 在 node disable、queue_free、Scene rollback 或动画被 kill 时没有显式 cancel callback；逻辑最终落在旧格还是目标格取决于 Godot Tween/coroutine 生命周期，源码未定义。
3. `_move_to()` 没有验证 target 在 await 期间仍存在，也没有 revision；角色确认裁图和章节推进与并发输入的边界没有 typed guard。
4. `MapState.path_gone` 名称暗示路径，但只记录角色确认 target；它与正常走过路径不一致。
5. `ui_settled` 是 UI 时钟状态，不是节点结算；直接迁移为 room settled 会产生语义错误。
6. `HEX_DIRS` 和 `chosen_char_index - 1` 与生成器的六方向/角色索引冲突，主要影响继续游戏/恢复分支。
7. `Saver.Buffer_Change()` 只对 `player_location` 做反向赋值样式的处理，其他 Buffer signal 没有实际写入 `Save_Buffer`；不过正式 `Save_game()` 直接从 MapState 读取，因此本阶段不应依赖 Buffer 作为移动提交契约。
8. Event Scene 默认引用缺失，不能拿 Event 的成功进入行为作为已验证基线。

## 5. 建议冻结的最小 typed contract

以下是 Unity 目标契约，不声称为 Godot 已有类型：

- `MapNodeId`：稳定、非空、值相等的节点身份；SceneFlow 只携带该身份，不从显示名或 GameObject 名反推。
- `AxialHexCoord(q, r)`：值类型；内置上述六个互异方向、距离和邻接，不引用异常 `HEX_DIRS`。
- `MapNodeType`：至少 `Start, Normal, Elite, Event, Boss, CharacterChoice, Blocked`；`Void` 可选择根本不进入节点表。
- `MapNodeSnapshot`：`Id, Coord, Type, IsAvailable, IsVisited, IsSettled, IsLocked`。`IsLocked`（章节/规则）、`Blocked`（地形）、`IsSettled`（outcome）必须分开。
- `OverworldMovementSnapshot`：`Revision, CurrentNodeId, Phase, Nodes, VisitedNodeIds, SettledNodeIds, ActiveMove`；所有集合防御性复制。
- `MoveCommand`：`CommandId, ExpectedRevision, SourceNodeId, TargetNodeId`。
- `MoveTicket`：Request/Begin 成功后返回的不可伪造值，包含 command identity 和 source/target；Presentation 只凭 ticket 播放动画。
- 状态最小为 `Idle -> Moving -> Arrived -> RoomPending/Idle`。`Previewing` 可由 View 保持，不必污染原子移动 Domain。
- `RequestMove/BeginMove` 显式拒绝：source 非 current、同格、source/target 缺失、非邻接、blocked、locked、settled、已有 active move、stale revision、重复 command。
- `CommitArrival(ticket)` 是唯一修改 current/visited/revision 的入口；同 ticket 重复提交返回 `AlreadyCommitted`，不得二次副作用。
- `CancelOrFail(ticket)` 清除 active move 并保持 source current/visited/settled 不变；重复 cancel 返回稳定结果。
- `RoomLaunchIdentity`/`RoomOutcomeIdentity` 至少绑定现有 run/transition sequence、`MapNodeId` 和 launch/outcome sequence。只有匹配当前 `RoomPending` 的一次 outcome 能写 settled；不匹配和 replay 均无副作用。

可见节奏可以继承默认 `0.3s Sine EaseOut + 0.5s room preparation`，但 typed contract 应采用 Wave 03P 收紧后的顺序：验证并锁输入 -> 动画 -> 原子 CommitArrival -> RoomPending/SceneFlow；Camera 可以同播，不能决定 Domain commit。

## 6. 最小测试矩阵

| 层 | 用例 | 关键断言 |
| --- | --- | --- |
| Hex | 六方向 | 六个方向互异、距原点均为 1、各自有反方向；不包含重复方向。 |
| Hex | 距离/投影 | distance 对称；默认 `(272,304,152)` 的投影和已知坐标一致；pixel roundtrip 覆盖边界附近。 |
| Graph | 三节点 fixture | 起点连接两个真实相邻目标；两个目标之间按其坐标得出真实邻接/非邻接，不靠手填布尔值。 |
| Request | 合法相邻 | 返回 ticket，尚不修改 current/visited/revision。 |
| Request | 同格/非邻接/缺失 | 各有不同失败原因；snapshot 完全不变。 |
| Request | blocked/locked/settled | 各显式拒绝；不把章节锁、地形阻挡、房间结算混为一个原因。 |
| Request | reentrant/stale/duplicate | active move 时拒绝第二请求；旧 revision 拒绝；同 CommandId 重放幂等。 |
| Commit | 正常到达 | current 只改变一次，visited 只加入一次，revision 只加一次。 |
| Commit | stale/错误 ticket/重复 | 均不修改位置；重复返回 AlreadyCommitted。 |
| Cancel | 动画失败/Escape/disable | 回到 Idle，current/visited/settled/revision 无部分副作用；重复 cancel 幂等。 |
| Presentation | 时间线 | `t=0/0.15/0.3s` 保留起/中/末帧；Player 与 Camera 使用 Sine EaseOut，Domain 只在末帧成功后提交。 |
| Input | 拖拽阈值 | `0, 4.99, 5.0px` 不算 drag，`5.01px` 算 drag；任何被判定为 drag 的 release 都不请求移动。产品实现还应避免 sub-threshold pan+click 的源缺陷。 |
| Input | wheel/keyboard/focus | zoom clamp、平滑目标、键盘平移、focus/pointer 选择互不抢占；全局/transition lock 下均无请求。 |
| Room | 0.5 秒准备 | 到达后进入 RoomPending；等待期重复点击/确认/Escape 不产生第二 launch。 |
| SceneFlow | launch identity | payload 的 node id/coord/type/seed/tier 与 committed target 完全一致。 |
| SceneFlow | Victory/不匹配/replay | 仅匹配 node+sequence 的首个 outcome 写 settled；不匹配和重放无副作用。 |
| SceneFlow | Defeat/切场失败 | Defeat 不误写 Victory settled；切场失败保留已到达节点、恢复焦点和输入，并允许受控重试。 |
| Persistence | save timing | 只保存已提交 arrival；Moving 中断不会把视觉中间位置写入；重载恢复同一 MapNodeId/coord。 |
| Source regression | 异常护栏 | 专门断言 Unity 六方向不等于 Godot 异常 `HEX_DIRS`，并记录继续游戏映射差异。 |

## 7. 结论与所有权交回

可继承的 Godot 可见基线是：轴向六边、六方向距离 1、正值相邻格可走、默认 0.3 秒 Player 正弦缓出、5px 拖拽判定、滚轮平滑缩放、键盘平移、章节/扇区 Camera 边界、默认 0.5 秒房间准备、坐标型 room context 和一次性 pending 消费。

不得照搬的部分是：异常 `HEX_DIRS`、未锁住的 Camera/room-delay 窗口、缺少普通 visited/settled、`path_gone` 不完整、缺失 Event Scene、失败后局部/MapState 分裂，以及没有 action/node sequence 的返回幂等。Wave 03P 的 typed Domain 应保留可见节奏，同时收紧这些一致性缺口。

本 Agent 未修改任何 Godot/Unity/Scene/Prefab/共享文档/证据/Git 文件。对 `docs/migration/unity-3d/03-workstreams/agents/reports/overworld-movement-godot-audit.md` 的独占所有权现已交回主智能体。
