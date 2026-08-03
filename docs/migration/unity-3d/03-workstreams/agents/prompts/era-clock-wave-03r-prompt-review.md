# Wave 03R Era Clock Prompt Review

## 结论

主 Prompt 可执行，无定义内硬阻塞。当前 HEAD 已有 authoritative Era/Phase 数据、现成时钟 PNG、Silver 字体、Unity 6000.4.10f1、可用 EditMode/PlayMode 基础设施和独立白名单目录。正式 Scene/Prefab 接线不属于本智能体交付完成条件。

## 最低风险解释

1. `typed snapshot` 是 Application 层不可变值；构造时验证 Era >= 1、Phase 1..8、Sequence >= 0，并携带明确 anchor intent。
2. 状态机只消费 snapshot，不推进 Domain。相邻 phase、rollover、重复、stale、jump、anchor move 都产生可测试 transition plan。
3. Presenter 立即接受最新 authoritative snapshot 并更新最终文案；指针、进度、pulse 与 anchor 通过可取消 coroutine 呈现。新 snapshot 到达时取消旧时间线，generation token 阻止旧完成回调污染新状态。
4. `zero-duration` 在同一帧落到确定终态；disable/destroy/rebind 均落到 latest snapshot 的终态且不遗留 input lock。
5. Presenter 视觉层全部 `raycastTarget=false`，root CanvasGroup 不拦截输入；本模块不拥有全局 input lock。
6. 非相邻 jump 采用 snap。制造遗漏的中间 phase 动画会伪造未发生的 authoritative event，因此不采用。

## 分层与互斥所有权

- Contract：`Runtime/Application/EraClock/**` 与 `Tests/EditMode/EraClock/**`，纯 C#、无 UnityEngine 依赖。
- Presenter：`Runtime/Presentation/EraClock/**` 与行为 PlayMode tests，拥有 animation state 与 RectTransform 写入。
- Evidence：`Editor/EraClock/**`、visual/evidence PlayMode tests 与 `evidence/era-clock-animation-gate-*`，只构建隔离 harness 和证据，不写正式资产。

Contract 不持有 GameObject；Presenter 不写 Domain 或正式 consumer；Evidence 不改变正式 Scene/Prefab/BuildSettings。三类模块不存在共享文件写入。

## 验收循环

1. 先写 reflection-based contract 红测并运行，验证因类型不存在而失败，而非编译失败。
2. 实现 snapshot、adapter、transition planner/state machine，跑目标 EditMode 再跑 full EditMode。
3. 写 Presenter 红测，随后实现指针/进度/rollover/anchor/cancel/rebind/zero-duration，跑目标与 full PlayMode。
4. 用隔离 evidence harness 采集 1280x720、1920x1080、2560x1080、dynamic resize、rollover timeline；检查 PNG 像素非空、重叠/裁切和输入状态。
5. 构建仅含隔离 EraClock evidence scene 的 Windows Player，运行 smoke 并采集截图/summary；不修改 BuildSettings。
6. 输出 integration handoff、测试结果、timeline、visual index、allowlist audit 与下一 Gate。保持 staged=0，不提交。
