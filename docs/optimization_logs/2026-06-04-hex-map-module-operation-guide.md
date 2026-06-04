# Hex Map Module Operation Guide

Date: 2026-06-04

## Purpose

This document records the current `hex_map.gd` decomposition work so teammates can review, tune, and extend each extracted module without re-reading the whole battle map script.

The refactor policy is behavior-preserving unless a later gameplay task explicitly changes card targeting, map generation, reward availability, or destruction timing.

## Extracted Modules

### `scene/in_scene/HexCoordRules.gd`

Responsibility:
- Generate fan-map axial coordinates.
- Generate circular-map axial coordinates.
- Compute axial distance from origin.
- Convert axial coordinates to project pixel coordinates.

Main callers:
- `hex_map.gd::_get_fan_coords()`
- `hex_map.gd::_get_circular_coords()`
- `hex_map.gd::_get_hex_pixel_pos()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `fan_radius`
- `fan_angle_span`
- `map_radius`
- `spacing_x`
- `spacing_y`
- `tile_scale`

Adjustment notes:
- Change `fan_angle_span` to widen/narrow the fan-shaped battle map.
- Change `spacing_x/spacing_y/tile_scale` together; these values affect both visual layout and hitbox alignment.

### `scene/in_scene/HexTerrainRules.gd`

Responsibility:
- Calculate fan-map terrain tier.
- Roll height by tier with seeded RNG.
- Calculate circular-room height range by room type.
- Map height to terrain type.
- Provide terrain/landform debug names.

Main callers:
- `hex_map.gd::_generate_fan_map_data()`
- `hex_map.gd::_generate_circular_map_data()`
- `hex_map.gd::get_terrain_from_height()`
- `hex_map.gd::terrain_type_to_string()`
- `hex_map.gd::terrain_name()`
- `hex_map.gd::landform_name()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `inner_tier_radius`
- `base_h_min`
- `base_h_max`
- `elite_h_bonus`
- `terrain_by_height`

Adjustment notes:
- Use `terrain_by_height` to tune visual terrain by height without changing height generation.
- Use `base_h_min/base_h_max/elite_h_bonus` to tune circular room danger curve.
- The fan-map tier probabilities are currently code constants; move them to exports only if designers need frequent iteration.

### `scene/in_scene/TileDestructionBatchQueue.gd`

Responsibility:
- Queue height-limit tile destruction requests.
- Collect multiple requests into batches.
- Start destruction callbacks in batches.
- Wait until each stack removes the queued meta.

Main caller:
- `hex_map.gd::_queue_tile_destruction_and_wait()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `tile_destruction_batch_size`
- `tile_destruction_batch_collect_delay`
- `tile_destruction_batch_interval`
- `tile_destruction_shake_count`
- `tile_destruction_shake_step_duration`
- `tile_destruction_shake_distance`
- `tile_destruction_dissolve_duration`

Adjustment notes:
- `tile_destruction_batch_size` controls how many over-limit tiles vanish together.
- `tile_destruction_batch_collect_delay` should stay small; it collects one wave of terrain changes before deletion starts.
- VFX timing exports still feed `_perform_tile_destruction()` through `VFXManager`, not the queue module.

### `scene/in_scene/HexTargetRules.gd`

Responsibility:
- Decide whether the selected card can target the current stack.
- Collect valid map stacks from a card's absolute effect range.

Main callers:
- `hex_map.gd::_is_stack_valid_target()`
- `hex_map.gd::_update_aoe_display()`

Related exported tuning variables:
- None yet.

Adjustment notes:
- Current target validation preserves old behavior: valid stack plus selected card means valid target.
- Add concrete damage/heal/build target restrictions here first, then keep `hex_map.gd` as the visual coordinator.
- Do not put shader, tooltip, or CardManager lookup logic in this module.

### `scene/in_scene/EnemyIntentMapPresenter.gd`

Responsibility:
- Highlight the enemy intent source stack.
- Create/reuse target ripple overlay sprites.
- Restore the source stack's previous shader state when the preview is cleared.
- Keep map-side enemy intent visuals separate from timeline rules and tooltip UI.

Main callers:
- `hex_map.gd::show_enemy_intent_preview()`
- `hex_map.gd::clear_enemy_intent_preview()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `enemy_intent_frame_tex`
- `enemy_intent_target_shader`
- `enemy_intent_overlay_scale`
- `enemy_intent_overlay_z_index`
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
- `hitbox_width`
- `hitbox_base_height`
- `hitbox_offset_x`
- `hitbox_offset_y`

Adjustment notes:
- Change `enemy_intent_target_*` exports to tune target ripple motion and alpha.
- Change `enemy_intent_source_*_blend` exports to tune source tile highlight strength by valid/self/invalid intent state.
- `enemy_intent_source_highlight_width` remains exported for legacy/source-overlay experiments, but the current presenter does not consume it.

### `scene/in_scene/SettlementRewardPresenter.gd`

