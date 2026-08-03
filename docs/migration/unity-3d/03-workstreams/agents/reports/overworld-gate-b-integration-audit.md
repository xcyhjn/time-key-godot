# Overworld Gate B Integration Audit

## Scope and notation

This was a read-only audit of the existing Unity and Godot call sites, with this report as
the only write. No Unity process was started; no source, Scene, Prefab, test, evidence,
shared document, Git index, branch, commit, or remote state was changed.

- **Evidence** means the behavior is directly present in the cited source.
- **Recommendation** means the minimum Gate B integration ordering inferred from those
  sources and the frozen Gate B requirements.

## Existing integration call sites

| Boundary | Repo-relative path and symbol | Evidence | Gate B integration point |
| --- | --- | --- | --- |
| Menu command source | `unity/Assets/_Project/Runtime/Presentation/MainMenu/MainMenuPresenter.cs` `OnEnable`, `SetContinueAvailable`, `RequestNavigation` | `OnEnable` forces Continue unavailable at line 159. No production caller of `SetContinueAvailable` exists. | `MainMenuSceneNavigation.OnEnable` must query the production save repository and set Continue availability from a validated save, not file existence alone. |
| New/seed/continue payload creation | `unity/Assets/_Project/Runtime/Composition/SceneFlow/MainMenuSceneNavigation.cs` `NavigateToShellAsync`, `CreateRunStartPayload` | All three commands enter the same method. `Continue` currently falls through to a default-seed `RunStartKind.NewGame` payload. | Load/migrate/validate Continue here, then inject the restored typed run/map through the existing `BootstrapRoot.TransitionAsync`; do not load `OutOfBattleShell` directly. |
| Single SceneFlow owner | `unity/Assets/_Project/Runtime/Composition/SceneFlow/BootstrapRoot.cs` `ReserveTransitionSequence`, `TransitionAsync`; `unity/Assets/_Project/Runtime/Application/SceneFlow/SceneFlowCoordinator.cs` `TransitionAsync` | Bootstrap owns the one coordinator and the global transition sequence. The coordinator owns route, replay, stale, busy, phase, and recovery decisions. | Keep every new/seed/continue/combat return route on this owner. Persistence and Overworld Application are participants in `SceneFlowStateStore`, not scene loaders. |
| Allowed payload routes | `unity/Assets/_Project/Runtime/Application/SceneFlow/SceneFlowCoordinator.cs` `IsAllowedRouteAndPayload` | Only empty start/menu, `RunStartPayload`, `CombatLaunchPayload`, and `CombatOutcome` routes are accepted. | Extend the existing typed route only if Continue needs a distinct resume payload. Event/Shop outcomes can remain in-shell Application commands unless a real separate Scene is introduced later. |
| Transactional in-memory state | `unity/Assets/_Project/Runtime/Composition/SceneFlow/SceneFlowStateStore.cs` `Record`, `Commit`, `Rollback` | `Record` builds a copied next `OutOfBattleShellState` and retains the prior state in `PendingRecord`; `Commit` drops the prior record; `Rollback` restores it. | Add the sole Overworld Application/session snapshot and persistence transaction token to this same pending record. Do not create another persistent MonoBehaviour store. |
| Binding phase | `unity/Assets/_Project/Runtime/Composition/SceneFlow/UnitySceneFlowEffects.cs` `ExecuteAsync(BindingPayload)` | `stateStore.Record(request)` runs before `_targetEntry.Bind(request.Payload)`. A failure is still pre-commit. | Validate identity, apply the room outcome to copied run/map state, build and validate the complete save candidate, and prepare a temporary save here. No durable partial fields. |
| Current commit point | `unity/Assets/_Project/Runtime/Composition/SceneFlow/UnitySceneFlowEffects.cs` `UnloadAsync` | The code starts source unload, calls `stateStore.Commit`, then awaits unload. The coordinator marks `sourceCommitted=true` only after the whole `UnloadingSource` effect succeeds. | Reorder this boundary for Gate B: prepare all fallible data earlier, atomically promote the complete save before starting source unload, retain rollback backup until unload succeeds, commit the no-fail in-memory record after unload, then discard the backup. |
| Transition recovery | `unity/Assets/_Project/Runtime/Application/SceneFlow/SceneFlowCoordinator.cs` `RecoverAsync` | Failures through `UnloadingSource` use `RollingBackTarget -> RestoringSource -> Revealing -> InputUnlocked`; reveal/input failures keep the target and retry reveal/unlock only. | Persistence rollback and Overworld rollback must join `RollingBackTarget`. Anything after the durable/source commit boundary must be non-mutating and recover on the target. |
| Cover and source restoration | `unity/Assets/_Project/Runtime/Composition/SceneFlow/UnitySceneFlowEffects.cs` `RollBackTargetAsync`, `RestoreSource`; `TransitionCanvasPresenter.SetCovered` | Rollback unloads the target, re-enables the source camera, makes the source active, reveals the transition cover, and unlocks input. | Reapply the source presentation snapshot after restoring the state. Keep cover/reveal on the existing transition presenter. |
| Focus ownership gap | `unity/Assets/_Project/Runtime/Composition/SceneFlow/PersistentInputGate.cs` `SetLocked`; `unity/Assets/_Project/Runtime/Composition/SceneFlow/SceneContentEntry.cs` `DefaultFocusId` | Locking clears `EventSystem.current` selection. `DefaultFocusId` is stored but never consumed. `RestoreSource` does not restore focus. | Capture source selection before clearing it; on rollback restore that live source object (or its stable focus ID). On success select the target default only after reveal completes and immediately before input unlock. |
| Map arrival | `unity/Assets/_Project/Runtime/Presentation/OverworldMovement/OverworldMovementPresenter.cs` `AnimateMove`; `OutOfBattleShellSceneNavigation.OnArrivalCommitted` | Movement commits first, then raises `ArrivalCommitted`. Navigation directly mutates `OutOfBattleShellState.SetCurrentRoom`, outside `SceneFlowStateStore` and without revision/idempotency. | Replace the direct mutation with one typed Overworld Application enter/select command. The animation model remains presentation-time movement, not the chapter lifecycle authority. |
| Combat preparation | `unity/Assets/_Project/Runtime/Composition/SceneFlow/OutOfBattleShellSceneNavigation.cs` `LaunchCombatAsync` | `OutOfBattleShellState.CreateCombatLaunch` supplies launch/run/room/battle identity. Failure unlocks presenters and rejects confirmation, but `PreparedLaunch` remains cached. | Create the launch from the Gate A active node and the existing run projection. Reuse `PreparedLaunch` only for an exact transient retry; clear it after a typed conflict or a different selection. |
| Reward claim | `unity/Assets/_Project/Runtime/Presentation/VerticalSliceController.cs` `HandleBattleRewardRequested`; `unity/Assets/_Project/Runtime/Domain/BattleFlow/BattleSettlementState.cs` `TryClaimReward` | Reward claim is idempotent by its own command sequence and is required before a victory outcome can be created. | Reward claim stays inside Combat. It must precede return payload construction. The durable save must contain both the claimed reward effects and the settled/unlocked room in one envelope. |
| Combat return boundary | `unity/Assets/_Project/Runtime/Composition/SceneFlow/CombatSceneNavigation.cs` `SubmitTerminalOutcomeAsync` | The controller builds `BattleReturnPayload`; `CombatOutcome.TryCreate` verifies launch/tag/seed/reward; the same Bootstrap route returns to shell or GameOver. | Consume this unchanged typed outcome. During `SceneFlowStateStore.Record`, apply it once to both the legacy run projection and Gate A Application state using the same identities. |
| Outcome replay/conflict | `unity/Assets/_Project/Runtime/Application/SceneFlow/OutOfBattleShellState.cs` `TryApplyOutcome`; `SceneFlowStateStore.RecordOutcome` | Exact `CombatOutcome.Fingerprint` replay is accepted and marked `WasAlreadyApplied`; mismatched launch/run/room or changed outcome identity fails. | Persist enough replay identity to preserve this guarantee after Continue. Do not regenerate outcome or launch correlation IDs on retry. |
| Defeat cleanup | `unity/Assets/_Project/Runtime/Composition/SceneFlow/GameOverSceneNavigation.cs` `ReturnToMainMenuAsync`; `SceneFlowStateStore.Record` GameOver-to-MainMenu branch | The defeat outcome reaches GameOver; in-memory run state is cleared only on the later GameOver-to-MainMenu transition. | Atomically retire/delete the resumable save when the defeat transition commits. If that transition rolls back, restore the prior save. The later menu transition clears memory and must remain idempotent. |
| Gate A authority | `unity/Assets/_Project/Runtime/Domain/Overworld/OverworldChapterState.cs` `EnterRoom`, `ResolveRoom`, `Snapshot` | It owns adjacency, active-room mutex, revision, operation replay/conflict, visited/settled sets, and one-time Boss advancement. | Application coordinates this class; `OutOfBattleShellState` and `OverworldMovementModel` become projections/interaction helpers, not competing room state machines. |
| Current presentation restore | `unity/Assets/_Project/Runtime/Presentation/OverworldMovement/OverworldMovementPresenter.cs` `ApplySceneFlowState`, `Rebind` | It restores only current and settled IDs into a fixed three-node axial model. It loses Gate A topology, active room, visited set, revision, and journal. | Do not use this method as the Gate A persistence restore API. Gate C should project the authoritative chapter snapshot into runtime node/edge views. |

