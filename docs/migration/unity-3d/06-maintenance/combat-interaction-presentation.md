# 战斗交互表现维护指南

> 状态：Wave 02B4 Gate D 已验证
> 字体：Silver

## 设计者可直接调整的位置

- 卡牌尺寸与基础层级：`Prefabs/Battle/Cards/CardView.prefab`。
- hover/selected/drag 的 lift 与 scale：CardView 上的 `CardHandView` Inspector；当前为 `24/48`、`1.08/1.20/1.26`。
- 正式手牌间距与响应式区域：Scene 中 `Canvas/CardHandHost` 及其 `HorizontalLayoutGroup`/`CardHandHost` 参数。当前由 BattleFlow hand snapshot 动态显示 5 张；必须同时检查 1280x720、1920x1080、2560x1080。
- 卡牌/行动详情框：`Prefabs/Battle/UI/CardEffectFrame.prefab` 的背景、标题、描述和元数据文字。
- 玩家/敌人行动框：`Prefabs/Battle/UI/TimelineActionFrame.prefab`。玩家使用 teal；敌人必须保留 amber stripe、来源徽标或边框纹理，不能只改成另一种纯色。
- 锚点与边界：Scene 中保存的 `EffectFrameHost`、`TimelineActionLayer` 和右侧 HUD。详情框由 presenter 钳制到屏幕内，设计者不需要修改代码。
- 牌区/资源与结算：`Prefabs/Battle/BattleFlow/BattleFlowPanel.prefab`。只调布局、颜色和 Silver 文本；牌数、时间币、outcome、奖励和输入锁均来自 snapshot。

所有 `Text`/`TextMesh` 保持 Silver。修改 world-space `TextMesh` 时，Font 和 `MeshRenderer.sharedMaterial` 必须一起指向 Silver，否则字形会不可见。

## 保存对象与动态对象

进入 Play 前 Scene 已保存 `EffectFrameHost`、`TimelineActionLayer`、36 个 Timeline cell 和 Presenter 引用。每个当前 action/intent 对应的 frame 可以动态创建，但只能从保存的 `TimelineActionFrame.prefab` 实例化；详情框复用保存的 `CardEffectFrame.prefab`。禁止在运行时用 `new GameObject`/`AddComponent` 拼整棵 UI。

动态 frame 的身份来自 Application snapshot。不要用卡名、stable card ID、颜色或格子文本判断它属于哪个 action；相邻同名 action 也必须有不同 identity 和完整外轮廓。

## 交互验收

1. 1280x720 下当前 5 张正式手牌均能点击，选中中间牌后相邻关键文字与有效点击区仍可见。
2. 选牌、目标选择、排程、拖拽、结算中状态互斥；取消、失败、提交、disable/enable 后 parent、sibling、scale、rotation 和 raycast 恢复。
3. hover 玩家 frame 或地图目标得到同一详情和 range；enemy frame/map source 也得到同一 intent snapshot。
4. target 选择时 enemy overlay 不抢高亮；source 死亡、Clear、phase 切换后 frame、tooltip、source/target/range 一起消失。
5. 四个 yaw 下 Tower HP 和 Poison 层数朝向相机且不与美术重叠。
6. 三视口下牌区计数与实体手牌一致；Victory 只显示一次奖励入口且锁住底层输入，领取后按钮消失；Defeat 不显示奖励。

结构测试在 `CombatSceneAssetTests`，表现测试在 `Tests/PlayMode/Actions`、`Tooltips`、`Cards`、`Occupants` 与 `BattleFlow`。最终截图和人工结论位于 `04-verification/evidence/deck-battle-flow-gate-d/`。

## 效果框与交互稳定化

- 新 action frame 样式在 `TimelineActionFrame.prefab` 的隐藏 `CellBackgroundTemplate`/`EdgeTemplate` 上调整；必须同步 `VerticalSliceSceneAuthoring.CreateTimelineActionFramePrefab()`，根 background/outline 保持不渲染且全部 graphic `raycastTarget=false`。
- 新卡牌映射必须把提交时的 `CardInstanceId` 保存到 immutable action snapshot；不要用 stable ID 匹配重复实体卡。地图 hover source/target 使用 `ActionIdentityIndex.FindByMapRuntimeId()` 反查 action IDs。
- 新交互模式先在 `CombatOverlayOwner` 选择准确优先级，再在 Binding Refresh 建立/释放 owner；不得由多个 Presenter 直接竞争详情框。
- `IdleTileInspectPort` 只保存逻辑坐标。Controller 负责 BoardTileView 高亮、同屏同格 toggle、blank/Escape/短右键、右拖仲裁和所有 command/rebind/lock 清理。
- 新增 action shape 后同时添加缺格不填充测试、三视口 Scene 截图和同实例 resize 测试；不要只断言根 bounding rect。
