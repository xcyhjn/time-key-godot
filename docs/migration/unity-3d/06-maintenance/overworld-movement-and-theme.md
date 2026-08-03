# Wave 03P Maintenance: Overworld Movement and UI Theme

## Scene/Prefab contract

`OutOfBattleShell.prefab` owns the stable `GateDCanvas/RoomRevealLayer/MapViewport/MapHost`, `EdgeHost`, `PlayerMarker`, `OverworldMapInputController`, `OverworldMovementPresenter`, feedback label and `UiThemeScope`. `OverworldNode.prefab` is the editable dynamic node template. Do not rebuild these stable hosts from runtime code. Dynamic node instances may be created from the saved template after a future map snapshot is bound.

## Movement contract

Use `MapNodeId` and `AxialHexCoord` from `Runtime/Domain/OverworldMovement`. Call `TryMove` only from a current, adjacent, available node. `OverworldMovementModel` rejects stale, duplicate, non-adjacent, settled, locked and re-entrant commands atomically. Presentation owns the 0.3-second Sine EaseOut marker/camera interpolation and locks map input while a move or transition is active. `ArrivalCommitted` is the only handoff for room identity; SceneFlow continues to consume the existing typed payload/outcome contracts.

## Theme contract

`Resources/UIThemes/TimeKeyDefaultUiTheme.asset` is the formal default resource. `UiThemeValidator` is Editor-only and must report missing style IDs, missing Silver font, missing sprites and invalid 9-slice borders. `UiThemeBinder` is idempotent and applies only explicit local overrides. All player-visible text uses Silver and its attribution file. ARK Pixel and unverified Godot UI images remain audit references only.

## Verification commands

Use the Unity 6000.4.10f1 D3D12 batch commands from `04-verification/evidence/overworld-movement-theme-gate-d/verification-summary.md`. The authoritative artifacts are the full EditMode/PlayMode XML, targeted visual XML, SceneFlow roundtrip XML, `authoring.log`, `build-player.log`, `player-smoke.txt` and the three viewport plus resize PNGs in the same directory. Run `git diff --check` and the cached-file audit before any checkpoint.

## Known boundary

The next map stage must extend the same `MapNodeId` contract into deterministic chapter topology and versioned save migration. It must not reintroduce `OverworldNodeId`, a second global state machine, a Service Locator, or runtime creation of stable Scene/Prefab hosts.
