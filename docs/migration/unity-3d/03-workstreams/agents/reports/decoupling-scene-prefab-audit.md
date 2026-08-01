# Decoupling Scene/Prefab Read-only Audit

> Agent: `decoupling-scene-prefab-audit`
> Date: 2026-08-01
> Branch observed: `unity_7.31`
> Scope: read-only audit; this report is the only owned write
> Unity execution: not run, by agent ownership rule

## Conclusion

`CombatVerticalSlice.unity` is not yet an authored combat scene. It serializes one root object, `VerticalSliceRoot`, one `VerticalSliceController`, and the `lighting` / `earthquake` fixture references. The root has no serialized children (`m_Children: []`). There are no `.prefab` assets below `unity/Assets/_Project`.

All stable world, camera, lighting, EventSystem, Canvas, HUD, timeline, hand host, preview components, demo target and visual materials are created by `VerticalSliceController.Awake() -> BuildSceneGraph()`. The current idempotence guard only checks `_generatedRoot != null`; it does not prove that the saved scene is complete, that Inspector references are valid, that disable/enable or repeated composition does not duplicate listeners, or that an author edit survives Play Mode.

R1 should therefore be a scene-authoring migration, not a broad architecture rewrite. Keep the current controller public API as a compatibility facade, save stable objects and current view prefabs through Unity Editor APIs, and restrict runtime creation to fixture-sized board/card/action instances through serialized prefab factories. Do not mix R2 Application extraction or R3 resource/diagnostics work into the R1 checkpoint.

## Audit Basis

- Prompt: `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md`.
- Saved scene: `unity/Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`.
- Runtime Presentation: all `.cs` files below `unity/Assets/_Project/Runtime/Presentation`.
- Editor harness: `unity/Assets/_Project/Editor/VerticalSliceAutomation.cs`.
- Tests: `CombatVerticalSliceTests`, `CardHandHostTests`, `CardHandViewTests`, `HexTileColumnTests`, and Targeting PlayMode tests.
- Assets and configuration: `_Project/Resources`, all asmdefs, `EditorBuildSettings.asset`, `Packages/manifest.json`, and `ProjectVersion.txt`.
- Prior integrated verification: `unity-slice-02b2a/verification-summary.md` and `harness-summary.json` (`67/67` EditMode, `25/25` PlayMode, build and Player smoke passed before this stage).
- Repository had no `AGENTS.md`; the conversation-provided instructions remain authoritative.
- No Unity editor process or `unity/Temp/UnityLockfile` was present during this audit. Unity Hub and its licensing client were present but were not writing the project.

## Current Saved Scene

The scene's complete authored hierarchy is:

```text
CombatVerticalSlice.unity
└── VerticalSliceRoot
    └── VerticalSliceController
        ├── lightingFixture
        └── earthquakeFixture
```

Evidence:

- The only `GameObject` is `VerticalSliceRoot`: `CombatVerticalSlice.unity:60-76`.
- Its transform has no children: `CombatVerticalSlice.unity:77-91`.
- The only serialized component data beyond the transform is the controller script and two fixtures: `CombatVerticalSlice.unity:92-105`.
- There are no `.prefab` files under `unity/Assets/_Project`.
- `EditorBuildSettings.asset` contains only this scene.

This means there is currently nothing to edit persistently in the Hierarchy or Prefab Mode except the root and two fixture assignments.

## Runtime Creation Matrix

