# Decoupling Domain/Application Audit

> Status: complete, read-only audit
> Date: 2026-08-01
> Branch/baseline: `unity_7.31` at `53567ea`
> Scope: Domain/Application call chains, Controller callers, change points, minimal contracts, characterization coverage, lifecycle, and asmdef direction
> Verification mode: source/XML inspection only; Unity was intentionally not run

## Executive finding

`TimeKey.Domain` is currently a valid pure C# leaf assembly: its asmdef has no references, sets `noEngineReferences=true`, and a source search finds no `UnityEngine` import. The current assembly graph is acyclic. The blocking maintainability issue is that no `TimeKey.Application` or Diagnostics boundary exists. `CardPlaySession` carries a narrow use-case state machine inside Domain, while `VerticalSliceController` owns the rest of the application workflow plus resource parsing, runtime scene/UI construction, input policy, fixed enemy intent, Domain mutation, and view synchronization.

This is not a line-count problem. The concrete coupling failures are:

- Presentation directly references Infrastructure and calls `CardJsonAdapter.Parse` and `Resources.Load` (`VerticalSliceController.cs:166-171`, `872-915`, `975-983`).
- Target validity and target type are encoded as Controller stable-ID branches (`202-204`, `208-230`, `268-307`, `336-382`).
- Timeline labels, colors, and resolved copy branch on `lighting` versus `earthquake` (`456-470`, `530-532`).
- The hand is rebuilt from two hard-coded definitions (`918-935`), so adding a normal card still requires Controller edits.
- Domain parses six effect kinds but `TimelineGrid.ApplyPlayerEffects` silently executes only Damage and Elevation (`TimelineGrid.cs:123-154`). A newly scheduled Recover/Built/Poison action would currently commit and resolve without an explicit unsupported-effect failure.
- Lifecycle idempotence is guarded only by `_generatedRoot != null` (`VerticalSliceController.cs:154-159`); the mutable dictionaries/lists and event subscriptions have no reset/unbind contract.

Recommended verdict for contract freeze: **CONCERNS, non-blocking for R1 after characterization tests are added**. Preserve all current public Controller methods as a facade, introduce one concrete pure-C# application session, and add interfaces only at the three real variation boundaries: card catalog, effect handler registry, and trace sink.

## Sources inspected

- All eight C# files under `unity/Assets/_Project/Runtime/Domain/`.
- `VerticalSliceController`, `CardHandHost`, targeting previews, `HexTileColumn`, `BoardTileView`, camera/target views, `CardJsonAdapter`, and `VerticalSliceAutomation`.
- All six project/test/editor asmdefs.
- `CombatVerticalSlice.unity`; it contains only `VerticalSliceRoot` plus `VerticalSliceController` and two serialized TextAssets. No `.prefab` exists under `unity/Assets/_Project` at this baseline.
- EditMode/PlayMode/Infrastructure test sources and the final Wave 02B2A XML (`67/67` EditMode and `25/25` PlayMode, both zero failures).
- Current integration contracts, target architecture, Wave 02B1/02B2A Domain reports, current status, and final Wave 02B2A verification summary.

## Current execution chain

### Initialization

1. `CombatVerticalSlice.unity` instantiates `VerticalSliceRoot`.
2. `VerticalSliceController.Awake()` calls `BuildSceneGraph()` (`103-106`). The Editor harness and PlayMode tests also call the same public method.
3. `BuildSceneGraph()` parses `lightingFixture` and `earthquakeFixture`, constructs `CombatSliceState`, `CombatBoardState`, and `TimelineGrid`, then creates `GeneratedSlice`, preview components, world, interface, and fixed enemy intent (`154-181`).
4. `BuildWorld()` loads models/art, creates camera/lights/ground, generates 19 tile objects, mirrors their starting logical layers into `CombatBoardState`, and creates the fixed target (`559-634`, `738-754`).
5. `BuildInterface()` creates EventSystem/Canvas/HUD/timeline/hand/resolve UI and subscribes all callbacks (`636-736`, `756-776`).
6. `PlaceEnemyIntent()` constructs a fixed `TimelineAction`, commits it directly to `TimelineGrid`, and directly colors its UI cell (`802-825`).

### Card command path

