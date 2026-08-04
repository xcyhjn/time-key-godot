# Wave 04 Combat Feedback Agent Prompt

## 角色与单一目标

你负责 Gate B 首个 result-to-feedback 垂直切片：`draw -> lighting select -> timeline confirm -> typed
damage result -> 真实 3D anchor hit VFX/SFX -> HP/effect frame update -> 自动回收`，并提供可扩展的
typed feedback projection。你不是仓库唯一工作者，保护所有继承 dirty/untracked。

## 必读资料

- `docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md`
- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_CONTENT_AND_EXPERIENCE_PROMPT.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts-wave-04.md`
- `docs/migration/unity-3d/04-verification/parity-matrix.md`
- `docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md`
- `unity/Assets/_Project/Runtime/Application/CombatApplicationModels.cs`
- `unity/Assets/_Project/Runtime/Application/Ports/ICombatTraceSink.cs`
- `unity/Assets/_Project/Runtime/Domain/ResolutionSnapshot.cs`
- 既有 `CombatOccupantView`、`BoardRangePreview`、`CombatCompositionRoot` 和对应 tests

使用 `Verification & Quality Assurance` 的 XML/JSON/PNG 可追溯证据规则；不引入第三方 VFX 包。

## 独占所有权

允许写：

- `unity/Assets/_Project/Runtime/Presentation/Feedback/**`
- `unity/Assets/_Project/Prefabs/Feedback/**`
- `unity/Assets/_Project/Materials/Feedback/**`
- `unity/Assets/_Project/Tests/EditMode/Feedback/**`
- `unity/Assets/_Project/Tests/PlayMode/Feedback/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-04-feedback.md`
- 独立候选 evidence 子目录；不得改历史 Gate A-E PNG

禁止写：Domain/Application/Timeline/Card/SceneFlow/save/identity、任何正式 Scene 或 Prefab、Bootstrap、
Settings/Presenter、共享 asmdef/Packages/ProjectSettings、共享文档和其他 Agent 路径。主智能体负责把
候选 prefab 绑定到正式 Scene。

## 实现契约与非目标

只消费既有 `CombatCommandResult`/`TurnLifecycleResult`/`BattleSettlementResult`/`CombatTraceEntry`，
输出 immutable `CombatFeedbackEvent`（sequence/sourceId/targetId/worldAnchor/kind/phase/duration/cancel）。
涵盖 CardDraw/CardConfirm/Damage/ElevationPulse/Recover/Built/TowerDecay/PoisonApply/PoisonTick/Clear、
EnemyIntentHover/Resolve/UnsupportedSourceCommand、Victory/Defeat/ClockRollover/Shuffle/SceneTransition。
不改变任何 Domain snapshot、HP、回合、胜负、奖励或 save；UnsupportedSourceCommand 必须是明确 no-effect。
VFX 必须从保存 prefab 创建到既有 one-shot root，使用 MaterialPropertyBlock/保存材质，具备池化或明确
回收，不能每次播放新建材质或永久增加 material count。

## 验收与报告

先让缺口测试可重现，再实现 lighting 首切片；覆盖 VFX 生命周期、取消/重复 bind、真实 anchor、材质计数、
四 yaw、最低/最高堆叠高度、1280x720/1920x1080/2560x1080 和动态 resize。保存初始/中间/完成 PNG、
结构化 feedback timeline JSON、PlayMode XML；实际 Player 无法听觉/视觉验证时明确标注未完成。

不切分支、不 stash、不回退、不暂存、不 commit、不 push，不并发运行 Unity。报告改动、事件映射、测试/XML/PNG、
性能/material 计数、失败项、回滚与下一步；共享契约冲突立即报告主智能体并停止写共享路径。