| Current object or responsibility | Current creation path | Correct R1 boundary |
|---|---|---|
| `GeneratedSlice` root | `BuildSceneGraph()` creates it and adds both preview components (`VerticalSliceController.cs:154-180`) | Remove as a generated stable-tree root. Keep a serialized composition root or use the scene root directly. |
| Perspective camera and orbit component | `BuildWorld()` creates `SliceCamera`, adds `Camera` and `BoardOrbitCameraController`, then assigns all settings (`:561-571`) | Saved `CameraRig/OrbitTarget/Main Camera` hierarchy; Inspector-bound `Camera` and controller. |
| Key/fill lights | `new GameObject` plus `AddComponent<Light>` (`:573-587`) | Saved scene lights with editable intensity, color and rotation. |
| Battlefield plane/material | `CreatePrimitive(Plane)` and runtime `Material` (`:589`, `:594-600`, `:836-870`) | Saved scene ground plus saved `.mat`; no runtime shader search/material creation. |
| Board root | Board columns are parented directly to `GeneratedSlice` (`:601-610`, `:738-754`) | Saved `CombatBoardRoot/Columns` container; column count remains dynamic from fixture. |
| Hex column root | `CreateTile()` uses `new GameObject`, adds `HexTileColumn` and `BoardTileView` (`:738-754`) | Instantiate a serialized `HexColumn.prefab` through one Presentation factory. |
| Hex blocks | `HexTileColumn.CreateBlock()` creates wrapper, instantiates raw FBX and adds `MeshCollider` (`HexTileColumn.cs:108-130`) | `HexBlockGrass.prefab` / `HexBlockDirt.prefab` containing model, Renderer and Collider; each logical layer remains a distinct prefab instance at exact `0.32`. |
| Occupant anchor | `HexTileColumn.EnsureOccupantAnchor()` creates it (`HexTileColumn.cs:195-205`) | Saved child of `HexColumn.prefab`, serialized into the component. |
| Demo target and altar art | Runtime Cylinder, runtime material, runtime sprite, runtime collider, two `WorldTargetView` components and billboard (`VerticalSliceController.cs:612-633`, `:872-889`) | Current target view prefab with authored renderer/collider/billboard; scene owns stable target/unit roots and fixture anchor. Runtime only binds it to the dynamic column anchor. |
| EventSystem/input module | Created in `BuildInterface()` (`:638-641`) | Saved scene `EventSystem`; exactly one. |
| Canvas/scaler/raycaster | Created and configured at runtime (`:643-653`) | Saved scene Canvas with editable `CanvasScaler` and `GraphicRaycaster`. |
| Header/status | `CreatePanel` / `CreateText` (`:655-666`, `:986-1024`) | Saved HUD hierarchy and serialized `Text` references. |
| Timeline root/grid/cells | Panel and `GridLayoutGroup` created; 36 buttons, labels and `EventTrigger`s created in loops (`:668-701`, `:756-776`) | Saved Timeline root, grid and 36 `TimelineCell.prefab` instances. Runtime actions/markers remain dynamic. |
| Invalid timeline outline | `TimelinePlacementPreview.Register()` adds missing `Outline` (`TimelinePlacementPreview.cs:32-50`) | `Outline` authored in `TimelineCell.prefab`; `Register` validates and records it, never adds it. |
| Card hand stable hierarchy | Controller creates host; `CardHandHost` creates `Cards` and each card root (`VerticalSliceController.cs:703-717`; `CardHandHost.cs:163-195`) | Saved `CardHandHost/Cards`; hand entries instantiate a serialized `CardView.prefab`. |
| Card view hierarchy | `CardHandView.EnsureHierarchy()` creates `CardSlot`, visual, shadow and artwork and adds UI components (`CardHandView.cs:361-404`, `:455-460`) | All children and UI components saved in `CardView.prefab`; runtime `Build()` only assigns view-model state and artwork. |
| Detail/target/intent/resolve HUD | Runtime panels/text/button and listener (`VerticalSliceController.cs:719-735`) | Saved HUD objects, serialized text/button references; composition subscribes once and unsubscribes. |
| Range/timeline previews | Added to `GeneratedSlice` (`:173-176`) and filled through registration | Saved under `CombatBoardRoot/Previews` and `Timeline/Previews`; dynamic registrations are rebuilt from spawned columns and saved cells. |

## Serialized Boundary Audit

### Current Inspector bindings

