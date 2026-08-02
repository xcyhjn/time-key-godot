# Combat Shell Agent A：SceneFlow contracts / Bootstrap runtime

你不是仓库唯一工作者。保护所有既有脏改，不得回退、stash、stage、commit、push、切分支或运行 Unity/Godot。

## 独占写入

- 新增 `unity/Assets/_Project/Runtime/Application/SceneFlow/**`
- 新增 `unity/Assets/_Project/Runtime/Composition/SceneFlow/**`
- 新增 `unity/Assets/_Project/Tests/EditMode/SceneFlow/**`
- 新增 `unity/Assets/_Project/Tests/PlayMode/SceneFlow/**`
- 自己的 `docs/migration/unity-3d/03-workstreams/agents/reports/combat-shell-agent-a-scene-flow.md`

禁止修改任何既有文件、asmdef、Scene、Prefab、Build Settings、Controller、CompositionRoot、Binding、Editor harness、共享文档和其他 Agent 路径。需要引用或 Scene 接线时只在报告列出，由主智能体处理。

## 目标

按 ADR 0010 和主 Prompt 实现最小 Unity-free typed SceneFlow：SceneId、request/phase/failure/result、typed payload interface、CombatLaunchPayload、CombatOutcome、OutOfBattleShellState、effects port 与 coordinator；实现 Bootstrap 所需的 Unity runtime 组件，但不创建/修改 Scene。

必须覆盖合法 phase chain、非法 route/payload、Busy、同 sequence 幂等/冲突、stale、generation/cancel、每个 failure phase 的恢复、防御性复制、outcome consume-once 与 Victory reward-before-return guard。保持 Application `noEngineReferences`，不得复制 02B4 settlement 状态机，不使用 `object`/Dictionary/Unity 引用或静态 singleton。

完成后只报告文件、API、测试覆盖、主线所需 asmdef/Scene 接线和风险；不要宣称 Unity 测试已运行。
