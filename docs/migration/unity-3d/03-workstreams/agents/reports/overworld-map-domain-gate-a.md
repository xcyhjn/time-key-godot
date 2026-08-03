# Wave 03 Overworld Map Domain Gate A Report

## Ownership

- Agent-owned writes:
  - `unity/Assets/_Project/Runtime/Domain/Overworld.meta`
  - `unity/Assets/_Project/Runtime/Domain/Overworld/**`
  - `unity/Assets/_Project/Tests/EditMode/Overworld.meta`
  - `unity/Assets/_Project/Tests/EditMode/Overworld/**`
  - this report
- No existing `OverworldMovement`, Application, Composition, Presentation, asmdef,
  Scene, Prefab, ProjectSettings, Git index, or shared progress document was changed.
- Unity was not started by this agent. The primary agent owns compilation and test execution.

## Read-only Godot source audit

| Source | SHA-256 | Frozen observable behavior |
| --- | --- | --- |
| `scene/out_scene/map_generator.gd` | `F9B8D384E5C284E7BBCA74E81EC09A2797C4C023A261A3E8734EC54DA16E7053` | Axial hex distance; fixed six directions; start at origin; bosses on layer boundaries; seeded room selection; path repair; normal-room feature pools. |
| `scene/out_scene/map_renderer.gd` | `2E4E5E5AE6B180B8C8B565BB1DA5199D744ABB58B42CB4013BDDEE20A6B97C56` | Current tile highlight, distance-one availability highlight, radius-gated visibility. |
| `scene/out_scene/out_scene_map_exp.gd` | `F364062ED53C967F6859EE242E7F9DB79FC04684C0A2BB9F4693725060FAFB4D` | Move only at hex distance one; movement commits before room entry; fixed seed restores map; current tier controls visible radius; boss resolution may unlock one next tier. |
| `scene/out_scene/out_scene_modules/RoomResolutionController.gd` | `8881FA03E982D43442A98911D5CAE2AD3B4FD84D54F87EB174A5C07193FE77E7` | Only a completed, identity-matched boss on the current boundary advances; defeat and non-boss outcomes do not advance. |

### Compatibility level

This slice implements **observable invariant compatibility**, not bit-for-bit Godot
topology compatibility. Godot uses `RandomNumberGenerator`, `String.hash()`, a radius-12
hex field, void digging, six character sectors, and `[4, 8, 12]` boundaries. The new
Unity-free chapter model uses a small layered graph and an explicitly stable xorshift32
PRNG. Therefore the same integer seed is reproducible across Unity runs, but it is not
claimed to reproduce Godot's tile coordinates or RNG sequence.

The preserved semantics are: deterministic fixed-seed generation, stable node identity,
ordered chapters/layers, only adjacent saved edges are traversable, entry-to-boss
reachability, visible available/visited/settled state, a single active room, and boss
victory advancing exactly once. Character-sector cutting, mountain/void tiles, feature
economy, event narrative, shop inventory, layout coordinates, animation, persistence,
SceneFlow, and payload adaptation remain explicit non-goals for this Domain slice.

## Implemented contract

- All graph and state contracts reuse the existing ordinal, immutable
  `TimeKey.Domain.OverworldMovement.MapNodeId`; no second stable node identity exists.
- `OverworldMapDefinition` defensively copies immutable nodes and directed edges and
  rejects missing endpoints, self-loops, duplicate edges, illegal layer skips, duplicate
  identities, missing predecessor/successor links, and unreachable bosses.
- `DeterministicOverworldMapGenerator` creates one entry, 1-32 intermediate layers,
  deterministic Battle/Elite/Event/Shop rooms, and one Boss. Every edge joins adjacent
  layers; every non-entry has a predecessor and every non-Boss has a successor.
- `OverworldChapterState` starts with the entry visited and settled. Availability is the
  unsettled outgoing set of the current settled node. Entering a room marks it visited
  and exclusively active; successful resolution settles it and unlocks successors.
- Enter/resolve operations share one positive sequence journal. Exact replay returns the
  original result; changed payload or operation kind returns `SequenceConflict`; stale
  revision, source mismatch, missing/non-adjacent/settled target, concurrent room, wrong
  room identity, and invalid outcome are explicit failures without partial state changes.
- Combat/Elite/Boss accept Victory or Defeat. Event/Shop accept Completed or Cancelled.
  Only Boss Victory sets `ChapterCompleted` and increments `ChapterAdvanceCount`; exact
  replay does not increment again, and Defeat never settles or completes the Boss.
- Definition and state snapshots expose read-only defensive collection copies and contain
  no Unity object or API reference.

## Directed tests added

- Same seed/config equivalence for `0`, positive, negative, `int.MinValue`, and
  `int.MaxValue`; different seeds produce an observable fingerprint difference.
- Unique nodes/edges, no self-loop, adjacent-layer edges, predecessor/successor coverage,
  entry-to-Boss reachability, constructor rejection, config bounds, and defensive copies.
- Initial availability, non-adjacent rejection, no partial mutation, exact replay,
  sequence conflict including cross-operation reuse, stale revision, active-room mutex,
  typed resolution compatibility, Event/Shop settlement, Boss single advance, Boss Defeat,
  and state snapshot defensive copies.

Primary-agent test command:

```powershell
$env:TIMEKEY_REPOSITORY_ROOT='D:\godot\时之钥\时之钥'
& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' `
  -batchmode -force-d3d12 -projectPath 'D:\timekey-unity-731' `
  -runTests -testPlatform EditMode `
  -testFilter 'TimeKey.Tests.EditMode.Overworld' `
  -testResults 'D:\timekey-wave03-map-domain\editmode-overworld.xml' `
  -logFile 'D:\timekey-wave03-map-domain\editmode-overworld.log'
```

## Static verification

- Restricted-path `git diff --check`: passed.
- `UnityEngine`, `UnityEditor`, and `Unity.*` scan in new Domain/tests: no matches.
- New Unity meta GUIDs: present and unique in the current project.
- Unity's bundled Mono compiler built the pure Domain assembly and NUnit test assembly
  outside the project lock. A reflection-only NUnit invocation executed all 20 generated
  test cases with 0 failures. The primary agent should still run the same tests through
  Unity Test Runner as the authoritative project verification.

## Integration handoff

Application can pass the shared `OverworldMovement.MapNodeId` through both modules without
an identity adapter, but should not merge the two state machines. The new map Domain owns
chapter topology and room lifecycle; the existing movement module still owns animation-time
request/commit/cancel and axial presentation. Persistence should serialize
seed, chapter, stable node IDs, revision, visited/settled IDs, active room identity, and
the operation sequence cursor through a versioned DTO, without serializing Domain objects
or Unity objects directly.
