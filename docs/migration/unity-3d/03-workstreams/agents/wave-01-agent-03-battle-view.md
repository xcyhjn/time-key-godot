# Wave 01 Agent 03：3D 战斗表现与交互

> 状态：已审查，未启动；所有权已交回主智能体
> 负责人：Agent 03
> 最后验证日期：2026-07-31
> 证据来源：3D 产品边界、Godot 实跑截图、集成契约

## 角色与单一目标

实现固定 seed 的 3D 六边形白盒战场和 uGUI 时间轴，连通选卡→选世界目标→放置→结算→目标视觉更新。你不是仓库里唯一的工作者；不得回退或覆盖他人改动。

## 必读

- `docs/migration/unity-3d/01-assessment/godot-baseline-evidence.md`
- `docs/migration/unity-3d/01-assessment/visual-3d-strategy.md`
- `docs/migration/unity-3d/02-architecture/godot-to-unity-mapping.md`
- `docs/migration/unity-3d/02-architecture/harness-design.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`

可用 `codebase-migrate` 保持行为等价，使用 Unity 自带场景/UI/测试 API 完成表现；不使用 Godot 实现 skill，因为产物是 Unity C#。

## 独占所有权

- `unity/Assets/_Project/Runtime/Presentation/**`
- `unity/Assets/_Project/Scenes/VerticalSlice/**`
- `unity/Assets/_Project/Tests/PlayMode/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-03-battle-view.md`

禁止修改 Domain、Infrastructure、Editor harness、Packages、ProjectSettings、共享文档和 Godot 文件。

## 契约、步骤与停止条件

1. 等待 Domain 与 adapter 可编译。
2. 以 flat-top axial XZ 公式生成至少 7 个真实六边形 mesh，Y 表示高度；正交斜视相机。
3. uGUI Canvas 显示卡牌、12×3 时间轴、目标 HP、敌人意图和阶段。
4. Presentation 只调用 Domain 规则，不重复计算伤害/顺序。
5. 按 `integration-contracts.md` 实现冻结的 `VerticalSliceController` 公共方法和可测试属性；`BuildSceneGraph()` 必须幂等，供运行时与主智能体 Editor harness 共用。
6. PlayMode 测试用公共方法驱动完整路径并断言 10→0、意图顺序与 UI/3D 状态。
7. 写独占报告。

非目标：最终美术、动画打磨、局外地图、教程、音频、完整敌方 AI。若接口冲突或必须写非拥有路径，停止并报告。

## 验收与 Git

主智能体运行 PlayMode、batchmode scene/build 和 1920×1080、1280×720 截图；目标 60 FPS 只作白盒观察值，不作为当前硬门禁。画面不得空白，主体需入镜，主要文本/时间轴不得裁切或重叠。

不得切分支、合并、push、commit、暂存或回退他人改动。报告格式：改动、截图/测试证据、失败项、风险、建议下一步。