## State and identity flow

### Required durable identities

| Stage | Fields that must remain stable |
| --- | --- |
| Run creation/resume | schema version, game version, run ID, run seed, seed text/kind, character stable ID, chapter number, era, phase, timecoins, ordered deck stable IDs, map generator/config version. |
| Map topology/state | map seed, chapter, stable `MapNodeId` values, edges or a verified deterministic topology fingerprint, revision, current node, active-room flag/node, visited IDs, settled IDs, chapter-completed/final-run state, chapter advance count. |
| Operation replay | next Overworld operation sequence plus the committed enter/resolve replay identities needed to distinguish exact replay from `SequenceConflict`. A high-water mark alone is insufficient if exact replay must survive restart. |
| Combat launch | launch correlation ID, run ID, room/node ID, run seed, chapter, character ID, era, phase, timecoins, ordered deck IDs, battle tag, battle seed. |
| Reward/outcome | reward entry stable ID and claimed/effect snapshot, outcome correlation ID, launch correlation ID, outcome fingerprint, battle outcome, returned era/phase/timecoins/deck, room ID. |
| Save transaction | save revision/ETag or equivalent expected revision, prior envelope identity, candidate envelope checksum, and transaction state sufficient to restore the prior file after a pre-commit failure. |

### Authoritative flow

