# Scene 与 Prefab 维护指南

## 权威资产

- Scene：`unity/Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`
- UI Prefab：`Prefabs/Battle/UI/TimelineCell.prefab`
- 卡牌 Prefab：`Prefabs/Battle/Cards/CardView.prefab`
- 地形 Prefab：`Prefabs/Battle/Terrain/HexBlockGrass.prefab`、`HexBlockDirt.prefab`、`HexColumn.prefab`
- 目标 Prefab：`Prefabs/Battle/Targets/TargetView.prefab`

## 稳定与动态边界

Play 前必须存在：Camera/rig、Environment/双灯、BattlefieldGround、CombatBoardRoot、TargetAnchor、EventSystem、Canvas/HUD、Header、36 格 Timeline、CardHandHost、DetailPanel 和两个 Preview。

运行时允许变化：19 个 fixture 棋盘柱与各层 block、两张手牌实例、目标实例、Timeline action 状态和后续敌人/VFX。动态对象必须来自保存 Prefab 或职责明确的组件工厂。

## 编辑流程

1. 直接打开 Scene 或对应 Prefab 修改布局、灯光、材质与序列化字段。
2. 不在 `VerticalSliceController` 中新增稳定对象构造，也不使用 `GameObject.Find` 补引用。
3. 若层级损坏，可在干净 Unity 写入窗口执行 `Time Key > Author Editable Combat Scene`；该命令会重建本垂直切片 Scene，因此执行前先确认没有需要保留的未保存 Scene 编辑。
4. 修改 Prefab/Scene 后先跑 `CombatSceneAssetTests`，再跑完整 EditMode/PlayMode 和视觉 harness。

## Inspector 门禁

Controller 的 Content、Stable Scene References 与 Dynamic Prefabs 三组字段必须全部非空；`timelineCells` 必须恰好 36 个且坐标唯一。`HexColumn` 必须包含 `HexTileColumn`、`BoardTileView` 和 `OccupantAnchor`；每种 HexBlock 必须包含 mesh、renderer、collider。

## earthquake 不变量

任何 Scene/Prefab 修改都必须保持每层间距 `0.32`。earthquake 结算后七个有效柱各新增两块，顶面与 `TargetAnchor` 上移 `0.64`，并在 yaw 0/90/180/270 仍能选择抬高后的中心格。
