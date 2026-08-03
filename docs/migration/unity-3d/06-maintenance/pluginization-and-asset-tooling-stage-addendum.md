# Pluginization and Asset Tooling Stage Addendum

> Date: 2026-08-03
>
> Scope: P0 SceneContractValidator slice

## Running the Tool

Open `Tools/Time Key/Scene Contract Validator`. The window defaults to
`Assets/_Project/Scenes/VerticalSlice/CombatVerticalSlice.unity`. Select
`Validate` to run a read-only scan or `Copy JSON` to copy the deterministic
report. Errors identify a contract ID, asset path, object path, serialized
property and remediation.

For automation, call
`TimeKey.Editor.AssetTooling.SceneContracts.SceneContractValidator.ValidateCombatScene()`.
The method opens the formal scene additively, never saves it, and closes only the
scene it opened.

## Maintenance Rules

- Keep stable Camera, Canvas, EventSystem, HUD, Timeline, CardHandHost, BoardRoot,
  anchors and layout in saved Scene/Prefab data.
- Keep the validator and its tests in their dedicated Editor-only assemblies.
- Add a contract only for a stable, observable invariant. Include a negative test
  and prove the formal Scene bytes do not change.
- Never add auto-fix, `SaveScene`, `SaveAsPrefabAsset`, importer rewrites or
  runtime object generation to this tool.
- Do not move gameplay rules into Editor code or add runtime dependencies on the
  tool assembly.
- Use `D:\timekey-unity-731` as the Unity project entry while the current
  Library Package Manager cache is bound to that checked junction.

## Package and Asset Policy

UI Toolkit is accepted only because it is already installed and the window is
Editor-only. Input System, Cinemachine, Addressables and Timeline require
independent trials with exact versions, licenses, dependency review, performance
evidence and complete removal proof. Network search never authorizes download or
import.

Consult `01-assessment/asset-and-plugin-candidate-ledger.md` before using any
font, card, texture, audio, shader, Blender/FBX output or third-party plugin.
Unknown source or license means reject or defer, not implicit permission.

## Removal

Remove `Assets/_Project/Editor/AssetTooling/**` and
`Assets/_Project/Tests/EditMode/AssetTooling/**`, plus the stage-owned reports
and evidence. Do not remove the pre-existing UI Toolkit manifest dependency.
Then run full EditMode, full PlayMode, Windows build and Player smoke. Formal
Scene/Prefab/package files should remain byte-identical.

The canonical maintenance guide was protected as a pre-stage untracked file, so
this addendum intentionally avoids modifying it.