1. New/seed: `MainMenuSceneNavigation` creates the run identity and Overworld Application
   state, then sends one typed payload through Bootstrap.
2. Continue: the same navigation loads, migrates, validates, and reconstructs the run/map,
   then sends it through the same Bootstrap route. No direct SceneManager call.
3. Map selection: movement presentation commits arrival, then a typed Application command
   enters the exact adjacent Gate A node and makes it the active room.
4. Battle/Boss launch: Application projects that active node into the existing
   `CombatLaunchPayload`; `SceneFlowStateStore.Record` records it transactionally.
5. Victory: Combat claims reward once, builds `BattleReturnPayload`, and creates the
   existing `CombatOutcome` with the original launch identity.
6. Return binding: a copied Application state applies the outcome, resolves the exact
   Gate A active node once, unlocks successors or advances/generates the next chapter,
   and builds one complete save candidate containing reward plus room/map results.
7. Commit: the save candidate is atomically promoted and the source Scene is unloaded;
   only then does the pending in-memory state become authoritative. Reveal completes,
   target focus is selected, and input unlocks last.
8. Exact replay returns the already committed result and performs no second reward,
   settle, chapter advance, or save revision. Same identity with changed data is a typed
   conflict and leaves all state/file/UI unchanged.

## Safest ordering

