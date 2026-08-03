# Wave 03P Agent Prompt Review

> Date: 2026-08-03
>
> Status: PASS before dispatch

## Reviewed Prompts

- `agents/overworld-movement-godot-audit.md`
- `agents/ui-theme-godot-audit.md`
- `agents/overworld-movement-theme-unity-audit.md`

## Ownership

Each agent has one exclusive report and no overlapping write path. All are
read-only outside that report. Main ownership remains exclusive for runtime and
Editor code, asmdef, meta, formal Scene/Prefab/theme assets, Unity import,
tests, visual evidence, shared documentation and Git.

## Required Fields

All prompts define one goal, required inputs, exclusive output, forbidden paths,
read-only commands, non-goals, stop condition and repository concurrency rules.
They forbid Unity/Godot/Blender/test/build execution, downloads, staging,
commits, branches and rollback of other work.

## Live Gate

P0 is committed and pushed. The interrupted Era Clock task and its child no
longer own Wave 03P paths; its single intake is protected. Unity/Godot/build/test
writers and Unity locks are absent. Blender remains an open non-writing user
process and all agents are forbidden to call it.

The three reports can be produced concurrently. No implementation agent may
start until the main agent reviews these reports and freezes the Domain and
Theme contracts.
