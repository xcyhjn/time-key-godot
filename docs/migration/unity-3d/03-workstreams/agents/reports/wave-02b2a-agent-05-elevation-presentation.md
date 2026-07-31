# Wave 02B2A Agent 05 Elevation Presentation Report

> Status: component implementation and PlayMode gate complete
> Verification date: 2026-08-01

## Scope

`HexTileColumn` is a reusable presentation-only component. It consumes a logical layer count and two existing FBX-backed prefabs; it does not own card rules, timeline rules, scene wiring, or domain state.

## Contract

- Each logical layer is an independent block with its own visual renderer collection and `MeshCollider`.
- Block local Y positions are `0`, `0.32`, `0.64` for three layers.
- `TopBounds` is recomputed from the actual top block renderer/collider bounds.
- `OccupantAnchor` follows the top bounds and resets to the column origin when cleared.
- Reapplying the same layer count is idempotent; shrinking rebuilds renderer/collider collections so removed blocks cannot remain highlighted or selectable.
- `Changed` is emitted after a structural layer update.

## Files and evidence

- `unity/Assets/_Project/Runtime/Presentation/Terrain/HexTileColumn.cs`
- `unity/Assets/_Project/Tests/PlayMode/Terrain/HexTileColumnTests.cs`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-elevation-agent/playmode-results.xml`

The component test fixture covers 1 -> 3 independent blocks, exact layer spacing, top bounds and anchor, idempotent reapply, 3 -> 1 shrink without ghost components, and full clear.

## Verification

- Unity 6000.4.10f1 Terrain PlayMode subset: `3/3 passed`, `0 failed`, `0 skipped`.
- The first run found stale renderer/collider lists after `Clear`; the implementation now rebuilds both collections after every structural change, and the rerun is clean.
- Main integration then found that placing the FBX mesh on a collider at the block root discarded the FBX child transform. The collider now lives on the actual `MeshFilter` object; the four-yaw integrated selection test passes.
- Final integrated before/after and four-yaw renders are recorded under `evidence/unity-slice-02b2a/`; the raised occupant remains grounded and all seven columns show the two-layer increase.
- Shared Controller, BoardTileView, scene and Editor harness remain reserved for main integration.
