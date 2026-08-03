# Overworld Gate B Save Schema And Migration

## Ownership

- Agent-owned runtime writes are limited to
  `unity/Assets/_Project/Runtime/Infrastructure/Persistence/**` and its folder meta.
- Agent-owned tests are limited to
  `unity/Assets/_Project/Tests/EditMode/OverworldPersistence/**` and its folder meta.
- No Domain, Application, SceneFlow, live state store, Composition, Presentation,
  Scene/Prefab, asmdef, ProjectSettings, shared progress document, or Git state was
  changed. Unity was not started.
- The repository accepts a caller-provided absolute or relative file path. It does not
  choose or touch a production user-save location.

## Persistence Boundary

`OverworldSaveDocument` is an immutable, schema-2 persistence-owned document. It uses
only primitive scalars, stable string identities, immutable outcome entries, and
ordered read-only collections. It does not serialize Domain objects, Application
coordinators, dictionaries, `object`, or Unity objects. The field names deliberately
align with the Gate B Application snapshot so the primary agent needs a visible mapping,
not another state machine.

| JSON field | Type | Validation / ordering |
| --- | --- | --- |
| `schemaVersion` | integer | Must equal current version `2`; schema `1` migrates by adding the operation journal defaults. |
| `runStartKind` | integer | Non-negative primitive value of the existing run-start kind. |
| `runId` | string ID | Required. |
| `runSeed` | integer | Preserved exactly, including negative seeds. |
| `seedText` | string | Preserved; null normalizes to empty. |
| `chapter`, `finalChapter` | integers | `chapter >= 1`, `finalChapter >= chapter`. |
| `mapFingerprint` | string ID | Required deterministic topology fingerprint. |
| `mapConfigVersion` | integer | Positive generator contract version. |
| `intermediateLayerCount` | integer | Gate A range `1..32`. |
| `minimumNodesPerLayer`, `maximumNodesPerLayer` | integers | Gate A range and ordering, maximum `16`. |
| `currentNodeId` | string ID | Required and present in `visitedNodeIds`. |
| `activeRoomId` | optional string ID | Empty when no room is active; otherwise current and unsettled. |
| `activeLaunchCorrelationId` | optional string ID | Empty without an active room; may be empty for typed Event/Shop rooms. |
| `chapterCompleted` | boolean | Must agree with `chapterAdvanceCount`. |
| `chapterAdvanceCount` | integer | `0` or `1`, preserving Gate A single-advance behavior. |
| `domainRevision` | integer | Non-negative scalar cursor. |
| `operationSequenceCursor` | long integer | Non-negative operation cursor. |
| `persistenceRevision` | integer | Non-negative save revision owned by the Application/composition adapter. |
| `characterId` | string ID | Existing Continue identity. |
| `era`, `phase`, `timecoins` | integers | Positive era/phase, non-negative timecoins. |
| `deckStableIds` | ordered string IDs | Defensive read-only copy; deck order and duplicates are preserved. |
| `visitedNodeIds`, `settledNodeIds` | ordered string IDs | Defensive copy, ordinal sort, duplicate rejection; settled is a subset of visited. |
| `processedOutcomes` | ordered immutable entries | Each has `outcomeKind`, `outcomeCorrelationId`, and `fingerprint`; unique correlation IDs, ordinal sort. |
| `domainJournal` | ordered immutable entries | Gate A operation kind, identity, expected/before/after revision, result and chapter-advance bit; unique sequence, ascending order. |

`OverworldSaveLoadResult` distinguishes `Missing`, `Valid`, `Migrated`, `Corrupt`,
`UnsupportedFutureVersion`, and `IoFailure`. It also identifies `Primary` versus
`Backup` and exposes `RecoveredFromBackup` without changing the result taxonomy.

## Atomic Write Sequence

1. Serialize the immutable document in deterministic property order.
2. Parse and schema/content-validate the serialization before touching the filesystem.
3. Delete only the repository's stale `.tmp` path.
4. Write the complete temporary file with UTF-8 without BOM, `WriteThrough`, writer
   flush, and `FileStream.Flush(true)`.
5. Read the flushed temporary file back, parse it, schema-validate it, and compare all
   normalized document fields to the requested document.
6. If no primary exists, atomically move the accepted temporary file into place.
7. If the primary is valid/migratable, use `File.Replace(temp, primary, backup)` so the
   prior primary becomes the recoverable `.bak` file.
8. If the primary is corrupt/future and the backup is valid, replace the primary with
   no backup target so the valid backup is not overwritten by bad data.
9. Delete the temporary file in `finally` after success or any reported failure.

