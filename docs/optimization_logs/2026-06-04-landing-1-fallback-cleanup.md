# Landing 1: fallback cleanup

Date: 2026-06-04

## Scope

- Remove low-risk defensive branches around fixed autoloads and fixed scene nodes.
- Keep dynamic plugin/card/reward protocols guarded for later, isolated cleanup.
- Ignore local Claude desktop settings through `.gitignore`.

## Code changes

- `scene/in_scene/in_scene.gd`
  - Simplified TimelineManager signal wiring.
  - Removed the stale EffectProcessor wiring branch from the missing TimelineManager path.
  - Replaced fixed GlobalClock, MapState, Signal_Bus, TimelineManager, and HexMap API probes with direct calls.
  - Kept object validity checks where node lifetime still matters.
- `scene/in_scene/hex_map.gd`
  - Removed the obsolete commented `pick_landform` implementation.
  - Replaced `GlobalClock.tile_h_pool` property probes with direct height-pool access.
  - Emitted the locally declared `tile_topology_changed` signal directly.

## Verification

- `git diff --check`: passed.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no new parse error was introduced by this landing.

## Remaining risks

- Dynamic reward scenes still use method probing until a shared reward close/open contract is introduced.
- Card-framework and entity behavior calls still use method probing because those objects are polymorphic.
- Existing import/resource warnings are tracked separately from this landing.