```text
CardHandView input
  -> CardHandHost event
  -> VerticalSliceController.SelectCard(stableId)
  -> new CardPlaySession(CardDefinition)
  -> Presentation hand/camera/status mutation

world click / test facade
  -> SelectTarget(entity id) OR SelectEarthquakeTarget(HexCoord)
  -> CardPlaySession.SelectTarget(targetId, coord)
  -> BoardRangePreview.Show

timeline hover
  -> PreviewTimelineSelected
  -> CardPlaySession.PreviewTimeline
  -> TimelineGrid.CanPlace(TimelineAction.FromCard(...))
  -> TimelinePlacementPreview.Show

timeline click
  -> TryPlaceSelected
  -> PreviewTimeline + Commit
  -> TimelineGrid.TryPlace
  -> timeline/hand/camera Presentation mutation

resolve
  -> VerticalSliceController.ResolveTimeline
  -> TimelineGrid.Resolve(CombatSliceState)
  -> ApplyDamage OR CombatBoardState.TryApplyElevation
  -> ResolutionSnapshot
  -> ApplyTileEffects + HP/HUD/material/status mutation
```

The preview and commit legality source is correctly singular: `CardPlaySession` delegates both to `TimelineGrid` (`CardPlaySession.cs:87-138`; `TimelineGrid.cs:38-76`). That invariant should be retained.

## Controller caller surface

| Public surface | Runtime caller | Test/harness caller | Ownership after decoupling |
| --- | --- | --- | --- |
| `BuildSceneGraph()` | `Awake`, `EnsureBuilt` | `CombatVerticalSliceTests`, `VerticalSliceAutomation` | Compatibility facade; validate serialized references and initialize dynamic state only |
| `SelectCard(string)` | hand `CardSelected` event; Player smoke | PlayMode tests; harness | Delegate to Application, then present returned state |
| `SelectTarget(string)` | world raycast for `lighting`; Player smoke | PlayMode tests | Compatibility mapping to typed entity target |
| `SelectEarthquakeTarget(HexCoord)` | `SelectTile` while earthquake selected | PlayMode tests; harness | Compatibility mapping to typed tile target |
| `PreviewTimelineSelected(int,int)` | EventTrigger pointer enter | PlayMode tests; harness | Delegate pure preview result; Presentation only renders it |
| `TryPlaceSelected(int,int)` | timeline button; Player smoke | PlayMode tests; harness | Preserve combined preview+commit facade; Application owns both commands |
| `CancelSelectedCard()` | hand cancel event | indirectly exercised by PlayMode | Delegate to Application; clear views from result |
| `ResolveTimeline()` | resolve button; Player smoke | PlayMode tests; harness | Delegate to Application; Presenter consumes snapshot/result |
| `TrySelectWorldAtScreenPoint` / `IsScreenPointOverInterface` | `Update()` | PlayMode tests; harness | Presentation input coordinator; no Domain rule |
| `SetBoardView` / `GetTileColumn` | none in gameplay | tests and harness | Presentation diagnostics; keep while evidence depends on it |
| state/view properties | UI/tests/harness | tests and harness | Read from Application read model or serialized view references |

Compatibility therefore does not require keeping Controller internals. It requires keeping method names, return semantics, constants, and observable properties until tests/harness are migrated.

## Coupling matrix

