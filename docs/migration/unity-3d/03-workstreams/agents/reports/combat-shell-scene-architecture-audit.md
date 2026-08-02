# Combat Shell scene architecture audit

> 结论：PASS WITH CONCERNS；Gate A 可开始
> 模式：只读子智能体审计
> 日期：2026-08-02

## 当前结构

- Build Settings 仅含 `CombatVerticalSlice.unity`；尚无 Bootstrap/MainMenu/OutOfBattle/GameOver/SceneFlow。
- Domain 与 Application 继续 `noEngineReferences`，Presentation 只引用 Domain/Application，Composition 汇合 Infrastructure/Diagnostics/Presentation。纯 SceneFlow 应进入 `Runtime/Application/SceneFlow/**`，Unity 适配进入 `Runtime/Composition/SceneFlow/**`。
- 02B4 `BattleSettlementState` 与 `CombatApplicationSession` 已提供胜负互斥、一次 reward、typed return；必须适配，不重写。
- 现有 `CombatCompositionRoot.Awake()` 使用 fixture seed/deck/time/battle tag；正式 Combat 接线前必须先绑定 `CombatLaunchPayload` 再启用内容根。
- Combat Scene 当前自带 EventSystem，且历史 Scene test/guide 要求该对象；ADR 0010 明确取代这一点，由 Bootstrap 唯一拥有。
- 历史 PlayMode fixture 与 Player build 都假设单 Combat Scene；阶段专用 fixture/harness 必须迁移到 Bootstrap/additive 入口。

## 冻结架构

- Application：typed `SceneId`、request/phase/failure/result、launch/outcome/shell state、effects port 与 coordinator；不得引用 Unity。
- Composition：Bootstrap root、SceneFlow runtime/effects、route catalog、content entry、persistent input gate、transition presenter 和 state store。
- Bootstrap 保持加载；内容 Scene 每个恰好一个 content entry，不使用全局查找或静态 singleton。
- 遮罩下禁用来源 camera/input，绑定并启用目标；失败则卸载目标、恢复来源/focus、揭罩解锁。
- Busy 不排队；sequence/correlation 提供幂等、冲突和 stale 语义；所有晚到回调验证 generation。

## Gate A 关注点

必须自动化验证唯一持久根、阶段顺序、payload 在内容启用前绑定、首个可渲染帧、三次往返无重复对象，以及 load/activate/bind/first-frame/unload 失败回滚。移除 Combat EventSystem 后同步迁移 direct-load 测试，不能让生产 Scene 为测试保留重复对象。
