# Combat Shell Gate E verification summary

> Status: passed; Gate E closed locally
> Date: 2026-08-03
> Baseline: Combat Shell Gate D `105a66d`

## Final verification

The final full EditMode run passed `343/343` in `editmode-full-final.xml`. The final graphical Direct3D12 PlayMode run passed `100/100` in `playmode-full-delivery.xml`. Failed, skipped and inconclusive counts are zero in both suites.

Targeted results are saved-assets EditMode `3/3`, build-smoke EditMode `3/3`, layered reveal PlayMode `7/7`, post-review Combat entrance PlayMode `5/5`, three-cycle stability PlayMode `1/1`, and visual capture PlayMode `1/1`. The post-review results are `playmode-combat-entrance-post-review.xml` and `playmode-stability-post-review.xml`; earlier failing or superseded XML remains diagnostic only.

The Windows Development build succeeded with the frozen six-Scene order. The artifact is `227249074` bytes, its executable SHA-256 is `8B85CC34BECB9A5E63DC14325C0266CF01CDAF798F5F35638FC01C0BE0CE7878`, and Silver attribution is present.

The visible Direct3D12 Player exited 0. Its final log contains one PASS marker, three PERF markers and no FAIL marker. All three cycles retained one Bootstrap and one content entry, used distinct room/launch/outcome identities, returned to OutOfBattleShell, and ended with `finalInputLocked=false`.

## Visual and asset review

Seventeen canonical PNGs were manually inspected: nine reveal frames, three responsive ocean backgrounds and five actual Player route frames. No clipping, overlap, unreadable text, missing controls, blank rendering or broken resize was found. Silver remains visible on player-facing Chinese text. The visible Player's requested 2560x1080 window was desktop-constrained to an actual 1680x1050 capture; exact 2560x1080 coverage comes from the graphical PlayMode ocean capture.

The out-of-battle room-selection shell now reuses the Godot ocean tile `image/outscene_block/out-bg_sea.png` through the byte-identical Unity import `Resources/Art/Battle/Background/out-bg_sea.png`. Both files have SHA-256 `3226E113FA2EB03B85E89287BB25CA24F97E40AD351980A6B49BF0D5B939AE3F`. Responsive evidence covers 1280x720, 1920x1080 and 2560x1080; the tiling preserves square pixels instead of stretching the source.

The four combat yaw views remain inherited from Gate B because Gate E did not change the combat camera, battle background material or board geometry. Gate E refreshed the affected reveal and integrated route views instead of rewriting trustworthy unaffected evidence.

## Performance limits

The Player sampled `SetPass Calls Count` because Direct3D12 did not expose a valid positive Draw Calls or Batches recorder in this build. The observed value was 18 in each cycle and the actual recorder name is retained in JSON. CPU/GPU samples stayed bounded during the three-cycle smoke.

Allocated memory increased by `412086` bytes across the three samples. Raw samples are strictly monotonic, but both the material-growth predicate and the post-review sustained-slope predicate are false: the second positive delta is lower than the first rather than stable or increasing. The targeted post-GC stability test passed. This bounded smoke does not claim long-session leak freedom or profiler-grade rendering performance.

## Closure

Gate E is closed locally. The canonical evidence set includes final XML, build/player JSON, the structured animation timeline and 17 reviewed PNGs; raw logs remain local diagnostics. Push was intentionally skipped per user instruction. Current user-decision blockers: none.
