# Wave 03R Era Clock Staged File Candidates

> 2026-08-04 主智能体补充：下方 63 项是独立实现阶段的历史候选。最终检查点还必须包含正式接线与 Wave 03R-F closeout；其专属新增范围为 `Editor/EraClock/**` 中的 FormalIntegration automation、两个 FormalIntegration EditMode 文件、两个 RevealLayout PlayMode 文件、`evidence/era-clock-closeout/**` 全部 23 项，以及 `era-clock-formal-integration-report.md`、`era-clock-closeout-intake-2026-08-04.md`。最终 cached manifest 由 `git diff --cached --name-only` 逐项审计，不使用 `git add -A`。

本智能体没有执行 `git add`、commit 或 push。以下是主智能体可串行审阅的 63 个交付物：55 个是普通 untracked 候选，8 个 `.log` 被仓库现有 ignore 规则忽略，只有主智能体明确决定保留日志时才应精确 force-add。清单明确排除所有任务开始前已有文件，包括旧 intake 和 `era-clock-current-progress-sync.md`。

## Contract / Presenter / Tests / Evidence Scene (25)

```text
unity/Assets/_Project/Editor/EraClock.meta
unity/Assets/_Project/Editor/EraClock/EraClockEvidenceAutomation.cs
unity/Assets/_Project/Editor/EraClock/EraClockEvidenceAutomation.cs.meta
unity/Assets/_Project/Editor/EraClock/EraClockPlayerEvidence.unity
unity/Assets/_Project/Editor/EraClock/EraClockPlayerEvidence.unity.meta
unity/Assets/_Project/Runtime/Application/EraClock.meta
unity/Assets/_Project/Runtime/Application/EraClock/EraClockPresentationSnapshot.cs
unity/Assets/_Project/Runtime/Application/EraClock/EraClockPresentationSnapshot.cs.meta
unity/Assets/_Project/Runtime/Application/EraClock/EraClockStateMachine.cs
unity/Assets/_Project/Runtime/Application/EraClock/EraClockStateMachine.cs.meta
unity/Assets/_Project/Runtime/Application/EraClock/EraClockTransitionPlanner.cs
unity/Assets/_Project/Runtime/Application/EraClock/EraClockTransitionPlanner.cs.meta
unity/Assets/_Project/Runtime/Presentation/EraClock.meta
unity/Assets/_Project/Runtime/Presentation/EraClock/EraClockPlayerEvidenceDriver.cs
unity/Assets/_Project/Runtime/Presentation/EraClock/EraClockPlayerEvidenceDriver.cs.meta
unity/Assets/_Project/Runtime/Presentation/EraClock/EraClockPresenter.cs
unity/Assets/_Project/Runtime/Presentation/EraClock/EraClockPresenter.cs.meta
unity/Assets/_Project/Tests/EditMode/EraClock.meta
unity/Assets/_Project/Tests/EditMode/EraClock/EraClockContractTests.cs
unity/Assets/_Project/Tests/EditMode/EraClock/EraClockContractTests.cs.meta
unity/Assets/_Project/Tests/PlayMode/EraClock.meta
unity/Assets/_Project/Tests/PlayMode/EraClock/EraClockPresenterTests.cs
unity/Assets/_Project/Tests/PlayMode/EraClock/EraClockPresenterTests.cs.meta
unity/Assets/_Project/Tests/PlayMode/EraClock/EraClockVisualEvidenceTests.cs
unity/Assets/_Project/Tests/PlayMode/EraClock/EraClockVisualEvidenceTests.cs.meta
```

## Reports / Prompt Review (9)

```text
docs/migration/unity-3d/03-workstreams/agents/prompts/era-clock-wave-03r-prompt-review.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-fresh-agent-intake-2026-08-03.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-integration-handoff.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-maintenance-guide.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-source-parity.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-staged-file-candidates.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-test-results.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-visual-evidence-index.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-whitelist-audit.md
```

## Gate 0 Required Red Evidence (4)

```text
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/editmode-red-20260803.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/editmode-red-20260803.xml
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/playmode-red-20260803.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/playmode-red-20260803.xml
```

## Canonical Editor Test / Visual Evidence (17)

```text
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/animation-timeline.json
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/center-1280x720.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/center-1920x1080.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/center-2560x1080.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/dynamic-resize-1600x900.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/editmode-full-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/editmode-full-final.xml
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/editmode-targeted-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/editmode-targeted-final.xml
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/playmode-full-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/playmode-full-final.xml
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/playmode-targeted-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/playmode-targeted-final.xml
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/rollover-complete-1920x1080.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/rollover-initial-1920x1080.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/rollover-middle-1920x1080.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-evidence/visual-evidence-index.json
```

## Canonical Windows Player Evidence (8)

```text
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/build-summary.json
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-build-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-hud-terminal-1280x720.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-rollover-complete-1280x720.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-rollover-initial-1280x720.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-rollover-pulse-1280x720.png
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-run-final.log
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-player/player-smoke-summary.json
```

## Explicit Exclusions

```text
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-current-progress-sync.md
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/fresh-agent-intake.md
```

正式 Scene/Prefab integration 产生的后续 diff 不属于本清单，应由主智能体在独立串行步骤审阅和 staging。

## Final formal integration tracked allowlist

```text
docs/migration/unity-3d/02-architecture/migration-roadmap.md
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-summary.json
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/evidence-index.md
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-combat-1280x720.png
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-main-menu-1280x720.png
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-out-of-battle-1280x720.png
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-returned-shell-2560x1080.png
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-summary.json
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-victory-1280x720.png
docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/verification-summary.md
docs/migration/unity-3d/05-progress/current-status.md
unity/Assets/_Project/Prefabs/Battle/CombatShell/CombatTopHUD.prefab
unity/Assets/_Project/Prefabs/Shell/MainMenu.prefab
unity/Assets/_Project/Prefabs/Shell/OutOfBattleShell.prefab
unity/Assets/_Project/Runtime/Composition/SceneFlow/MainMenuSceneNavigation.cs
unity/Assets/_Project/Runtime/Composition/SceneFlow/OutOfBattleShellSceneNavigation.cs
unity/Assets/_Project/Runtime/Composition/SceneFlow/SceneFlowPlayerSmoke.cs
unity/Assets/_Project/Runtime/Presentation/Bindings/CombatPresentationBinding.cs
unity/Assets/_Project/Scenes/Shell/MainMenu.unity
unity/Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity
unity/Assets/_Project/Tests/PlayMode/SceneFlow/CombatShellGateEStabilityTests.cs
```

全量 PlayMode 重写的其余 `combat-shell-gate-e/**`、所有非 EraClock 证据、`era-clock-current-progress-sync.md`、Gate 0 `fresh-agent-intake.md`、原始日志和 build 目录仍明确排除。

## Ignore Audit

以下 8 项存在于磁盘，但被现有 ignore 规则忽略：Gate 0 的 2 个 `.log`、canonical Editor evidence 的 4 个 `.log`、Player evidence 的 2 个 `.log`。其余 55 项均由 `git ls-files --others --exclude-standard` 逐项确认。不要使用 `git add -A`；主智能体应按本清单精确暂存，并单独决定是否 force-add 日志。