`VerticalSliceController` exposes only:

- `lightingFixture` (`VerticalSliceController.cs:26`)
- `earthquakeFixture` (`VerticalSliceController.cs:27`)

Every stable view reference is a private runtime field. `SceneCamera`, `SceneCanvas`, `BoardCamera`, `CardHandHost`, previews, HUD texts, Resolve button, target object, target renderer, materials, model prefabs and card sprites are populated by programmatic construction or `Resources.Load`.

`BoardOrbitCameraController` has four serialized tuning values (`BoardOrbitCameraController.cs:13-16`) but the component itself does not exist in the saved scene. No Presentation component implements `OnValidate`.

### Required minimum R1 bindings

Use one explicit scene-reference surface, either serialized fields on the compatibility controller or a small `CombatSceneReferences` MonoBehaviour consumed by it. Do not add an interface per field. The minimum authored references are:

- Camera: `Camera`, `BoardOrbitCameraController`, orbit target/rig `Transform`.
- Roots: board columns, units/targets, one-shot VFX, Timeline, HUD and CardHandHost containers.
- UI: Canvas, EventSystem, status text, target text, Resolve button, timeline cell list/container, CardHandHost.
- Preview: `BoardRangePreview`, `TimelinePlacementPreview`.
- Dynamic factories/assets: HexColumn prefab, grass/dirt HexBlock prefabs, CardView prefab, target/enemy view prefab, card fixtures and existing art/resource adapter inputs.
- Fixed presentation assets: saved ground material and any target material/sprite references required by the prefab.

`OnValidate` and runtime initialization must report the exact missing field and owning scene object. A generic null-reference later in `BuildWorld()` is not acceptable. Validation must not mutate or rebuild layout.

## Current Callers That R1 Must Preserve

Only the Editor harness and PlayMode integration tests call `VerticalSliceController` directly. R1 should keep these methods/properties while changing their internals:

- Initialization facade: `BuildSceneGraph()` is called by `VerticalSliceAutomation.cs:43` and `CombatVerticalSliceTests.cs:25`.
- Commands: `SelectCard`, `SelectTarget`, `SelectEarthquakeTarget`, `TryPlaceSelected`, `PreviewTimelineSelected`, `ResolveTimeline`, `SetBoardView`, and `TrySelectWorldAtScreenPoint`.
- Observable state/views: `SceneCamera`, `SceneCanvas`, `BoardCamera`, `CardHand`, `CardHandHost`, `BoardRangePreview`, `TimelinePreview`, tile/target/timeline counts and `GetTileColumn`.

Minimal compatibility rule: `BuildSceneGraph()` may remain named as-is in R1, but it should become an idempotent `validate references -> bind listeners -> initialize runtime state -> spawn dynamic fixture content` facade. It must not create the stable hierarchy.

## Desired R1 Hierarchy

The following is the smallest authored hierarchy that satisfies the prompt without prebuilding fixture-dependent columns or hand cards:

```text
CombatVerticalSlice
├── CombatCompositionRoot
│   └── VerticalSliceController
├── CameraRig
│   ├── OrbitTarget
│   └── Main Camera [Camera, BoardOrbitCameraController]
├── Environment
│   ├── KeyLight
│   ├── FillLight
│   └── BattlefieldGround
├── CombatBoardRoot
│   ├── Columns                 # dynamic HexColumn prefab instances
│   ├── UnitsAndTargets         # fixture target/enemy view binding
│   ├── Vfx                     # dynamic one-shot instances
│   └── Previews
│       └── BoardRangePreview
├── EventSystem
└── SliceCanvas [Canvas, CanvasScaler, GraphicRaycaster]
    ├── HUD
    │   ├── Header
    │   │   ├── Title
    │   │   └── Status
    │   └── DetailPanel
    │       ├── TargetStatus
    │       ├── Intent
    │       └── Resolve
    ├── Timeline
    │   ├── Grid [GridLayoutGroup]
    │   │   └── 36 x TimelineCell prefab instance
    │   ├── ActionMarkers       # dynamic
    │   └── TimelinePlacementPreview
    └── CardHandHost
        └── Cards               # dynamic CardView prefab instances
```

