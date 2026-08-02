# Wave 02B4 Gate D 最终验证与维护审计

> 审计日期：2026-08-02
> 分支：unity_7.31
> 审计方式：只读代码、Scene/Prefab、XML、日志、维护文档与 Git 状态；除本报告外未修改任何文件
> 结论：Gate A/B 已关闭，Gate C 实现已集成且最终全量测试已绿；Gate D 尚需关闭一项胜利判定契约风险，并完成截图、Build、实际 Player、人工视觉、维护账本和精确 Git 检查点。

> 关闭复核：主智能体已按本报告完成全部整改。胜利条件改为 `maximumHp > 0 && currentHp / maximumHp <= 10%` 的纯 Domain 规则；最终 EditMode `300/300`、Direct3D12 PlayMode `61/61`，18 张 PNG、Windows build、增强 Player smoke、人工视觉与维护账本均已刷新。当前完成判定以 `deck-battle-flow-gate-d-final.md` 和 `04-verification/evidence/deck-battle-flow-gate-d/verification-summary.md` 为准。

## 1. 当前真实状态

- HEAD=2e6f101，origin/unity_7.31=2e6f101，审计时 ahead/behind 为 0/0。既有检查点为 Gate 0 41743fd、Gate A 239e878、Gate B 2e6f101。
- Unity D:\timekey-unity-731 是指向仓库 unity/ 的 Junction；抽查 CombatApplicationSession.cs 两侧 SHA-256 一致。因此当前 XML 确实来自同一工作区，不是脱节副本。
- Gate A 正式证据：定向 EditMode 44/44、完整 EditMode 280/280。
- Gate B 正式证据：Application 10/10、集成 60/60、完整 EditMode 293/293、完整图形 PlayMode 53/53。
- Gate C 的 BattleFlow Presentation、Prefab、Scene authoring、Binding、Composition、Controller、实例化手牌和集成测试已在工作区。Scene 已保存 BattleFlowPanel 和 battleFlowPresenter 引用，并把 36 个 Timeline Button 与结算 Button 写入终局输入锁列表。
- BattleFlow Prefab 的 8 个 UnityEngine.UI.Text 均引用 Silver.ttf；Scene 静态扫描无空字体引用。这只是引用完整性证据，不能替代实际字形渲染检查。
- 最新最终态完整 EditMode 为 293/293、0 失败、0 跳过。最新完整图形 PlayMode 为 61/61、0 失败、0 跳过；日志确认使用 Direct3D 12/NVIDIA 图形设备，命令未传 -nographics。该结果已覆盖 Gate C 早期 57/58 的历史失败，历史失败 XML 不得再作为最终结果引用。
- deck-battle-flow-gate-d/ 目前已有最终 editmode.xml、playmode.xml 与原始日志；尚未形成 PNG、视觉 JSON、Build JSON、Player smoke JSON 或人工总结。
- Agent D 已完成并返回；所有权账本仍写成“等待 Gate B API”，Gate D 必须修正文档事实。

## 2. Gate D 前必须关闭的实现风险

### 2.1 自动胜利判定仍不符合冻结源语义

CombatApplicationSession.ResolveTimeline() 当前以 _state.TargetHp <= 1 自动触发 VictorySettlement。冻结的 Godot 语义是：累计登记最大生命必须大于 0，当前/累计最大生命比例 <= 0.1。在当前 10HP fixture 中两者碰巧相同，所以现有 PlayMode 全绿不能证明通用正确性；100HP 目标会被错误延迟到 1HP 才胜利。判定也位于 Application 的绝对常量分支，而 Gate 0 报告要求由可测试规则输入产生 authoritative typed outcome。

Gate D 前必须二选一并记录裁决：

1. 推荐：新增或复用纯 Domain 的胜利谓词，输入累计最大生命与当前生命，明确 max > 0 且 current/max <= 0.1，Application 只提交 typed outcome；覆盖 10HP、100HP、max=0、阈值内外和重复结算。
2. 若本切片刻意只支持固定 10HP fixture，则把此限制写成允许差异并禁止宣称“Godot 胜利语义等价”。但这弱于 Wave 02B4 已冻结契约，不建议用作 Gate D 关闭方式。

修正后必须重新运行完整 EditMode、完整图形 PlayMode、截图 Harness、Windows Build 和 Player smoke；现有 XML 只能作为修正前基线。

