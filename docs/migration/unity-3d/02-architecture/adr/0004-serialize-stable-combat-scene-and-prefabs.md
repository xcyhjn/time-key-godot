# ADR 0004：序列化稳定战斗场景并保存动态 Prefab

> 状态：Accepted
> 日期：2026-08-01

## 背景

Wave 02B2A 的 `CombatVerticalSlice` 只保存 Controller 与两个 fixture；Camera、灯光、地面、EventSystem、Canvas、HUD、36 格 Timeline、CardHandHost 和 Preview 全由 `Awake -> BuildSceneGraph()` 创建。设计者无法在 Play 前检查或编辑实际层级，重复初始化与事件监听也只能依赖临时根节点早退。

## 决策

- Camera/rig、双灯、地面、BoardRoot、TargetAnchor、EventSystem、Canvas、HUD、Timeline 及 36 格、CardHandHost、BoardRangePreview、TimelinePlacementPreview 全部保存到 Scene。
- `TimelineCell`、`CardView`、草/土 `HexBlock`、`HexColumn`、`TargetView` 保存为六个 Prefab。
- Controller 只通过 `[SerializeField]` 接收稳定引用和动态 Prefab；缺失引用在初始化前显式失败。
- `BuildSceneGraph()` 保留为兼容 facade，但只验证引用并初始化动态战斗内容，不创建稳定对象。
- Timeline 与手牌事件采用对称 bind/unbind；重复调用、disable/enable 不复制监听、棋盘、目标或敌方 intent。
- Scene authoring 工具保留，作为可重复生成/修复垂直切片资产的 Editor 命令；运行时不会覆盖人工编辑。

## 后果

设计者可在 Play 前直接打开完整 Scene 和 Prefab，Inspector 引用可审计，布局修改会持久化。动态棋盘柱、块、卡牌实例与目标仍按 fixture 生成，但来源是保存的 Prefab 或明确工厂。R2/R3 必须继续保持 Controller 不重新获得稳定对象构造职责。
