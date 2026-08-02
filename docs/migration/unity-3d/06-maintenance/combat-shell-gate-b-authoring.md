# Combat Shell Gate B authoring and maintenance

## Saved ownership

- `CombatTopHUD.prefab`: stable top bar, counters, pause/settings buttons and modal.
- `CombatBattleBackground.prefab`: sea, shallow layer and four horizon panels.
- `CombatVerticalSlice.unity`: stable Prefab instances, Presenter references, sorting order and disabled legacy ground renderer.
- Runtime-generated objects remain limited to existing board occupants, card/action frames and other previously documented combat dynamics. Gate B does not generate stable HUD or background objects at runtime.

`CombatPresentationBinding` is the only Top HUD refresh entry. Add fields to the immutable Application view first, then map them in `CombatTopHudPresenter`; do not read `VerticalSliceController`, deck Domain objects or UI labels as state. The pause/settings modal owns a disposable input-lock lease. Never replace it with `SetLocked(false)`, which could clear a SceneFlow transition lock.

`CombatShellEntrancePresenter` is an `ISceneRevealPresentation`. SceneFlow must call `PlayReveal` after uncovering and wait for `IsComplete` before input unlock. A disabled presenter, missing reference or non-positive duration must complete immediately so loading cannot deadlock.

## Reauthoring

Use the ASCII project junction and explicit repository root:

```powershell
$env:ALLUSERSPROFILE = $env:ProgramData
$env:TIMEKEY_REPOSITORY_ROOT = 'D:\godot\时之钥\时之钥'
& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' `
  -batchmode -projectPath 'D:\timekey-unity-731' `
  -executeMethod TimeKey.Editor.CombatShellGateBAutomation.AuthorGateB `
  -logFile '<local-log>' -quit
```

The authoring method is deterministic and preserves the Godot source files. After a Gate A build reauthor Gate B, then run `CombatShellGateBAssetTests`; Gate A rewrites shell scenes and Build Settings while Gate B owns the Combat visual additions.

## Visual maintenance

Run `CombatShellGateBVerificationAutomation.CaptureGateB` with the same environment variables. Inspect every generated PNG at 1280x720, 1920x1080 and 2560x1080, four yaw angles, both pitch/zoom limits, coexistence and modal states. The static harness disables entrance animation by design; animation evidence comes from `CombatShellEntranceVisualEvidenceTests`.

When diagnosing a black or empty background, verify the old `BattlefieldGround` renderer is disabled but its collider is enabled, all six background renderers reference saved materials, and the four horizon panels still surround the camera bounds. When world sprites penetrate UI, verify the top-level `SliceCanvas.sortingOrder` remains 500. When combat responds behind a modal, verify the modal lease is alive and the controller event callbacks still honor `SceneInputLockState.IsLocked`.

Raw editor/player logs are local-only. Commit NUnit XML, JSON summaries, reviewed PNGs, asset manifest and the verification summary.