**Recommendation:** the minimum ordering that satisfies the no-partial-state requirement
is:

```text
reward claim in Combat
-> construct and validate CombatOutcome
-> clone current run/map state
-> apply identity-matched Gate A room resolution
-> apply returned deck/economy/clock values to the same candidate
-> generate/restore next chapter if Boss victory requires it
-> serialize + validate complete SaveEnvelope to a temporary file
-> load/bind target and prove first renderable frame
-> atomically replace the save while retaining a rollback backup
-> unload source Scene without cancellation
-> commit the pending in-memory state and discard backup
-> reveal target to completion
-> restore/select focus
-> unlock input
```

For NewGame/SeedGame, the same transaction prevents a failed scene transition from
destroying an older valid Continue save. For Defeat, the candidate operation is an atomic
save retirement; rollback restores the old save if GameOver loading/binding fails.

## Rollback matrix

| Failure point | Current behavior | Required Gate B state/file recovery | Required visual/input recovery |
| --- | --- | --- | --- |
| Request identity/route validation | No effect phase runs. | No map or save mutation. | Source selection and input stay unchanged. |
| Input lock / cover / target load / activation | Coordinator rolls back target and restores source. | No save candidate promotion; discard any temp file. | Reveal existing source, restore captured focus, unlock. |
| Binding before/inside `stateStore.Record` | Pending legacy state can roll back. | Roll back pending Gate A/Application state and discard temp save. | Unload target, restore source camera/active Scene, selected node/confirmation/focus, cover, input. |
| First-render check | Same pre-commit recovery. | Same rollback; candidate remains non-durable. | Same recovery; do not leave a hidden or duplicate content entry. |
| Save prepare/validation/write-temp | Not currently present. | Preserve previous save byte-for-byte; delete only the failed temp. | Stay on source and restore interaction/focus. |
| Atomic promote before source unload | Current phase model has no persistence compensation. | Keep a rollback backup/token until source unload succeeds; on failure restore the prior file and pending map. | Restore source/cover/focus/input. |
| Source unload | Current code calls `stateStore.Commit` before await, while the coordinator still classifies an exception as pre-commit. This is an ownership mismatch. | Move the no-fail in-memory commit after successful unload. An ambiguous unload must retain enough persistence/state compensation to recover or be explicitly classified post-commit. | Never allow both content Scenes interactive. If target is authoritative, finish source cleanup before unlock. |
| Reveal or input unlock after commit | Coordinator keeps target and retries reveal/unlock. | Do not roll back reward/map/save; they are already one committed transaction. | Complete reveal, select target default focus, unlock once. Surface the original failure without duplicating Bootstrap/content. |
| Navigation callback/cancellation | Out-of-battle and combat navigation clear `_inFlight`; some cached prepared payload remains. | Exact cached identity may retry; typed conflict or changed room must clear it. | Reopen the prior confirmation/settlement focus and clear local transition locks. |

## Gate A restore conflicts

### Hard public-API conflict

`OverworldChapterState` has only `OverworldChapterState(OverworldMapDefinition map)`, which
always initializes at the entry with revision zero. Its snapshot constructor is internal,
there is no restore factory/constructor, and its private `_journal` is not exposed. Gate B
cannot honestly restore revision/current/active/visited/settled/chapter/journal state from
a save using the current public API.

Minimum resolution owned by the primary agent: add a validated Domain restore contract
that reconstructs the existing state machine, including replay state, without public
setters or a second implementation. Invalid node IDs, topology mismatch, impossible
active/current combinations, negative revisions, and inconsistent chapter completion must
fail before mutating live state.

### Identity-space conflict with the current formal shell

Gate A generates IDs in the form
`chapter-01-layer-00-node-00`, while the saved OutOfBattleShell Prefab currently contains
the fixed presentation IDs `room-01` and `room-02` (for example lines 3176 and 3290).
`OverworldMovementPresenter.ApplySceneFlowState` silently falls back to its first fixed
node when the restored ID is absent.

