# Wave 02B2A Verification Summary

> Status: passed
> Verified: 2026-08-01
> Unity: 6000.4.10f1

## Automated gates

- EditMode: `67/67 passed`, including seven-card adapter and `9/9` earthquake Domain coverage.
- PlayMode: `25/25 passed`, including two-card host, mutable terrain columns, full lighting regression, and earthquake integration.
- Editor harness: `TIMEKEY_EFFECTS_HARNESS_PASS`.
- Windows x64 development build: `Succeeded`, 206,446,599 bytes.
- Windows Player smoke: exit code `0`, log contains `TIMEKEY_PLAYER_SMOKE_PASS`.

## Frozen earthquake evidence

- Center plus six axial neighbors produced seven `EffectResults`; missing coordinates create no tiles.
- Every affected column gained exactly two independent block objects.
- Each block has a mesh, renderer and collider; consecutive block local Y positions differ by `0.32`.
- A one-layer center became three layers and its measured top moved by `0.64`.
- The occupied target tile also gained two layers; its actual-bounds occupant anchor moved by `0.64`.
- After resolution, the raised center coordinate remained selectable at yaw `0/90/180/270`.
- Earthquake timeline shape occupies two cells only after Commit; right-edge preview remains pure and shows a yellow outline in addition to red.

## Visual inspection

The 11 integrated PNGs were opened and inspected at their native aspect ratios. Both original card faces remain readable at 1280x720 and 2560x1080. The seven highlighted columns are consistent at all four yaws. No card, timeline, detail panel or header is clipped. Before/after images clearly show two newly stacked blocks on every affected column, no floating occupant, no stretched mesh, and no stale highlight.

Structured capture measurements and build data are in `harness-summary.json`. Raw Unity and Player logs remain local and are ignored.
