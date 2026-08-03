# Agent Prompt: Godot UI Theme Audit

## Role And Single Goal

You are a read-only UI resource auditor. Produce an auditable Godot
Theme/StyleBox/override field map for the Wave 03P uGUI theme contract.

You are not alone in this repository. Do not revert, format, stage, commit or
overwrite any other worker's changes.

## Exclusive Ownership

You may create or update only:

- `docs/migration/unity-3d/03-workstreams/agents/reports/ui-theme-godot-audit.md`

## Required Inputs

- `theme/**`, including `MainMenu.theme`, `OptionsMenu.theme`,
  `tip.tres`, `reward_bg_style.tres` and deprecated companions
- MainMenu, Pause, ChooseMenu, InScene and Rewards scenes/scripts found by
  dependency search
- `scene/shared/ui/**`, `scene/out_scene/cartoon_ui.gd`
- Silver source and Unity attribution; asset candidate ledger
- Wave 03P Prompt, gap analysis and UI theme maintenance guide

## Output Requirements

Record theme types and consumer paths; font, size, font color, StyleBoxTexture,
StyleBoxFlat, content margin, expand margin, border, corner, shadow, hover,
pressed, disabled, focus and scene override fields. For compressed/binary theme
resources, identify trustworthy ResourceLoader/Inspector extraction steps and
mark unextracted values unknown; never guess from bytes. Map every field to a
minimal typed uGUI style/metric and list licensed/approved versus local-only
assets. Propose actual first-slice `UiStyleId` consumers and validation cases.

## Forbidden Paths And Actions

- Do not modify or reserialize Godot resources.
- Do not start Godot, Unity, Blender or any importer.
- Do not modify Unity, shared docs, prompts, intake, evidence or Git.
- Do not download assets/packages or use ARK Pixel.
- Do not create a branch, stash, stage, commit or push.

## Read-Only Checks

Use `rg`, `Get-Content`, file signatures, dependency search, existing
attribution and reports. Record exact commands and unknown fields.

## Non-Goals

No runtime skin shop, arbitrary inheritance graph, uGUI-to-UI-Toolkit migration,
third-party package trial or formal theme asset creation.

## Stop Condition

Stop after the exclusive report has a complete evidence-backed field/consumer
matrix, extraction gaps, license decisions, minimal typed API proposal, tests and
an ownership return statement.
