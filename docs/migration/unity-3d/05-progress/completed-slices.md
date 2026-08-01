# 已完成切片

> 状态：Wave 00、Slice 01、Wave 02A、Wave 02B1、Wave 02B2A 与解耦 R1 已完成
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：Slice Definition of Done

## Wave 00：迁移评估基线

完成日期：2026-07-31。

- 确认 `unity_7.31`、Godot 4.6.2 与 Unity 6000.4.10f1。
- 运行 Godot headless/import 与图形流程，保存日志和 8 组关键截图。
- 完成可行性 4/5 评估、依赖/数据/风险盘点和 3D 边界。
- 冻结首切片契约、路线图、ownership 与 agent prompts。

这是一项迁移门禁成果，不计作 Unity 功能切片。Slice 01 只有在测试、batchmode、build、视觉证据和 parity 全部通过后才会登记到这里。

## Wave 01：战斗垂直切片 01

完成日期：2026-07-31。

- Unity 6000.4.10f1、URP 17.4.0、uGUI 2.0.0、Test Framework 1.6.0 工程可导入和构建。
- 真实 `lighting.json` 哈希与 Godot 源文件一致，适配到纯 C# 卡牌/时间轴/结算领域模型。
- 固定 seed 731 的 3D flat-top axial 白盒场景完成选卡→选目标→放置→解析；目标从 10 HP 变为 0，敌方意图按冻结顺序记录。
- Unity EditMode 19/19、PlayMode 2/2、Windows Player 运行时冒烟全部通过。
- 初始/解析后 1920×1080 及解析后 1280×720 截图通过像素门禁与人工布局审查。
- 回合推进、胜负奖励、局外闭环、完整卡牌效果和授权素材仍按契约留给后续波次。

## Wave 02A：局内 3D 战斗棋盘

完成日期：2026-07-31。

- 19 格 flat-top 棋盘使用透视轨道镜头，可 360° 旋转、俯仰、缩放和平移。
- UI 覆盖区域阻止世界选择；同一目标在 0/90/180/270 度均可 raycast 选中。
- elevation 改为每层 `0.32` 的独立 mesh/collider 实体堆叠，两处高地均为真实两层。
- Blender 草地/裸土模型各 120 三角形、0 非流形边，可复现源文件、FBX 和三张模型证据已落盘。
- 原 `center_altar.png` 作为完整面向相机的世界 billboard，保留原项目视觉语言。
- Unity EditMode 19/19、PlayMode 4/4、四向 1920×1080、1280×720、Windows build 与 Player 冒烟全部通过。

## Wave 02B1：原版 lighting 卡牌交互

完成日期：2026-07-31。

- 原 `lighting.png` 与 `behide.png` 逐字节接入；卡面保持 `1135×1590` 原尺寸与底部 `125×175` 交互基准。
- 手牌完成 idle、hover、selected/targeting/scheduling、右键/Escape 取消与输入隔离；选牌期间轨道镜头暂停，取消或提交后恢复。
- `TimelineGrid.CanPlace` 与 `CardPlaySession` 提供无副作用的目标/时间轴预览，失败、取消与重复 commit 不改变占格。
- `effect_range` 直接投影到 3D 棋盘实体；时间轴 valid/invalid 颜色仅消费 Domain 合法性结果，确认后才写入 `LIGHTING`。
- 原有结算闭环保持 seed 731、10 HP -> 0、敌方意图后处理；局外 Godot 流程未修改。
- Unity EditMode 31/31、PlayMode 15/15、12 张集成截图、Windows build 与 Player 冒烟全部通过。

## Wave 02B2A：七卡 Schema 与 earthquake 垂直切片

完成日期：2026-08-01。

- 七张真实 JSON 无损进入 typed effect schema，保留 stable/numeric ID、`front_image`、range、普通 shape 与 clear mask；错误 token 类型显式失败。
- 七张 `1135×1590` 原卡面与 Godot 源哈希一致；两卡手牌实际用 `FrontImage` 加载 lighting 与 earthquake，并保持单选互斥、取消和拖拽事件。
- `earthquake` 使用保存的地图坐标和两格时间轴 shape，结算中心加六邻格；边缘缺失坐标不创建幽灵 tile。
- 七个有效柱各新增两个独立 FBX mesh/renderer/collider block，层距严格 `0.32`；顶面与 occupant anchor 同步上移 `0.64`，四向仍能选择抬高后的同一格。
- Unity EditMode `67/67`、PlayMode `25/25`、11 张集成截图、Windows build 和 Player smoke 全部通过。

## 解耦 R1：可编辑 Scene 与 Prefab

完成日期：2026-08-01。

- 稳定 Camera、灯光、地面、BoardRoot、TargetAnchor、EventSystem、Canvas/HUD、36 格 Timeline、CardHandHost 与 Preview 已序列化到 Scene。
- TimelineCell、CardView、两种 HexBlock、HexColumn 和 TargetView 六个 Prefab 已保存并由 Inspector 引用。
- `BuildSceneGraph()` 仅保留验证/初始化兼容面；Controller 不再运行时创建稳定节点。
- 对称事件绑定覆盖重复初始化和 disable/enable；动态棋盘、目标与 intent 不重复。
- Unity EditMode `70/70`、PlayMode `26/26`、11 张刷新截图、Windows build 和 Player smoke 全部通过。
