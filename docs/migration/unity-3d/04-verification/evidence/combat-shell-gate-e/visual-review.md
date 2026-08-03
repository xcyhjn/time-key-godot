# Combat Shell Gate E visual review

> Status: passed; 17 canonical PNGs manually reviewed

## Reviewed evidence

| View | Evidence | Result |
| --- | --- | --- |
| OutOfBattle reveal | `out-of-battle-reveal-{initial,middle,complete}.png` | Black initial, ocean/background middle, full context/room terminal; pass |
| Combat reveal | `combat-reveal-{initial,middle,complete}.png` | Clean board initial, status/top middle, timeline/enemy/hand terminal; pass |
| GameOver reveal | `game-over-reveal-{initial,middle,complete}.png` | Black initial, dim island middle, centered panel terminal; pass |
| Ocean 1280x720 | `out-of-battle-ocean-1280x720.png` | Square-pixel tiling, readable room selector; pass |
| Ocean 1920x1080 | `out-of-battle-ocean-1920x1080.png` | No stretch, clipping or overlap; pass |
| Ocean 2560x1080 | `out-of-battle-ocean-2560x1080.png` | Ultrawide coverage and stable layout; pass |
| Actual Player route | five `player-*.png` files | MainMenu, shell, combat, Victory and returned resized shell render correctly; pass |

All frames were inspected for blank rendering, clipping, overlap, missing controls, unreadable Chinese text, Silver font presentation, interaction state and resize behavior. Defect count is zero.

The Player requested 2560x1080 for the returned-shell capture, but the visible desktop constrained the actual PNG to 1680x1050; the historical filename retains the requested size. Exact 2560x1080 coverage is provided by `out-of-battle-ocean-2560x1080.png` from the graphical PlayMode capture.

`animation-timeline.json` confirms the intended layer order and terminal alpha 1 for OutOfBattle, Combat and GameOver. The reveal tests also cover deterministic completion for missing layers, zero duration, disable, destroy and replay.

The Gate B 1280x720/1920x1080/2560x1080 and yaw 0/90/180/270 combat background evidence remains valid: Gate E does not change the battle camera, board geometry or battle background material. Gate E refreshed the affected reveal and shell/player views, including the newly required ocean background.
