# Combat Shell Gate D authoring

Run the idempotent authoring entry from Unity or batch mode:

```text
TimeKey/Migration/Author Combat Shell Gate D
TimeKey.Editor.CombatShellGateDAutomation.AuthorGateD
```

Set `TIMEKEY_REPOSITORY_ROOT` to the canonical repository root before using the batch entry. The authoring command reads the eight source tiles from `image/outscene_block/`, copies them into `Assets/_Project/Resources/Art/Shell/OutOfBattleShell/`, and never writes the Godot-side files.

## Know what the command owns

The command creates or refreshes:

- `Prefabs/Shell/OutOfBattleShell.prefab`
- `Prefabs/Shell/GameOver.prefab`
- `Scenes/Shell/OutOfBattleShell.unity`
- `Scenes/Shell/GameOver.unity`
- the single `CombatSceneNavigation` connection in `Scenes/VerticalSlice/CombatVerticalSlice.unity`
- the eight imported out-of-battle tile sprites and their import settings

It also reuses `CombatTopHUD.prefab`, `BG.png`, `titleBG.png`, and `Resources/Fonts/Silver.ttf`. Missing required assets fail authoring instead of creating placeholders.

## Keep the saved hierarchy intact

Each Gate D Prefab has an ordinary root with `CanvasGroup`, Presenter, and scene navigation. `GateDCanvas` is the only child Canvas and owns the full visual tree; the camera is a separate sibling under the Prefab root. Keep this arrangement. Putting the Canvas component on the content root can produce a valid-looking hierarchy that renders as an empty or black formal-scene capture.

The Canvas uses `Scale With Screen Size`, a 1920x1080 reference resolution, and balanced width/height matching. The content Scene stores one inactive Prefab instance under `SceneContentEntry`; Bootstrap binds the typed payload, enables the camera, and then reveals the content. Do not activate the content root early to work around a binding issue.

`OutOfBattleShell.prefab` saves the map background/tint, map decoration, shared top HUD, run context, one room, and the confirm/cancel row. `GameOver.prefab` saves the background, veil, defeat panel, title, detail, and return command. Runtime code changes state, text, and interaction only; it does not construct a replacement UI tree.

## Preserve the typed boundaries

- `OutOfBattleShellPresenter` consumes `OutOfBattleShellState` and emits only `OutOfBattleRoomConfirmationRequest(roomId)`.
- `OutOfBattleShellSceneNavigation` creates and retains one `CombatLaunchPayload`, locks the Presenter during transition, and restores the selected state after a failed transition.
- `CombatSceneNavigation` waits for authoritative defeat or victory plus reward claim, creates one typed `CombatOutcome`, and routes to the outcome target.
- `GameOverPresenter` accepts only a typed defeat outcome. `GameOverSceneNavigation` owns the request back to Main Menu.

Do not move payload creation into a Presenter, derive state from visible text, or add `SceneManager` calls to Presentation. Keep `roomId` (`combat-room-01`) and `battleTag` (`combat-vertical-slice`) stable unless the corresponding contract tests and route data change together.

## Diagnose common failures

- Blank or black UI: verify the root has no `Canvas`, there is exactly one child `GateDCanvas`, the camera is its sibling, and `SceneContentEntry` references that camera and the root `CanvasGroup`.
- Room stays unavailable: check that Bootstrap bound an `OutOfBattleShellState` and neither `SceneInputLockState` nor the navigation transition lock remains held.
- Room stays confirming after failure: inspect `LastResult`; the failure path must call `RejectPendingConfirmation` and release only the navigation-owned lock.
- Combat uses the fixture deck or seed: verify `SceneFlowStateStore.ActiveLaunch` exists before `CombatCompositionRoot` activates.
- Victory never returns: reward claim must complete before `CombatSceneNavigation` submits the outcome. Defeat must not wait for a reward.
- Game Over rejects data: only a typed defeat outcome is valid. Victory targets the out-of-battle shell.
- Duplicate input or audio behavior: content scenes must not add EventSystem, persistent Audio, Bootstrap, or Transition roots.

After authoring or layout changes, run `CombatShellGateDAssetTests`, the two formal round-trip tests, and the three-viewport visual test before the full EditMode and graphical PlayMode suites. Inspect the 1280x720 and 2560x1080 captures manually for clipping, overlap, black frames, and Silver glyph rendering. Windows build and actual Player smoke belong to the stage-wide Gate E pass.
