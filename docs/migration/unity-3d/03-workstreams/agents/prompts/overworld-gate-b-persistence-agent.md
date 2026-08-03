# Overworld Gate B Persistence Agent Prompt

## Objective

Add a new versioned, recoverable persistence boundary for the Overworld run/map snapshot. This task owns serialization and atomic file replacement only; it does not own map rules, SceneFlow, MainMenu Continue, or the live state store.

## Exclusive write ownership

You may write only:

```text
unity/Assets/_Project/Runtime/Infrastructure/Persistence.meta
unity/Assets/_Project/Runtime/Infrastructure/Persistence/**
unity/Assets/_Project/Tests/EditMode/OverworldPersistence.meta
unity/Assets/_Project/Tests/EditMode/OverworldPersistence/**
docs/migration/unity-3d/03-workstreams/agents/reports/overworld-gate-b-save-schema-and-migration.md
```

Temporary fixtures must live below the owned test directory and be deleted by test teardown. You are not the only worker in the repository; never revert or format files outside this list.

## Required persistence behavior

- Use an explicit schema version and immutable DTOs made only of primitives, stable string IDs, ordered/read-only collections, and scalar cursors. Do not serialize Domain objects, Application coordinators, `Dictionary`, `object`, or Unity objects.
- Freeze fields for run ID, seed, chapter/config version, current node, visited/settled IDs, optional active room identity, revision, operation sequence cursor, deck/resource identity needed by Continue, and Event/Shop state only where a typed field is already justified.
- Preserve the previous valid file until a fully written temporary file has been flushed, parsed, schema-validated, and accepted. Then atomically replace/move; keep a recoverable backup when replacing an existing save.
- Define typed load results for missing, valid, migrated, corrupt, unsupported-future-version, and I/O failure states. Corrupt/future data must not overwrite a valid backup.
- Provide the smallest approved old-version migration fixture. Do not invent undocumented legacy fields; migration may be a narrow V0-to-current transform with explicit defaults and a report explaining them.
- Make write failure injectable/deterministic in tests so temp-write, validation, and replace failures prove that the previous save remains readable.
- Do not choose the production save path or touch a real user save. The primary agent will own the path/composition adapter.

## Tests

Add focused EditMode tests for round trip, deterministic ordering, defensive DTO copies, missing file, corrupt JSON, future version, old version migration, backup recovery, replace failure, temp cleanup, and no partial new state. Use per-test temporary directories only.

## Dependency boundary

This agent may read Gate A and existing JSON/package patterns. Because the Application agent runs concurrently, do not require its concrete types to compile. Define a persistence-owned document/repository boundary that the primary agent can map to the final Application snapshot without a second state machine.

## Prohibitions

- Do not modify Domain, Application, SceneFlow, `SceneFlowStateStore`, MainMenu, Composition, Presentation, Scene/Prefab, asmdef, ProjectSettings, shared docs, formal saves, or Git state.
- Do not run Unity, stage, commit, push, switch branches, stash, reset, checkout, or clean.

## Handoff

Run static scans and `git diff --check` only for the owned paths. The report must include the schema table, atomic write sequence, migration matrix, failure recovery behavior, file list, and exact Unity test filter for the primary agent. Leave staging untouched.
