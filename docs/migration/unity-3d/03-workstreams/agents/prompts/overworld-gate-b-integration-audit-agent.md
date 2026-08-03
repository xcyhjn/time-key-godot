# Overworld Gate B Integration Audit Agent Prompt

## Objective

Perform a read-only audit of the existing MainMenu, Bootstrap SceneFlow, OutOfBattle shell, combat launch/outcome/reward path, GameOver cleanup, Gate A Domain, and Godot CFG/save sources. Produce the exact integration call points, rollback boundary, ownership risks, and minimum changes required for Gate B.

## Exclusive write ownership

You may write only:

```text
docs/migration/unity-3d/03-workstreams/agents/reports/overworld-gate-b-integration-audit.md
```

All source code, Scene/Prefab, tests, evidence, shared docs, and Git state are read-only. You are not the only worker in the repository.

## Audit questions

- Where new/seed/continue commands currently create `RunStartPayload`, and where a restored map/run can be injected without creating a second SceneFlow owner.
- Which `SceneFlowStateStore`, `OutOfBattleShellState`, navigation, reward, combat outcome, and GameOver methods own prepare/commit/rollback today.
- Exact identity fields that must survive map selection, combat launch, reward claim, outcome return, room settlement, save commit, and replay/conflict handling.
- The safest ordering for reward, map resolution/unlock, persistence commit, scene transition, focus restore, reveal completion, and input unlock.
- Which failures occur pre-commit versus post-commit, what current rollback restores, and the minimum main-agent changes needed to restore map node, cover, focus, and input without duplicate Bootstrap/content entry.
- Current Continue/save placeholders and existing JSON/file patterns that can be reused.
- Godot Event/Shop/Boss/save behavior that is actually observable and can be implemented without inventing content.
- Any hard conflict between Gate A restore needs and its current public API.

## Required output

Write a concise call-site table with absolute repo-relative paths and symbols, a state/identity flow, rollback matrix, ownership hazards, recommended main-agent edit order, and a list of tests that must be real additive PlayMode rather than pure unit tests. Clearly distinguish evidence from inference.

## Prohibitions

Do not edit code, tests, Scene/Prefab, prompts, shared docs, or evidence. Do not run Unity, stage, commit, push, switch branches, stash, reset, checkout, or clean.