Gate B must not add an ID translation dictionary that becomes another owner. Until Gate C
runtime presentation is authored, additive Gate B tests should drive a Gate A generated
node identity through Application/SceneFlow directly. Gate C must instantiate views from
the authoritative definition with the exact `MapNodeId` values.

### Duplicate state ownership hazards

- `OutOfBattleShellState.CurrentRoomId` and `SettledRoomIds` overlap Gate A current/settled
  state. Keep them as a compatibility projection for existing combat payloads and assert
  equality at boundaries; do not issue independent mutations.
- `OverworldMovementModel` owns animation-time request/commit/cancel and fixed axial
  adjacency. It must not decide Gate A graph reachability or persistence.
- `OutOfBattleShellSceneNavigation.OnArrivalCommitted` currently mutates the legacy state
  directly. This bypasses both the Gate A revision journal and SceneFlow rollback.
- `PreparedLaunch` and `PreparedOutcome` are useful exact-retry identities but become
  hazards if reused after a different room selection or typed conflict.
- `SceneFlowStateStore.PendingRecord` is the correct single pending-state owner. A second
  save/session singleton would split commit/rollback authority.

## Continue and persistence evidence

- Unity currently has no run-save repository. The only production persistence in
  `MainMenuSceneNavigation` is settings via `PlayerPrefs`.
- Continue is permanently reset disabled by `MainMenuPresenter.OnEnable`; the navigation
  branch currently creates a new default-seed run.
- `CardJsonAdapter` demonstrates `JsonUtility` DTO parsing, but it is a static content
  adapter, not a save transaction pattern.
- The frozen architecture document requires a versioned `SaveEnvelope` and stable string
  IDs. Unsupported versions must fail explicitly.
- Godot `scene/global/Saver.gd` uses `user://save_0.cfg`, `ConfigFile`, and direct
  `config.save`. It has no schema version or atomic backup. It stores era, player hex,
  character/cut state, map seed/tier/initialized flag, JSON topology/features/path, and
  deck. Phase and timecoins are not actually written by `Save_game`.
- Godot Continue checks only file existence, ignores the boolean result of `Load_game`,
  and then transitions. This is observable legacy behavior but must not be copied: Unity
  Continue should require a successfully validated/migrated envelope and show a recoverable
  Chinese error for corrupt/future versions.
- A Godot CFG importer, if implemented, must be an Infrastructure adapter and must preserve
  the original CFG/backup. The current migration architecture explicitly does not promise
  bitwise topology compatibility, so it cannot claim an exact Gate A topology restore from
  Godot coordinates without a documented conversion decision.

## Observable Godot Event, Shop, Boss, and save behavior

| Area | Evidence | Safe compatibility claim for Gate B/C |
| --- | --- | --- |
| Event | `map_generator.gd` generates Event tiles; `out_scene_map_exp.gd` routes them to exported `res://event.tscn`, but that resource is absent in the repository. | Preserve typed Event enter plus Completed/Cancelled outcomes and save the result. Do not invent dialogue, rewards, or choices from a missing scene. |
| Shop | There is no overworld Shop tile in the Godot tile enum. `TRADE` is a feature on normal rooms. The implemented shop is a battle-settlement overlay: it consumes timecoins before purchase, adds the card to the deck after the flight completes, supports paid refresh/era upgrade, and exits back to Combat. | Gate A's `Shop` room may use typed Completed/Cancelled lifecycle, but its inventory/economy content is not Godot overworld evidence. Gate C may reuse only observed purchase/deck/timecoin semantics when a real Shop UI is authored. |
| Boss | A completed payload must identify `boss_stage`, match the current boundary Boss hex, and not exceed the final tier. Only then does `current_tier` advance and the next radius reveal/save. | Existing `CombatOutcome` plus Gate A Boss Victory can settle/advance exactly once. Defeat must not settle or advance. Final chapter needs an explicit typed terminal state rather than another implicit tier integer. |
| Combat return | Godot builds one payload containing completed result, battle tag/seed, era, timecoins, deck, and room context, stores it pending, covers the screen, then switches. | Unity already has a stronger typed equivalent. Continue using `CombatOutcome`; do not add Dictionary/object payloads. |
| Defeat | Godot displays GameOver and deletes slot 0 immediately. | Retire the Unity resumable save atomically with the committed defeat transition; preserve it on transition rollback. |