### 2.2 最终 smoke 必须证明 02B4，而非只复用旧 marker

当前 Player 路径会先打一轮 Recover，再打一轮 Lighting 并输出 TIMEKEY_PLAYER_SMOKE_PASS，已穿过抽弃与第二手牌，但 marker 前只断言旧切片的 HP/intent 结果。最终 Player smoke 应在 marker 前至少断言：

- phase 已按两次正式 EndTurn 推进，时间币与冻结 occupied-cell 计算一致；
- 第二轮确实发生 discard 回洗，手牌仍为 5，card-instance identity 未丢失；
- 终局 outcome 为 Victory、输入锁定、相反 Defeat 命令返回 typed conflict；
- typed return boundary 成功且携带 12 张 deck snapshot、Era/phase/timecoins、battle tag/seed；
- marker 只输出一次，任一断言失败必须非零退出。

奖励领取可由 Harness/PlayMode 覆盖，不强制在无图形 Player 路径点击按钮；但 smoke summary 必须说明此边界。

### 2.3 最终互斥证据要来自同一事务

Domain XML 已覆盖 Victory→Defeat、Defeat→Victory、重复同结果、sequence conflict 和 reward once；Scene PlayMode 分别覆盖 Victory 与 Defeat。Gate D 的 visual-summary.json 或额外结构化 summary 还应记录同一 authoritative settlement 实例的首次结果、相反结果 failure reason、最终 outcome 和 reward count，避免把两张独立场景截图误写成“互斥事务证明”。

## 3. 必须补跑的结构化测试

所有命令必须从最终工作区 Junction D:\timekey-unity-731 运行 Unity 6000.4.10f1，并把结果写回仓库 docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/。

### 3.1 完整 EditMode

- 使用 -batchmode -nographics -runTests -testPlatform EditMode。
- 产物：editmode.xml；原始日志可保留为本地诊断，最终摘要记录命令、Unity 版本、开始/结束时间和 XML SHA-256。
- 验收：root result=Passed，failed=0、skipped=0、inconclusive=0；最终测试数不得低于当前 293。
- 必须仍包含 Deck、BattleRound、BattleSettlement、BattleFlow hook、Application session、lifecycle、Scene/Prefab/Silver 全部用例；新增胜利比例规则后测试数应上升。

### 3.2 完整 graphical PlayMode

- 使用 -batchmode -runTests -testPlatform PlayMode，禁止 -nographics。
- 产物：playmode.xml；日志必须显示真实 Direct3D 设备而非 NullGfxDevice。
- 验收：root result=Passed，0 failed/skipped/inconclusive；最终测试数不得低于当前 61。
- 必须覆盖 BattleFlowPresenterTests、CombatBattleFlowIntegrationTests、重复 stable ID/实例 View、完整 CombatVerticalSliceTests 和所有 02B3 回归。
- 日志扫描不得出现 C# compile error、missing script、YAML/Prefab error、assertion failure、unhandled exception 或 null reference。

### 3.3 建议的定向失败定位顺序

完整测试失败时只用于定位，不可用局部结果替代最终 XML：

1. EditMode：TimeKey.Tests.EditMode.Deck、TimeKey.Tests.EditMode.BattleFlow、TimeKey.Tests.EditMode.Application.BattleFlow。
2. PlayMode：TimeKey.Tests.PlayMode.BattleFlow、TimeKey.Tests.PlayMode.Cards.CardHandHostTests、TimeKey.Tests.PlayMode.CombatVerticalSliceTests。
3. 修复后重新跑两套完整结果，不拼接多个 filter 宣称 full pass。

## 4. 必须生成的视觉证据

使用 VerticalSliceAutomation.CaptureDeckBattleFlowGateD，启用真实图形设备，并设置 TIMEKEY_REPOSITORY_ROOT=D:\godot\时之钥\时之钥。Harness 当前计划生成 18 张 PNG，覆盖三视口和两个终局。

必须保留以下状态：