The scene may keep the historical `VerticalSliceRoot` name as the composition root to minimize harness churn. The important change is serialized content and references, not renaming.

## Minimal Prefab Set

| Prefab | Saved contents | Runtime data only |
|---|---|---|
| `TimelineCell.prefab` | RectTransform, Image, Button, Outline, label and pointer relay/trigger component | coordinate, label, base/preview color, click callback binding |
| `CardView.prefab` | CardHandView, CardSlot, CardVisual, Shadow, Artwork, Outline and CanvasGroup | stable ID, front image Sprite, selected/interactable state |
| `HexBlockGrass.prefab` | Grass FBX instance, Renderer and correctly aligned MeshCollider | layer index/local Y |
| `HexBlockDirt.prefab` | Dirt FBX instance, Renderer and correctly aligned MeshCollider | layer index/local Y |
| `HexColumn.prefab` | HexTileColumn, BoardTileView, Blocks root and OccupantAnchor | axial coordinate, logical layer count, block instances |
| Current target/enemy view prefab | WorldTargetView, authored base renderer/material, original altar SpriteRenderer, BoxCollider, CameraFacingBillboard | target stable ID, HP presentation, column-anchor binding |

The FBX importers currently have `addColliders: 0`; changing the global FBX importer is unnecessary and higher blast radius. Put the collider on each saved HexBlock prefab so each `0.32` layer remains independently selectable/collidable and aligned with the transformed mesh.

Card and altar textures are imported as default textures, and current code creates runtime Sprites. R1 does not need to solve the R3 catalog/resource boundary, but `CardView.prefab` must receive its artwork from `CardDefinition.FrontImage`; it must not hard-code or infer a stable-ID filename.

## Minimal R1 Implementation Boundary

1. Add characterization tests before changing construction. Freeze current lighting/earthquake behavior and add the missing saved-scene/repeated-composition assertions listed below.
2. Add an Editor-only, idempotent authoring command that creates/saves the scene hierarchy, Prefabs and materials with Unity APIs (`PrefabUtility`, `EditorSceneManager.SaveScene`, serialized fields). Do not hand-edit large YAML.
3. Save the resulting Scene/Prefab assets and inspect them in Unity. If the tool has no ongoing use after the committed assets exist, delete it in the same R1 slice. If retained, it must never run automatically or overwrite existing designer layout.
4. Convert controller construction helpers into validation and binding. Preserve the public facade and test/harness accessors.
5. Move runtime instantiation behind narrow Presentation factories/components:
   - board fixture -> HexColumn prefab -> HexBlock prefab instances;
   - hand view models -> CardView prefab instances;
   - current target fixture -> target/enemy view prefab;
   - timeline state -> marker instances only.
6. Subscribe to saved buttons/hand events exactly once and unsubscribe in `OnDisable`/`OnDestroy`. Reinitialization should clear and rebuild runtime registries without replacing authored objects.
7. Run affected tests and replace all presentation/build/visual evidence. Domain/XML evidence whose tested code is unchanged may remain inherited, but previous PlayMode screenshots cannot prove the new authored hierarchy.

R1 should not introduce Application interfaces, effect registries, diagnostics sinks or a general enemy system. Those belong to R2/R3 unless a tiny factory contract is strictly required to stop runtime `new GameObject` calls.

## Characterization Tests Required Before or With R1

### Editor-only saved-asset tests

Create a separate Editor test assembly (for example `Tests/Editor/TimeKey.Tests.Editor.asmdef`) referencing Presentation. Do not add Presentation to the current Domain-focused `TimeKey.Tests.EditMode.asmdef` merely to inspect assets.

