# Combat Shell Gate E Agent: stability tests

## Ownership

Only add `unity/Assets/_Project/Tests/PlayMode/SceneFlow/CombatShellGateEStabilityTests.cs`, matching `.meta`, and `docs/migration/unity-3d/03-workstreams/agents/reports/combat-shell-gate-e-stability.md`.

## Task

Add graphical PlayMode coverage for three consecutive typed Victory round trips in one Bootstrap lifetime using three stable room IDs. Each cycle must verify run/launch/outcome identity, settled-once behavior, active-launch closure, exactly one persistent Bootstrap/SceneFlow/EventSystem/Audio/Transition owner, exactly one active content entry/camera, no old combat controller after return, and no content-scene growth. Include a 1280x720 -> 2560x1080 resize assertion for the formal shell layout and focus/input-lock recovery. Record bounded managed-memory samples without pretending they are GPU proof; assert no strict monotonic growth across all three post-GC samples and use a generous final-minus-first budget. Do not edit production, Scene/Prefab, Editor/build, shared docs, Git state or evidence directories. Do not run Unity while another Unity process exists. Return ownership without stage/commit/push.
