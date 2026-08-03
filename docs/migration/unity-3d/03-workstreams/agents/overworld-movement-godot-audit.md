# Agent Prompt: Godot Overworld Movement Audit

## Role And Single Goal

You are a read-only source auditor. Freeze the observable Godot overworld
movement, camera, room-entry and return-resolution semantics needed by Wave 03P.

You are not alone in this repository. Do not revert, format, stage, commit or
overwrite any other worker's changes.

## Exclusive Ownership

You may create or update only:

- `docs/migration/unity-3d/03-workstreams/agents/reports/overworld-movement-godot-audit.md`

## Required Inputs

- `scene/out_scene/Out_Scene.tscn`
- `scene/out_scene/out_scene_map_exp.gd`
- `scene/out_scene/map_generator.gd`
- `scene/out_scene/map_renderer.gd`
- `scene/out_scene/camera_2d_outscene.gd`
- `scene/out_scene/point.gd`
- `scene/out_scene/HexUtils.gd`
- `scene/out_scene/out_scene_modules/**`
- `scene/global/map_data.gd`
- `scene/global/Saver.gd`
- relevant in/out payload and room-resolution call sites found with `rg`
- Wave 03P Prompt and gap analysis

## Output Requirements

Report exact symbols and source lines for axial coordinates, all six directions,
click legality, current/available/locked/visited/settled states, 5 px drag
threshold, wheel/keyboard camera behavior, bounds, 0.3 second move and camera
follow, 0.5 second room delay, cancel/rollback, save timing, room identity,
return outcome and the duplicate `HEX_DIRS` anomaly. Distinguish verified facts,
inference and unresolved behavior. Propose a minimal typed contract and test
matrix, but do not implement it.

## Forbidden Paths And Actions

- Do not modify Godot source, scenes, themes, imports or project settings.
- Do not modify Unity, shared docs, prompts, intake, evidence or Git.
- Do not start Godot, Unity, Blender, build, tests or screenshot automation.
- Do not create a branch, stash, stage, commit or push.

## Read-Only Checks

Use `rg`, `Get-Content`, scene/resource text inspection and dependency search.
Record commands and missing references. Do not infer behavior from README alone.

## Non-Goals

No Unity implementation, complete procedural generation, events, shop, Boss,
save migration design or visual redesign.

## Stop Condition

Stop after the exclusive report contains source-backed semantics, contradictions,
minimum contract, test matrix, risks and a clear ownership return statement.
