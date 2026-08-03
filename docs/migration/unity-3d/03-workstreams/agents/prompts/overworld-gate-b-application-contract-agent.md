# Overworld Gate B Application Contract Agent Prompt

## Objective

Continue from the committed Overworld Domain Gate A. Add the smallest Unity-free Application boundary needed to orchestrate a deterministic chapter map, adjacent room selection, existing combat launch/outcome consumption, typed Event/Shop outcomes, Boss advancement planning, and a versioned persistence snapshot contract.

Do not recreate the Gate A graph/state machine. Reuse `TimeKey.Domain.OverworldMovement.MapNodeId`, `OverworldMapDefinition`, `DeterministicOverworldMapGenerator`, and `OverworldChapterState`.

## Exclusive write ownership

You may write only:

```text
unity/Assets/_Project/Runtime/Application/Overworld.meta
unity/Assets/_Project/Runtime/Application/Overworld/**
unity/Assets/_Project/Tests/EditMode/OverworldApplication.meta
unity/Assets/_Project/Tests/EditMode/OverworldApplication/**
docs/migration/unity-3d/03-workstreams/agents/reports/overworld-gate-b-application-contract.md
```

No other file is writable. You are not the only worker in the repository; accommodate concurrent files and never revert another worker's edits.

## Required contract

- Reuse existing `RunStartPayload`, `CombatLaunchPayload`, `CombatOutcome`, deck/Era/phase/timecoin identity, and `MapNodeId` by reference. Do not introduce replacement payloads or a second node identity.
- Keep the Domain `OverworldChapterState` as the only map topology/room lifecycle state machine. Application may coordinate commands and produce immutable plans/results, but must not duplicate visited/settled/active-room state.
- New game and seed game must create a deterministic map/run projection. Same seed/config yields the same fingerprint; different seeds are observably different.
- Only an adjacent available Battle/Elite/Boss node may produce an existing `CombatLaunchPayload`.
- Consume existing `CombatOutcome` by room/run/launch identity exactly once. Exact replay is idempotent; changed replay is an explicit conflict. Defeat must not settle the room or masquerade as completion.
- Define separate typed Event and Shop room outcomes. They must not wrap or reuse `CombatOutcome`, and no cross-scene contract may contain `Dictionary`, `object`, a Domain instance, or a Unity object.
- Produce an explicit transaction/commit plan that orders reward acceptance, room resolution, successor unlock, chapter advancement, and persistence commit. Do not perform file I/O.
- Define a versioned immutable persistence snapshot made only of primitives, stable IDs, ordered/read-only collections, and operation/revision cursors. Defensively copy all collections.
- Boss Victory advances exactly once and exposes a typed next-chapter/final-chapter decision. Do not invent narrative, economy, enemy, or UI rules.
- If Gate A lacks a required restore/re-hydration API, do not modify Domain. Record the exact minimal gap in the report for the primary agent.

## Tests

Add focused EditMode tests for deterministic creation, adjacency, existing combat payload identity, outcome replay/conflict, defeat, Event/Shop separation, Boss single advance, immutable snapshots, defensive copies, and transaction ordering. Tests must not start Unity or write canonical repository evidence.

## Prohibitions

- Do not modify existing Application/SceneFlow files, `OutOfBattleShellState`, `SceneFlowStateStore`, MainMenu, Composition, Presentation, Scene/Prefab, asmdef, ProjectSettings, Domain, Infrastructure, shared progress docs, or Git state.
- Do not run Unity while another worker may be active. Do not stage, commit, push, switch branches, stash, reset, checkout, or clean.
- Do not add adapters that hide a conflict with current SceneFlow or Gate A. Report the conflict instead.

## Handoff

Run static scans and `git diff --check` only for your allowlist. In the report, list files, public contract, assumptions, Domain/SceneFlow gaps, and the exact Unity test filter the primary agent should run. Leave staging untouched.
