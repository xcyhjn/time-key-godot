# Landing 4: hex terrain rules

Date: 2026-06-04

## Scope

- Extract pure terrain and height rules from `hex_map.gd`.
- Keep `hex_map.gd` wrapper functions so existing call sites remain unchanged.

## Completed Modules

- `HexCoordRules`: fan/circular coordinate sampling and axial-to-pixel conversion.
- `HexTerrainRules`: fan tier calculation, height rolls, circular room height range, height-to-terrain fallback, terrain/landform name mapping.

## Code Changes

- Added `scene/in_scene/HexTerrainRules.gd`.
- Updated `scene/in_scene/hex_map.gd` to delegate:
  - fan tier calculation
  - tier height rolls
  - circular room height rolls
  - terrain lookup by height
  - terrain and landform debug names

## Still To Extract

- Target validation and card range checks.
- Enemy intent preview presentation.
- Settlement reward highlighting and tooltip behavior.
- Height view visual state and pillar/label management.
- Tile destruction queue and height-limit destruction batches.
- Landform placement and runtime landform registration.

## Verification

- `git diff --check`: passed.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no terrain rules parse error was introduced.
