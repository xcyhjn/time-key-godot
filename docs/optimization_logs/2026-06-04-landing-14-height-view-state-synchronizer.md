# Landing 14: height view state synchronizer

Date: 2026-06-04

## Scope

- Extract flat/3D height-view cache and single-stack synchronization helpers from `hex_map.gd`.
- Keep the global height-view toggle flow, full-map compress/restore animation loops, and elevation mutation logic in `hex_map.gd`.
- Preserve runtime behavior for entities, health bars, collision nodes, settlement reward tooltip repositioning, and newly spawned landforms during flat view.

## New Module

### `scene/in_scene/HeightViewStateSynchronizer.gd`

Responsibility:
- Calculate flat-view drop distance for a stack.
- Build and update the 3D original-position cache.
- Read positions from `Node2D` and `Control` nodes.
- Sync node y positions directly or through HexMap's tween callback.
- Sync one runtime-changed stack to the current flat/3D state.
- Keep health-bar lookup and reward tooltip refresh decoupled through injected callbacks.

Functions:
- `get_drop_delta(stack, config)`
  Calculates flat-view y offset from height and spacing config.
- `get_stack_height(stack)`
  Reads safe stack height metadata.
- `get_or_create_cache(stack, sprites, config)`
  Builds or reuses the per-stack original-position cache.
- `get_or_add_sprite_cache(cache, sprite)`
  Adds runtime-created stack child sprites to cache.
- `get_visual_node_position(node)`
  Reads local position for `Node2D` or `Control`.
- `sync_node_position_y(node, target_y, animate, config)`
  Writes/tweens a node's y position.
- `sync_cached_node_to_flat(cache, key, node, drop_delta, animate, config)`
  Adds/updates cached node data and applies flat offset.
- `sync_stack_to_current_view(coord, animate, config)`
  Synchronizes a single stack after runtime visual changes.
- `_build_health_bar_cache_data(occupant, config)`
  Builds `health_bar_data` for landform occupants.
- `_find_health_bar_for_occupant(occupant, config)`
  Uses an injected callback to resolve health bars.

## HexMap Changes

- Added `HEIGHT_VIEW_STATE_SYNCHRONIZER` preload and `_height_view_state_synchronizer`.
- Converted these helpers into wrappers:
  - `_get_height_view_drop_delta()`
  - `_get_or_create_height_view_cache()`
  - `_get_or_add_sprite_cache()`
  - `_get_visual_node_position()`
  - `_sync_node_position_y()`
  - `_sync_cached_node_to_flat()`
  - `_sync_stack_to_current_view()`
- Added `_build_height_view_state_synchronizer_config()`.
- Added `_update_settlement_reward_tooltip_position_for_stack()` as a callback bridge.

## Exported Tuning Variables

No new exported variables were added.

The synchronizer consumes existing runtime/export data through config:

- `stack_nodes`
- `height_view_original_materials`
- `filler_block_spacing`
- `tile_scale`
- `REF_SCALE`
- `current_view_state`
- `_cleanup_stack_sprites`
- `_find_health_bar_for_landform`
- `_tween_position_y`
- `_settlement_reward_presenter.update_tooltip_position`

## Adjustment Notes

- To tune flat-view vertical offset, continue adjusting `filler_block_spacing`, `tile_scale`, and `REF_SCALE` flow.
- To change tween duration for sync wrapper calls, update `position_tween_duration` in `_build_height_view_state_synchronizer_config()`.
- Do not move full-map `_compress_to_single_height_view()` and `_restore_original_height_view()` into this module until manual height-view regression is stable.
- Do not mutate tile height or `map_data` inside this module; elevation changes remain in `hex_map.gd`.
- Reward tooltip positioning is callback-driven so this module does not depend on `SettlementRewardPresenter`.

## Verification

Run after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Manual regression focus:
- Toggle into flat height view and confirm top sprites, occupants, collision nodes, and health bars drop together.
- Spawn or build a runtime landform while flat view is active and confirm it immediately aligns to the flat plane.
- Trigger settlement reward tooltip display and confirm tooltip position updates after flat/3D switch.
- Toggle back to 3D and confirm cached original positions restore correctly.
- Use elevation while flat view is active and confirm cached positions remain stable.
