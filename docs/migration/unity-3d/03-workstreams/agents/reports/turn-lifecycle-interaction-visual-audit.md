# Turn Lifecycle 交互与视觉审查

> 状态：Gate 0 只读审查完成
> 日期：2026-08-02
> 写入范围：仅本报告；审查智能体未修改文件、未启动 Unity

## 当前差距

- `CardHandView` 将 `125x175`、hover/selected/drag scale `1.10/1.50/1.55`、lift 和动画响应写死在代码；`CardHandHost` 将 slot/spacing 写死为 `145` 与 `102..116`。
- 1280 下选中卡会吞掉相邻卡主体与命中区；2560 下手牌未围绕棋盘中心稳定排布。当前测试没有七卡首/中/末 selected、独立点击净宽、取消复位或宽屏中心断言。
- Unity 没有卡牌效果详情框；`CardViewModel` 缺显示载荷，JSON 的说明文本未进入 typed content。
- `TimelinePresenter.RenderAction()` 只逐格写 label/color，没有 ActionId 容器、连续底板、整体外轮廓、来源图标、结算态或成组 hover。
- enemy intent 是 Composition 硬编码单红格，没有 source identity、详情框、地图双向映射、invalid/unsupported 说明或非颜色编码。
- 当前 committed action 由 Controller 手动渲染，SessionView 不发布 action snapshot；正常 Resolve 后也没有统一 ClearingTimeline 表现事务。

## Godot 产品语义

- 卡牌 hover/selected 具有抬升、缩放、阴影、shader、tooltip；selected/holding 的 parent、z、scale、rotation、输入和材质可完整恢复。
- 每个 Timeline action 有独立容器；同一 shape 使用连续 backplate、仅外边界 outline 和弱内部 seam，相邻同色 action 仍可区分。
- 敌方 action 复用整体形状容器，并叠加 pulse + stripe；地图 hover 与 Timeline 任一占格 hover 共享同一意图数据、范围和 tooltip。

## 冻结交互优先级

1. `Resolving/Disabled`：清当前 tooltip/hover/preview并阻断输入；已提交 frame 保留到 `ClearingTimeline` 按 ActionId 移除。
2. Card targeting：卡详情与当前 target/range 独占地图通道，敌方 overlay 不覆盖。
3. Scheduling/drag/clear：活动 Timeline preview 独占；已提交 frame 保持可辨，hover 不替换活动 preview。
4. Idle：同一时间最多一个 hovered ActionId；切换 identity 前原子清旧 tooltip、地图 overlay 和整组 hover。
5. clear/revalidation/death/rebind：按 ActionId 幂等清 frame、occupied-cell map、tooltip、source/target/range 与订阅。

## Gate B 最小表现面

- 可序列化 card layout/style 与 hand min/max width、spacing、selected reserved extent。
- 保存的 `CardEffectFrame`、`TimelineActionFrame` Prefab；敌方复用 action frame 容器并应用独立 stripe/source badge/state style。
- Scene 在 Play 前保存 EffectFrameHost、IgnoreLayout ActionLayer、tooltip/overlay anchor 和 Presenter 引用。ActionLayer 不可直接成为 Timeline `GridLayoutGroup` 的第 37 个 item。
- `TimelinePresenter` 只维护 `ActionId -> frame` 和 `cell -> ActionId` View 索引；`ActionMapOverlayPresenter` 只消费同一快照，不重算目标或范围。
- 单一 overlay coordinator 拥有优先级与统一清理；Controller 停止手动逐格补渲染。

## 视觉与交互门禁

- 三视口覆盖七卡 idle、首/中/末 hover/selected、targeting、scheduling、drag/cancel、disable/enable；断言每张非选中卡仍有独立 raycast 净宽，hand 围绕棋盘中心，不侵入 Timeline/HUD。
- 详情框显示中文名称、说明/数值、地图范围、Timeline shape、目标或不可用原因，不泄露 stable ID/调试坐标；边缘钳制且不挡当前决策对象。
- 玩家 1x1、多格、相邻同色 action 均一 ActionId 一 frame；任一占格 hover 得到同一详情和地图范围，Clear 整组移除且无残留。
- enemy valid、invalid、unsupported/no-effect 和 resolving 静态/动态可区分；地图 source 与 Timeline 任一格 hover 得到同一 ActionId/source/target/range。
- 四 yaw 验证玩家 target/range 与 enemy source/target/range 的同一 ActionId；结构断言和人工逐图检查同时存在。

硬阻塞：无。Gate A 必须先冻结 Unity-free snapshot，Gate B 再实现表现。
