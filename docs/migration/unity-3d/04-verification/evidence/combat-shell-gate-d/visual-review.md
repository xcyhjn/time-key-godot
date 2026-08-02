# Combat Shell Gate D visual review

> Result: passed
> Renderer: Direct3D 12
> Evidence: 18 final PNGs from the successful full graphical PlayMode run

| Evidence | Manual finding |
| --- | --- |
| `out-of-battle-*-idle.png` | The source map, saved hex decoration, shared top HUD, run context, room, and action row render as one coherent screen. The room is visibly available without hiding map or HUD information. |
| `out-of-battle-*-hover.png` | Hover feedback changes the room status and emphasis without resizing the room or shifting the surrounding layout. |
| `out-of-battle-*-selected.png` | Selected state is distinct from hover, and the confirm/cancel commands remain visible and readable. |
| `out-of-battle-*-confirming.png` | Confirming feedback is explicit, the room reports entry in progress, and the screen does not expose a black or uninitialized transition frame. |
| `out-of-battle-*-settled.png` | Returning state marks the same room complete and visually disables it without losing the run context or shared HUD. |
| `game-over-*-defeat.png` | The source-style background, dark veil, centered defeat panel, seed detail, and return command remain readable at narrow, standard, and ultrawide viewports. |

The `1280x720` and `2560x1080` extremes, plus the key intermediate states, were opened and inspected after the final capture. No black screen, clipping, incoherent overlap, missing control, exposed canvas edge, or unreadable Chinese glyph was found. All Gate D `Text` components use Silver; the saved-asset test verifies the serialized font reference, and the visual evidence confirms the font renders in the formal scenes.

Automated evidence also verifies the exact PNG dimensions and visible pixel variation. The 18 files have 18 distinct SHA-256 hashes, so no state or viewport capture is a byte-for-byte duplicate of another.
