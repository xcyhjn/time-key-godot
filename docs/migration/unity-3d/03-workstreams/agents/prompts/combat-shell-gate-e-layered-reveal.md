# Combat Shell Gate E Agent: layered reveal

## Ownership

Only add `unity/Assets/_Project/Runtime/Presentation/SceneFlowFinale/**`, `unity/Assets/_Project/Tests/PlayMode/SceneFlowFinale/**`, matching `.meta`, and `docs/migration/unity-3d/03-workstreams/agents/reports/combat-shell-gate-e-layered-reveal.md`.

## Task

Create one saved-asset-friendly `ISceneRevealPresentation` component for ordered `CanvasGroup` layers. It must use unscaled frame progression, expose explicit completion, restore terminal alpha/raycast state for disabled/zero-duration/destroyed cases, avoid `Task.Delay`, and never own SceneFlow or application state. Add focused PlayMode tests for initial/middle/final ordering, zero duration, disable, repeated play and destruction. Do not author or edit any Scene/Prefab, existing component, shared asmdef, Editor script, build setting, Git state or evidence directory. Do not run Unity while another Unity process exists. Return ownership without stage/commit/push.
