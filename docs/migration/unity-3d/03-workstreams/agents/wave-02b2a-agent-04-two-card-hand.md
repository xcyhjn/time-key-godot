# Wave 02B2A Agent 04：两卡手牌协调器

> 状态：已审查；仅在 Gate B 后启动
> 负责人：Wave 02B2A Agent 04
> 最后验证日期：2026-08-01

## 角色与单一目标

复用现有 `CardHandView`，增加底部多卡 host/coordinator，使真实 `lighting` 与 `earthquake` 卡面可稳定排列、互斥选择、取消和拖放。只输出稳定 ID 和交互意图，不解析效果。你不是仓库唯一工作者，不得回退他人改动。

## 启动前置、必读与能力

主智能体必须确认 Agent 01/02/03 已交回，schema、两张卡资源路径和 earthquake 交互流程已冻结。

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `scene/card/custom_card.gd`
- `addons/card-framework/hand.gd`
- `unity/Assets/_Project/Runtime/Presentation/Cards/CardHandView.cs`
- `unity/Assets/_Project/Tests/PlayMode/Cards/CardHandViewTests.cs`
- Agent 01/02 报告

使用 `codebase-migrate` 保持原手牌状态；使用 `Verification & Quality Assurance` 做三视口实际渲染检查。禁止重新设计原卡面。

## 独占写入范围

- `unity/Assets/_Project/Runtime/Presentation/Cards/**`
- `unity/Assets/_Project/Prefabs/Battle/Cards/**`
- `unity/Assets/_Project/Tests/PlayMode/Cards/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b2a-agent-04-two-card-hand.md`

禁止修改 Controller、Targeting、Effects/Terrain、Scenes、Editor、Domain、Infrastructure、Resources 卡图、asmdef、ProjectSettings、共享文档和其他 Agent 路径。

## 冻结输入输出契约

- 新 host 接收有序 `CardViewModel` 集合并为每张创建独立 `CardHandView`。
- 同时最多一张 selected；选择第二张会明确取消第一张并各自恢复 pose。
- 对外事件仍以 stable ID 表达：selected/cancel/drag；不持有 CardDefinition、不判断 target/range/CanPlace。
- 继续保留单卡 `CardHandView.Build` 公共行为和旧测试。
- 设计基准为 125x175；两卡采用轻微扇形/重叠但完整卡面可辨，布局用容器和约束，不写死 1920 坐标。
- UI 必须消费 pointer；Selected/Targeting/Scheduling 的镜头锁仍由主 Controller 接线。

## 实现与验收

1. 先写 PlayMode：两 ID 映射、互斥选择、右键取消、drag 事件归属、重复 Build 幂等、pointer gate。
2. 复用单卡组件，新增最小 host/coordinator，不把单卡类改成混合列表管理器。
3. 生成 1920x1080、1280x720、2560x1080 的 idle/lighting-selected/earthquake-selected 证据。
4. 实际检查卡面比例、底边裁切、相互遮挡、选中层级、时间轴/棋盘遮挡和状态切换后的布局漂移。
5. 运行 Cards PlayMode 子集和 `git diff --check`；写独占报告。

不实现完整七卡扇形、tooltip、抽弃牌、时间币、效果规则、世界目标或 timeline commit。若资源路径或 schema 未冻结，停止写共享接线并报告。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash。回报：组件/API、事件次数、测试计数、截图、视觉差异、失败项和 Controller 接线示例。
