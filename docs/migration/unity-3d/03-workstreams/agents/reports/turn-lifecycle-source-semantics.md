# Turn Lifecycle Godot 源语义审查

> 状态：Gate 0 只读审查完成
> 日期：2026-08-02
> 写入范围：仅本报告；审查智能体未修改源码、资产或 Git 状态

## 结论

当前 Godot 源足以冻结 02B3 的生命周期、意图、Tower、Poison、死亡和交互语义，没有硬阻塞。

## 回合与时间轴

- 结束回合顺序是：锁定输入并弃牌、等待整条 Timeline、执行 `step_next` 建筑行为、清空 Timeline UI、进入新回合。
- 新回合顺序是：回合开始信号、状态处理、后续阶段/抽牌、刷新敌方意图、解锁输入；首次进入战斗也复用 `_start_turn(false)`。
- Timeline 外层扫描 `x=0..11`，内层扫描 `y=0..2`。同一多格 `TimelineAction` 写入全部占格，以同一对象身份去重并只执行一次；移除时清除全部占格和 hover。
- Unity 冻结为稳定 `ActionId`，不复制 Godot 对象引用、`instance_id` 或像素坐标身份。

## 敌方意图

- 活跃来源包括 Village、Animal Husbandry、Altar、Center Altar、Iron Mine 和 Radar；Tower 不生成意图。
- 默认优先级为 0，Center Altar 为 999；同级候选与时间轴位置目前使用隐式全局随机。Unity 必须使用显式 seed，保留“高优先级先放、同级确定性选择、最多 5 个”的算法类别。
- Village 的 `011` 运行时实际占用偏移为 `(1,0),(2,0)`，边界为 `3x1`，不会归一化。Unity 以可执行源为准保留该前导空位。
- 源/目标目前由 Node 引用、逻辑 source coord 和像素 target position 混合表达。Unity 必须冻结 source/target runtime ID 与 `HexCoord`。
- `EffectProcessor._parse_enemy_intent()` 返回空命令。Unity 生成真实 typed intent，但结算必须返回显式 `UnsupportedSourceCommand`，不得伪造 0 伤害成功。
- 地图 hover 与 Timeline hover 当前共享 `EnemyIntentData`；整个 action 容器共同 pulse/stripe、tooltip 和地图范围。Unity 保留这一产品语义，并改由同一 presentation snapshot 驱动。

## Tower、Poison 与死亡

- Tower 初始 HP100、每次建筑阶段自损 50；当回合由 Timeline 创建后进入同一建筑快照，因此立即 `100 -> 50`，下一结束回合 `50 -> 0`。
- Tower 死亡策略为 `Remove`，权威 occupant/map 占用先同步清除，退场表现不能延迟 Domain removal。
- 普通地貌/敌人死亡策略为 `RemainBroken`：保留占位，但不能生成意图或接受正常状态处理。Radar 从属实体的源清理不完整，Unity 归入 `Remove` 并执行原子占用清理。
- Poison 定义为每个入口层数造成 `ceil(MaxHP * 0.1 * stacks)` 伤害、向六邻传播 1、当前层数衰减 1。
- Unity 采用严格全局三 pass：入口快照；先聚合旧来源传播，再仅对旧来源伤害，最后仅对旧来源当前层数减 1。新感染本回合不传播、不受毒伤、不衰减。

## 冻结行动快照

共享表现契约至少包含：`ActionId`、actor、priority、source runtime ID/coord、target runtime ID/coord、card/effect ID、显示载荷、origin、shape、完整 occupied cells、validity、invalid reason 和 resolve state。执行前按稳定身份、坐标、shape 与 effect 重判；失效 action 跳过一次并原子移除全部表现。

## 已消解差异

- Godot 隐式 RNG 不可复现：以 Prompt 的显式 seed 规则修正。
- Radar 的 `includes_self=false` 与实际 self target/range 冲突：以实际 target/range 为准。
- Radar 从属实体 raw free 可能遗留占用：以 typed `Remove` 和原子清理修正。
- Poison 源实现是按实体交错执行：以 Prompt 的三个全局 pass 修正确定性与新感染隔离。

以上均有当前 Prompt 给出的确定性裁决，不需要用户决策。
