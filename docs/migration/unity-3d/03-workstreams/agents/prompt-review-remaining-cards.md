# Remaining Cards 写入 Agent Prompt 审查

> 状态：Gate 0 审查通过；按 A -> B1/B2 -> C1 -> C2 串并行门禁执行

## 前置证据

- 前置所有权已交回；`HEAD=origin/unity_7.31=da00914`。
- Gate 0 最小冒烟：EditMode `24/24`、PlayMode `10/10`，0 失败。
- 三份只读报告均 `git diff --check` 通过，无硬阻塞。

## Prompt 完整性

| Prompt | 单一目标 | 必读 | 拥有/禁止 | 冻结契约 | 非目标 | 测试 | 报告 | 停止 | Git | 结论 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Agent A Recover | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| Agent B1 Built | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| Agent B2 Poison | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| Agent C1 Clear Domain | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |
| Agent C2 Clear Presentation | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | 是 | PASS |

## 路径互斥与依赖

- A 独占共享 occupant/result、TimelineGrid、Application；完成并由主智能体测试/收回后才开放 B。
- B1 与 B2 只新增各自 handler/test/report，路径完全互斥，可并行；共享 registry/Application/Scene 由主智能体串行集成。
- C1 在 B 集成后独占 TimelineGrid/Application；完成冻结后再启动 C2。
- C2 只拥有 Timeline Presenter/preview/cell 与其测试；Controller/Binding/Scene 仍由主智能体独占。
- 所有 `.meta` 随资产/脚本审查；任何 Agent 都不得修改 `VerticalSliceController.cs`、Scene、Composition、Editor harness、共享文档或 Git。

## 主智能体独占集成面

`VerticalSliceController.cs`、`CombatPresentationBinding.cs`、`CombatCompositionRoot.cs`、Infrastructure registry/parity tests、Scene、Tower/Poison Prefab 与源资产、Editor authoring/harness、asmdef、共享 tests/docs/evidence/Git。Gate A 额外断言 Controller diff 为空。

结论：Prompt 字段完整、所有权无交叉、依赖顺序明确；可以立即启动 Agent A，不能只交付计划。
