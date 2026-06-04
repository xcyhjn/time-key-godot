# Landing 11: card target restrictions

Date: 2026-06-04

## Scope

- Centralize card target restrictions in `HexTargetRules.gd`.
- Make HexMap hover highlights and MainBoard target tooltip use the same rule module.
- Block invalid map clicks before a card enters timeline placement.
- Keep timeline command classes as the final execution-time safety layer.

## Updated Module

### `scene/in_scene/HexTargetRules.gd`

Responsibility:
- Decide whether a selected card can target a stack.
- Expand card `effect_range` to real map stacks.
- Validate build, damage, recover/heal, poison, elevation, and clear effects.
- Keep unknown/experimental effects permissive so refactor work does not break old content.

Functions:
- `is_stack_valid_target(stack, selected_card, context)`
  Main entry used by HexMap and MainBoard.
- `is_empty_build_target(stack, context)`
  Checks stack `occupant` plus `map_data.landform/landform_in`.
- `get_effect_range_stacks(card, center_coord, stack_nodes)`
  Expands a card's existing `get_absolute_effect_range()` output into valid `Area2D` stacks.
- `_get_card_info(card)`
  Reads the card JSON dictionary from `card.card_info`.
- `_get_effects(card_info)`
  Reads `effects` safely.
- `_get_stack_coord(stack, context)`
  Resolves the selected stack coordinate from explicit context or `stack_nodes.find_key()`.
- `_range_has_target_for_effect(card, center_stack, center_coord, stack_nodes, effect_type)`
  For AOE effects, checks whether at least one affected stack has a valid recipient.
- `_get_stack_occupant(stack)`
  Reads a live occupant node from stack metadata.
- `_can_damage_entity(entity)`
  Matches `DamageCommand`: entity exists, supports `take_damage`, and is alive if HP exists.
- `_can_recover_entity(entity)`
  Matches `RecoverCommand`: entity exists, supports `heal`, and is not full HP when HP fields exist.
- `_can_poison_entity(entity)`
  Matches `PoisonCommand`: entity exists, supports `add_status`, and is alive if HP exists.
- `_is_live_object(value)`
  Shared object validity gate for dynamic values from CardManager and stack metadata.

## Rule Details

- `built` / `build`:
  - Target stack must have no live `occupant`.
  - Target map data must exist.
  - `map_data[coord].landform` and `landform_in` must not be live objects.
- `damage`:
  - At least one stack in `effect_range` must contain a live entity with `take_damage()`.
  - If the entity has `HP`, it must be greater than 0.
- `recover` / `heal`:
  - At least one stack in `effect_range` must contain a live entity with `heal()`.
  - If `HP` and `Max_Blood` both exist, HP must be below Max_Blood.
- `poison`:
  - At least one stack in `effect_range` must contain a live entity with `add_status()`.
  - If the entity has `HP`, it must be greater than 0.
- `elevation`:
  - Any valid stack remains a valid target.
- `clear`:
  - Remains valid because clear cards use the no-map-target timeline path.
- Unknown effect types:
  - Remain valid to avoid blocking existing prototype cards during refactor.

## HexMap Changes

- `_is_stack_valid_target()` now passes `stack_nodes`, `map_data`, and center coordinate into `HexTargetRules`.
- Added `_build_hex_target_rules_context(center_stack)`.
- `_handle_tile_click()` now blocks invalid card targets before calling `active_card.play_card(stack)`.
- Hover highlighting still uses `TileVisualState.HOVER_TARGET_VALID` / `HOVER_TARGET_INVALID`; only rule ownership changed.

## MainBoard Changes

- Added `HEX_TARGET_RULES` preload.
- `is_valid_target()` now delegates to `HexTargetRules.is_stack_valid_target()`.
- Removed duplicated local helpers:
  - `_is_empty_build_target()`
  - `_card_has_effect_type()`
- Added `_build_hex_target_rules_context(center_stack)` so tooltip logic and map hover logic consume the same rule inputs.

## Exported Tuning Variables

No new exported variables were added.

Target validation depends on these existing runtime fields:

- `card.card_info.effects`
- `card.get_absolute_effect_range(center_coord)`
- `hex_map.stack_nodes`
- `hex_map.map_data`
- stack `occupant` metadata
- entity methods/properties:
  - `take_damage`
  - `heal`
  - `add_status`
  - `HP`
  - `Max_Blood`

## Adjustment Notes

- To add a new effect type, extend `HexTargetRules.is_stack_valid_target()` first.
- If a new effect is AOE and needs an entity recipient, add a small `_can_*_entity()` helper and route it through `_range_has_target_for_effect()`.
- If a new effect targets terrain instead of entities, keep it stack-based like `elevation`.
- Do not put shader, tooltip text, or timeline placement code in `HexTargetRules`; those remain in HexMap, MainBoard, and DragShapeController.

## Verification

Run after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Manual regression focus:
- Select `tower` and confirm occupied stacks show invalid target state and cannot enter timeline placement.
- Select `lighting` and confirm an AOE center is valid only when the range contains a damageable live entity.
- Select `recover` and confirm fully healed entities do not count as valid recover targets.
- Select `poison` and confirm empty/dead targets are invalid.
- Select `earthquake` and confirm terrain stacks remain valid.
- Select `wind` or `tornado` and confirm the no-map-target timeline clear flow still starts from card selection.
