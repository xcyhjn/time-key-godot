# Git 提交与推送状态

> 状态：解耦 R3 本地检查点已完成，推送受 GitHub 443 reset/timeout 阻挡
> 负责人：主智能体
> 最后验证日期：2026-08-01
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
| 解耦 R3 Composition/Presentation/Content | `0b02791` | 已连续安全重试；当前 HTTPS 连接发生 reset/443 timeout，implementation commit 尚未到达远端 | Composition root、四 Presenter + Binding、七卡 catalog/front_image、effect registry、结构化 trace、Scene 接线、EditMode `92/92`、PlayMode `31/31`、14 张截图、build 与 Player smoke |

用户原有四个未提交文件、两个未跟踪 Prompt 和来源不明的旧证据图不得出现在任一检查点。每次提交前记录 `git diff --cached --name-only` 并确认只包含表中范围。网络恢复后先推送本地 R3 与文档检查点，再更新本账本为远端 `0/0`。