- initial-empty-discard-{1280x720,1920x1080,2560x1080}.png：初始 7/5/0、Era 1/phase 1、0 时间币。
- round-2-draw-discard-{...}.png：第一轮后 2/5/5、phase 2、时间币变化。
- round-2-committed-action-frame-1920x1080.png：源卡已离手但已提交 action frame 仍显示保存的中文 name/effect/source/target/identity。
- round-3-post-shuffle-empty-discard-{...}.png：抽牌堆耗尽并从弃牌回洗后 7/5/0、phase 3、累计时间币正确。
- victory-committed-action-frame-1920x1080.png、victory-reward-entry-{...}.png、victory-reward-claimed-1920x1080.png。
- defeat-no-reward-{1280x720,1920x1080,2560x1080}.png。
- visual-summary.json：每张图的真实尺寸、像素存在性指标、seed、抽弃/洗回/资源/终局/返回边界断言和捕获 marker。像素指标只能证明非空，不能替代人工结论。

### 4.1 逐图人工检查清单

每张 PNG 必须实际打开查看，并在 visual-review.md 逐文件写 PASS/FAIL 与观察，不允许批量写“均正常”：

- 1280x720：顶部牌堆/手牌/弃牌、Era/phase、时间币不与原 Header/Status 重叠；五张手牌、Timeline、DetailPanel 和结算框不裁切。
- 1920x1080：action frame 的卡名、效果、source/target、identity 可读；源 CardView 消失后 frame 不变；下一轮清空后没有陈旧 action frame。
- 2560x1080：HUD 不被错误拉伸到超宽空白，不漂移、不脱离安全区，中央结算框仍居中。
- 多轮状态：7/5/0、2/5/5、7/5/0 与截图状态一致；时间币/phase 文本和 Domain snapshot 一致。
- Victory：只显示胜利与一次奖励入口，终局遮罩不漏可交互状态；领取后按钮消失且没有重复奖励文案。
- Defeat：只显示失败，不出现 reward entry、reward label 或胜利残留。
- Silver：全部新增简体中文、阿拉伯数字和标点无方框、缺字、fallback、模糊或 Material 错配；静态 Font 引用通过不等于本项通过。
- 所有图：没有黑屏、单色、粉材质、Missing Script、重叠、文字溢出、卡牌变形、意图/action 残影或关键控件被遮挡。

## 5. Windows Build 与实际 Player smoke

### 5.1 Windows Build

- 运行 VerticalSliceAutomation.BuildDeckBattleFlowGateD。
- Target 必须为 StandaloneWindows64，Development，唯一 Scene 为最终 CombatVerticalSlice.unity。
- 验收 BuildResult.Succeeded、产物大小大于 0、exe 和 _Data 完整、Silver attribution 已复制。
- 产物：build-summary.json，至少含 Unity 版本、target、development、result、bytes、player path、Scene、font、attribution、最终 Git tree/working-copy 标识。
- Build 日志不得以“命令退出 0”代替 BuildReport.summary.result。

### 5.2 Actual Player smoke

- 从刚生成的 Builds/Windows/TimeKeySlice.exe 启动，不得运行 Editor 或旧 build。
- 建议参数：-batchmode -nographics -timekeySmokeQuit -logFile <gate-d/player-smoke.log>。
- 等待真实进程退出；验收 exit code 0、TIMEKEY_PLAYER_SMOKE_PASS 恰好一次、无 TIMEKEY_PLAYER_SMOKE_FAIL/Exception/NullReference。
- 产物：player-smoke-summary.json，含 exe path、exe timestamp/hash、arguments、exit code、marker count、required marker、02B4 状态断言摘要和 raw-log 路径。
- Build 与 smoke 必须在胜利判定修正和最终 Scene/Prefab 保存之后重跑；不得继承 02B3 build/player 作为 02B4 最终证据。

## 6. 维护文档必须更新

Gate D 完成后至少同步以下文件，所有数字只从最终 XML/JSON 读取：

