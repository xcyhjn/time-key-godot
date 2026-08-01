# Git 提交与推送状态

> 状态：Wave 02B3 实现与交付文档均已推送
> 负责人：主智能体
> 最后验证日期：2026-08-02
> 证据来源：`git status`、`git log`、后续 push 输出

| 检查点 | 本地提交 | 推送 | 范围 |
| --- | --- | --- | --- |
| 评估门禁 | `4c38131` | 已成功推送至 `origin/unity_7.31` | preflight、assessment、architecture、workstreams、Godot evidence |
| Slice 01 | `e3d196a` | 已成功推送至 `origin/unity_7.31` | Unity 工程、代码、测试、Unity evidence、状态账本 |
| Wave 02B1 | `5c6492a` | 2026-08-01 重试成功，已推送至 `origin/unity_7.31` | 原卡面/牌背、CardPlaySession、手牌/目标预览、Controller 接线、测试、截图与迁移账本；四个用户脏文件排除 |
| Wave 02B1 push 状态 | `c5ec5bc` | 2026-08-01 与上项一起推送成功；保留历史记录，不 amend/rebase | 记录先前三次 443 失败；远端现已同步 |
| Wave 02B2A 交接 | `7a2ed96` | 2026-08-01 已推送至 `origin/unity_7.31` | 下一阶段总 Prompt、五份 Agent Prompt、互斥审查、共享契约、测试计划和状态账本 |
| Wave 02B2A typed cards + earthquake | `cdb09ab` | 2026-08-01 已推送至 `origin/unity_7.31` | 七卡 schema/原卡面、两卡 hand、earthquake Domain 与真实高度表现、测试、截图、build、Player smoke、02B2B Prompt；四个用户脏文件与两个来源不明 Prompt 排除 |
| 解耦 R1 Scene/Prefab | `8c8d5b1` | 2026-08-01 已推送至 `origin/unity_7.31` | 序列化稳定层级、六个 Prefab、对称绑定、Scene authoring、测试/视觉/build/Player 证据 |
| 解耦 R2 Application/Diagnostics | `f11fb77` | 2026-08-01 已推送至 `origin/unity_7.31`，推送后 `0/0` | Application session、typed target、handler fail-fast、trace sink、Controller facade、EditMode `86/86`、PlayMode `26/26`、11 张截图、build 与 Player smoke；保护清单全部排除 |
| 解耦 R3 Composition/Presentation/Content | `0b02791` | 2026-08-01 在多次 443 reset/timeout 后重试成功，已推送至 `origin/unity_7.31` | Composition root、四 Presenter + Binding、七卡 catalog/front_image、effect registry、结构化 trace、Scene 接线、EditMode `92/92`、PlayMode `31/31`、14 张截图、build 与 Player smoke |
| 解耦 R3 维护与验收文档 | `7a72414` | 2026-08-01 与上项一起推送成功 | 架构/ADR、集成契约、继承账本、parity/test/command、进度账本、九份维护指南和 R3 人工验证总结 |
| Remaining Cards Gate 0 | `83d0e1d` | 2026-08-02 已推送至 `origin/unity_7.31` | 冻结五卡语义、所有权、保护清单与自动化矩阵 |
| Remaining Cards Gate A | `c8501ef` | 2026-08-02 已推送至 `origin/unity_7.31` | Recover 公共 handler/session、全量 `107/107`、Scene PlayMode `1/1` 与 5 张 PNG |
| Remaining Cards Gate B | `3ad237d` | 2026-08-02 已推送至 `origin/unity_7.31` | Built/Poison、occupant state/result、Tower/Poison Prefab、全量 `130/130`、PlayMode `3/3` 与 8 张 PNG |
| Remaining Cards Gate C | `4fdcbb0` | 2026-08-02 已推送至 `origin/unity_7.31`；推送后 `0/0` | 独立 Clear session、Wind/Tornado、三态 UI、全量 `152/152`、PlayMode `4/4` 与 9 张 PNG |
| Remaining Cards Gate D | `0ff4c30` | 2026-08-02 前四次 HTTPS reset/443 无法连接，第五次重试成功推送至 `origin/unity_7.31` | full EditMode `152/152`、full PlayMode `38/38`、七卡终验 harness、54 张 PNG、Windows build/Player smoke、架构/维护文档与 02B3 Prompt |
| Unity 战斗切片简体中文 | `93d6b0d` | 2026-08-02 五次 HTTPS reset/443 建连失败；本地 `ahead 1`，待网络恢复后推送 | 集中式中文文案、Silver 字体/署名、Scene/Prefab、EditMode `161/161`、PlayMode `38/38`、8 张 PNG、Windows build/Player smoke 与维护文档 |
| 简体中文交付文档 | `4c25b9a` | 与上项同受 HTTPS reset 阻塞 | 汉化验证总结、维护账本和交付状态 |
| Turn Lifecycle Gate 0 | `cc98d68` | 2026-08-02 push 时 `curl 28 Recv failure: Connection was reset`；远端未前进 | 源语义/架构/交互审计、五份互斥 Agent Prompt、共享契约与测试矩阵 |
| Turn Lifecycle Gate A | `108ff54` | 2026-08-02 push 时 `curl 55 Recv failure: Connection was reset`；当前本地 `ahead 4` | lifecycle runner、ActionId、不可变 snapshot、共享 Timeline/Application 接线、EditMode `183/183`、PlayMode `38/38` |

用户原有四个未提交文件、两个未跟踪 Prompt、来源不明的旧证据图和后续出现的未知迁移路线图改动均未进入检查点。每次提交前记录 `git diff --cached --name-only` 并确认只包含表中范围。最终状态提交推送后以 `git rev-list --left-right --count HEAD...origin/unity_7.31 = 0/0` 为同步门禁。

## Wave 02B3 推送

- Gate B-D 实现：`e70988c`，2026-08-02 已推送至 `origin/unity_7.31`；范围为 lifecycle coordinator、intent、Tower/Poison/death、action identity/UI Prefab、Scene、full `236/236 + 53/53` 与 build/Player harness。
- Gate D 文档与证据：`cf82430`，2026-08-02 已推送至 `origin/unity_7.31`；范围为 ADR、维护/状态账本、Gate B/D XML/JSON/PNG/人工总结和 02B4 Prompt。

`e70988c` 的推送把远端从 `053a6ef` 推进到该实现提交，因此先前受 reset 阻塞的汉化、Gate 0/A 和状态祖先提交也已全部到达远端；历史失败记录保留，不改写。`cf82430` 推送后 `git rev-list --left-right --count HEAD...origin/unity_7.31` 为 `0/0`。

本阶段实际保护清单扩大为 Godot 四文件、dirty 本阶段 Prompt、dirty migration roadmap、5 张历史 targeting PNG、两个 ProjectSettings 和 3 个来源不明 Prompt；均未进入 `e70988c`。
