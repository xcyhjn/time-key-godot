# Decoupling R2 Verification Summary

> Status: passed
> Verified: 2026-08-01
> Unity: 6000.4.10f1

## Inherited and refreshed evidence

R1 serialized Scene/Prefab evidence remained trustworthy until Controller integration. R2 changed command orchestration, so the affected EditMode, full PlayMode, harness, build, Player smoke and 11 rendered images were refreshed. The saved stable hierarchy and six Prefabs were not modified.

## Automated gates

- Domain handler contract before Agent A: `13/13 passed`.
- Agent A Application/Diagnostics filter: `14/14 passed`.
- Full EditMode after Controller integration: `86/86 passed`.
- Full PlayMode after Controller integration: `26/26 passed`.
- Editor harness: `TIMEKEY_DECOUPLING_R2_HARNESS_PASS`.
- Windows x64 development build: `Succeeded`, 206,690,380 bytes.
- Windows Player smoke: exit code `0`, log contains `TIMEKEY_PLAYER_SMOKE_PASS`.
- `TimeKey.Domain` and `TimeKey.Application` contain zero `UnityEngine` references.

## Application boundary

`CombatApplicationSession` owns card selection, typed entity/tile target, timeline preview/commit, cancel, resolve, one-time initial enemy intent and structured failures. `VerticalSliceController` preserves the existing public facade but no longer constructs or commands `CardPlaySession`, calls `TimelineGrid.Resolve`, or duplicates the enemy intent placement. Unsupported effects fail before occupying player cells and are traced.

## Visual inspection

All 11 PNGs in `unity-decoupling-r2` were opened. The two original card faces remain unclipped and keep their aspect ratio at 1280x720 and 2560x1080. Invalid timeline placement has a red cell plus yellow border; committed placement uses two amber QUAKE cells. The earthquake before/after views visibly show seven affected columns changing from one to three blocks, with the target anchored to the raised top. Harness selection checks pass at yaw 0/90/180/270.