`IOverworldSaveWriteFaultInjector` has deterministic hooks at `BeforeTempWrite`,
`BeforeTempValidation`, and `BeforeReplace`. Tests inject an I/O exception or corrupt
the flushed temp file at those exact stages. Each failure returns a typed write failure,
leaves the previous primary readable, exposes no partial new state, and removes `.tmp`.

## Migration Matrix

| Input | Result | Transform |
| --- | --- | --- |
| Missing primary and backup | `Missing` | None. |
| Schema `2` | `Valid` | Strict constructor/schema validation; no transform. |
| Schema `1` | `Migrated` | Preserve all schema-1 fields and add an empty Gate A operation journal; processed outcomes retain their old identity fields. |
| Schema `0` approved fixture | `Migrated` | Preserve run/map/resource/node collections; default `mapConfigVersion=1` to the Gate B generator contract, empty active room/launch, incomplete chapter/count `0`, operation cursor `0`, persistence revision `0`, and empty processed outcomes. |
| Schema greater than `1` | `UnsupportedFutureVersion` | Never deserialized as current data. |
| Negative/missing/non-integer schema or invalid fields | `Corrupt` | None. |
| Read/access/path failure | `IoFailure` | None. |

V0 is a deliberately narrow fixture-only predecessor for proving the migration path. It
does not claim that a shipped historical save format existed, and it introduces no
undocumented gameplay or economy fields.

## Recovery Behavior

- A valid or migrated primary wins.
- When the primary is missing, corrupt, future-version, or unreadable, a valid/migrated
  backup is returned with `Source=Backup` and `RecoveredFromBackup=true`.
- If neither copy is usable, the primary's more specific non-missing failure is returned;
  otherwise the backup result is returned.
- Saving over a valid primary refreshes backup with the complete previous primary.
- Saving while the primary is corrupt/future preserves an already-valid backup.

## Files

- `unity/Assets/_Project/Runtime/Infrastructure/Persistence.meta`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveDocument.cs`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveDocument.cs.meta`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveRepository.cs`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveRepository.cs.meta`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveResults.cs`
- `unity/Assets/_Project/Runtime/Infrastructure/Persistence/OverworldSaveResults.cs.meta`
- `unity/Assets/_Project/Tests/EditMode/OverworldPersistence.meta`
- `unity/Assets/_Project/Tests/EditMode/OverworldPersistence/OverworldSaveRepositoryTests.cs`
- `unity/Assets/_Project/Tests/EditMode/OverworldPersistence/OverworldSaveRepositoryTests.cs.meta`
- this report

## Verification

- Unity-bundled Roslyn static compile of the three runtime files against Unity's
  Newtonsoft.Json assembly: passed.
- Unity-bundled Roslyn static compile of the EditMode test source against the runtime
  output, NUnit, and UnityEngine.CoreModule: passed.
- Standalone reflection execution of all 12 NUnit test methods without starting Unity:
  12 passed, 0 failed. This covered round trip, deterministic ordering, defensive
  copies, missing/corrupt/future loads, V0 migration, backup recovery, all three write
  failure stages, temp cleanup, no partial new state, and future-primary backup
  preservation.
- Unity Test Runner remains authoritative and is owned by the primary agent.

Primary-agent test filter:

```powershell
$env:TIMEKEY_REPOSITORY_ROOT='D:\godot\时之钥\时之钥'
& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' `
  -batchmode -force-d3d12 -projectPath 'D:\timekey-unity-731' `
  -runTests -testPlatform EditMode `
  -testFilter 'TimeKey.Tests.EditMode.OverworldPersistence' `
  -testResults 'D:\timekey-wave03-overworld-gate-b\editmode-persistence.xml' `
  -logFile 'D:\timekey-wave03-overworld-gate-b\editmode-persistence.log'
```

## Integration Gaps

- Gate A currently has no restore/rehydration API or operation-journal export. This
  repository can recover a validated document, but the primary agent cannot reconstruct
  the live `OverworldChapterState` exactly without the minimal Domain restore seam
  already identified by the Application agent. Persistence does not work around that
  gap with a second state machine.
- The primary agent owns the explicit mapper between Application
  `OverworldPersistenceSnapshot` and `OverworldSaveDocument`, the production path, save
  timing, and composition.
- `TimeKey.Tests.EditMode.asmdef` does not currently list `TimeKey.Infrastructure`
  directly. If Unity's compilation does not expose it through the existing Editor
  reference closure, the primary agent must add that single asmdef reference because
  this agent was prohibited from changing asmdefs.
