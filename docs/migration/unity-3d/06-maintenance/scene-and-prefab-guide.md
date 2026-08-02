# Scene 与 Prefab 维护指南

> 状态：Wave 02B4 Gate D 已验证
> 入口场景：`unity/Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`

## 可编辑边界

进入 Play 前，场景中应已存在相机 rig、Main Camera、双灯、地面、`BoardRoot`、`TargetAnchor`、EventSystem、Canvas/HUD、`Timeline`、`CardHandHost`、BattleFlow/Settlement UI、范围预览、普通时间轴预览和 Clear 三态预览组件。稳定对象不得由 `VerticalSliceController.BuildSceneGraph()` 重新创建。

十一个权威 Prefab 位于 `unity/Assets/_Project/Prefabs/Battle/`：`TimelineCell`、`CardView`、草地/裸土 `HexBlock`、`HexColumn`、`TargetView`、`Tower`、`PoisonStatus`、`CardEffectFrame`、`TimelineActionFrame` 和 `BattleFlowPanel`。Prefab 的外观、碰撞体和稳定子层级在 Prefab Mode 修改；不要把实例改动留成未应用 override。Tower billboard 没有 collider；PoisonStatus 使用原图标与整数层数。

只允许动态实例化运行数据决定的对象：19 个 fixture 六边形列及其高度块、当前正式手牌实例、目标/Tower occupant、状态 View、时间轴行动标记和一次性预览。手牌 View 以 `CardInstanceId` 为键，不能按重复 stable ID 合并。地块必须从 Prefab 创建；每层有独立 mesh/renderer/collider，中心间距由 `HexTileColumn.BlockSpacing = 0.32` 约束。

## Inspector 接线

场景根的 `CombatCompositionRoot` 持有 `VerticalSliceController`、`CombatPresentationBinding`、`UnityCombatTraceSink` 和卡牌 `TextAsset` 列表。卡牌列表由 authoring 工具按内容目录排序写入；增加普通卡后可重跑工具或在 Inspector 添加对应 JSON，不能在 Controller 增加 stable-ID 分支。

`CombatPresentationBinding` 显式引用 CardHand、BoardRange、Timeline、CombatHud、CombatOccupant、InteractionOverlay 与 BattleFlow Presenter。BattleFlow 引用 deck/hand/discard、Era/phase/timecoin 文本、Settlement 层、奖励按钮和终局需禁用的输入控件；规则与 outcome 不在 Presenter 中推导。Occupant Presenter 保存 creation→Prefab 表、PoisonStatus Prefab 与 Scene Camera；`TimelinePresenter` 同时引用两个 Preview 和 36 个唯一槽位。`CardHandPresenter` 引用保存的 `CardView` Prefab；`VerticalSliceController` 只保留兼容 facade 与序列化引用。

缺失引用时先检查组件本身和 Prefab GUID，再运行 `CombatSceneAssetTests`。禁止用 `GameObject.Find`、字符串层级路径、singleton 或 Service Locator 补洞。

## 人工编辑流程

1. 在 Unity 打开 `CombatVerticalSlice.unity`，停止 Play 后修改稳定布局或 Prefab。
2. 保存 Scene/Prefab，重新打开场景确认修改持久化。
3. 若确需重建权威资产，运行 Editor 菜单对应的 scene authoring 入口；这是覆盖式维护操作，执行前先审查工作树。
   仅更新 Remaining Cards 资源时优先运行 `Time Key/Author Remaining Cards Gate B Assets`，避免重建无关 Scene fileID。
   仅接入或刷新 Clear UI 时运行 `Time Key/Author Remaining Cards Gate C Clear UI`；它只增加/接线 `ClearTimelinePreview` 并保存 36 格默认外观。
4. 运行 Scene asset EditMode、全量 PlayMode 和 harness；布局或表现变化必须重拍三视口与相关交互截图。

可复制验证命令见 `testing-and-evidence.md`。最小结构测试位于 `unity/Assets/_Project/Tests/EditMode/Composition/CombatSceneAssetTests.cs`，生命周期和集成测试位于 `Tests/PlayMode/`。

当前 Scene/十一个 Prefab 状态已由 02B4 Gate D 的 `300/300 + 61/61`、18 张人工复核 PNG、Windows build 和 actual Player 重新验证。

## 常见故障与回滚

- 进入 Play 后稳定对象复制：检查是否重新引入运行时 `new GameObject`/`AddComponent`，以及 Binding 是否重复订阅。
- 手牌或 Timeline 丢失：检查 Presenter 的 Prefab/槽位引用和 Canvas 锚点，不在 Domain 重算布局。
- 地震后目标浮空或选择失效：检查 `HexTileColumn.TopBounds`、`OccupantAnchor` 和动态 collider 是否随真实 block collection 刷新。
- 卡牌在 1280 宽度遮住右侧 HUD：检查 `CardHandHost` 的底部左侧锚点、67% 宽度与 280 高度约束。
- 牌区计数与实体手牌不一致：检查 Presenter 是否用 `BattleFlow.Hand` 重建 View，并以 `CardInstanceId` 作为 ViewId；不要回退到内容目录的七卡调试 hand。
- 胜利后仍可交互或重复领赏：检查 Settlement snapshot、终局输入控件列表和 reward command sequence，不在按钮回调内直接改 UI 状态。

回滚以单一 Scene/Prefab 检查点为单位，先恢复可验证的序列化引用，再重跑受影响门禁。不得覆盖 Godot 资源、用户脏文件或清理未知目录。

## Wave 02B3 保存资产

权威 Prefab 增加为十个：新增 `CardEffectFrame` 和 `TimelineActionFrame`；Tower 增加 Silver HP TextMesh，Poison 层数切换为 Silver Font/Material。Scene 在 Play 前保存 `EffectFrameHost`、`TimelineActionLayer` 及 Presenter 引用；只允许当前 action/intent frame 从 Prefab 动态实例化。

常规样式调整直接使用 `06-maintenance/combat-interaction-presentation.md`，无需阅读代码。重跑完整 authoring 会产生无语义 YAML/材质/贴图 meta 漂移，执行后必须逐路径审查，只提交与本阶段资产相关的语义差异。

## Wave 02B4 保存资产

新增 `BattleFlowPanel` 后权威 Prefab 总数为十一个。Scene 在 Play 前保存牌区/回合资源 HUD、Settlement 层、奖励按钮、输入锁引用及 `battleFlowPresenter` 接线；动态 hand 只从 `CardView` Prefab 创建。所有新增 uGUI Text 均使用 Silver，世界 TextMesh 仍要求 Font/Material 成对。