| Concern | Current symbol/owner | Current dependencies | Coupling symptom | Minimum target |
| --- | --- | --- | --- | --- |
| Card data loading | `VerticalSliceController.BuildSceneGraph`, `CardJsonAdapter` | Presentation -> Infrastructure -> Domain | TextAssets and parser are known by the gameplay facade | Composition obtains an `ICardCatalog` and passes definitions into Application |
| Selected-card session | Controller fields plus Domain `CardPlaySession` | Presentation mutates Domain session directly | Use-case state is split across `_selectedCard`, `_selectedTargetId`, `_playerCell`, `_cardPlaySession` | One concrete `CombatApplicationSession` owns command order and read model |
| Target policy | `SelectTarget`, `SelectEarthquakeTarget`, `TrySelectWorldAtScreenPoint` | stable-ID string branches | Every new target mode edits Controller | Typed `CombatTarget` plus handler/target metadata; Controller only converts raycast hits |
| Placement legality | `CardPlaySession` + `TimelineGrid` | Domain only | Correctly centralized already | Retain Domain legality; Application exposes result unchanged |
| Effect resolution | `TimelineGrid.ApplyPlayerEffects` | Domain enum switch | Unsupported parsed effects silently no-op; every effect edits grid | Registered pure `ICardEffectHandler` keyed by `CardEffectKind`, fail fast when missing |
| Enemy intent | Controller `PlaceEnemyIntent` | Presentation creates Domain action and UI | Fixed enemy action and its view are one method | Inject initial/domain actions into Application; Timeline Presenter renders read model |
| Board state vs view | `CombatBoardState` and Controller `ApplyTileEffects` | Domain result consumed by Presentation | Acceptable direction, but Controller owns synchronization | Board Presenter consumes immutable `EffectResults`; Domain never sees views |
| Card hand | Controller `RefreshHand` | two hard-coded cards/sprites | Third card edits Controller | CardHand Presenter maps catalog/read model to `CardViewModel` list |
| UI/status | Controller `BuildInterface`, `SetStatus` | rules, fixed strings, uGUI construction | Layout and workflow copy are inseparable | Serialized views plus dedicated HUD/Timeline/CardHand presenters |
| Diagnostics | ad hoc `Debug.Log` only | Controller/Editor | No structured command/failure/effect trace | Optional `ICombatTraceSink`; no-op default and collecting test sink |
| Lifecycle | Controller `Awake`/`OnDestroy` | generated children and event callbacks | No explicit initialize/dispose/unbind transition | Composition `Initialize()` once, `Dispose()` once; symmetric subscriptions |

## Change-point audit

These are minimum production touch counts under the current design; content, tests, harness, evidence, scene, and `.meta` files add to each total.

| Requested change | Minimum production files today | Concrete coupling points |
| --- | ---: | --- |
| Add a normal card using an existing effect/target mode | 2 plus assets (`VerticalSliceController.cs`, scene YAML) | serialized fixture fields; parse/add dictionary; `RefreshHand`; stable-ID target/status/label/result branches; harness expectations |
| Add a new effect kind | 3, normally 4-5 | `CardDefinition.cs` enum/payload validation; `CardJsonAdapter.cs` type mapping; `TimelineGrid.cs` resolution switch; often `CombatSliceState.cs` and `ResolutionSnapshot.cs` |
| Add a meaningful second enemy/intent | at least 3 | Controller world construction and `PlaceEnemyIntent`; single-target `CombatSliceState`; snapshot/UI model. Current enemy action only toggles a boolean and applies no behavior |
| Change HUD/timeline composition | 1 large production file | `BuildInterface`, helper constructors, event binding, state strings, timeline coloring all reside in `VerticalSliceController.cs`; PlayMode and harness also change |

Specific stable-ID branch sites in `VerticalSliceController.cs` are `202-204`, `208-230`, `268-307`, `336-382`, `456-470`, `530-532`, and the hard-coded two-card projection at `918-935`. This is the concrete surface that must disappear before the remaining-card Prompt.

## Initialization and disposal audit

### What is currently safe

- Recalling `BuildSceneGraph()` while the original `_generatedRoot` is alive returns immediately, so the existing `SceneBuild_IsCompleteAndIdempotent` test passes.
- `CardHandHost.Build()` reuses existing `CardHandView` objects and subscribes each view once at creation (`CardHandHost.cs:36-100`, `186-194`); component tests cover repeated Build.
- Controller destroys its runtime-owned materials and runtime-created sprites in `OnDestroy()` (`VerticalSliceController.cs:1074-1097`). Loaded textures and prefabs are not incorrectly destroyed.
- Targeting previews clear active property blocks/colors in `OnDisable()`.

### Gaps and failure modes

