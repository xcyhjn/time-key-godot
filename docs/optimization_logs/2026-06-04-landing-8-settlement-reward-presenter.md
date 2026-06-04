# Landing 8: settlement reward presenter

Date: 2026-06-04

## Scope

- Extract settlement reward tooltip/highlight presentation from `hex_map.gd`.
- Keep reward eligibility, reward payload creation, click signal emission, and used-state mutation in `hex_map.gd`.
- Keep the public reward flow unchanged: victory settlement mode scans reward stacks, hover shows emphasis, click emits `settlement_reward_requested`, and Main calls `mark_settlement_reward_used()`.

## New Module

### `scene/in_scene/SettlementRewardPresenter.gd`

Responsibility:
- Write settlement reward shader instance parameters onto stack sprites.
- Create/reuse `SettlementRewardTooltip` panels under each stack.
- Refresh tooltip text from reward info or landform-provided custom text.
- Animate tooltip hover scale.
- Reposition tooltips when the map switches between 3D view and flat height view.
- Remove tooltip nodes and clear reward shader values when leaving reward mode.

Functions:
- `refresh_stack(stack, reward_info, is_available, config)`
  Refreshes one stack's reward shader state and tooltip text.
- `set_hover_state(stack, is_available, is_hovered, config)`
  Applies hover shader strength and tooltip scale animation.
- `clear_stack(stack, config)`
  Clears highlight state and removes the tooltip node.
- `apply_shader_state(stack, is_available, is_hovered, config)`
  Writes shader instance parameters, filtered to sprites that use the HexMap block shader.
- `ensure_tooltip(stack, config)`
  Creates or reuses a stack child tooltip panel.
- `refresh_tooltip(stack, reward_info, config)`
  Updates tooltip label text.
- `update_tooltip_position(stack, config)`
  Places one tooltip according to stack height and current map view mode.
- `animate_tooltip(stack, is_hovered, config)`
  Cancels the previous tooltip tween and starts a new scale tween.
- `remove_tooltip(stack)`
  Kills tooltip tween and queues the panel for deletion.
- `refresh_positions(stacks, config)`
  Repositions all tracked reward tooltips after a height-view transition.
- `_get_stack_sprites(stack)`
  Safely reads registered render sprites from stack metadata.
- `_build_tooltip_text(reward_info)`
  Builds label text from landform custom text or reward label fallback.
- `_kill_tooltip_tween(stack)`
  Cancels a stored tooltip tween before replacing/removing the panel.

## HexMap Changes

- Added `SETTLEMENT_REWARD_PRESENTER` preload and `_settlement_reward_presenter`.
- Kept these gameplay/rule functions in `hex_map.gd`:
  - `enter_settlement_reward_mode`
  - `exit_settlement_reward_mode`
  - `_collect_settlement_reward_stacks`
  - `_get_settlement_reward_landform`
  - `_is_valid_settlement_reward_landform`
  - `_matches_settlement_reward_bind_state`
  - `_build_settlement_reward_info`
  - `_is_settlement_reward_stack_available`
  - `_is_settlement_reward_used`
  - `_handle_settlement_reward_hover`
  - `_handle_settlement_reward_click`
  - `mark_settlement_reward_used`
- Added `_build_settlement_reward_presenter_config()` to collect current exported tuning values.
- Replaced direct tooltip position refresh in `_sync_stack_to_current_view()` with `SettlementRewardPresenter.update_tooltip_position()`.
- Replaced post height-view transition tooltip refresh with `SettlementRewardPresenter.refresh_positions()`.
- Removed old map-side private helpers from `hex_map.gd`:
  - `_apply_settlement_reward_shader`
  - `_ensure_settlement_reward_tooltip`
  - `_refresh_settlement_reward_tooltip`
  - `_update_settlement_reward_tooltip_position`
  - `_animate_settlement_reward_tooltip`
  - `_remove_settlement_reward_tooltip`

## Exported Tuning Variables

These remain on `hex_map.gd` and are passed into the presenter through `_build_settlement_reward_presenter_config()`:

- `settlement_reward_tooltip_offset`
  Controls tooltip anchor offset from the stack top.
- `settlement_reward_tooltip_size`
  Controls tooltip panel fixed size and pivot.
- `settlement_reward_tooltip_font_size`
  Controls tooltip label font size.
- `settlement_reward_tooltip_z_index`
  Controls tooltip draw order above map tiles/buildings.
- `settlement_reward_highlight_color`
  Controls the idle available reward highlight color.
- `settlement_reward_hover_color`
  Controls the hover overlay color.
- `settlement_reward_highlight_blend`
  Controls constant reward highlight strength.
- `settlement_reward_hover_blend`
  Controls hover-only shader movement/emphasis strength.
- `settlement_reward_tooltip_hover_scale`
  Controls tooltip scale while hovered.

Presenter config also includes these HexMap runtime values:

- `step_height`
- `tile_scale`
- `REF_SCALE`
- `current_view_state`
- `block_material.shader`

These runtime values are not separate designer-facing reward exports; they keep tooltip placement and shader filtering aligned with the active HexMap state.

## Adjustment Notes

- To move all reward tooltips, tune `settlement_reward_tooltip_offset`.
- To prevent text/layout jump, keep `settlement_reward_tooltip_size` fixed unless the tooltip copy changes significantly.
- To make reward buildings more subtle, reduce `settlement_reward_highlight_blend`.
- To make hover feedback less bouncy, reduce `settlement_reward_hover_blend` or `settlement_reward_tooltip_hover_scale`.
- Tooltip node name remains `SettlementRewardTooltip` for compatibility with existing debug searches.
- Reward eligibility stays outside the presenter. Do not add landform state checks or reward payload mutation to `SettlementRewardPresenter`.

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
- No new parser, missing script, or preload error was introduced by this landing.

Manual regression focus:
- Enter victory settlement reward mode and confirm eligible enemy buildings show tooltip/highlight.
- Hover an eligible reward stack and confirm highlight/tooltip scale changes.
- Click an eligible reward stack and confirm `settlement_reward_requested` still opens the reward flow.
- After reward confirmation, confirm the tooltip remains but the stack is no longer interactable/highlighted.
- Toggle flat height view while reward tooltip is visible; tooltip should reposition without drifting.
