# Wave 03 Overworld Gate D Verification

## Result

Gate D passed. A real process restart restores the exact Event boundary, the same run
continues through an exact-once Shop purchase and the deterministic multi-room route,
Boss victory advances exactly one chapter, and a later defeat returns to MainMenu while
deleting the run. The implementation continues to use the single Gate A map authority,
schema 2 save and existing typed SceneFlow state machine.

## Runtime correction

The red restart test exposed sequence reuse after a fresh Bootstrap: the persisted
Application operation cursor could be higher than the new process transition counter.
Continue now raises the Bootstrap sequence floor from the validated prepared snapshot
before reserving the transition. No schema, payload or second state machine was added.

## Automated verification

| Check | Result |
| --- | --- |
| SceneFlow additive and Gate D restart/route | `10/10` |
| Full graphical EditMode | `446/446` |
| Full graphical Direct3D12 PlayMode | `130/130` |
| Failed / skipped / inconclusive | `0 / 0 / 0` |

Canonical XML files are `additive-results.xml`, `editmode-full.xml` and
`playmode-full-d3d12.xml` in this directory.

## Two-process Player route

The first visible Player process starts seed 4, selects and settles Event room
`chapter-01-layer-01-node-01`, persists 100 timecoins and 12 cards, writes the phase-one
summary and exits 0 with `TIMEKEY_OVERWORLD_GATE_D_PLAYER_PREPARE_PASS`.

The second fresh Player process enables Continue, restores that exact boundary, purchases
`earthquake` for 50 timecoins exactly once, finishes the five-room route through Boss,
advances to chapter 2, rejects Boss replay as already applied, then resolves a chapter-2
combat as Defeat. Returning from GameOver deletes the save, clears the run and exits with
input unlocked. It exits 0 with `TIMEKEY_OVERWORLD_GATE_D_PLAYER_RESUME_PASS`.

Exact operation facts are in `player-phase1-summary.json` and
`player-smoke-summary.json`. External final logs are hash-addressed in
`delivery-summary.json`.

## Visual verification

Ten actual Player PNGs cover Event at 1280x720, Continue/Shop/Boss at 1920x1080 and
chapter 2/GameOver/returned MainMenu at 2560x1080. Every final image was manually checked
for nonblank output, clipping, Silver readability, responsive layout and overlap. After
combat return and wide resize the Era Clock is settled at the top center; it does not
enter the map-node or room-detail regions.

## Build

The frozen six enabled Scenes built successfully for Windows64. The launcher is 667648
bytes with SHA-256
`FE5E81292DF0F6591DCEEC172141B6F0F22D7CBB853DE83786B725E1BC68BEEE`.
`Silver-ATTRIBUTION.txt` is present beside the Player. Build details are in
`build-summary.json`.

## Closure

Wave 03 Overworld Gate A through Gate D are closed. Event source content remains the
previously documented transparent safe-skip difference; it is not a Gate D blocker and
no substitute story or reward was invented.
