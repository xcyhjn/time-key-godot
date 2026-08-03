# Overworld Gate B Application Contract Report

## Ownership

This agent wrote only the assigned new paths:

- `unity/Assets/_Project/Runtime/Application/Overworld.meta`
- `unity/Assets/_Project/Runtime/Application/Overworld/OverworldApplicationContracts.cs`
- `unity/Assets/_Project/Runtime/Application/Overworld/OverworldApplicationContracts.cs.meta`
- `unity/Assets/_Project/Runtime/Application/Overworld/OverworldRunApplication.cs`
- `unity/Assets/_Project/Runtime/Application/Overworld/OverworldRunApplication.cs.meta`
- `unity/Assets/_Project/Tests/EditMode/OverworldApplication.meta`
- `unity/Assets/_Project/Tests/EditMode/OverworldApplication/OverworldRunApplicationTests.cs`
- `unity/Assets/_Project/Tests/EditMode/OverworldApplication/OverworldRunApplicationTests.cs.meta`
- this report

No existing Domain, SceneFlow, state-store, Infrastructure, Presentation, Composition,
Scene, Prefab, asmdef, ProjectSettings, shared progress document, or Git state was changed.
Unity was not started.

## Public contract

`TimeKey.Application.Overworld.OverworldRunApplication` is the stateful Application
coordinator. It creates the committed Gate A map and chapter state from the existing
`RunStartPayload`, an existing `OverworldMapGenerationConfig`, and a composition-supplied
final chapter number. It exposes an immutable `OverworldMapProjection`, available
`MapNodeId` values, room type lookup, and a versioned persistence projection.

The coordinator directly owns one `OverworldChapterState`; it does not copy visited,
settled, current, available, active-room, or chapter-advance collections into a second
map state machine. Its additional mutable state is limited to cross-scene run identity,
the active existing `CombatLaunchPayload`, run-return projection values, and Application
idempotency records.

Public operations are:

- `TryBeginCombat(EnterOverworldRoomCommand, launchCorrelationId, battleTag, battleSeed)`
- `TryEnterEventRoom(EnterOverworldRoomCommand)`
- `TryEnterShopRoom(EnterOverworldRoomCommand)`
- `TryApplyCombatOutcome(sequence, expectedRevision, CombatOutcome)`
- `TryApplyEventOutcome(sequence, expectedRevision, EventRoomOutcome)`
- `TryApplyShopOutcome(sequence, expectedRevision, ShopRoomOutcome)`
- `CreatePersistenceSnapshot()`

Only adjacent available Battle, Elite, or Boss nodes can create the existing
`CombatLaunchPayload`. The payload carries the original run, room, launch, character,
seed, chapter, deck, Era, phase, timecoin, battle tag, and battle seed identities.

`EventRoomOutcome` and `ShopRoomOutcome` are separate immutable typed
`ISceneTransitionPayload` contracts. Neither contains or wraps `CombatOutcome`. Their
public properties contain only stable values and the required shared `MapNodeId`.

Combat, Event, and Shop outcomes use both a global operation-sequence fingerprint and
an outcome-correlation fingerprint. An exact replay returns `WasAlreadyApplied=true`
without changing Domain revision, chapter advancement, or persistence revision. Reusing
the same sequence with changed command content returns `SequenceConflict`; reusing an
outcome correlation with changed content or outcome kind returns `OutcomeConflict`.

## Transaction and chapter decisions

Every successful room outcome returns an immutable `OverworldCommitPlan`. Boss Victory
orders exactly:

1. `AcceptReward`
2. `ResolveRoom`
3. `UnlockSuccessors`
4. `AdvanceChapter`
5. `CommitPersistence`

Non-Boss Victory omits only `AdvanceChapter`. Event/Shop completion orders room
resolution, successor unlock, and persistence. Defeat and cancellation record their
non-settling result and then persistence, so they cannot masquerade as completion.

