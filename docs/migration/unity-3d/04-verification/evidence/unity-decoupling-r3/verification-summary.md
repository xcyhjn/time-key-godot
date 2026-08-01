# Unity Decoupling R3 Verification Summary

> Status: passed
> Verified: 2026-08-01
> Unity: 6000.4.10f1

## Scope and inherited evidence

Godot authoritative behavior, source card-art hashes and historical slice records remained trustworthy because this stage did not change their source files. R3 changed Presentation, Infrastructure catalog/resource mapping, Composition, diagnostics, Scene wiring and runtime assemblies, so the affected EditMode, PlayMode, harness, build, Player smoke and all rendered views were refreshed.

## Automated gates

- Infrastructure Agent-C-equivalent filter: `6/6 passed`.
- R3 integration EditMode: `21/21 passed`.
- R3 integration PlayMode: `15/15 passed`.
- Full EditMode: `92/92 passed`, 0 failed, 0 skipped.
- Full PlayMode: `31/31 passed`, 0 failed, 0 skipped.
- Scene authoring: `TIMEKEY_EDITABLE_SCENE_AUTHORING_PASS`.
- Editor harness: `TIMEKEY_DECOUPLING_R3_HARNESS_PASS`.
- Windows x64 development build: `Succeeded`, `206747014` bytes.
- Windows Player smoke: exit code 0 and `TIMEKEY_PLAYER_SMOKE_PASS` present.

Structured results are `agent-c-editmode-results.xml`, `integration-editmode-results.xml`, `integration-playmode-results.xml`, `editmode-results.xml`, `playmode-results.xml` and `harness-summary.json` in this directory.

## Architecture and editability

The saved Scene contains the stable camera, lights, ground, board root, target anchor, EventSystem, Canvas/HUD, 36-cell Timeline, CardHandHost and both previews before Play. Six saved Prefabs remain the only dynamic presentation sources. `CombatCompositionRoot` owns catalog/session/trace/runtime-sprite lifetime; `CombatPresentationBinding` owns input subscriptions; four narrow Presenters own card hand, board range, Timeline and HUD refresh. `Presentation` no longer references `Infrastructure`, and Domain/Application remain free of UnityEngine.

The seven real fixtures load in stable order with artwork derived only from `CardDefinition.FrontImage`. Existing but unregistered Recover/Built/Poison/Clear content remains visible and explicitly unavailable; adding an ordinary card that uses a registered effect does not require a Controller route.

## Rendered evidence review

All 14 PNGs were opened and checked manually:

- `seven-card-hand-1280x720.png`, `seven-card-hand-1920x1080.png`, `seven-card-hand-2560x1080.png`: all seven original faces are complete, retain aspect ratio and do not overlap the right HUD.
- `lighting-selected-1920x1080.png`, `lighting-targeted-1920x1080.png`: selected card remains visible and the world target/range is clear.
- `earthquake-selected-1920x1080.png` and four yaw range images: the same seven valid columns are highlighted at 0/90/180/270 without coordinate drift.
- `earthquake-timeline-invalid-1920x1080.png` and `earthquake-timeline-valid-1920x1080.png`: invalid red/yellow and valid green states remain distinguishable without relying only on text.
- `earthquake-before-1920x1080.png` and `earthquake-after-1920x1080.png`: seven affected columns visibly change from one to three independent blocks; the target follows the raised top and no stale range remains.

The Domain and PlayMode assertions additionally prove two new mesh/renderer/collider blocks per valid column, strict `0.32` spacing, `TopBounds`/occupant-anchor delta `0.64`, and selectable top colliders at all four yaws.

## Result

R3 closes the decoupling prompt without changing lighting, earthquake, timeline or Godot out-of-combat semantics. Remaining typed effects and the real enemy system are intentionally outside this stage and remain documented as explicit follow-up work.