- `_timelineButtons`, `_tiles`, `_columns`, `_cards`, and `_cardSprites` are never cleared. If the generated root is destroyed/replaced and initialization is retried, dictionary `.Add` calls and stale references can fail or duplicate state.
- Controller subscribes a lambda plus two named handlers to `CardHandHost`, timeline/resolve button listeners, and EventTrigger callbacks (`694-717`, `735`, `756-776`) without a symmetric unbind path. Destruction of the entire generated hierarchy currently masks this issue; serialized stable views will outlive Controller disable/re-enable and make it observable.
- `Awake()` initializes, `Start()` may run the smoke scenario, and `EnsureBuilt()` can initialize lazily. There is no explicit state (`Uninitialized/Initialized/Disposed`) or protection against initialize-after-dispose.
- Missing fixtures throw only during `BuildSceneGraph`; there is no `OnValidate` reference report. After R1, missing serialized views/prefabs need an error that names the field and scene object.
- The current idempotence test only compares root child count after an immediate second call. It does not assert listener counts, domain state identity, enemy-intent count, disable/enable behavior, teardown/reinitialize behavior, or the absence of stable-node generation.

Recommended lifecycle contract: scene composition owns a single `Initialize()`/`Dispose()` pair; `OnEnable` may bind and `OnDisable` must unbind if stable views persist. Calling `Initialize()` twice must be a no-op (or an explicit failure before side effects), and `Dispose()` must be idempotent. The compatibility `BuildSceneGraph()` should call reference validation plus `Initialize()` only.

## Current asmdef direction

```text
TimeKey.Domain                 -> []                         (noEngineReferences=true)
TimeKey.Infrastructure         -> Domain
TimeKey.Presentation           -> Domain, Infrastructure
TimeKey.Editor                 -> Domain, Infrastructure, Presentation
TimeKey.Tests.EditMode         -> Domain
TimeKey.Tests.Infrastructure   -> Domain, Infrastructure
TimeKey.Tests.PlayMode         -> Domain, Infrastructure, Presentation
```

There is no cycle. The two architectural deficiencies are the missing Application/Diagnostics assemblies and `Presentation -> Infrastructure`, which lets UI/composition code parse/load content directly.

Recommended acyclic direction:

```text
Domain                         -> []
Application                    -> Domain
Infrastructure                 -> Domain, Application        # implements Application-owned ports
Diagnostics                    -> Application, Domain        # optional sink implementation
Presentation                   -> Domain, Application         # no Infrastructure
Composition                    -> Application, Infrastructure, Presentation, Diagnostics
Editor                         -> Composition, Presentation
Tests.EditMode                 -> Domain, Application
Tests.Infrastructure           -> Domain, Application, Infrastructure
Tests.PlayMode                 -> Domain, Application, Presentation, Composition
```

Both `Domain` and `Application` should set `noEngineReferences=true`. Do not allow `Application -> Infrastructure`, `Domain -> Application`, or `Presentation -> Infrastructure`; those edges recreate the current coupling or form a cycle.

## Proposed minimum contract

### Concrete Application session, not an interface

Use one ordinary C# `CombatApplicationSession` (name may vary) constructed with card definitions, `CombatSliceState`, `TimelineGrid`, initial enemy actions, effect registry, and an optional trace sink. It owns selected card/session/target/timeline-origin/committed state and exposes:

```text
SelectCard(stableId) -> CombatCommandResult
SelectTarget(CombatTarget) -> CombatCommandResult
PreviewTimeline(TimelineCell) -> CombatCommandResult
CommitTimeline() -> CombatCommandResult
CancelCard() -> CombatCommandResult
ResolveTimeline() -> CombatCommandResult + ResolutionSnapshot
Current -> immutable CombatSessionView
```

`CombatCommandResult` should carry success/failure, session phase, selected stable ID, typed target, timeline origin, placement validity, and optional resolution snapshot. Presentation must not infer command success from strings or recompute legality. A concrete class is sufficient; an `ICombatApplicationSession` adds no current variation point.

### Typed target value

Introduce a pure value representing entity versus tile targets. Preserve `SelectTarget(string)` and `SelectEarthquakeTarget(HexCoord)` only as Controller facades that construct this value. This removes synthetic `"hex-q-r"` IDs and stable-ID checks from use-case code while keeping existing tests/harness callable.

### Three justified interfaces

