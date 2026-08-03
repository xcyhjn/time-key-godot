# Wave 03P Overworld Movement and UI Theme Intake

> Status: Gate 0 workspace protection frozen
>
> Owner: /root
>
> Recorded: 2026-08-03

## Queue And Baseline

- Branch: `unity_7.31`
- P0 checkpoint: `bea6adf457ff63acd273a58f86c64c975908bbd3`
- Upstream: `origin/unity_7.31`, ahead/behind `0/0`
- P0 Gate 0-E and push are complete.
- The previously queued Era Clock parent and its Godot audit child were interrupted
  because the newest user instruction makes Wave 03P the active stage.
- The Era Clock task wrote only
  `04-verification/evidence/era-clock-animation-gate-0/fresh-agent-intake.md`;
  it and every other pre-stage path are protected and excluded from this stage.
- After Wave 03P Gate D, resume
  `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` at its first unclosed Gate and inherit
  the movement and theme contracts.

## Process And Lock Gate

- Unity Editor/batchmode, Godot, Player, ShaderCompiler, build and test writers:
  none at intake.
- Unity lock and EditorInstance: absent.
- Blender PID 18072 is open on HexTile_Dirt without an unsaved marker. Exclusive
  read/write open succeeded and its timestamp remained stable; it is treated as
  a non-writing user process. Wave 03P will not call Blender/MCP or touch ArtSource.
- Unity Hub/licensing background services are not project writers.
- Machine-readable check:

```json
{"RecordedAt":"2026-08-03T15:44:32.3551471+08:00","Branch":"unity_7.31","HEAD":"bea6adf457ff63acd273a58f86c64c975908bbd3","Upstream":"origin/unity_7.31","AheadBehind":"0\t0","Dirty":383,"Staged":0,"UnityLock":false,"EditorInstance":false,"BlenderPid":18072,"BlenderExclusiveOpen":"PASS","BlenderTimestampStable":true,"BlenderLastWriteTimeUtc":"2026-07-31T10:44:03.3688785Z"}
```
## Ownership Strategy

- All 383 paths below predate Wave 03P and are user/predecessor owned.
- Existing modified files, historical evidence, prompts, assessments, maintenance
  guides and test artifacts remain untouched and unstaged.
- Wave 03P writes only newly reviewed ownership paths plus main-agent shared
  integration paths explicitly allowed by the stage Prompt.
- Any Unity/test rewrite of a protected path is restored only if its pre-run bytes
  are proven to match this inventory's owner baseline; otherwise work stops on
  that path and records a conflict.
- No stash, reset, checkout, clean, broad formatting or `git add -A`.

## Complete Dirty / Untracked Inventory

