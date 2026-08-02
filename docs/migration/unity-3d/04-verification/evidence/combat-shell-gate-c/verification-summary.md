# Combat Shell Gate C verification summary

> Status: Gate C passed
> Unity: 6000.4.10f1
> Date: 2026-08-03

## Automated gates

| Gate | Result | Canonical evidence |
| --- | --- | --- |
| Full EditMode | `340/340` | `editmode-full.xml` |
| Full Direct3D12 PlayMode | `85/85` | `playmode-full.xml` |
| Saved assets, presenter behavior and additive SceneFlow | covered by the full suites | both full XML files |
| Formal-scene visual states | 51 manually inspected PNGs | `visual-summary.json` and `visual-review.md` |

All XML runs have `total>0`, `failed=0` and `skipped=0`. The full PlayMode log confirms Direct3D 12. Raw editor logs remain local and are not canonical evidence.

## Contract result

- `GameStart` stores the source `key.png`, three independently moving Silver characters, the black/gold/black 3-second non-skippable sequence and an explicit completion boundary.
- `MainMenu` stores the original map, byte-identical source-scale clock and six distinct source button pairs. Continue is disabled at right-top, Settings remains at left-bottom, seed accepts arbitrary text and Return/KeypadEnter, and Database/Exit use modal flows.
- Settings is a closeable saved panel. Presentation emits a typed settings snapshot; Composition persists/applies master volume and fullscreen. Unsupported Music/SFX channels remain visibly disabled instead of pretending to work.
- Presentation emits typed requests only. Composition creates `RunStartPayload` with explicit run seed/state, owns increasing sequence identity, settings persistence and `Application.Quit`.
- The persistent transition overlay waits for actual cover/reveal completion before camera disable or input unlock. Zero duration lands immediately at the requested terminal state.
- Source adapters use their Scene lifetime token only before Bootstrap accepts a transition. Normal source unload cannot cancel post-commit reveal or input unlock.
- Bootstrap remains the only EventSystem/Transition/Audio owner; content scenes contain no persistent duplicates.

Windows build and actual Player smoke are intentionally deferred to the stage-wide Gate E after Gate D round-trip integration changes the same build. Gate B build/Player evidence is not claimed as verification of the new Gate C code.