## Recommended primary-agent edit order

1. Review the Application and Persistence agent handoffs; freeze their primitive DTO and
   repository APIs before touching shared contracts.
2. Add the validated Gate A restore API and tests. This is a hard prerequisite for honest
   Continue and must not be simulated in Composition.
3. Extend the existing MainMenu-to-shell typed boundary for resume, including
   `RunStartKind.Continue` or a distinct typed resume payload, and update route validation.
4. Make `SceneFlowStateStore.PendingRecord` own the Overworld Application candidate and
   persistence transaction token alongside the existing legacy state.
5. Integrate new/seed/continue creation in `MainMenuSceneNavigation`; set Continue from a
   validated repository probe and preserve the prior save until transition commit.
6. Replace direct room mutation in `OutOfBattleShellSceneNavigation` with typed Application
   selection/enter and project the active Gate A node into the existing combat launch.
7. Apply CombatOutcome to legacy and Gate A candidate state in one `Record` operation;
   resolve Boss/next chapter/final state there and serialize one complete candidate.
8. Reorder `UnitySceneFlowEffects.UnloadAsync` and rollback hooks so state/save/source Scene
   share the commit boundary. Add focus capture/restore around `PersistentInputGate`.
9. Integrate defeat save retirement and GameOver-to-menu in-memory cleanup.
10. Run directed unit tests first, then the additive tests below. Do not touch Gate C
    dynamic map UI until Gate B functional rollback and Continue are closed.

## Tests that must be real additive PlayMode

Pure unit tests are necessary for DTO validation, migrations, deterministic generation,
Application replay/conflict, and atomic repository mechanics. They are insufficient for
these behaviors, which must use the saved Bootstrap and real additive Scenes:

1. MainMenu no-save/corrupt-save/valid-save states, including clicking Continue and
   arriving at OutOfBattleShell with the same run ID, chapter, current/active node,
   visited/settled IDs, era/phase/timecoins/deck, one Bootstrap, and one content entry.
2. NewGame and SeedGame through real menu buttons, proving deterministic map identity and
   no overwrite of an older save when loading/binding fails.
3. Generated adjacent Battle node -> existing Combat Scene -> reward click twice ->
   existing CombatOutcome -> shell, proving one reward, one settle/unlock, one save
   revision, original run/launch/room identity, reveal completion, focus, and unlocked input.
4. Exact outcome replay versus changed-outcome conflict through the real state store and
   SceneFlow, with no duplicate room settlement, chapter advance, save write, Bootstrap,
   EventSystem, AudioSource, transition canvas, or content Scene.
5. Injected persistence failure during prepare and atomic promote, plus target
   loading/binding/first-frame failure. Each must preserve the old save, pre-outcome Gate A
   active room/current node, source Scene, cover, selected focus, and input.
6. Failure during combat launch from OutOfBattleShell, proving the arrived map node remains
   authoritative, the active-room reservation rolls back as specified, confirmation/focus
   is usable, and a changed selection cannot reuse the old `PreparedLaunch`.
7. Boss victory round trip, proving one chapter advance, deterministic next map, committed
   save, and exact replay no-op; Boss defeat must reach existing GameOver without settling
   or advancing.
8. Defeat commit deletes/retires Continue save; injected GameOver load/bind failure restores
   it; successful Return-to-MainMenu clears in-memory run state and leaves menu interactive.
9. Bootstrap reload in the same PlayMode test process, proving repository handles are
   closed and Continue reconstructs the same committed state without duplicate persistent
   objects. Actual process restart remains a Gate D Player requirement.

Gate C must add real additive Event/Shop UI flows and corrupt-save Chinese recovery visuals;
those cannot be closed by Application unit tests or by the currently missing Godot Event
resource.
