# Optimization Baseline

This document is the working checklist for the balanced optimization pass. Keep each change tied to one row in the baseline or one acceptance path below.

## Fixed Acceptance Paths

| Path | Steps | Pass criteria |
| --- | --- | --- |
| Startup | Launch `scene/game_start/game_start.tscn`, wait for or skip the intro, enter the main menu. | Main menu renders, buttons respond, settings/guide/quit overlays still open and close. |
| Out Scene | Start or continue a run, generate the out-scene hex map, choose a character, move to a room. | Map state, character selection, camera limits, era display, and room entry payload stay correct. |
| In Scene | Enter combat, draw cards, place at least one timeline action, resolve one full turn, trigger victory/settlement. | Timeline placement, enemy intents, tile damage/elevation, settlement UI, and return payload all behave as before. |

## Static Baseline

| Area | Current observation | Optimization focus |
| --- | --- | --- |
| Largest scripts | `hex_map.gd` (~2.6k lines), `in_scene.gd` (~1.5k+ lines), `DragShapeController.gd` (~1k lines), `timeline_ui.gd` (~800 lines). | Extract pure rules and UI presentation only after low-risk caching is stable. |
| Largest scenes | `in_scene.tscn` (~51 nodes), `main_menu.tscn` (~37 nodes), `Out_Scene.tscn` (~28 nodes). | Keep scene split decisions behavior-preserving and verify node paths after each split. |
| Runtime loading | Card framework scenes, reward scenes, status icons, and landform textures had repeat load sites. | Cache stable `PackedScene` and `Texture2D` resources before deeper refactors. |
| Frequent instancing candidates | Timeline action visuals, tooltip panels, tile VFX, health bars, card visual copies. | Introduce pools only after profiler confirms churn on the fixed paths. |
| Global state | `MapState`, `GlobalClock`, `Signal_Bus`, `GlobalDB`, and `GlobalTimecoin` share cross-scene responsibilities. | Keep `MapState` as snapshot data, `GlobalClock` as era/phase, and `Signal_Bus` as events. |

## Runtime Metrics To Record

Record these in Godot Debugger > Monitors or Profiler for each fixed path:

| Metric | Target note |
| --- | --- |
| FPS / frame time | 60 FPS means a 16.6 ms frame budget. Capture worst visible spikes. |
| Process time | Watch `_process()` cost during card drag, map hover, and menu idle. |
| Draw calls | Compare main menu, out scene, and in scene separately. |
| Object count | Check before and after scene switches; it should not grow across repeated cycles. |
| Video RAM | Watch for steady growth after opening reward pages, pile viewer, and settlement UI. |
| Scene switch time | Record out->in and in->out transitions, including dim animation and payload handoff. |

## First Landing Slice

- Cache stable combat/card `PackedScene` references at class scope in `in_scene.gd`.
- Cache settlement reward `PackedScene` instances by path after first load.
- Cache status icon and landform textures by resource path.
- Guard repeated signal connections in `in_scene.gd` and `sound_manager.gd`.
- Leave gameplay rules, timeline resolution, map generation, and reward behavior unchanged.

## Next Slices

- Profile the three fixed paths and fill actual metric values in this file.
- If object churn is confirmed, pool tooltip panels, timeline visuals, tile VFX, and health bars one type at a time.
- Split `hex_map.gd` only along existing behavior boundaries: terrain/coordinates, highlighting, enemy intent preview, height view, destruction, settlement rewards.
- Split `in_scene.gd` only after tests cover card setup, scene switching, reward entry, and combat settlement.