```text
 M default_bus_layout.tres
 M docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_TURN_LIFECYCLE_PROMPT.md
 M docs/migration/unity-3d/00-bootstrap/START_HERE_PROMPT.md
 M docs/migration/unity-3d/02-architecture/migration-roadmap.md
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-complete.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/combat-shell-entrance-1280x720-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1280x720-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1280x720-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1920x1080-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-1920x1080-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-2560x1080-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/game-start-2560x1080-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1280x720-entrance-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1280x720-entrance-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1920x1080-entrance-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-1920x1080-entrance-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-2560x1080-entrance-initial.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/main-menu-2560x1080-entrance-middle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-1280x720-defeat.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-1920x1080-defeat.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/game-over-2560x1080-defeat.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-confirming.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-hover.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-idle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-selected.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1280x720-settled.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-confirming.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-hover.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-selected.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-1920x1080-settled.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-confirming.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-hover.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-selected.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/out-of-battle-2560x1080-settled.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/animation-timeline.json
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/combat-reveal-complete.png
 M docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/combat-reveal-middle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-hover.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1280x720-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-hover.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/1920x1080-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-hover.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-card-ui-agent/2560x1080-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-0.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-180.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-270.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/board-range-yaw-90.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b-targeting-agent/timeline-invalid.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-earthquake-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1280x720-lighting-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-earthquake-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-1920x1080-lighting-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-earthquake-selected.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-idle.png
 M docs/migration/unity-3d/04-verification/evidence/wave-02b2a-two-card-hand-agent/two-card-hand-2560x1080-lighting-selected.png
 M docs/migration/unity-3d/05-progress/current-status.md
 M scene/in_scene/rewards/resources/default_craft_recipe_book.tres
 M shaders/color_BG.gdshader
 M shaders/game_over.gdshader
 M unity/ProjectSettings/ProjectSettings.asset
 M unity/ProjectSettings/URPProjectSettings.asset
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_ERA_CLOCK_ANIMATION_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_OVERWORLD_MOVEMENT_AND_UI_THEME_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_PLUGINIZATION_AND_ASSET_TOOLING_PROMPT.md
?? docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md
?? docs/migration/unity-3d/01-assessment/overworld-movement-and-ui-theme-gap-analysis.md
?? docs/migration/unity-3d/01-assessment/pluginization-and-tooling-assessment.md
?? docs/migration/unity-3d/03-workstreams/agents/prompt-review-overworld-movement-and-ui-theme.md
?? docs/migration/unity-3d/03-workstreams/agents/prompt-review-pluginization-and-asset-tooling.md
?? docs/migration/unity-3d/04-validation/combat-shell-gate-b/editmode-targeted-final.log
?? docs/migration/unity-3d/04-validation/combat-shell-gate-b/editmode-targeted-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-over-960x540.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-start-960x540.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/game-start.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-0/godot/in-scene-960x540.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/build-transaction-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-3.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full-3.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-remediation-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-remediation-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-review-closure-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-transaction.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow-transaction.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-scene-flow.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-transaction-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/editmode-transaction-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/player-smoke-transaction-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-card-lock-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-card-lock-review-closure.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final-3.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final-3.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-rendered-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-remediation-rendered-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-review-closure-rendered-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-scene-flow-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-scene-flow-review-closure.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-transaction-rendered-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a-remediation/playmode-transaction-rendered-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/build.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-assets.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-assets.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/editmode-scene-flow.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke-visible.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/player-smoke.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-frame.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow-frame.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/playmode-scene-flow.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/scene-authoring-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-a/scene-authoring.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-3.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-material-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-gate-b.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/author-post-build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-material-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/capture.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-gate-b-scene-flow-review-closure.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-post-build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted-material-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted-material-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/editmode-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-combat-shell-review-closure.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-entrance-reuse-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-entrance-reuse-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-scene-flow-review-closure.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-scene-flow-review-closure.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-b/playmode-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/author-gate-c.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-asset-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-asset-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-component-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-component-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-scene-flow-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-scene-flow-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-visual-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/playmode-visual-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d-canvas-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/author-gate-d.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-assets-post-format.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-assets-post-format.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-full-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-sceneflow-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/editmode-sceneflow-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-3.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final-3.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-full-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-gate-c-visual-regression.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-gate-c-visual-regression.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-out-of-battle-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-out-of-battle-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-roundtrip-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-roundtrip-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-d/playmode-visual-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-final-reveal.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-layered-reveal.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/author-ocean-and-enemy-reveal.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-delivery-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-delivery-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build-post-review.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/build.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-assets-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-build-smoke-compile.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/editmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-delivery-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-delivery-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-exit-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-post-review.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke-visible.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/player-smoke.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-combat-entrance-post-review.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-delivery.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-layered-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-final.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-post-review.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-stability-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-final.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-ocean-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-ocean-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-2.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-2.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-3.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-3.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-4.log
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted-4.xml
?? docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-e/playmode-visual-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-a/editmode-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-a/editmode.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode-application.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode-integration.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/editmode.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-b/playmode.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/editmode-scene.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-battle-flow-integration.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-regression.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted-graphical.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/playmode-targeted.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-c/scene-authoring.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/build.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/capture-final.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/capture.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-victory-rule.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode-victory-rule.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/editmode.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/player-smoke.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-rule.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-rule.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-smoke.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-battleflow-smoke.xml
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode-final.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode.log
?? docs/migration/unity-3d/04-verification/evidence/deck-battle-flow-gate-d/playmode.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-contracts-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-contracts.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/editmode-gate0.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-0/playmode-geometry-gate0.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-controller.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-controller.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/editmode-post-controller.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-3.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea-3.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-geometry-gatea.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-resize-debug.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-a/playmode-resize-debug.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-final.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-b/playmode-geometry-poison-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-2.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-3.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice-3.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-combat-vertical-slice.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-cancel-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final-2.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-final.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-2.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-3.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-3.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-4.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-4.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-5.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-5.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-6.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability-6.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-c/playmode-interaction-stability.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-final.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-settled-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-stable-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-nonrect-visual-stable-final.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-2.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-3.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-3.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-d/playmode-visual-three-viewport.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/build-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/build.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/editmode-full.xml
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/player-smoke-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/player-smoke.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical-final.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical.log
?? docs/migration/unity-3d/04-verification/evidence/effect-frame-stability-gate-e/playmode-full-graphical.xml
?? docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/fresh-agent-intake.md
?? docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md
?? docs/migration/unity-3d/06-maintenance/ui-theme-resource-guide.md
?? unity/Assets/InitTestScene4fa99104-2a41-4d45-95ce-47775840620a.unity
?? unity/Assets/InitTestScene4fa99104-2a41-4d45-95ce-47775840620a.unity.meta
?? unity/TestResults/effect-frame-geometry-targeted-final.xml
?? unity/TestResults/effect-frame-geometry-targeted.xml
?? unity/TestResults/timeline-action-frame-existing.xml
?? unity/docs/migration/unity-3d/04-verification/evidence/combat-shell-gate-c/author-gate-c-review-fix.log
```
