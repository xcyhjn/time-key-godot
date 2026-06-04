# Landing 9: height view indicator presenter

Date: 2026-06-04

## Scope

- Extract height-view pillar and label presentation from `hex_map.gd`.
- Keep height-view state switching, stack flatten/restore logic, and elevation data mutation in `hex_map.gd`.
- Preserve current behavior for pillar creation, label creation, hover floating, and flat-view height label bounce.

## New Module

### `scene/in_scene/HeightViewIndicatorPresenter.gd`

Responsibility:
- Create `HeightIndicatorLine` light pillars.
- Create `HeightIndicatorLabel` height numbers.
- Store indicator nodes in the same stack metadata keys used by the existing map flow.
- Animate pillar hover floating in flat height view.
- Animate label scale/color feedback after flat-view elevation changes.
- Remove indicator nodes and kill pillar tweens when leaving height view.

Functions:
- `create_indicator(stack, original_height, config)`
  Creates pillar and/or label for one stack.
- `remove_indicator(stack)`
  Removes indicator nodes and clears related stack metadata.
- `start_pillar_floating(stack, config)`
  Starts the looping pillar hover animation.
- `stop_pillar_floating(stack, config)`
  Stops the looping pillar hover animation and preserves the previous visual baseline.
- `animate_label_height(stack, visual_height, config)`
  Updates the height label and plays the bounce feedback.
- `_get_stack_sprites(stack)`
  Safely reads the stack sprite metadata.
- `_get_indicator_position(top_sprite, height, config)`
  Computes pillar/label anchor in flat or 3D view.
- `_create_pillar(stack, original_height, position, config)`
  Creates the `Line2D` pillar and writes metadata.
- `_create_label(stack, original_height, position, config)`
  Creates the `Label` and writes metadata.
- `_queue_meta_node_free(stack, meta_key)`
  Frees indicator nodes referenced by stack metadata.
- `_kill_pillar_tween(stack)`
  Stops and clears presenter-owned pillar tweens.

## HexMap Changes

- Added `HEIGHT_VIEW_INDICATOR_PRESENTER` preload and `_height_view_indicator_presenter`.
- Removed `height_view_pillar_tweens` from `hex_map.gd`; presenter owns these tweens now.
- Reduced these functions to presenter wrappers:
  - `_create_height_indicator`
  - `_remove_height_indicator`
  - `_start_pillar_floating_animation`
  - `_stop_pillar_floating_animation`
- Added `_build_height_view_indicator_presenter_config()` to collect current exported values.
- Replaced the flat-view elevation label bounce block in `animate_elevation_change()` with `HeightViewIndicatorPresenter.animate_label_height()`.

## Exported Tuning Variables

These remain on `hex_map.gd` and are passed through `_build_height_view_indicator_presenter_config()`:

- `show_height_pillars`
- `show_height_labels`
- `height_view_pillar_length`
- `height_view_pillar_width`
- `height_view_pillar_color`
- `height_view_pillar_offset`
- `height_label_font_size`
- `height_label_color`
- `height_label_outline_color`
- `height_label_outline_size`
- `height_label_offset`
- `height_label_font`
- `height_view_hover_speed`
- `height_view_hover_amplitude`

Runtime values also passed:

- `ele_anim_duration`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `current_view_state`

## Adjustment Notes

- To hide pillars but keep numbers, disable `show_height_pillars`.
- To hide numbers but keep pillars, disable `show_height_labels`.
- To move both pillar and number together, tune `height_view_pillar_offset`.
- To move only the number relative to the pillar anchor, tune `height_label_offset`.
- To change flat-view hover motion, tune `height_view_hover_speed` and `height_view_hover_amplitude`.
- Current `stop_pillar_floating()` preserves old behavior: it stops the loop and tweens to the current y value. Returning to the original anchor should be a separate behavior change.

## Verification

Completed after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Result:
- All three commands exited with code 0.
- Existing TileSet atlas errors still appear during combat scene load.
- Existing headless quit resource leak warnings still appear.
- No new parser, missing script, or preload error remains after adding explicit `Tween` typing.

Manual regression focus:
- Toggle into flat height view and confirm every valid stack still shows the expected pillar/label.
- Hover a stack in flat view and confirm pillar floating still plays.
- Leave hover and confirm no pillar tween accumulates.
- Use elevation in flat view and confirm the label updates and bounces.
- Toggle back to 3D view and confirm all height indicators are removed.