1. `ICardCatalog`: external/configuration boundary. Consumer is Composition/Application; Infrastructure implements lookup/enumeration of parsed `CardDefinition`. It prevents Controller fixture fields and hand projection from growing per card.
2. `ICardEffectHandler`: actual rule extension point keyed by `CardEffectKind`. Consumer is the Domain resolver. Each handler declares supported target kind and returns structured effect results while mutating only Domain state. Missing registration must fail before resolution, never silently no-op.
3. `ICombatTraceSink`: external diagnostics boundary. Consumer/producer is Application; a no-op sink is default and a collecting sink supports tests. Trace payload contains command, phase before/after, stable ID, typed target, timeline cell, effect before/after, and failure reason. It must never be queried to decide gameplay.

Do not add interfaces for `TimelineGrid`, `CombatBoardState`, presenters, camera, clock, or every view. There is no current alternative implementation or test seam that justifies them.

### Effect/target compatibility note

`CardEffectKind` already includes Recover, Built, Poison, and Clear, but only Damage/Elevation execute. Before R3 or the remaining-card stage, resolve-time registration must be validated when the catalog is built. `Clear` remains a separate immediate timeline session and must not be forced through ordinary `TimelineAction`; its existing schema boundary remains valid.

## Characterization coverage

### Existing coverage that can freeze behavior

| Behavior | Existing evidence |
| --- | --- |
| Lighting select/target/place/resolve and damage clamp | `CardPlaySessionTests`, `TimelineGridTests`, `CombatVerticalSliceTests.CompletePath_ResolvesLightingBeforeEnemyIntent` |
| Preview legality has no side effect; commit is single mutation | `CardPlaySessionTests` and `TimelineGridTests` |
| Cancel/idempotent cancel and commit-after-cancel failure | `CardPlaySessionTests.Cancel_AfterPreviewIsIdempotentAndPreventsCommit`; Controller right-click integration |
| Two-card exclusive selection | `CardHandHostTests.SelectingSecondCard_CancelsFirstAndRestoresBothPoses`; `CombatVerticalSliceTests.TwoCardHand_UsesFrontImagesAndSwitchesSelection` |
| Drag phases and pointer consumption | `CardHandViewTests.Drag_EmitsPhasesConsumesInputAndRestoresPose`; `CardHandHostTests.DragEvents_BelongToOriginatingCardAndRestoreParent` |
| Range and valid/invalid timeline previews | targeting component tests plus `TargetAndTimelinePreview_DoNotMutateUntilCommitAndSurviveOrbitChanges` |
| Enemy intent after early player action | `TimelineGridTests.Resolve_ProducesFrozenFixtureSnapshot`; Controller complete path |
| Earthquake seven-grid +2 and real presentation | `EarthquakeResolutionTests`; `Earthquake_RaisesSevenRealColumnsAndKeepsCardinalSelection`; `HexTileColumnTests` |
| Four-yaw selection | Controller PlayMode tests and final harness summary |
| Component-local repeated Build | CardHandView/CardHandHost/HexTileColumn component tests |

The final inherited XML is trustworthy for this baseline: EditMode `67/67`, PlayMode `25/25`, both zero failures. Any R2 change to Domain/Application invalidates at least the EditMode portion and the Controller integration subset; any R1 scene/presenter change invalidates the full PlayMode and visual/build evidence.

### Required gaps before structural change

1. **Serialized pre-Play structure:** assert Camera, Canvas, EventSystem, HUD, Timeline, CardHandHost, BoardRoot, both previews, and required prefab references exist before runtime initialization.
2. **Stable-node non-generation:** call compatibility `BuildSceneGraph()` and assert no stable node, listener, or timeline cell is created/reparented/reordered.
3. **Lifecycle:** initialize twice, disable/enable, dispose twice, and initialize-after-dispose; assert one enemy intent, one subscription per view event, and no duplicate dynamic board/card content.
4. **Missing reference diagnostics:** each required serialized dependency produces a field/object-specific failure before partial state mutation.
5. **Pure Application command sequence:** unknown card, target-before-card, wrong target kind, preview-before-target, invalid preview, concurrent grid mutation, repeated preview/commit/resolve, cancel at every non-committed phase, and deterministic seed.
6. **Card switching at Application level:** selecting a second card cancels/replaces the first session without Presentation events being required.
7. **Unsupported handler fail-fast:** a parsed Recover/Built/Poison card cannot commit/resolve as a silent no-op when its handler is absent.
8. **Effect failure atomicity:** characterize whether an exception/missing handler leaves the timeline and partially mutated state intact; choose and document one behavior before multi-effect cards.
9. **Enemy intent source:** repeated initialization and resolution preserve exactly one intent and the frozen column-then-row order.
10. **Facade equivalence:** existing Controller public calls return the same bool/exception/snapshot semantics after delegating to Application.
11. **Trace neutrality:** no-op and collecting sinks produce byte/value-equivalent snapshots; failed commands record a reason without mutating state.

