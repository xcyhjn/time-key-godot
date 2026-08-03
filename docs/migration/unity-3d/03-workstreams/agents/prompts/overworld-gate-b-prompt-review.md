# Overworld Gate B Multi-Agent Prompt Review

## Decision

Approved for parallel dispatch after the Era Clock checkpoint. The three prompts have disjoint write allowlists, require no concurrent Unity process, and leave all shared integration to the primary agent.

## Ownership proof

| Agent | Writable code/tests | Writable report | Explicitly excluded shared state |
| --- | --- | --- | --- |
| Application Contract | new `Runtime/Application/Overworld/**`, new `Tests/EditMode/OverworldApplication/**` | `overworld-gate-b-application-contract.md` | existing SceneFlow/Application files, Domain, persistence, state store, Scene/Prefab, asmdef |
| Persistence | new `Runtime/Infrastructure/Persistence/**`, new `Tests/EditMode/OverworldPersistence/**` | `overworld-gate-b-save-schema-and-migration.md` | Domain, Application, SceneFlow, MainMenu, formal save path, Scene/Prefab, asmdef |
| Integration Audit | none | `overworld-gate-b-integration-audit.md` | all code/tests/assets/evidence/shared docs |
| Primary | existing Application/SceneFlow contracts, `SceneFlowStateStore`, MainMenu Continue, Composition, formal Scene/Prefab, shared asmdef, harness/evidence/docs/Git | final Gate B reports | does not write inside agent-owned new directories until handoff/review |

No path appears in more than one agent allowlist. Report filenames are distinct. The Application and Persistence agents can compile independently because persistence owns a primitive document boundary and does not depend on the concurrent Application implementation.

## Dependency review

- Gate A remains the sole topology and room-lifecycle state machine. Application coordinates it; persistence serializes primitives; neither recreates it.
- Existing `CombatLaunchPayload`/`CombatOutcome` remain the combat cross-scene boundary. Event/Shop receive separate typed outcomes.
- Infrastructure performs file I/O but does not choose the production path or mutate live state. Composition will map the final Application snapshot to the repository.
- Presentation/Composition may depend on Application/Domain/Infrastructure after primary integration. No lower layer depends on Presentation, Composition, Scene, Prefab, or Unity object state.
- Existing shared contracts and all rollback/commit ownership remain primary-agent writes, preventing concurrent edits to the same state authority.

## Safety review

- Every prompt states that the agent is not alone, forbids branch/stash/reset/checkout/clean/stage/commit/push, and forbids concurrent Unity.
- Agent tests use only owned test directories and temporary files. Canonical evidence, real save paths, ProjectSettings, build artifacts, and shared docs are excluded.
- Conflicts with Gate A, `MapNodeId`, or SceneFlow must be reported, not hidden behind duplicate adapters or state machines.

## Dispatch order

Dispatch all three after confirming staging is empty and no Unity/Player writer exists. The primary agent may inspect existing shared call sites in parallel but must wait for handoff before editing any agent-owned directory. Review all delivered diffs before Unity compilation or shared integration.
