# Combat Shell Gate D verification summary

> Status: Gate D passed
> Unity: 6000.4.10f1
> Date: 2026-08-03

## Automated gates

| Gate | Result | Evidence |
| --- | --- | --- |
| Gate D authoring | pass marker and successful batch exit | `author-gate-d-final.log` |
| targeted SceneFlow EditMode | `33/33` | `editmode-sceneflow-targeted.xml` |
| saved Gate D asset tests | `3/3` inside the full EditMode suite | `editmode-full.xml` |
| targeted out-of-battle PlayMode | `4/4` | `playmode-out-of-battle-targeted.xml` |
| targeted formal round trips | `2/2` | `playmode-roundtrip-targeted.xml` |
| targeted formal-scene visual capture | `1/1` | `playmode-visual.xml` |
| full EditMode | `343/343` | `editmode-full.xml` |
| full Direct3D12 PlayMode | `92/92` | `playmode-full.xml` |
| three-viewport visual review | 18 PNGs passed | `visual-summary.json` and `visual-review.md` |

Every canonical XML above has `total>0`, `failed=0`, `skipped=0`, and `inconclusive=0`. The full PlayMode log identifies Direct3D 12. Earlier failed visual attempts and the superseded `playmode-full-final.xml` are diagnostic artifacts, not canonical pass evidence; raw editor logs remain local.

## Typed round-trip result

- Main Menu creates a run with explicit character identity. `OutOfBattleShellState.CreateCombatLaunch` preserves run ID, seed, chapter, era, phase, timecoins, character, deck snapshot, room ID, and stable launch correlation.
- Combat Composition consumes the active launch instead of the direct-load fixture. The formal test verifies battle tag/seed, era, phase, timecoins, and deck contents against the launch payload.
- Victory does not leave Combat until its reward is claimed. Repeated reward clicks produce one typed outcome, clear the active launch, return to the same run/room, and settle that room once.
- Defeat produces no reward, transitions to Game Over, and then returns to Main Menu with run, launch, outcome, and apply-result state cleared.
- Each active content scene has one bound, interactive `SceneContentEntry`; Bootstrap, persistent input gate, and persistent audio ownership remain singular through both tested paths.
- The targeted SceneFlow suite covers pre-commit rollback, post-commit recovery, sequence/correlation identity, input release, and source/target restoration. Gate D navigation retains the prepared launch/outcome identity for retry and rejects/unlocks a failed room confirmation.

## Saved scene and visual result

- `OutOfBattleShell.prefab` and `GameOver.prefab` each store one child `GateDCanvas`, one sibling camera, a root `CanvasGroup`, typed Presenter/Composition references, and Silver on every player-visible `Text`.
- `OutOfBattleShell.unity` and `GameOver.unity` each store one inactive, prefab-backed content root below a typed `SceneContentEntry`. `CombatVerticalSlice.unity` stores exactly one `CombatSceneNavigation` on its controller.
- The 18 final screenshots cover five out-of-battle states and typed defeat at 1280x720, 1920x1080, and 2560x1080. Dimensions, visible variation, unique hashes, layout, and glyph rendering passed.
- Gate D inherits the unaffected 02B4 victory/reward/defeat combat evidence in `deck-battle-flow-gate-d`; the new round-trip tests exercise those same authoritative settlement boundaries through SceneFlow.

Windows build, actual Player smoke, three consecutive Player round trips, and final stage animation/performance checks remain Gate E work. This Gate D summary does not claim those deferred checks.
