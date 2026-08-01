# Decoupling R1 Verification Summary

> Status: passed
> Verified: 2026-08-01
> Unity: 6000.4.10f1

## Automated gates

- Scene authoring: `TIMEKEY_EDITABLE_SCENE_AUTHORING_PASS`.
- EditMode: `70/70 passed`, including three serialized Scene/Prefab asset tests.
- PlayMode: `26/26 passed`, including disable/enable and repeated-build lifecycle coverage.
- Editor harness: `TIMEKEY_DECOUPLING_R1_HARNESS_PASS`.
- Windows x64 development build: `Succeeded`, 206,629,063 bytes.
- Windows Player smoke: exit code `0`, log contains `TIMEKEY_PLAYER_SMOKE_PASS`.

## Scene and Prefab evidence

- The saved Scene contains Camera/rig, both lights, ground, BoardRoot, TargetAnchor, EventSystem, Canvas/HUD, Timeline, CardHandHost and both previews before Play.
- All required Controller references are serialized and non-null; `timelineCells` contains 36 unique coordinates.
- Six production Prefabs exist for TimelineCell, CardView, grass/dirt HexBlock, HexColumn and TargetView.
- `VerticalSliceController.cs` contains no `new GameObject`, `CreatePrimitive`, `AddComponent` or `GameObject.Find` construction path.
- Two disable/enable cycles plus a repeated compatibility build leave 19 tiles, 36 timeline cells, one target and one enemy intent.

## Visual inspection

The 11 PNGs were opened and inspected. The two-card hand and timeline remain unclipped at 1280x720 and 2560x1080. The same seven columns are highlighted at all four yaws. Earthquake before/after images visibly show two added blocks on each affected column; target anchoring follows the raised top, and no stale range highlight remains. Structured pixel and build measurements are in `harness-summary.json`.
