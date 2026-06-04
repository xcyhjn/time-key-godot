# Landing 2: reward close contract

Date: 2026-06-04

## Scope

- Replace reward-scene button path scanning with one explicit close signal.
- Keep reward business logic unchanged: acquire, remove, craft, and shop still decide when close is allowed.

## Code changes

- Added `reward_scene_close_requested(scene_instance: Node)` to:
  - `scene/in_scene/rewards/AcquireReward.gd`
  - `scene/in_scene/rewards/RemoveReward.gd`
  - `scene/in_scene/rewards/CraftReward.gd`
  - `scene/in_scene/rewards/ShopManager.gd`
- Each reward scene now emits the signal after its own close/exit cleanup.
- `scene/in_scene/in_scene.gd` now connects the reward close signal directly after instancing a reward scene.
- Removed `_connect_exit_signal_for_external_scene()`, which scanned multiple possible button paths.

## Verification

- `git diff --check`: passed. Godot warned that `CraftReward.gd` line endings will normalize from CRLF to LF.
- `Godot --headless --path . --quit --no-header`: exit code 0.
- `Godot --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header`: exit code 0.
- Combat scene load still prints existing TileSet atlas errors and resource leak warnings; no reward signal parse error was introduced.

## Remaining risks

- Reward open/setup still uses `has_method()` for `set_deck_manager`, `open_shop`, and `open`; this remains dynamic until all reward scenes share a fuller interface.
- `CraftReward.gd` line endings changed from CRLF to LF when edited.
