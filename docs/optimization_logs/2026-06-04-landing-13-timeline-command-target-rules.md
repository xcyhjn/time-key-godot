# Landing 13: timeline command target rules

Date: 2026-06-04

## Scope

- Extract repeated runtime target validation from timeline command scripts.
- Keep command scripts responsible for execution side effects: VFX, damage/heal/status calls, creation registration, and wait timing.
- Keep `HexTargetRules.gd` as the pre-placement/user-facing rule module; this landing adds the final execution-time safety layer.

## New Module

### `scene/in_scene/timeline/commands/TimelineCommandTargetRules.gd`

Responsibility:
- Read a live `occupant` from a target stack.
- Resolve a stack coordinate from `hex_map.stack_nodes`.
- Validate build targets against stack occupant and `map_data`.
- Validate damage, recover/heal, and poison entity recipients.
- Provide a shared dynamic property checker for command scripts.

Functions:
- `get_occupant(tile)`
  Reads a live Node from stack `occupant` metadata.
- `get_tile_coord(tile, hex_map)`
  Finds a stack coordinate in `hex_map.stack_nodes`.
- `can_build_on_tile(tile, hex_map)`
  Final runtime build validation.
- `is_map_data_empty_at(coord, hex_map)`
  Checks `map_data.landform` and `map_data.landform_in`.
- `can_damage_entity(entity)`
  Requires `take_damage()` and alive HP when present.
- `can_recover_entity(entity)`
  Requires `heal()` and not-full HP when HP fields exist.
- `can_apply_poison(entity)`
  Requires `add_status()` and alive HP when present.
- `object_has_property(target, property_name)`
  Shared dynamic property lookup.

## Command Changes

### `BuiltCommand.gd`

- Added `TARGET_RULES` preload.
- Replaced `_can_build_on_tile()`, `_get_tile_coord()`, `_is_map_data_empty_at()`, and `_object_has_property()` with calls to `TimelineCommandTargetRules`.
- Added function comments to `_init()`, `execute()`, `_create_creation()`, `_register_creation()`, and `_notify_map_changed()`.

### `DamageCommand.gd`

- Added `TARGET_RULES` preload.
- Uses `TARGET_RULES.get_occupant()` and `TARGET_RULES.can_damage_entity()`.
- Added function comments to `_init()` and `execute()`.

### `RecoverCommand.gd`

- Added `TARGET_RULES` preload.
- Removed local `_can_recover_entity()`.
- Uses `TARGET_RULES.get_occupant()` and `TARGET_RULES.can_recover_entity()`.
- Added function comments to `_init()` and `execute()`.

### `PoisonCommand.gd`

- Added `TARGET_RULES` preload.
- Removed local `_can_apply_poison()`.
- Uses `TARGET_RULES.get_occupant()` and `TARGET_RULES.can_apply_poison()`.
- Updated header comments and added function comments to `_init()` and `execute()`.

### `ElevationCommand.gd`

- Added `TARGET_RULES` preload.
- Uses `TARGET_RULES.object_has_property()` for `elevation_resolution_padding`.
- Added function comments to `_init()`, `execute()`, and `_wait_for_tiles_to_finish()`.

## Exported Tuning Variables

No new exported variables were added.

Runtime data consumed:

- `EffectCommand.target_tiles`
- `EffectCommand.hex_map`
- stack `occupant` metadata
- `hex_map.stack_nodes`
- `hex_map.map_data`
- entity methods/properties:
  - `take_damage`
  - `heal`
  - `add_status`
  - `HP`
  - `Max_Blood`

## Adjustment Notes

- Add new command recipient checks in `TimelineCommandTargetRules.gd`.
- Keep gameplay side effects inside each command class.
- Keep card JSON/effect parsing in `EffectProcessor.gd`.
- Keep placement preview and click gating in `HexTargetRules.gd`.
- If `HexTargetRules` changes a target rule, check whether this final safety layer should change too.

## Verification

Run after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Manual regression focus:
- Damage command still damages live targets and ignores empty/dead stacks.
- Recover command still heals damaged healable entities and ignores full HP entities.
- Poison command still applies poison to live status-capable entities.
- Built command still creates tower entities only on empty stacks.
- Elevation command still waits for height animation/destruction completion.
