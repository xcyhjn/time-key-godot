# Decoupling Agent B Report: Presentation Presenter and Lifecycle

> Agent: `decoupling-agent-b-presentation`
> Date: 2026-08-01
> Gate inherited: R2 `f11fb77` pushed; Application API frozen; no Unity Editor process at start
> Ownership status: all Agent B paths returned to the main integrator

## Delivered files

Production:

- `Runtime/Presentation/Presenters/CardHandPresenter.cs`
- `Runtime/Presentation/Presenters/BoardRangePresenter.cs`
- `Runtime/Presentation/Presenters/TimelinePresenter.cs`
- `Runtime/Presentation/Presenters/CombatHudPresenter.cs`
- `Runtime/Presentation/Bindings/CombatPresentationBinding.cs`
- matching Unity `.meta` files and folder metadata

Tests:

- `Tests/PlayMode/Bindings/CombatPresentationBindingTests.cs`
- `Tests/PlayMode/Presenters/CombatPresenterRefreshTests.cs`
- matching Unity `.meta` files and folder metadata

No Controller, existing Cards/Targeting/Terrain component, Application, Domain,
Infrastructure, Scene, Prefab, asmdef, harness, shared document, or protected user
file was modified.

## Lifecycle guarantees

- `Bind()` is idempotent on card-hand, timeline, HUD, and aggregate binding
  components.
- `Unbind()` is idempotent and removes every event/listener added by `Bind()`.
- `OnDisable()` unbinds symmetrically; enable after disable restores exactly one
  listener path.
- Explicit `Bind()` validates serialized dependencies with the owning component
  name and missing field name before subscription.
- `OnEnable()` binds only after serialized dependencies exist, so adding a new
  component in the Editor does not fail before the Inspector can be populated.
- Timeline cells are validated for null and duplicate coordinates before any
  listener is attached.

## State and rule boundary

`CombatPresentationBinding.Refresh(CombatSessionView)` fans one immutable
Application view state to four single-region presenters:

- card hand maps only `CombatSessionPhase` to interaction state;
- range preview consumes the typed target coordinate and
  `CardDefinition.Range`, and clears on commit/resolve;
- timeline preview consumes `TimelineOrigin`, `CardDefinition.Shape`, and
  `IsPlacementValid`;
- HUD consumes phase, typed target, and the frozen resolution snapshot.

`TimelinePresenter.RenderAction(...)` is a generic rendering entry point for a
composition-owned label/color descriptor. The production files contain no
`lighting`, `earthquake`, `CardEffectKind`, stable-ID switch, Infrastructure
reference, resource load, `GameObject.Find`, service locator, or runtime object
construction.

## Test coverage and local checks

The new tests cover:

- double bind produces one Resolve intent;
- unbind stops intent forwarding;
- disable/enable rebinds exactly once;
- missing aggregate dependency names `cardHandPresenter` and the owner;
- arbitrary entity-card and tile-card stable IDs still render one/two Timeline
  cells and one/seven range cells from typed target/shape/range state;
- repeated refresh does not duplicate active preview entries;
- commit clears range, enables Resolve, and resolution updates target HP/status
  while disabling Resolve.

Per Agent B ownership rules, Unity and screenshots were not run. Both new
production sources and both test sources were compiled successfully with the
Unity 6000.4.10f1 bundled Roslyn compiler against the frozen
Domain/Application/Presentation assemblies. `git diff --check` and the static
forbidden-reference scans passed. The main integrator must run the formal
PlayMode filter and owns the resulting XML.

## Main-integrator Scene and Controller wiring

1. Add the four presenters and `CombatPresentationBinding` to the authored
   combat scene; serialize the existing `CardHandHost`, `BoardRangePreview`,
   `TimelinePlacementPreview`, all 36 `TimelineCellView` instances, status/target
   `Text`, and Resolve `Button` references.
2. Subscribe the composition/controller facade once to binding intent events:
   card select/cancel/drag, timeline select/hover/exit, and resolve.
3. After each Application command, call
   `CombatPresentationBinding.Refresh(_applicationSession.Current)`.
4. Feed committed action label/color descriptors to
   `TimelinePresenter.RenderAction`; the descriptor belongs to composition or
   catalog data, not a presenter stable-ID branch.
5. Keep artwork creation outside Agent B. The card composition path must build
   `CardViewModel` from the catalog entry selected by
   `CardDefinition.FrontImage`.

After real Scene integration, the Controller can remove or redirect its current
view-event bind/unbind fan-out, range show/clear coordination, Timeline preview
coordination, `SetTimelineCell`, phase-driven card interaction toggles,
`SetStatus`, target text refresh, and Resolve button state. World raycast input,
Application facade compatibility, dynamic board/target instantiation, effect
application to real columns, and artwork/catalog composition remain main-agent
responsibilities.

## Required main-agent verification

Run the prompt-owned PlayMode filter:

```powershell
-testFilter "TimeKey.Tests.PlayMode.Presenters;TimeKey.Tests.PlayMode.Bindings"
```

Write the XML to
`04-verification/evidence/decoupling-r3/agent-b-playmode-results.xml` and require
`total=passed`, `failed=0`. The main integrator also owns real Inspector wiring,
three viewports, four yaw directions, and earthquake before/after evidence.

## Protection and ownership return

The original four modified user files and two untracked stage prompts remain
untouched. A concurrent modification to `05-progress/push-status.md` was observed
and left untouched. Agent B did not stash, stage, commit, push, switch branches,
clean unknown files, or run Unity. All Agent B-owned paths are now returned to
the main integrator.
