# 《时之钥》Unity 牌库、正式回合推进与战斗结算接手 Prompt

> 阶段：Wave 02B4
> 状态：仅在 Wave 02B3 Gate D 已完成并推送后启动
> 前置报告：`03-workstreams/agents/reports/turn-lifecycle-gate-d-final.md`

# 主 Prompt

你是《时之钥》Godot 到 Unity 迁移的 Wave 02B4 主智能体。进入 `D:\godot\时之钥\时之钥`，确认分支为 `unity_7.31`，完整读取本文、ADR 0008、02B3 Gate D 最终报告、继承证据账本和当前真实代码。本文“主 Prompt”是本阶段唯一执行规范。

先执行 Gate 0：保护全部既有未提交改动，确认 02B3 的智能体和 Unity 进程均已结束，确认 `e70988c` 及其交付文档已在当前分支，解析 02B3 最终 XML/JSON，并用最小只读测试证明 lifecycle runner、Tower、Poison、enemy intent、action identity 和 Silver 表现仍可信。若 Gate D 未完成或共享代码仍有活跃所有者，才算硬阻塞。

## 范围

本阶段只实现：

1. 正式回合推进、Era/回合资源和时间币。
2. starter deck、手牌、抽牌堆、弃牌堆、弃置、抽牌和确定性洗牌。
3. 在 ADR 0008 的 02B4 hook 接入抽弃与资源步骤；不得重写 lifecycle/Tower/Poison/intent 顺序。
4. 手牌 View 消失后，已提交的玩家 action frame 仍从保存的 display payload 显示卡名、效果、source/target 和 identity，不依赖 hand 中仍有 View。
5. 胜利/失败互斥、战斗结算、局内奖励入口和返回边界。

非目标：新增敌人/建筑内容、重做现有战斗 UI、局外 Unity 迁移、旧存档兼容、网络、性能专项或素材替换。局外流程继续留在 Godot，除非用户重新排定优先级。

## 冻结契约

- 固定 seed 必须使初始牌库顺序、洗牌和多回合结果可复现；不得使用 Unity frame time 或全局随机状态。
- 空抽牌堆时只按 Godot 可观察语义把弃牌堆洗回；空牌库/空弃牌堆必须返回 typed 结果，不死循环、不凭空造牌。
- 同一实体卡在 deck/hand/discard 中有稳定 card-instance identity；card stable ID 仍只表示内容类型。
- action identity 与 card-instance identity 分离。弃牌或抽牌不能改变已提交 action 的 identity/presentation snapshot。
- 时间币和回合资源只由 Application/Domain 修改，HUD 消费只读结果。动画、按钮和 View 销毁不能决定资源或胜负。
- 胜利和失败同一事务内互斥；结算后输入锁定，不能再提交 action 或重复发奖励。
- 继续使用简体中文和 Silver；新 TextMesh 必须 Font/Material 成对。

## 门禁

### Gate 0：接手与源语义

冻结 Godot starter deck、抽/弃/洗、时间币、Era、turn advance、胜负和奖励入口的真实顺序；更新契约、所有权、test plan 和互斥 Agent Prompt，形成单一目的检查点。

### Gate A：纯牌库与资源 Domain

实现无 Unity 依赖的 deck/hand/discard、固定 seed shuffle、资源与 typed result。EditMode 覆盖重复卡、空堆、洗回、多周期和失败无副作用。

### Gate B：Application hook 集成

只接入 ADR 0008 保留 hook，证明生命周期顺序不变；回合结束后弃/抽/资源更新恰好一次，重复命令幂等，action display payload 不依赖 CardView。

### Gate C：胜负、奖励与 Presentation

实现互斥终局状态、最小局内奖励入口、中文/Silver HUD 与保存的 Scene/Prefab 引用。不得在 Presenter 推导牌库、资源或胜负。

### Gate D：最终交付

运行完整 EditMode、完整 graphical PlayMode、Windows build 和实际 Player smoke。视觉证据至少覆盖 1280x720、1920x1080、2560x1080，多回合抽弃/洗牌、空堆、资源变化、胜利和失败互斥；逐图检查裁切、重叠、残留 action frame 和 Silver 缺字。保存 XML、JSON、PNG 和人工总结。

## 多智能体与 Git

所有 Agent 所有权互斥；主智能体独占共享 Scene、Composition、Controller、asmdef、harness、文档、暂存、提交和推送。Agent 不得切分支、stash、commit、push 或回退他人修改。每个 Gate 用精确路径检查点；禁止 `git add -A`、历史改写和清理未知文件。只 push `unity_7.31`，失败时记录准确错误与 ahead/behind。

除前置未完成、权威语义不可调和冲突或共享文件确有活跃写者外，不得停在计划、复述或普通实现问题上。

# 主 Prompt 结束
