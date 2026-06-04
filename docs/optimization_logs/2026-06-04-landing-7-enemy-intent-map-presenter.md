# Landing 7: enemy intent map presenter

Date: 2026-06-04

## Scope

- Extract map-side enemy intent presentation from `hex_map.gd`.
- Keep the public `HexMap.show_enemy_intent_preview()` and `HexMap.clear_enemy_intent_preview()` API unchanged.
- Keep enemy intent resolution, timeline presentation, and tooltip coordination in the existing enemy intent system.

## New Module

### `scene/in_scene/EnemyIntentMapPresenter.gd`

Responsibility:
- Highlight the intent source tile by writing tile sprite shader instance parameters.
- Cache and restore the source tile's previous shader state.
- Create/reuse target ripple overlay sprites on target stacks.
- Hide target overlays when the preview is cleared.
- Apply target ripple shader parameters from HexMap tuning config.

Functions:
- `show(intent_data, stack_nodes, source_color, target_color, config)`
  Displays one intent on the map using already-resolved `EnemyIntentData`.
- `clear()`
  Restores source highlight and hides all target overlays.
- `_get_source_blend_config(intent_data, config)`
  Selects valid/self/invalid source blend strengths.
- `_show_source_highlight(...)`
  Applies source tile highlight and records restore data.
- `_show_target_overlay(...)`
  Shows or creates one target ripple overlay.
- `_hide_overlay(...)`
  Hides an overlay without freeing it.
- `_restore_source_highlight(...)`
  Restores cached shader values.
- `_ensure_overlay(...)`
  Creates/reuses a stack child `Sprite2D` with independent `ShaderMaterial`.
- `_update_overlay_transform(...)`
  Aligns overlay to the stack hitbox and scales it from texture size.
- `_configure_overlay_material(...)`
  Writes target ripple shader tuning values.

## HexMap Changes

- Added `_enemy_intent_map_presenter`.
- Reduced `show_enemy_intent_preview()` to a delegating wrapper.
- Reduced `clear_enemy_intent_preview()` to a delegating wrapper plus optional card-hover refresh.
- Added `_build_enemy_intent_map_presenter_config()` to collect current exported values.
- Removed map-side private helpers from `hex_map.gd`:
  - `_show_source_intent_highlight`
  - `_show_target_intent_overlay`
  - `_hide_intent_overlay`
  - `_restore_source_intent_highlight`
  - `_ensure_intent_overlay`
  - `_update_intent_overlay_transform`
  - `_configure_intent_overlay_material`

## Exported Tuning Variables

These remain on `hex_map.gd` and are passed into the presenter through `_build_enemy_intent_map_presenter_config()`:

- `enemy_intent_frame_tex`
- `enemy_intent_target_shader`
- `enemy_intent_overlay_scale`
- `enemy_intent_overlay_z_index`
- `enemy_intent_source_highlight_width`
- `enemy_intent_source_valid_highlight_blend`
- `enemy_intent_source_valid_selected_blend`
- `enemy_intent_source_self_highlight_blend`
- `enemy_intent_source_self_selected_blend`
- `enemy_intent_source_invalid_highlight_blend`
- `enemy_intent_source_invalid_selected_blend`
- `enemy_intent_target_ripple_speed`
- `enemy_intent_target_ripple_density`
- `enemy_intent_target_min_alpha`
- `enemy_intent_target_max_alpha`

Position/size values consumed for overlay placement:

- `hitbox_width`
- `hitbox_base_height`
- `hitbox_offset_x`
- `hitbox_offset_y`

## Adjustment Notes

- To change target ripple appearance, adjust the `enemy_intent_target_*` exports on HexMap.
- To change source tile highlight strength, adjust the `enemy_intent_source_*_blend` exports on HexMap.
- `enemy_intent_source_highlight_width` is currently preserved in the config list but source highlighting writes existing tile sprite shader parameters directly; if the tile shader supports width tuning later, wire it in `EnemyIntentMapPresenter._show_source_highlight()`.
- Target overlay nodes are reused instead of freed to avoid repeated hover allocations.

## Verification

- `git diff --check`: passed before writing this log.
- Pending after this log:
  - Godot project headless load.
  - Godot combat scene headless load.
  - Manual hover regression in editor when available.

## Manual Regression Focus

- Hover an enemy with a valid intent: source should highlight and target tiles should ripple.
- Hover an enemy whose intent includes itself: source highlight should use the self-included strength/color supplied by `EnemyIntentPresentationController`.
- Hover an invalid intent: source and target visuals should use invalid colors/blends.
- Move hover away: source shader values should restore and target overlays should hide.
