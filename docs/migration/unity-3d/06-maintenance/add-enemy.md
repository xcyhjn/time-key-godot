# 添加敌人

> 状态：已支持确定性 enemy intent 生命周期；当前权威空 command 为显式 no-effect
> 负责人：主智能体
> 最后验证日期：2026-08-02

## 先明确当前能力

Unity 现在具有纯数据 intent source、显式 seed/priority/shape 调度、source/target/effect 最终重判、最多五个意图、action identity、Timeline frame、地图 source/target/range 和中文详情框。source 死亡或 action clear 会按 runtime ID/identity 清理全部表现，下一周期才重新生成。

当前 vertical slice 的 source fixture 仍不是完整敌人内容，Godot 权威 command 也仍为空。因此执行结果明确为 `UnsupportedSourceCommand` no-effect，不能把它宣传为真实攻击。Tower 是 Neutral occupant，不是敌人；它只证明 building/status/death lifecycle。

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

## 最小实现顺序

1. 让现有 occupant state 表达该敌人的稳定 runtime ID、HP/MaxHP、`HexCoord`、attitude 与 intent capability；不得使用 `GameObject` 作为 identity。
2. 在 `IEnemyIntentSourceCatalog` 中投影真实 priority、shape、target resolver、description/range 和 command，不修改 scheduler。
3. command 有权威 typed effect 时增加独立 intent handler 和 before/after tests；command 为空则保持显式 no-effect。
4. 通过保存 Prefab 和 Composition/Presenter Inspector 引用增加实体外观；不在 Domain/Application 加载资源。
5. 运行 intent、lifecycle、Prefab/PlayMode、三视口/四 yaw、build 和 Player 门禁。

## Inspector 与视觉验收

实体外观必须来自保存 Prefab，intent frame 复用 `TimelineActionFrame.prefab` 的 enemy 编码。视觉门禁至少包含 1280x720、1920x1080、2560x1080；四个 yaw 的实体选择；升高地形后 occupant anchor 正确；意图范围、Timeline 与目标标记一致；多敌人 runtime ID 不串联。

## 测试、调试与回滚

测试必须覆盖相同 seed、priority tie、边界/冲突、意图最终失效无副作用、多敌人顺序、source 死亡清理和 trace invalid reason。首要断点依次是 source catalog、scheduler、resolver、lifecycle coordinator 和 identity binding。常见错误是 View 当 identity、Presentation 重算范围，或为了“完整”而改变 MIG-002。

回滚目标是该敌人的 source/handler/Prefab/fixture 单一提交，不得恢复旧固定 marker，不得回退 Godot 权威脚本、用户资源或无关脏文件。