## Priority risks

| Priority | Risk | Impact | Required mitigation |
| --- | --- | --- | --- |
| P0 | Parsed but unhandled effects silently resolve as no-op | Remaining cards can appear playable while violating Godot behavior | Validate handler registration and fail before commit/resolve |
| P0 | R1 serializes views without symmetric event unbinding | Disable/enable or reinitialize can duplicate commands and placement | Add lifecycle characterization and bind/unbind owner before scene conversion |
| P1 | Target rules remain stable-ID branches | Each new card reopens Controller and duplicates target policy | Typed target + effect/target metadata in Application/Domain |
| P1 | Presentation depends on Infrastructure | Resource/parsing changes force UI/controller changes and block test substitution | Move catalog port to Application and parsing/loading to Infrastructure/Composition |
| P1 | `CombatSliceState` models one target and enemy intent has no behavior | Add-enemy guide cannot truthfully describe a small extension path | Document current one-target boundary; do not generalize until an actual enemy slice requires it |
| P2 | Current idempotence is root-existence-only | Destruction/replacement leaves stale collections and `.Add` failures | Explicit state and reset/dispose contract |
| P2 | Synthetic tile target string and duplicated `_selectedTargetId` | String formatting leaks into Domain action and state can diverge | Typed target; derive compatibility ID only at facade/snapshot edge |

## Recommended ownership paths

### Application/Diagnostics writer

Own exclusively:

- `unity/Assets/_Project/Runtime/Application/**`
- `unity/Assets/_Project/Runtime/Diagnostics/**` only if the sink implementation is separated
- `unity/Assets/_Project/Tests/EditMode/Application/**`

Deliver the pure session, immutable command/read models, typed target, trace port/no-op/collector, and characterization tests. Do not edit Controller, scene, Presentation, Infrastructure, shared asmdefs, or Git state.

### Domain effect writer

If separated from the Application writer, own:

- new effect-resolution files under `Runtime/Domain/Effects/**`
- new focused tests under `Tests/EditMode/Effects/**`

Do not change existing Domain files until the main integrator freezes handler construction and failure behavior; `TimelineGrid.cs` is a shared integration point and should remain main-agent owned unless explicitly handed over.

### Infrastructure writer

Own `Runtime/Infrastructure/Cards/**` and `Tests/Infrastructure/**`: implement the catalog/resource adapter from `FrontImage` and real fixtures. Do not reference Presentation or decide target/effect behavior.

### Main integrator

Retain exclusive ownership of:

- `VerticalSliceController.cs` and its public facade
- all asmdefs and new assembly wiring
- shared scene/Prefab/composition assets
- `TimelineGrid.cs` integration if handlers are introduced
- Editor harness, shared integration tests, final evidence, shared documents, staging/commit/push

## Recommended order

1. Add the lifecycle/facade/unsupported-effect characterization tests without moving responsibilities.
2. Complete R1 scene/Prefab serialization while keeping the current Controller command behavior behind the facade.
3. Add `TimeKey.Application` and move command orchestration behind the facade; keep `CardPlaySession` behavior stable during the first pass.
4. Introduce catalog and trace ports, then remove `Presentation -> Infrastructure`.
5. Introduce the effect handler registry only when its fail-fast contract and existing Damage/Elevation handlers are green; do not mix Recover/Built/Poison implementation into the decoupling checkpoint.
6. Refresh affected EditMode/PlayMode/visual/build evidence after each slice.

No user decision is required for these boundaries. The only product-sensitive future boundary is widening `CombatSliceState` from one fixed target to a true enemy collection; defer that until an actual enemy behavior slice supplies authoritative Godot requirements.