Responsibility:
- Apply settlement reward shader instance parameters to eligible stack sprites.
- Create/reuse `SettlementRewardTooltip` panels.
- Refresh tooltip label text from reward info or landform custom copy.
- Animate tooltip hover scale.
- Reposition reward tooltips after 3D/flat height-view changes.
- Clear reward presentation when reward mode exits or a stack loses eligibility.

Main callers:
- `hex_map.gd::exit_settlement_reward_mode()`
- `hex_map.gd::_collect_settlement_reward_stacks()`
- `hex_map.gd::_handle_settlement_reward_hover()`
- `hex_map.gd::mark_settlement_reward_used()`
- `hex_map.gd::_sync_stack_to_current_view()`
- `hex_map.gd::_refresh_all_settlement_reward_tooltip_positions()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `settlement_reward_tooltip_offset`
- `settlement_reward_tooltip_size`
- `settlement_reward_tooltip_font_size`
- `settlement_reward_tooltip_z_index`
- `settlement_reward_highlight_color`
- `settlement_reward_hover_color`
- `settlement_reward_highlight_blend`
- `settlement_reward_hover_blend`
- `settlement_reward_tooltip_hover_scale`

Adjustment notes:
- Reward eligibility and payload creation stay in `hex_map.gd`; do not add reward-state rules to the presenter.
- Change tooltip placement through `settlement_reward_tooltip_offset`.
- Change reward visual intensity through `settlement_reward_highlight_blend` and `settlement_reward_hover_blend`.
- Tooltip position also consumes `step_height`, `tile_scale`, `REF_SCALE`, and `current_view_state` at runtime.

### `scene/in_scene/HeightViewIndicatorPresenter.gd`

Responsibility:
- Create and remove height-view pillar `Line2D` nodes.
- Create and update height-view number `Label` nodes.
- Own pillar hover tweens while flat height view is active.
- Animate height label feedback after flat-view elevation changes.
- Keep indicator nodes stored under the existing stack metadata keys.

Main callers:
- `hex_map.gd::_create_height_indicator()`
- `hex_map.gd::_remove_height_indicator()`
- `hex_map.gd::_start_pillar_floating_animation()`
- `hex_map.gd::_stop_pillar_floating_animation()`
- `hex_map.gd::animate_elevation_change()`

Related exported tuning variables still adjusted in `hex_map.gd`:
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

Adjustment notes:
- Change `show_height_pillars/show_height_labels` to enable each indicator layer independently.
- Change `height_view_pillar_offset` to move pillar and number anchor together.
- Change `height_label_offset` to move only the number relative to the pillar anchor.
- Change `height_view_hover_speed/height_view_hover_amplitude` to tune hover motion.
- The presenter does not change tile height data; elevation mutation stays in `hex_map.gd`.

### `scene/in_scene/RuntimeLandformRegistrar.gd`

Responsibility:
- Register newly created runtime landform entities.
- Refresh landform visuals for old paths that already wrote `map_data`.
- Attach entities to their stack and update `occupant` metadata.
- Apply enemy/middle groups without owning scene-level signals.
- Apply landform sprite block shader parameters.
- Recollect stack render sprites after visual changes.

Main callers:
- `hex_map.gd::register_runtime_landform()`
- `hex_map.gd::add_landform_visual_at()`
- `hex_map.gd::_cleanup_stack_sprites()`

Related exported tuning variables still adjusted in `hex_map.gd`:
- `step_height`
- `tile_scale`
- `hitbox_offset_x`
- `hitbox_offset_y`
- `landform_instance_offset`
- `block_material`
- `runtime_landform_spawn_vfx_enabled`
- `runtime_landform_spawn_vfx_duration`

Adjustment notes:
- Use `register_runtime_landform()` for build cards, village expansion, radar summons, and any future runtime entity creation.
- Use `add_landform_visual_at()` only when a legacy path already wrote `map_data` and needs a visual refresh.
- Keep `enemy_roster_changed`, `tile_topology_changed`, and VFX playback in `hex_map.gd`.
- Change entity anchor tuning through `hitbox_offset_x`, `hitbox_offset_y`, and `landform_instance_offset`.
- Change runtime spawn VFX through `runtime_landform_spawn_vfx_enabled` and `runtime_landform_spawn_vfx_duration`.

## Verification Checklist

Run after changing any extracted module:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Expected known noise:
- The combat scene currently prints existing TileSet atlas errors.
- The combat scene currently prints resource leak warnings on headless quit.
- Treat new parser errors, missing script errors, or changed exit codes as blockers.

## Manual Regression Paths

Use these in the editor after parser checks:
- Start a normal battle scene and confirm the map generates.
- Hover/select a card target and confirm AOE highlight still follows the card effect range.
- Use elevation effects to push tiles above/below limits and confirm batched destruction still completes.
- Return from battle if the room flow is available in the current test save.

## Next Extraction Candidates

Priority order:
- Concrete card target restrictions in `HexTargetRules.gd`.
- Enemy/settlement tooltip edge-case cleanup after target restrictions are stable.
