# Landing 5: tile destruction batch queue

Date: 2026-06-04

## Scope

- Extract height-limit tile destruction queue orchestration from `hex_map.gd`.
- Keep the actual tile deletion, map data mutation, global height pool cleanup, and signals inside `hex_map.gd`.

## Completed Modules

- `HexCoordRules`: fan/circular coordinate sampling and axial-to-pixel conversion.
- `HexTerrainRules`: fan tier calculation, height rolls, circular room height range, height-to-terrain fallback, terrain/landform name mapping.
- `TileDestructionBatchQueue`: queue metadata, batch collection delay, batch sizing, inter-batch interval, and wait-until-destroyed coordination.

## Code Changes

- Added `scene/in_scene/TileDestructionBatchQueue.gd`.
- Replaced the old pending/running state in `hex_map.gd` with `_tile_destruction_queue`.
- Reduced `_queue_tile_destruction_and_wait()` to a thin delegate that passes current tuning exports and `_perform_tile_destruction` as a callback.
- Removed `hex_map.gd`'s private queue runner helpers:
  - `_run_height_limit_destruction_batches`
  - `_perform_queued_tile_destruction`
  - `_wait_for_height_limit_destruction_batch`

## Still To Extract

- Target validation and card range checks.
- Enemy intent preview presentation.
- Settlement reward highlighting and tooltip behavior.
- Height view visual state and pillar/label management.
- Landform placement and runtime landform registration.
- Tile destruction data mutation can be revisited later, but only after the queue behavior has been manually regression-tested in combat.

## Verification

- `git diff --check`: passed.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no queue script parse error was introduced.
