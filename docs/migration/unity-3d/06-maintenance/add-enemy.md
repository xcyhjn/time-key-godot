# 添加敌人

> 状态：当前只有敌方意图占位契约，尚不支持可执行敌人
> 负责人：主智能体
> 最后验证日期：2026-08-01

## 先明确当前能力

当前 Unity 切片没有敌人定义、敌人运行态、敌人 Prefab 或敌人效果结算。`VerticalSliceController` 组装一个 `cardId=enemy-intent`、伤害为 0 的 `TimelineAction`；`TimelineGrid.Resolve()` 对 enemy action 只在 `ResolutionSnapshot.EnemyIntentResolved` 中记录“已处理”。

这是 MIG-002 冻结的源行为，不是完整敌人系统。在 Godot 玩法波次明确敌人意图命令前，不得在 Unity 中自行发明敌人伤害、AI 或章节奖励。

## Godot 权威参考

Godot 战斗实体使用 `scene/in_scene/tile.gd` 的 landform 契约，包含 `HP/Max_Blood`、`take_damage()`、`heal()`、status 和 `get_intent_*`/`can_generate_intent()`/`get_intent_action()` 方法。调用链是：

```text
具体 landform/敌人
  -> EnemyIntentManager.generate_enemy_intents()
  -> TimelineManager.place_action()
  -> TimelineManager.resolve_timeline()
  -> EffectProcessor.process_action()
```

`EnemyIntentResolver.resolve_enemy_intent()` 产生地图与时间轴共用的 `EnemyIntentData`。`EffectProcessor._parse_enemy_intent()` 目前仍返回空队列；Unity 必须保持这个差异为可见技术债，不得隐式“修正”。

## 未来最小实现顺序

1. 在 Domain 中增加纯数据敌人运行态：stable ID、HP/MaxHP 和 `HexCoord`；不得用 `GameObject` 作为 identity。
2. 冻结敌人意图的来源、无效原因与 `TimelineAction` 转换；Application 只组织命令顺序。
3. 依据真实 Godot 规则扩展 `ResolutionSnapshot`，让 Presentation 消费不可变结果。
4. 在 `unity/Assets/_Project/Prefabs/Battle/Targets/` 增加可编辑 Prefab，通过序列化 Composition/Presenter 引用接线；不在 Domain/Application 加载资源。
5. 最后增加 Domain/EditMode、Application/EditMode、Prefab/PlayMode 和实际渲染证据。

只有以上运行态、意图、结算结果、Prefab 和测试均存在时，才能将此指南升级为“添加一个敌人”的操作教程。

## Inspector 与视觉验收

当前 `CombatVerticalSlice.unity` 中的 `TargetAnchor` 和 `TargetView.prefab` 只是冻结目标的表现。修改 Prefab 可编辑外观，但不会创建新敌人语义。

未来敌人切片的视觉门禁至少包含：1280x720、1920x1080、2560x1080；四个 yaw 的实体选择；升高地形后 occupant anchor 正确；意图范围、Timeline 与目标标记一致；多敌人 stable ID 不串联。

## 测试、调试与回滚

当前先用 EditMode 冻结 `TimelineGrid.Resolve()` 的 enemy marker 行为，并用 Player smoke 验证 `EnemyIntentResolved=true`。未来测试必须覆盖相同 seed 的确定性、意图无效无副作用、多敌人顺序、敌人死亡后的时间轴行为和 trace 的失败原因。

首要断点是意图数据转 `TimelineAction`、`TimelineGrid.Resolve()` 的 enemy 分支和 Presenter 的 stable ID 绑定。常见失败是把 Prefab 实例当成 Domain identity、表现重新计算范围、或为了“完整”而改变 MIG-002。

回滚应移除未完成的新敌人运行态/意图/Prefab 接线的单一目的提交，恢复到已验证的固定 enemy marker；不回退 Godot 权威脚本、用户资源或无关脏文件。
