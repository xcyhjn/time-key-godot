# Wave 02B4 多智能体 Prompt 审查

> 状态：Gate 0 审查通过；允许立即启动 Gate A
> 日期：2026-08-02

## 前置事实

- 当前分支 `unity_7.31`，开始时 `HEAD...origin/unity_7.31 = 0/0`。
- 02B3 Gate D 已完成并推送：EditMode `236/236`、PlayMode `53/53`、build `Succeeded`、Player exit 0 + `TIMEKEY_PLAYER_SMOKE_PASS`。
- 02B3 全部智能体已返回；Gate 0 检查时无 Unity/UnityHub 进程。
- 16 项既有未提交内容纳入保护清单；本阶段不得暂存、覆盖或清理。

## 路径互斥

| Agent | 独占写入 | 依赖 | 启动波次 |
| --- | --- | --- | --- |
| 0 source | 单一 source-semantics 报告 | 无 | Gate 0 |
| A deck | 新 `Domain/Deck/**` + `Tests/EditMode/Deck/**` + 报告 | source 报告 | Gate A |
| B round/outcome | 新 `Domain/BattleFlow/**` + `Tests/EditMode/BattleFlow/**` + 报告 | source 报告 | Gate A |
| C hook | 新 `Application/BattleFlow/**` + 对应 tests/report | A+B API | Gate B |
| D presentation | 新 `Presentation/BattleFlow/**`、新 Prefab/tests/report | Gate B API | Gate C |
| 主智能体 | 所有既有/共享文件、Controller、Composition、Binding、Scene、asmdef、Editor、证据、维护文档与 Git | 每波交回 | 全程 |

五个 Agent 的可写集合两两无交集；共同祖先目录不构成授权。Agent 不得运行共享 Unity 实例，不得切分支、stash、暂存、commit、push 或回退其他工作者内容。

## 契约裁决

- 不修改 ADR 0008 phase 顺序；02B4 只替换 reserved hook 的 no-op 实现。
- Timeline 空位奖励从 hook 输入的冻结 plan 计算，避免 timeline 已清空后误算 36。
- action identity、card-instance identity 和 card stable ID 三者分离。
- Presenter/动画/View 销毁不决定资源、抽弃或终局。
- 局外商店、锻造、删牌、地图跳转仍是 Godot 非目标；Unity 只交付 typed reward/return boundary。

结论：Prompt 字段完整、所有权互斥、依赖顺序明确。Agent 0 已返回，源语义与 Gate 0 总结无硬阻塞；立即并行启动 A/B。
