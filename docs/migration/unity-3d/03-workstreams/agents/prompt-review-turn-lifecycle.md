# Turn Lifecycle 多智能体 Prompt 审查

> 状态：Gate 0 审查通过；允许立即启动 Gate A
> 日期：2026-08-02

## 前置与只读门禁

- 分支为 `unity_7.31`；Gate 0 开始时 `HEAD...origin/unity_7.31 = 2/0`。
- 前置 Remaining Cards 与原有子智能体均已完成并交回；三份本阶段只读审查也已返回，无硬阻塞。
- Unity 6000.4.10f1；D 盘可用空间 73.94 GiB；Gate 0 结束时无 Unity/UnityHub/ShaderCompiler 写入进程。
- 本地化已由 `93d6b0d`、`4c25b9a` 形成独立检查点，不重复提交；继承 EditMode `161/161`、PlayMode `38/38`、8 PNG、build `210916374` bytes 与 Player smoke。
- 最小冒烟实跑：Application EditMode `25/25`、公共 Scene PlayMode `15/15`，均 0 失败；结果写系统临时目录，两个受保护 ProjectSettings 哈希前后不变。

## Prompt 完整性

| Prompt | 单一目标 | 必读 | 拥有/禁止 | 冻结契约 | 非目标 | 测试/视觉 | 报告 | 停止 | Git | 结论 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A runner/snapshot | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| B1 intent Domain | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| B2 interaction Presentation | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| C1 building/status/death | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| C2 occupant/status Presentation | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |

## 路径互斥与执行顺序

- A 仅新增 `Domain/Lifecycle/**`、ActionId、Application snapshot/port 与新 tests；所有既有共享文件由主线程独占。
- Gate A 集成并冻结 snapshot 后，B1 只新增 intent Domain/Application，B2 只写 Card/Action/Tooltip Presentation、两个新 Prefab 与独占 PlayMode tests。
- B1 与 B2 无运行时可写交集，但 B2 必须等待 snapshot 字段和 B1 intent result 稳定后启动。
- C1 只新增 building/status/death processor 与纯测试；C2 只写 occupant/status Presenter/Prefab/test，并等待 C1 result 稳定。
- 主线程始终独占共享 Scene、`VerticalSliceController.cs`、`CombatCompositionRoot.cs`、`CombatPresentationBinding.cs`、asmdef、registry 汇总、Editor authoring/harness、共享文档、最终证据和全部 Git 操作。

## 冻结边界审查

- `CombatSessionPhase` 保持卡牌交互状态；新 `TurnLifecyclePhase` 独立存在。
- ActionId 是 card/enemy/timeline/map/details 的唯一跨层身份；同一 display ID 的不同 action 不合并。
- Presentation 只消费 immutable snapshot，不反推规则。
- Clear 保持独立 session；生命周期只接收 clear 后的真实 action 集合，不复活已移除 action。
- MIG-002 以显式 `UnsupportedSourceCommand` 关闭，不伪造伤害。
- 02B4 hook 在 status 与 intent refresh 之间明确 no-op，不实现 deck/turn coin/win/loss。

结论：五份 Prompt 字段完整、路径互斥、依赖顺序明确，Scene/Composition 等共享边界由主线程独占。Gate A 可以立即启动，不能只交付计划。