- 03-workstreams/agents/reports/deck-battle-flow-gate-d-final.md：最终结论、测试数、build bytes、Player marker、截图数、人工审查和已推送检查点。
- 03-workstreams/ownership-map.md：Agent D 从“等待 Gate B API”改为完成交回；主智能体回收全部 02B4 路径。
- 03-workstreams/integration-contracts.md：增加 Gate C/D 实际冻结，记录 BattleFlow Presenter、Scene/Prefab、Silver、最终结果和胜利谓词裁决。
- 03-workstreams/master-backlog.md：勾选 Wave 02B4，下一阶段改为排队的 Combat Shell/Scene Flow。
- 04-verification/test-plan.md：用最终完整数字关闭 Gate C/D，并链接新的 XML/JSON/PNG/人工总结。
- 04-verification/parity-matrix.md：移除“02B4 no-op/留给未来”的陈旧描述；牌区、正式回合、时间币、胜负/奖励/return boundary 使用真实判定词。
- 04-verification/inherited-verification-ledger.md：登记 02B4 最终证据及后继阶段可继承条件。
- 05-progress/current-status.md、completed-slices.md、known-issues.md、push-status.md：同步完成状态、剩余允许差异、检查点和 push 结果。
- 02-architecture/combat-modular-architecture.md：把 02B4 no-op hook 更新为已接入的唯一 battle-flow hook，并写明三种 identity 和 typed outcome/return。
- 02-architecture/migration-roadmap.md：移除“下一阶段为剩余卡牌”等陈旧状态，指向 02B5/03A。
- 06-maintenance/module-ownership.md：登记 Domain/Deck、Domain/BattleFlow、Application/BattleFlow、Presentation/BattleFlow 与 Scene/Prefab owner。

最终维护审查还应运行：

- 搜索“02B4 no-op”“下一阶段为 Wave 02B4”“等待 Gate B API”“牌库/胜负留给 02B4”，逐条判断历史文档可保留还是当前账本必须修改。
- 解析最终 XML/JSON，核对文档中的 totals、build bytes、marker count、截图数，不手抄估算。

## 7. 精确 Git 检查点方案

### 7.1 保护与审查

每次暂存前运行：

- git branch --show-current，必须仍为 unity_7.31。
- git status --short、git diff --name-status、git diff --check。
- 明确排除受保护的既有 Godot、shader、历史 targeting PNG、ProjectSettings、来源不明 Prompt 与任何不属于 02B4 的改动。
- 不使用 git add -A、git add .、stash、reset、checkout 回退或历史改写。

### 7.2 建议检查点

1. Gate C implementation checkpoint：只暂存经 provenance 审查确认属于 02B4 的 Runtime/Presentation/Composition/Binding/Controller、BattleFlow Prefab+meta、Scene、Scene authoring、相关 EditMode/PlayMode tests 和必要的 CardHand instance-identity 修改。TargetView.prefab、TimelineCell.prefab 等早于本阶段已存在的改动不得因目录通配被带入。
2. Gate D verification/docs checkpoint：只暂存 VerticalSliceAutomation.cs、最终 deck-battle-flow-gate-d/ XML/JSON/PNG/人工总结、本审计/最终报告和上节列出的维护文档。原始 .log 若仓库规则忽略，则由 JSON 记录 hash/path，不强行纳入。
3. 每个检查点执行 git diff --cached --name-status、git diff --cached --check，人工确认没有保护项后再 commit。
4. 提交后重新检查 git status --short，确认用户原有未提交项仍在；再运行 git rev-list --left-right --count HEAD...origin/unity_7.31。
5. 仅执行 git push origin unity_7.31。push 成功后记录远端 hash 和 0/0；失败则原样记录错误、ahead/behind，不改历史、不强推。

## 8. Gate D 最终 PASS 条件

只有以下条件全部满足才可写 PASS 并恢复 NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md：

- 胜利比例判定风险已按冻结语义修正并有非 10HP 测试。
- 最终完整 EditMode、最终完整图形 PlayMode 均 0 failed/skipped/inconclusive。
- 18 张计划 PNG 全部生成、JSON 可解析、逐图人工审查通过。
- Windows x64 Development build 为 Succeeded，Silver attribution 存在。
- 刚构建的 actual Player exit 0，02B4 增强 smoke 断言和 marker 均通过。
- Victory/Defeat 相反命令在同一事务内产生 typed conflict，reward entry/claim 恰好一次，typed return payload 完整。
- 维护文档不存在当前状态互相矛盾，Agent D/全部共享所有权已交回。
- 两个精确 Git 检查点只含允许路径，既有未提交改动未被覆盖、暂存或丢失；push 状态已记录。

在此之前，Gate A/B 的结构化证据可继续继承，最新 293/293 与 61/61 可作为当前集成基线，但不得提前宣称 Wave 02B4 Gate D 已完成。