Boss Victory uses Gate A's `ChapterAdvanced` result and exposes one typed
`OverworldChapterAdvanceDecision`: `NextChapter` with the next numeric chapter, or
`FinalChapter`. The final chapter number is supplied by Composition; no narrative or
chapter-count rule was invented. Exact replay leaves `ChapterAdvanceCount` at one.

## Persistence snapshot

`OverworldPersistenceSnapshot` is immutable, versioned, and file-I/O free. It contains:

- `SchemaVersion` and `MapConfigVersion`
- run kind as an integer code, run ID, seed, seed text, chapter/final chapter
- character, Era, phase, timecoins, and ordered deck stable IDs
- deterministic map fingerprint and primitive map generation config values
- current/active room stable IDs and active launch correlation ID
- chapter completion/advance count, Domain revision, successful operation cursor, and
  persistence revision
- ordinally ordered visited/settled stable IDs
- ordinally ordered processed outcome kind/correlation/fingerprint records

All exposed collections are read-only defensive copies. The persistence DTO exposes no
`Dictionary`, `object`, Unity object, or Domain state instance. `OperationSequenceCursor`
means the greatest successfully committed non-replay Application operation; exact
outcome replays do not advance it.

## Assumptions

- `RunStartPayload.Chapter` must equal `OverworldMapGenerationConfig.Chapter`.
- Composition supplies `finalChapter`; Application only produces the typed decision.
- `CombatOutcome.TryCreate` remains the authority proving Victory reward acceptance.
- Battle tag and battle seed remain caller-owned existing combat selection inputs; this
  slice does not invent encounter or enemy selection rules.
- Persistence I/O and atomic replacement belong to the Persistence/Composition owners.

## Required primary-agent integration and gaps

1. Gate A currently has no restore/rehydration API and does not expose its enter/resolve
   operation journal. The smallest required Domain addition is a validated rehydrate
   boundary accepting current node, visited/settled IDs, optional active room, revision,
   completion/advance count, and the operation journal needed to preserve exact replay
   and conflict behavior. Without that API, the Application snapshot can be serialized
   and validated but cannot reconstruct an identical `OverworldChapterState` after
   Continue or roll back an in-memory commit after persistence failure.
2. Existing `SceneFlowStateStore` and `OutOfBattleShellState` do not yet own or bind this
   coordinator. `OutOfBattleShellState` also retains its historical settled-room set.
   Primary integration must make Gate A the room-lifecycle authority and treat the old
   set only as a compatibility projection; it must not create a parallel map authority.
3. The commit plan is declarative and deliberately performs no file I/O. Primary
   Composition must execute `CommitPersistence` last through the Persistence port and
   use the Domain rehydrate boundary for rollback/recovery semantics.

## Verification

- Unity-free runtime plus tests compiled with Unity's bundled Mono `csc.exe`: passed.
- Reflection invocation of all owned NUnit `[Test]` methods: `10 passed, 0 failed`.
- Runtime scan for `UnityEngine`, `UnityEditor`, and `Unity.*`: no matches.
- Public-contract scan: no public `Dictionary`, `object`, `OverworldMapDefinition`, or
  `OverworldChapterState` properties.
- Five new Unity meta GUIDs are present exactly once.
- No staging, commit, push, branch, stash, reset, checkout, clean, or Unity process was
  used.

The primary agent should run the authoritative Unity filter:

```powershell
$env:TIMEKEY_REPOSITORY_ROOT='D:\godot\时之钥\时之钥'
& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' `
  -batchmode -force-d3d12 -projectPath 'D:\timekey-unity-731' `
  -runTests -testPlatform EditMode `
  -testFilter 'TimeKey.Tests.EditMode.OverworldApplication' `
  -testResults 'D:\timekey-wave03-gate-b\editmode-overworld-application.xml' `
  -logFile 'D:\timekey-wave03-gate-b\editmode-overworld-application.log'
```
