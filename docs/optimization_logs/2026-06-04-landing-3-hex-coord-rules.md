# Landing 3: hex coordinate rules

Date: 2026-06-04

## Scope

- Extract pure battle-map coordinate rules from `hex_map.gd`.
- Keep `hex_map.gd` wrapper methods in place so existing call sites and behavior stay unchanged.

## Code changes

- Added `scene/in_scene/HexCoordRules.gd`.
- Moved the fan coordinate sampler, circular coordinate sampler, axial distance formula, and axial-to-pixel conversion into static rule functions.
- Updated `scene/in_scene/hex_map.gd` to preload `HexCoordRules` and delegate through the existing `_get_fan_coords()`, `_get_circular_coords()`, and `_get_hex_pixel_pos()` wrappers.

## Verification

- `git diff --check`: passed.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no coordinate module parse error was introduced.

## Remaining risks

- This is only the first extraction slice. Terrain height rules, target validation, and enemy intent candidate sorting are still mixed into `hex_map.gd`.
- A later pass can consider merging shared axial helpers with `scene/out_scene/HexUtils.gd`, but this landing intentionally avoids touching the out-scene map.
