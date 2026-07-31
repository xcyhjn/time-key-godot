# Wave 02B2A Two-Card Hand Verification

> Unity: 6000.4.10f1
> Date: 2026-08-01
> Scope: `TimeKey.Tests.PlayMode.Cards`

## Automated result

- Cards PlayMode: `9/9 passed`, `0 failed`, `0 skipped`.
- Existing single-card regression: `4/4 passed`.
- New host/coordinator coverage: `5/5 passed`.
- The structured result is `playmode-results.xml`; the raw Unity log is ignored.

The new cases cover ordered stable-ID mapping, mutually exclusive selection, explicit cancellation of the previous card, right-click cancellation, drag-event ownership, repeated `Build` idempotence, pointer consumption, disabled interaction, aspect preservation and three-viewport bounds.

## Rendered evidence

The directory contains actual RenderTexture captures for all combinations of:

- viewports: `1280x720`, `1920x1080`, `2560x1080`;
- states: `idle`, `lighting-selected`, `earthquake-selected`.

All nine captures were inspected at original resolution. Both original card faces remain recognizable and preserve the `125:175` design ratio. Idle cards overlap only slightly. The selected card is lifted, scaled and rendered above its sibling; neither card is clipped at the bottom or viewport edges, and rebuilding between states does not move the hand away from bottom center.

The component rig deliberately contains no battle board or timeline. It verifies the hand's responsive bounds and bottom-half limit, while final board/timeline occlusion must be checked again after the main controller connects this host to the shared combat scene.
