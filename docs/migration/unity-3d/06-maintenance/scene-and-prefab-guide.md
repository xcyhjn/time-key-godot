# Scene 与 Prefab 维护指南

> 状态：Remaining Cards Gate D 已验证
> 入口场景：`unity/Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`

## 可编辑边界

进入 Play 前，场景中应已存在相机 rig、Main Camera、双灯、地面、`BoardRoot`、`TargetAnchor`、EventSystem、Canvas/HUD、`Timeline`、`CardHandHost`、范围预览、普通时间轴预览和 Clear 三态预览组件。稳定对象不得由 `VerticalSliceController.BuildSceneGraph()` 重新创建。

八个权威 Prefab 位于 `unity/Assets/_Project/Prefabs/Battle/`：`TimelineCell`、`CardView`、草地/裸土 `HexBlock`、`HexColumn`、`TargetView`、`Tower` 和 `PoisonStatus`。Prefab 的外观、碰撞体和稳定子层级在 Prefab Mode 修改；不要把实例改动留成未应用 override。Tower billboard 没有 collider；PoisonStatus 使用原图标与整数层数。

只允许动态实例化运行数据决定的对象：19 个 fixture 六边形列及其高度块、七张手牌实例、目标/Tower occupant、状态 View、时间轴行动标记和一次性预览。地块必须从 Prefab 创建；每层有独立 mesh/renderer/collider，中心间距由 `HexTileColumn.BlockSpacing = 0.32` 约束。

## Inspector 接线

场景根的 `CombatCompositionRoot` 持有 `VerticalSliceController`、`CombatPresentationBinding`、`UnityCombatTraceSink` 和卡牌 `TextAsset` 列表。卡牌列表由 authoring 工具按内容目录排序写入；增加普通卡后可重跑工具或在 Inspector 添加对应 JSON，不能在 Controller 增加 stable-ID 分支。

`CombatPresentationBinding` 显式引用 `CardHandPresenter`、`BoardRangePresenter`、`TimelinePresenter`、`CombatHudPresenter` 和 `CombatOccupantPresenter`。Occupant Presenter 保存 creation→Prefab 表、PoisonStatus Prefab 与 Scene Camera；`TimelinePresenter` 同时引用 `TimelinePlacementPreview`、`ClearTimelinePreview` 和 36 个唯一槽位。每个 `TimelineCellView` 保存设计者默认文字/颜色，Clear/Cancel 后据此恢复；`CardHandPresenter` 引用保存的 `CardView` Prefab；`VerticalSliceController` 保留棋盘/目标 Prefab、相机和兼容调用面所需的序列化引用。

缺失引用时先检查组件本身和 Prefab GUID，再运行 `CombatSceneAssetTests`。禁止用 `GameObject.Find`、字符串层级路径、singleton 或 Service Locator 补洞。

## 人工编辑流程

1. 在 Unity 打开 `CombatVerticalSlice.unity`，停止 Play 后修改稳定布局或 Prefab。
2. 保存 Scene/Prefab，重新打开场景确认修改持久化。
3. 若确需重建权威资产，运行 Editor 菜单对应的 scene authoring 入口；这是覆盖式维护操作，执行前先审查工作树。
   仅更新 Remaining Cards 资源时优先运行 `Time Key/Author Remaining Cards Gate B Assets`，避免重建无关 Scene fileID。
   仅接入或刷新 Clear UI 时运行 `Time Key/Author Remaining Cards Gate C Clear UI`；它只增加/接线 `ClearTimelinePreview` 并保存 36 格默认外观。
4. 运行 Scene asset EditMode、全量 PlayMode 和 harness；布局或表现变化必须重拍三视口与相关交互截图。

可复制验证命令见 `testing-and-evidence.md`。最小结构测试位于 `unity/Assets/_Project/Tests/EditMode/Composition/CombatSceneAssetTests.cs`，生命周期和集成测试位于 `Tests/PlayMode/`。

Gate D 已在保存 Scene/八个 Prefab 的最终态通过 full EditMode `152/152`、full PlayMode `38/38`、harness/build/Player，并逐图检查三视口和 Tower/Poison 四 yaw；证据见 `04-verification/evidence/remaining-cards-gate-d/`。

## 常见故障与回滚

- 进入 Play 后稳定对象复制：检查是否重新引入运行时 `new GameObject`/`AddComponent`，以及 Binding 是否重复订阅。
- 手牌或 Timeline 丢失：检查 Presenter 的 Prefab/槽位引用和 Canvas 锚点，不在 Domain 重算布局。
- 地震后目标浮空或选择失效：检查 `HexTileColumn.TopBounds`、`OccupantAnchor` 和动态 collider 是否随真实 block collection 刷新。
- 卡牌在 1280 宽度遮住右侧 HUD：检查 `CardHandHost` 的底部左侧锚点、67% 宽度与 280 高度约束。

回滚以单一 Scene/Prefab 检查点为单位，先恢复可验证的序列化引用，再重跑受影响门禁。不得覆盖 Godot 资源、用户脏文件或清理未知目录。