1. `CombatScene_ContainsStableHierarchyBeforePlay`
   - Open `CombatVerticalSlice.unity` with `EditorSceneManager` without entering Play Mode.
   - Assert Camera + orbit component, EventSystem, Canvas/scaler/raycaster, HUD texts/button, Timeline grid and 36 cells, CardHandHost, Board root and both preview components exist.
   - Assert no `GeneratedSlice` object exists.
2. `CombatScene_ControllerReferencesAreSerialized`
   - Inspect the controller through `SerializedObject` and assert every required object/prefab field is non-null and belongs to the scene or expected asset.
3. `RequiredPrefabs_HaveExpectedComponentsAndLinks`
   - Load each prefab with `AssetDatabase.LoadAssetAtPath`/`PrefabUtility.LoadPrefabContents`.
   - Assert CardView hierarchy, TimelineCell Outline/Button/label, HexColumn OccupantAnchor, and each HexBlock mesh/renderer/collider.
4. `MissingReference_ReportsFieldAndOwner`
   - Use an instantiated scene/prefab copy, clear one required serialized reference, call validation and assert the error contains the field name and object/scene path.
5. `AuthoringCommand_IsIdempotentAndPreservesDesignerValues` if the tool remains
   - Run against a temporary copied scene/prefab, set a sentinel layout/camera value, rerun authoring and prove no duplicate nodes and no overwrite of the sentinel.

### PlayMode integration tests

1. Replace `SceneBuild_IsCompleteAndIdempotent` with assertions that record stable object instance IDs and transforms before/after repeated `BuildSceneGraph()` calls. Current test only records root child count after `Awake` (`CombatVerticalSliceTests.cs:18-52`).
2. Add `RepeatedInitialize_DoesNotDuplicateDynamicContentOrListeners`:
   - call the compatibility initializer twice and across disable/enable;
   - assert 19 columns, correct current total blocks, two hand cards, 36 saved cells, one EventSystem, one Camera and one Canvas;
   - click one card/cell once and assert one state transition/one timeline placement.
3. Add `SceneReload_RebindsOnce` to load, unload and reload the scene, then execute one lighting path and one earthquake path without stale static/event state.
4. Keep and rerun all existing integration behavior:
   - lighting resolves before enemy intent (`CombatVerticalSliceTests.cs:106-124`);
   - selection/cancel and orbit restoration (`:126-151`);
   - two-card FrontImage and exclusive selection (`:153-186`);
   - earthquake seven real columns, exact layer spacing/top/anchor delta and four-yaw selection (`:188-264`);
   - pure valid/invalid timeline preview and range persistence (`:266-308`);
   - artwork aspect and UI input blocking (`:310-328`).
5. Update component tests to instantiate saved Prefabs instead of manufacturing look-alike GameObjects. Current `HexTileColumnTests.CreatePrefab()` (`HexTileColumnTests.cs:78-97`) and card/targeting visual rigs can pass even when the repository Prefabs are missing or broken.

### Static construction gate

After R1, this source scan should find no stable hierarchy/material construction in the controller and no child construction in CardHandView/CardHandHost/HexTileColumn:

```powershell
rg -n "new GameObject|CreatePrimitive|AddComponent|CreatePanel|CreateText|CreateButton|new Material|Shader.Find" `
  unity/Assets/_Project/Runtime/Presentation/VerticalSliceController.cs `
  unity/Assets/_Project/Runtime/Presentation/Cards/CardHandHost.cs `
  unity/Assets/_Project/Runtime/Presentation/Cards/CardHandView.cs `
  unity/Assets/_Project/Runtime/Presentation/Terrain/HexTileColumn.cs
```

Prefab `Instantiate` calls in explicit factories remain allowed for data-sized dynamic content.

## Artificial-editing and Regression Risks

