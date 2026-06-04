# Landing 6: hex target rules

Date: 2026-06-04

## Scope

- Extract the map-side target/range entry points from `hex_map.gd`.
- Preserve current behavior: a stack is valid when there is a selected card and the stack is valid.
- Do not introduce new card targeting restrictions in this landing.

## Completed Modules

- `HexCoordRules`: fan/circular coordinate sampling and axial-to-pixel conversion.
- `HexTerrainRules`: fan tier calculation, height rolls, circular room height range, height-to-terrain fallback, terrain/landform name mapping.
- `TileDestructionBatchQueue`: height-limit destruction queue orchestration.
- `HexTargetRules`: selected-card target validation entry point and card effect-range stack collection.

## Code Changes

- Added `scene/in_scene/HexTargetRules.gd`.
- Updated `hex_map.gd` to delegate:
  - `_is_stack_valid_target()`
  - AOE effect-range stack collection inside `_update_aoe_display()`
- Kept AOE shader state changes, tooltip updates, occlusion updates, and card-manager lookup in `hex_map.gd`.

## Still To Extract

- Concrete card target restrictions, if gameplay needs them later.
- Enemy intent preview presentation.
- Settlement reward highlighting and tooltip behavior.
- Height view visual state and pillar/label management.
- Landform placement and runtime landform registration.

## Verification

- `git diff --check`: passed.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no target rules parse error was introduced.
