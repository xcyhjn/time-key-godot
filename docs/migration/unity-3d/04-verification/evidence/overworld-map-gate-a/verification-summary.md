# Wave 03 Map Gate A Verification

## Scope

This continuation closes the pure Domain Gate A slice only. `OverworldMapDefinition` and `DeterministicOverworldMapGenerator` provide deterministic layered topology, stable `MapNodeId` identity, typed Entry/Battle/Elite/Event/Shop/Boss rooms, adjacent-layer edges and entry-to-Boss validation. `OverworldChapterState` provides visited/settled/available snapshots, one active room, typed resolution rules, stale revision handling and exact sequence replay.

The compatibility level is observable invariant compatibility, not bit-for-bit Godot topology compatibility. The Godot sources use a radius-12 hex field, `RandomNumberGenerator`, `String.hash()` and `[4, 8, 12]` boundaries; Unity uses a documented stable xorshift32 layer generator. This difference is explicit in `agents/reports/overworld-map-domain-gate-a.md` and must not be hidden by a seed adapter.

## Evidence

- Unity D3D12 full EditMode: `editmode-full-map-gate-a.xml`, 396/396 passed.
- Agent-owned reflection fixture: 20/20 passed; no Unity API references.
- Source SHA and frozen semantics: `agents/reports/overworld-map-domain-gate-a.md`.
- `git diff --check` is clean for the new Domain/test paths; Unity-generated Prefab blank-value whitespace remains recorded as an existing generated-file check item in the parent stage audit.

## Non-goals and resume

No formal map SceneFlow, persistence/Continue, event/shop UI, Boss chapter scene transition or save migration is claimed. Resume `NEXT_STAGE_OVERWORLD_MAP_PROMPT.md` at Gate 0/B review, keeping `MapNodeId` as the only shared identity and retaining the existing OutOfBattle movement model rather than creating a second movement state machine.