1. **False scene migration by YAML:** manually adding a large hierarchy can produce invalid file IDs, missing prefab links or serialized-version churn. Generate/save through Unity APIs and inspect Prefab Mode/Inspector.
2. **Automation validates temporary runtime objects:** `VerticalSliceAutomation.BuildValidateAndCapture()` opens the scene, calls `BuildSceneGraph()` in Edit Mode, then validates the transient tree (`VerticalSliceAutomation.cs:21-45`). It does not save that tree. R1 must validate the authored tree before initialization and validate dynamic content after initialization.
3. **Current idempotence is only a sentinel:** `_generatedRoot` short-circuit (`VerticalSliceController.cs:154-159`) hides listener and registry duplication and cannot survive removal of `GeneratedSlice` without a replacement lifecycle state.
4. **Listener duplication:** timeline Button listeners and hand host subscriptions are added in construction (`VerticalSliceController.cs:694-717`, `:735`) and never explicitly removed. Saved buttons make lifecycle ownership visible; bind/unbind symmetrically.
5. **Prefab-looking tests:** multiple component tests build their own runtime UI/mesh rigs. They verify component behavior, not submitted assets, so add Editor prefab-asset tests rather than deleting useful isolated tests.
6. **Runtime material loss/visual drift:** runtime `Shader.Find` and new Materials bypass Inspector and saved assets. Moving to `.mat` assets changes evidence and requires new 1280x720, 1920x1080 and 2560x1080 captures.
7. **Collider transform drift:** raw FBX roots may contain transforms below the prefab root. The collider must live with the actual mesh transform, matching the existing Wave 02B2A correction, not on an untransformed wrapper.
8. **Card aspect drift:** authoring the CardView hierarchy must preserve `Image.preserveAspect` and the 125:175 design container while assigning FrontImage-derived artwork.
9. **Stable/dynamic boundary inversion:** do not serialize the 19 fixture columns, drawn cards, action markers or VFX into the scene. They are data/lifecycle dependent. Serialize their roots and Prefabs.
10. **Target anchoring race:** the demo target is stable presentation but its column anchor is dynamic. Initialize columns first, then bind/reposition the target once; subsequent `HexTileColumn.Changed` updates must not add duplicate subscriptions.
11. **UI layout overwrite:** controller helpers currently assign every anchor/offset. After migration, runtime initialization must not write those authored RectTransform values.
12. **Evidence invalidation:** the scene, layout, materials, camera and Presentation components are all affected by R1. Prior Domain behavior evidence can be inherited if untouched, but PlayMode, harness, build, smoke and visual evidence must be refreshed.

## R1 Verification Gate

- Editor tests prove all stable nodes and Inspector references exist before Play.
- Prefab tests prove exact required components and links.
- Repeated initialize/enable/reload tests prove no duplicate stable nodes, dynamic instances or callbacks.
- Existing lighting and earthquake contracts remain unchanged, including two true `0.32` additions on seven columns and four-yaw selection.
- Unity Hierarchy/Scene View and Prefab Mode are manually inspected; Inspector edits to camera, layout and materials survive Play/stop and reopen.
- New screenshots at 1280x720, 1920x1080 and 2560x1080 show no clipping or visual drift.
- Full affected PlayMode, batch harness, Windows build and Player smoke are regenerated because runtime assemblies and the scene changed.
- `git diff --check` passes and all Scene/Prefab/material `.meta` files accompany their assets.

## Ownership and Worktree Safety

This audit did not run Unity and did not modify code, Scene, Prefab, tests, shared documents, Package/ProjectSettings, or protected Godot files. At audit start, the protected modified files were:

- `default_bus_layout.tres`
- `scene/in_scene/rewards/resources/default_craft_recipe_book.tres`
- `shaders/color_BG.gdshader`
- `shaders/game_over.gdshader`

The untracked stage prompts `NEXT_STAGE_DECOUPLING_PROMPT.md` and `NEXT_STAGE_REMAINING_CARDS_PROMPT.md` were also treated as protected concurrent/user-owned files.
