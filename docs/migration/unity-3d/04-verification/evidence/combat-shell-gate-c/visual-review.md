# Combat Shell Gate C visual review

> Result: passed
> Renderer: Direct3D 12
> Review: every canonical PNG opened after the final full PlayMode run

| Evidence | Manual finding |
| --- | --- |
| nine `game-start-*.png` frames | At all three viewports, the source key and independently moving Silver characters progress from near-black to gold with the characters below the key, then return to a deliberate black terminal frame. |
| nine `main-menu-*-entrance-*.png` frames | Background, title, central clock and button layers enter in order at every viewport; the complete frame is visible before SceneFlow unlocks input. |
| three idle and three disabled frames | The source-scale central clock, 144-size title and six 512x168 source-shaped commands remain readable. Continue is disabled at right-top; Settings is at left-bottom; no element overlaps. |
| twelve hover/focus frames | Left and right columns switch to their matching source active texture for pointer and keyboard navigation at every viewport. |
| three pressed frames | The pressed command alone uses the explicit pressed tint; no stale second focus remains. |
| twelve seed/settings/database/exit frames | Full-screen dim layers block the menu at all three viewports; arbitrary-text seed, saved master/fullscreen controls, disabled unavailable-channel controls and confirmation actions remain readable without clipping. |

Automated evidence also asserts PNG dimensions, visible pixel variation and byte differences between animation/interaction states. All 14 states are captured at 1280x720, 1920x1080 and 2560x1080, plus the three-state GameStart sequence at each viewport, for 51 PNGs total. All visible Chinese uses Silver. No clipping, incoherent overlap, missing controls, exposed background, blank frame or unreadable glyph was found.
