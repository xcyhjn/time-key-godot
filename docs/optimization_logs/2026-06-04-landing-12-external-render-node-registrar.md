# Landing 12: external render node registrar

Date: 2026-06-04

## Scope

- Extract health bar and external render node registration from `hex_map.gd`.
- Keep health bar creation, intro-delay queueing, BarManager ownership, and view synchronization in `hex_map.gd`.
- Preserve the old behavior: external UI nodes are added to stack `sprites` metadata and receive height shader instance parameters, but their material is not replaced.

## New Module

### `scene/in_scene/ExternalRenderNodeRegistrar.gd`

Responsibility:
- Find the single health bar node for a `landform`.
- Register health bar or external UI render nodes into a stack's render list.
- Recursively collect supported child render nodes.
- Apply `block_idx` and `total_height` instance shader parameters to nodes that already have a material.
- Leave node ownership, lifecycle, and positioning to the existing BarManager/HexMap flow.

Functions:
- `recollect_for_landform(entity, bar_manager, context)`
  Finds `HealthBar_<entity_id>` and registers its render children.
- `register_node_at(coord, node, context)`
  Registers an arbitrary external node at a map coordinate.
- `find_health_bar_for_landform(entity, bar_manager)`
  Encapsulates the health-bar naming convention.
- `_get_or_init_stack_sprites(stack)`
  Reads or initializes stack `sprites` metadata.
- `_get_stack_height(stack)`
  Reads safe stack height metadata for shader parameters.
- `_collect_render_nodes(node, sprites_list, height)`
  Recursively walks a node tree and registers supported render children.
- `_is_supported_render_node(node)`
  Allows `Sprite2D`, `TextureRect`, and `TextureProgressBar`.
- `_register_render_node(node, sprites_list, height)`
  Adds one node to the render queue and writes height shader parameters.

## HexMap Changes

- Added `EXTERNAL_RENDER_NODE_REGISTRAR` preload and `_external_render_node_registrar`.
- `_find_health_bar_for_landform()` now delegates the naming lookup to the registrar.
- `recollect_sprites_for_landform()` now delegates health-bar render child registration.
- `register_extra_render_node()` now delegates arbitrary external node registration.
- Removed `_find_and_register_ui_sprites()` from `hex_map.gd`.
- Added `_build_external_render_node_registrar_context()`.

## Exported Tuning Variables

No new exported variables were added.

The registrar depends on existing runtime data:

- `stack_nodes`
- stack `sprites` metadata
- stack `height` metadata
- health-bar node names in the form `HealthBar_<landform_instance_id>`

## Adjustment Notes

- To register a new external UI node type, add it to `_is_supported_render_node()`.
- To change height shader parameters, adjust `_register_render_node()`.
- Do not replace materials in this module; health bars and UI nodes may use dedicated shaders.
- Do not create or free health bars in this module; BarManager still owns that lifecycle.
- If a registered node appears during flat height view, HexMap still calls `_sync_stack_to_current_view()` to position it.

## Verification

Run after this landing:

```powershell
git diff --check
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --quit --no-header
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --headless --path . --scene res://scene/in_scene/in_scene.tscn --quit-after 1 --no-header
```

Manual regression focus:
- Start combat and confirm single health bars still appear above enemies/buildings.
- Hover tiles with health bars and confirm highlight/shader response still affects registered UI nodes.
- Toggle flat height view and confirm health bars keep their relative position.
- Damage or heal a target and confirm health bar visuals still update.
