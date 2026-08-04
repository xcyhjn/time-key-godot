# Wave 04 Agent Prompt Review

> 状态：通过
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：`integration-contracts-wave-04.md` 与三个 Wave 04 Prompt 的路径/依赖/验收静态审查

## 结论

三个 Prompt 的单一目标、只读输入、写入 allowlist、禁止范围、测试/证据、停止条件和 Git 规则齐全；
写入路径互不重叠，且没有把正式 Scene/Prefab、Settings、Composition、asmdef、Package/ProjectSettings、
共享文档、最终 evidence 或 Git 所有权泄漏给子 Agent。审查结论为 `PASS`。

| Agent | 独占写入 | 与其他 Agent 重叠 | 共享依赖方向 | 结论 |
| --- | --- | --- | --- | --- |
| Audio | `Presentation/Audio/**`、Audio tests、`Audio/Wave04/**`、`Prefabs/Audio/**`、独立报告 | 无 | 只向主智能体暴露 Audio API；不持有 SceneFlow phase/input lease | PASS |
| Feedback | `Presentation/Feedback/**`、`Prefabs/Feedback/**`、`Materials/Feedback/**`、Feedback tests、独立报告 | 无 | 只消费既有 Application result/trace/snapshot；不反写 Domain | PASS |
| Tutorial audit | 单一 `reports/wave-04-tutorial-audit.md` | 无 | 只读 Godot/Unity；输出 typed step 审计，不写运行时 | PASS |
| 主智能体 | 正式 Scene/Prefab、Bootstrap、Settings、Composition、共享 asmdef/Packages/ProjectSettings、账本、最终证据、Git | 不授权给子 Agent | 唯一集成者和验收者 | PASS |

## 并发规则

- 三个 Agent 可同时做互斥路径工作；主智能体保留集成槽。
- 子 Agent 不得运行并发 Unity。需要 Unity XML/PNG 时先报告，主智能体串行运行；纯代码/只读审计可并行。
- 子 Agent 不切分支、不 stash、不回退、不暂存、不 commit、不 push；发现共享契约冲突停止写共享路径。
- Agent 回报后先由主智能体查看 `git diff -- <owned paths>`、测试和报告，再允许改正式 Scene/Prefab。

## 停止条件复核

当前没有分支、写入锁、Wave 03 证据、明确禁止本地用途的许可证或 Unity 许可证硬阻塞。未知公开发布
授权只限制发布声明；MIG-002/MIG-003 继续 no-effect/safe-skip。可以进入 Gate A/B/D 候选实现。
