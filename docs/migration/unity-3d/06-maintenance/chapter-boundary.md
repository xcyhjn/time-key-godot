# 章节与局内边界

> 状态：Godot 局外继续权威；Unity 桥接未实现
> 负责人：主智能体
> 最后验证日期：2026-08-01

## 责任边界

Unity 只负责一场局内战斗。地图揭示、房间上下文、奖励消费、章节进度与 active room 清理仍由 Godot 拥有。本阶段不添加 Unity `SceneManager` 章节服务，不迁移局外流程，也不改变 Godot 权威 payload 语义。

## 当前权威入口

- `scene/in_scene/in_scene_modules/scene_flow/InSceneExternalPayloadParser.gd` 解析局外入场字符串。合法格式是 `battle_normal <map_seed>`、`battle_elite <map_seed>` 或 `boss_stage <map_seed>`，输出 `battle_tag`、`map_seed` 和原 payload。
- `scene/in_scene/in_scene_modules/scene_flow/InSceneReturnPayloadBuilder.gd` 组装局内返回字典：`transition_type=return_from_combat`、`combat_result=completed`、`battle_state=settlement`、`battle_tag`、`map_seed`、`era`、`timecoins`、`deck_snapshot`、`deck_size`、`room_context`、`clear_active_room_context`。
- `scene/out_scene/out_scene_modules/RoomResolutionController.gd` 读取返回 payload。只有 completed + boss room + 当前边界匹配时，`should_advance_tier_from_boss_payload()` 才推进章节。

## 未来 Unity 桥接的最小修改面

桥接实施前必须先冻结带版本的纯数据 DTO，再在 Composition/Infrastructure 边界进行转换。Unity Domain、Application 和 Presentation 不得解析 Godot 场景字符串，Godot 也不得依赖 Unity `GameObject`、Prefab 或 asmdef 类型。

一次完整桥接只包含：

1. Godot 用现有 parser 产生战斗上下文。
2. 桥接把上下文转成版本化纯数据 DTO，启动一场 Unity 战斗。
3. Unity 只返回战斗结果和契约字段，不决定章节推进。
4. Godot 用现有 builder/controller 完成结算和章节判定。

## 验证与调试

当前没有 Unity 桥接资产或 Inspector 引用可编辑，也没有独立的跨引擎自动化测试；不得将此文档当作“桥接已完成”的证据。修改边界前至少执行：

```powershell
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --path . --editor --headless --quit --verbose
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --path . --headless --quit-after 900
```

断点放在 `InSceneExternalPayloadParser.parse()`、`InSceneReturnPayloadBuilder.build()` 和 `RoomResolutionController.should_advance_tier_from_boss_payload()`。重点检查 `battle_tag/map_seed`、`room_context`、`clear_active_room_context` 和 boss 边界；常见故障是字段丢失、seed 被重新生成、completed 结果被误判为普通房间，或 Unity 越权清理 active room。

视觉验收必须从实际局外地图进入普通、精英与 boss 战斗，返回后检查地图、奖励和章节边界；只跑 Unity Player smoke 不能证明该边界。

## 回滚

桥接仍未实现，当前回滚方式是保留 Godot 现有 parser/builder/controller 不变。未来变更必须以单一目的提交加入桥接；回滚时移除新增适配层和接线，不覆盖 Godot 存档、用户资源或原 payload 处理。
