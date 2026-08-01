# 战斗交互表现维护指南

> 状态：Wave 02B3 Gate D 已验证
> 字体：Silver

## 设计者可直接调整的位置

- 卡牌尺寸与基础层级：`Prefabs/Battle/Cards/CardView.prefab`。
- hover/selected/drag 的 lift 与 scale：CardView 上的 `CardHandView` Inspector；当前为 `24/48`、`1.08/1.20/1.26`。
- 七卡间距与响应式区域：Scene 中 `Canvas/CardHandHost` 及其 `HorizontalLayoutGroup`/`CardHandHost` 参数。必须同时检查 1280x720、1920x1080、2560x1080。
- 卡牌/行动详情框：`Prefabs/Battle/UI/CardEffectFrame.prefab` 的背景、标题、描述和元数据文字。
- 玩家/敌人行动框：`Prefabs/Battle/UI/TimelineActionFrame.prefab`。玩家使用 teal；敌人必须保留 amber stripe、来源徽标或边框纹理，不能只改成另一种纯色。
- 锚点与边界：Scene 中保存的 `EffectFrameHost`、`TimelineActionLayer` 和右侧 HUD。详情框由 presenter 钳制到屏幕内，设计者不需要修改代码。

所有 `Text`/`TextMesh` 保持 Silver。修改 world-space `TextMesh` 时，Font 和 `MeshRenderer.sharedMaterial` 必须一起指向 Silver，否则字形会不可见。

## 保存对象与动态对象

进入 Play 前 Scene 已保存 `EffectFrameHost`、`TimelineActionLayer`、36 个 Timeline cell 和 Presenter 引用。每个当前 action/intent 对应的 frame 可以动态创建，但只能从保存的 `TimelineActionFrame.prefab` 实例化；详情框复用保存的 `CardEffectFrame.prefab`。禁止在运行时用 `new GameObject`/`AddComponent` 拼整棵 UI。

动态 frame 的身份来自 Application snapshot。不要用卡名、stable card ID、颜色或格子文本判断它属于哪个 action；相邻同名 action 也必须有不同 identity 和完整外轮廓。

## 交互验收

1. 1280x720 下七张牌均能点击，选中中间牌后相邻关键文字与有效点击区仍可见。
2. 选牌、目标选择、排程、拖拽、结算中状态互斥；取消、失败、提交、disable/enable 后 parent、sibling、scale、rotation 和 raycast 恢复。
3. hover 玩家 frame 或地图目标得到同一详情和 range；enemy frame/map source 也得到同一 intent snapshot。
4. target 选择时 enemy overlay 不抢高亮；source 死亡、Clear、phase 切换后 frame、tooltip、source/target/range 一起消失。
5. 四个 yaw 下 Tower HP 和 Poison 层数朝向相机且不与美术重叠。

结构测试在 `CombatSceneAssetTests`，表现测试在 `Tests/PlayMode/Actions`、`Tooltips`、`Cards`、`Occupants`。最终截图和人工结论位于 `04-verification/evidence/turn-lifecycle-gate-b/` 与 `turn-lifecycle-gate-d/`。
