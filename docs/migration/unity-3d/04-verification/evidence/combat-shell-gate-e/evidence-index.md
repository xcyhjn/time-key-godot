# Combat Shell Gate E evidence index

> Status: final; Gate E closed locally
> Baseline: Gate D commit `105a66d`
> Unity: 6000.4.10f1 / Direct3D12

## Canonical

| Area | Artifact | Result |
| --- | --- | --- |
| Full EditMode | `editmode-full-final.xml` | `343/343` passed |
| Full graphical PlayMode | `playmode-full-delivery.xml` | `100/100` passed |
| Layered reveal | `playmode-layered-targeted.xml` | `7/7` passed |
| Combat entrance post-review | `playmode-combat-entrance-post-review.xml` | `5/5` passed; running completion stays terminal |
| Three-cycle stability | `playmode-stability-post-review.xml` | `1/1` passed; sustained-slope check enabled |
| Animation and ocean capture | `playmode-visual-final.xml`, `animation-timeline.json` | `1/1` passed; 12 frames captured |
| Windows build | `build-summary.json` | succeeded; exit 0; 6 Scenes; Silver attribution |
| Actual Player | `player-smoke-summary.json` | exit 0; PASS 1; PERF 3; FAIL 0, corroborated by excluded raw log |
| Manual visual review | `visual-review.md`, 17 PNGs | passed; 0 defects |

The 17 PNGs comprise nine initial/middle/complete reveal frames, three responsive ocean views and five actual Player route captures. The inherited Gate B four-yaw combat evidence remains valid because Gate E did not change that boundary.

## Diagnostic only

- `playmode-full.xml`: full-suite order exposed a persistent Bootstrap fixture leak; the intermediate `playmode-full-final.xml` predates post-review regression coverage. `playmode-full-delivery.xml` is canonical.
- `playmode-stability-targeted.xml`, `-2.xml` and `playmode-stability-final.xml`: superseded by the stronger post-review slope check.
- `playmode-visual-targeted-2.xml` and `-3.xml`: pre-fix reveal sampling diagnostics; `playmode-visual-final.xml` is canonical.
- Earlier build/player logs are non-canonical when a later delivery artifact exists.

Raw logs are retained locally for diagnosis and excluded from the Git checkpoint. Their final marker/exit facts are summarized in the reviewed JSON and verification summary.
