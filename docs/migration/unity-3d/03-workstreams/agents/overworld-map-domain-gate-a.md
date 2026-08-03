# Wave 03 Overworld Map Domain Gate A Prompt

## Ownership

Own only new Unity-free `unity/Assets/_Project/Runtime/Domain/Overworld/**` and `unity/Assets/_Project/Tests/EditMode/Overworld/**`, plus this report's matching path. Do not touch `Runtime/Application`, `Runtime/Composition`, existing `Runtime/Presentation`, formal Scene/Prefab, asmdef, ProjectSettings, Build Settings, shared docs, evidence, or Git.

## Objective

Audit Godot `scene/out_scene/map_generator.gd`, `map_renderer.gd`, `out_scene_map_exp.gd` and related room-resolution scripts, then implement the smallest deterministic map-definition slice. Reuse `TimeKey.Domain.OverworldMovement.MapNodeId`; do not introduce another node identity type. Keep immutable snapshots and defensive collections. Prove same seed/config identity/topology/type sequence, different-seed observable variance, no duplicate/self/cross-layer edge, entry-to-Boss reachability, and atomic room enter/resolve idempotency.

## Stop condition

Return an audit report, file list and EditMode command. Do not run Unity in parallel with the main agent. Stop after the pure Domain/tests slice; SceneFlow, persistence, events, shops, Boss UI and final Git remain with the main agent.
