# Landing 10: runtime landform registrar

Date: 2026-06-04

## Scope

- Extract runtime landform registration and visual refresh logic from `hex_map.gd`.
- Keep scene-level signals, spawn VFX, interactivity refresh, and topology events in `hex_map.gd`.
- Preserve existing behavior for build cards, village expansion, radar summons, and old paths that write `map_data` first and then call `add_landform_visual_at()`.

## New Module

### `scene/in_scene/RuntimeLandformRegistrar.gd`

Responsibility:
- Write runtime landform fields back into `map_data`.
- Attach a `landform` entity to its stack and refresh the stack `occupant` metadata.
- Apply enemy/middle group membership for runtime entities.
- Call the landform's own `attach_visual()` extension point.
- Apply HexMap block shader parameters to the generated landform sprite.
- Recollect stack render sprites after runtime visual changes.

Functions:
- `register_landform(coord, entity, landform_type, context)`
  Registers a newly created runtime landform instance and returns a result dictionary.
- `refresh_visual_at(coord, context)`
  Compatibility entry for old code that already wrote `map_data` before requesting a visual refresh.
- `attach_entity_to_stack(coord, entity, stack, context)`
  Reparents the entity to the stack and recomputes its 3D anchor position.
- `apply_landform_group(entity)`
  Adds the entity to `Enemies` or `Middle`, preserving the old non-removal behavior.
- `cleanup_stack_sprites(stack)`
  Rebuilds stack `sprites` metadata from cached sprites, direct stack sprites, occupant sprites, and occupant child sprites.
- `_get_stack_height(stack)`
  Reads safe stack height metadata for anchors and shader values.
- `_get_stack_sprites(stack)`
  Safely reads existing stack `sprites` metadata.
- `_append_stack_render_sprite(list, sprite)`
  Adds one valid render node while filtering status icons and duplicates.
- `_write_map_data(coord, entity, resolved_type, map_data)`
  Writes `landform`, `landform_in`, and `landform_type`.
- `_attach_visual_sprite(coord, entity, stack, tile_data, context)`
  Calls `attach_visual()` and applies material/shader post-processing.
- `_resolve_landform_sprite(coord, entity, stack)`
  Prefers `entity.tex`, then falls back to `LandformSprite_x_y`.
- `_apply_landform_sprite_shader(sprite, height, context)`
  Applies duplicated block material and per-instance shader parameters.
- `_resolve_landform_type(entity, landform_type)`
  Uses the explicit type or falls back to `entity.landform_name`.
- `_is_enemy_landform(entity)`
  Returns whether the entity should trigger roster refresh.
- `_make_result(success, stack, is_enemy)`
  Builds the result consumed by `hex_map.gd`.

## HexMap Changes

- Added `RUNTIME_LANDFORM_REGISTRAR` preload and `_runtime_landform_registrar`.
- Reduced `register_runtime_landform()` to:
  - call `RuntimeLandformRegistrar.register_landform()`,
  - sync the stack to the current 3D/flat view,
  - play runtime spawn VFX,
  - emit `enemy_roster_changed` for enemy landforms,
  - refresh stack interactivity,
  - emit `tile_topology_changed`.
- Reduced `add_landform_visual_at()` to:
  - call `RuntimeLandformRegistrar.refresh_visual_at()`,
  - sync the stack to the current view,
  - emit `enemy_roster_changed` for enemy landforms.
- Reduced `_cleanup_stack_sprites()` to a wrapper around `RuntimeLandformRegistrar.cleanup_stack_sprites()`.
- Added `_build_runtime_landform_registrar_context()` so the registrar receives a context snapshot instead of reading HexMap members directly.

## Exported Tuning Variables

These remain on `hex_map.gd` and are passed through `_build_runtime_landform_registrar_context()`:

- `step_height`
- `tile_scale`
- `REF_SCALE`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- `current_view_state`

Runtime spawn exports remain owned by `hex_map.gd` because VFX is not part of registration:

- `runtime_landform_spawn_vfx_enabled`
- `runtime_landform_spawn_vfx_duration`

## Adjustment Notes

- To tune the logical entity anchor, adjust `hitbox_offset_x`, `hitbox_offset_y`, or `landform_instance_offset`.
- To tune the visible sprite offset, keep using the existing landform visual offset flow; this module does not change `landform_sprite_offset`.
- To disable runtime spawn visuals, turn off `runtime_landform_spawn_vfx_enabled`.
- To change the spawn effect speed, tune `runtime_landform_spawn_vfx_duration`.
- Do not emit `enemy_roster_changed` or `tile_topology_changed` from the registrar; those remain scene-level orchestration events in `hex_map.gd`.

## Verification

Run after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Manual regression focus:
- Play a build card and confirm the new entity appears on the selected stack.
- Trigger village/radar runtime spawns if available and confirm the new entity lands in the correct stack.
- Confirm enemy runtime spawns still refresh enemy timeline/roster consumers.
- Toggle flat height view before a runtime spawn and confirm the spawned entity is immediately aligned to the flat view.
- Confirm the spawned entity still receives block shader parameters and is included in hover/highlight effects.
