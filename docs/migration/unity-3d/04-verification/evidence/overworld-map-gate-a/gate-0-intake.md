# Wave 03 Map Gate 0 Intake

- Branch: `unity_7.31`
- Pre-stage checkpoint: `bea6adf457ff63acd273a58f86c64c975908bbd3`
- Ahead/behind at intake: `0/0`
- Unity/Godot/Build/Test writers at intake and handoff: none; Blender PID 18072 remained a non-writing user process.
- Protected inventory: all files recorded by `03-workstreams/agents/reports/overworld-movement-theme-intake.md`, plus the era-clock Gate 0 intake and all pre-stage dirty/untracked files.
- Frozen inherited boundaries: Bootstrap, SceneFlow, typed payload/outcome, Silver localization, ocean OutOfBattle background, movement `MapNodeId`, and Wave 03P Theme/Scene contracts.
- Godot sources read by the Domain audit: `map_generator.gd`, `map_renderer.gd`, `out_scene_map_exp.gd`, `RoomResolutionController.gd`; SHA and observable semantics are in `overworld-map-domain-gate-a.md`.
- Compatibility decision: observable invariant compatibility, not bit-for-bit topology/RNG parity; this is explicit and testable.
- Gate A ownership: new `Runtime/Domain/Overworld/**` and `Tests/EditMode/Overworld/**` only. The main agent ran the Unity full suite after the agent returned.

Resume position after this record: map Prompt Gate 0/B review, then Gate B typed application integration. Do not duplicate `MapNodeId` or the existing movement state machine.
