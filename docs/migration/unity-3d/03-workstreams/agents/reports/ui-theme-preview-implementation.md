# Wave 03P UI Theme Preview Implementation

## Ownership

- Added `unity/Assets/_Project/Editor/ThemeMigration/UiThemePreviewWindow.cs` and its `.meta`.
- Did not modify Scene, Prefab, theme asset, validator, tests, asmdef, or shared documentation.
- Did not run Unity, Godot, build, tests, or Git commands.

## Implementation

- Added an Editor-only, read-only preview window at `TimeKey/Migration/Open UI Theme Preview`.
- Loads `TimeKeyUiTheme` from `UiThemeValidator.DefaultThemePath`.
- Enumerates every `UiStyleId`, including explicit missing-style diagnostics.
- Displays font name, font size, text color, frame fill, frame Sprite name, and button normal/highlight/pressed/disabled colors.
- Provides `Refresh` to reload the asset and `Validate` to render `UiThemeValidator` diagnostics without mutating the asset.

## Static Risk

- The window was inspected statically only, per ownership instructions. Unity compilation and rendered EditorWindow verification remain for the main agent.
- Color swatches use fixed 84 x 22 pixel cells inside a scroll view; the 760-pixel minimum width is intended to keep all four button-state swatches readable.
