# Wave 04 共享内容与体验契约

> 状态：Gate 0 冻结
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：Wave 04 主 Prompt、`scene/global/sound_manager.gd`、现有 Application trace/result、Godot tutorial directors

## 所有权和依赖方向

主智能体独占 `Scenes/**`、既有正式 `Prefabs/**`、Bootstrap AudioRoot、MainMenu/Settings/Composition、
共享 asmdef、Package/ProjectSettings、UI Theme、Editor harness、共享文档、最终 evidence 和全部 Git。
Audio Agent 只写 `Runtime/Presentation/Audio/**`、Audio tests、独立 `Audio/**` candidate asset、
独立 Audio prefab/mixer/catalog candidate 和 `reports/wave-04-audio.md`。Combat Feedback Agent 只写
`Runtime/Presentation/Feedback/**`、`Prefabs/Feedback/**`、`Materials/Feedback/**`、Feedback tests、
独立 evidence candidate 和 `reports/wave-04-feedback.md`。Tutorial Audit Agent 只读 Godot/Unity 资料，
只写 `reports/wave-04-tutorial-audit.md`。

依赖固定为 `Domain -> Application -> Presentation -> Composition`；Audio/Feedback 只消费已有
Application result/trace/snapshot，不可反向修改 Domain、Timeline、SceneFlow、save 或稳定 identity。
Tutorial typed contract 只能表达语义 step 和 input lease，不能持有 Unity object 或进入 schema 2 run save。

## AudioCue contract

稳定 cue id 如下：`bgm.main_menu`、`bgm.battle`、`bgm.battle_boss`、`sfx.clock_tick`、
`sfx.choose_role`、`sfx.walk_dim`、`sfx.draw_card`、`sfx.card_shuffle`、`sfx.confirm_timeline`、
`sfx.tile_damage`、`sfx.victory_short`、`sfx.victory_long`、`sfx.game_over`。每条 cue 是 Inspector
可编辑内容配置：stable id、clip、route（Master/Music/SFX）、loop、priority、volume、pitch range、
missing-resource diagnostic；ScriptableObject 不保存 playing/active/selected runtime state。

运行时必须有唯一可绑定的 AudioRoot，`Master/Music/SFX` Mixer group，双 BGM source、单 looping SFX
source 和有上限 one-shot pool。重复 bind/rebind 幂等；缺 clip、池耗尽、disable/destroy、pause 和
scene transition 有可观察诊断。BGM 切换使用 unscaled crossfade；相同 cue 幂等，不重复叠加。

## CombatFeedback contract

`CombatFeedbackKind` 只允许从既有 Application `CombatCommandResult`、`TurnLifecycleResult`、
`BattleSettlementResult`、`CombatTraceEntry` 投影：

`CardDraw`、`CardConfirm`、`Damage`、`ElevationPulse`、`Recover`、`Built`、`TowerDecay`、
`PoisonApply`、`PoisonTick`、`Clear`、`EnemyIntentHover`、`EnemyIntentResolve`、
`UnsupportedSourceCommand`、`Victory`、`Defeat`、`ClockRollover`、`Shuffle`、`SceneTransition`。

每条 immutable event 携带 `sequence`、`sourceId`、`targetId`、`worldAnchor`（坐标/锚点值，不是
GameObject 引用）、`kind`、`phase`、`duration` 和 `cancellationToken`/取消语义。Presentation
可在现有 one-shot VFX root 下实例化保存 Prefab，并用 `MaterialPropertyBlock` 或预存材质；不得
延迟/改写 HP、回合、胜负、奖励或 save commit。未知 source command 必须呈现
`UnsupportedSourceCommand` no-effect，不得发明伤害。

## TutorialStep contract

步骤至少包含 `stepId`、`enterCondition`、`completionCondition`、`focusTarget`、`allowedInput`、
`textKey`、`skipPolicy`、`version`。完成条件只读真实 SceneFlow/Overworld/Combat snapshot；焦点
目标以稳定语义 id 表示，不保存 Scene object。教程偏好使用独立 `tutorial_preferences`（schema/version/
promptSeen/enabled/completedVersion），与 schema 2 run save 分离；ESC、Skip、Scene unload 释放
教程焦点和 input lease。

## Gate 0 红测合同

主智能体先建立 `Tests/EditMode/Wave04/Wave04ContractGapTests.cs`，其中两个显式 red tests
验证当前缺少正式 AudioRoot/Mixer/Cue catalog 与 result-to-feedback/Tutorial typed runtime；
红测证据保存为 `evidence/wave-04-gate-0/contract-gap-results.xml`，Gate A/B/D 通过后由主智能体
将其改为正式 contract tests，并在对应 Gate 报告中引用。红测只证明缺口，不能触碰 Wave 03 证据。
