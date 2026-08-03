# Wave 03P Godot UI Theme Audit

> Audit date: 2026-08-03
> Owner: `wave03p_godot_theme_audit`
> Scope: Godot Theme/StyleBox/scene override evidence for the Unity uGUI theme contract
> Ownership return: this report is the only path written by this agent; no Godot/Unity/Blender/import/build/test process was started.

## 1. Read-only provenance and constraints

The assigned prompt, Wave 03P prompt, gap analysis, UI theme maintenance guide, asset candidate ledger, and Silver attribution were read before the audit. The following read-only commands were used from the repository root:

- `Get-Content -Raw docs/migration/unity-3d/03-workstreams/agents/ui-theme-godot-audit.md`
- `rg --files theme`
- `Get-Content -Raw theme/MainMenu.theme theme/OptionsMenu.theme theme/tip.tres theme/reward_bg_style.tres theme/1.tres`
- `Format-Hex -Path theme/MainMenu.theme/OptionsMenu.theme -Count 64`
- `Get-FileHash theme/* -Algorithm SHA256`
- `rg -n 'res://theme/(MainMenu|OptionsMenu|tip|reward_bg_style|1)' scene`
- `Select-String` over MainMenu, Pause, ChooseMenu, InScene and Rewards scenes for `theme`, `theme_override_*`, StyleBox and margin fields
- `rg -n` over the same scenes/scripts for `StyleBoxFlat`, `add_theme_*_override`, and `font`/`Color` consumers
- `Get-Content -Raw` for the relevant scene/scripts and `Silver-ATTRIBUTION.txt`

Godot was intentionally not started by this read-only auditor. The compressed Theme values below are never inferred from bytes. They remain `UNKNOWN` until the main agent performs the approved ResourceLoader/Inspector extraction in an isolated, read-only copy.

## 2. Theme resource inventory

| Resource | Godot type and evidence | Size / SHA-256 | Extracted fields | Decision |
| --- | --- | --- | --- | --- |
| `theme/MainMenu.theme` | `Theme`; header begins `RSCC` (compressed Godot resource) | 580 bytes / `08A2BC348DF4A63DDFE8B6D5EEDB4D27CEFA684EE89F60D4DB05A074203E44EF` | All font, color, icon, StyleBox, state and metric fields `UNKNOWN` | Preserve source; do not parse bytes or reserialize |
| `theme/OptionsMenu.theme` | `Theme`; header begins `RSCC` | 1480 bytes / `AC1E8DB670151E30A3FB09247B704499AD55BFDE9CDEA1EACF88C0915D6667DF` | Same fields `UNKNOWN` | Preserve source; do not parse bytes or reserialize |
| `theme/MainMenu.theme.depren` | Deprecated compressed companion; header begins `RSCC` | 586 bytes / `A1168DF67909457F9306E587D05B455576380FFFC444288DADC89A883D96222F` | `UNKNOWN`; not treated as a fallback | Preserve only; no new consumer |
| `theme/OptionsMenu.theme.depren` | Deprecated compressed companion; header begins `RSCC` | 1488 bytes / `55FBF1704C731C888DE50124CB79DD646252612538C6F9332B8A7AA6522DB949` | `UNKNOWN`; not treated as a fallback | Preserve only; no new consumer |
| `theme/tip.tres` | Text `Theme` | 622 bytes / `DF5F328F0EB116FF45BE3D064E9D5C08F11309881E7A24C1A26E716104F09BF2` | `default_font = fonts/ark-pixel...`; `AcceptDialog/styles/panel = StyleBoxTexture(black rect)`; expand margins 2048 on all sides | Local reference only; ARK Pixel is not approved for Unity |
| `theme/reward_bg_style.tres` | Text `StyleBoxTexture` | 332 bytes / `905C07F54E41722CB59BA5F80523AEA70FC3D56EC8CB3612DD26E6371CDBBC70` | texture `image/black rect.png`; expand L/T/R/B = 194/359/155/420 | Local reference only; keep content and visual expansion separate |
| `theme/1.tres` | Empty text `Theme` | 74 bytes / `5DEE7C3A652738EEAEB94B334D07B403F8E49F462FE1299C57EB1729201B9E63` | No `[resource]` fields | No consumer found; do not use as a default |

`MainMenu.theme` UID is `uid://dr1f72ftkx0ma`; `OptionsMenu.theme` UID is `uid://w172uwoow3s3`; `tip.tres` UID is `uid://dq68holf5037x`; `reward_bg_style.tres` UID is `uid://bqtefxup2k8n6`. The deprecated companions are separate bytes and must not silently replace the active resources.

## 3. Consumer and override matrix

### 3.1 `MainMenu.theme`

The active theme is referenced by:

- `scene/main_menu/main_menu.tscn`: root `custom_theme` is `tip.tres` for runtime dialogs; six `TextureButton` nodes (`NewGameBtn2`, `ContinueBtn`, `SettingsBtn`, `DatabaseBtn`, `QuitBtn` and their button family) reference `MainMenu.theme`; the separate visible labels use ARK Pixel at 64; title uses ARK Pixel at 144; seed `LineEdit` uses ARK Pixel.
- `scene/main_menu/menu_set/main_set.tscn`: `Back` Button.
- `scene/main_menu/menu_quit/main_quit.tscn`: `Tip` TextureRect, `Back` Button and `Confirm` Button; prompt label uses ARK Pixel at 48.
- `scene/main_menu/menu_guide/main_guide.tscn`: four guide Buttons and `Back`; HBox separation is 48.
- `scene/pause_menu/pause_menu.tscn`: Save/Back/Quit Buttons; `SaveSettings` Label also references `OptionsMenu.theme` with local font size 50.
- `scene/out_scene/choose_menu/choose_menu.tscn`: `SaveToTitle` and `Quit` Buttons; `Name` and `Description` Labels reference `OptionsMenu.theme` and override sizes 60/40.
- `scene/in_scene/in_scene.tscn`: `EndTurnButton`, `EndCombatButton`, `HeightViewToggleButton`; all are Buttons with local size 48 on the first two.
- `scene/in_scene/rewards/acquire_reward.tscn`, `craft_reward.tscn`, `remove_reward.tscn`: Back/Confirm Buttons; each local font size is 60.
- `scene/in_scene/rewards/shop.tscn`: Exit/Refresh/Upgrade Buttons; local font size 40 and cost labels 32.
- `scene/pile/pile_viewer.tscn`: one theme reference on the pile viewer root.
- `scene/in_scene/in_scene.tscn`: additional non-theme overrides for battle labels, tooltips, timecoin and outcome buttons are listed in section 3.3.

Runtime `scene/main_menu/main_menu.gd` assigns the exported `custom_theme` to dynamically created `ConfirmationDialog` instances for tutorial and database prompts. It also assigns the exported `tutorial_font` to dialog, title and button fonts. This is a runtime consumer of the same visual contract, but it is not a source of Theme field values.

### 3.2 `OptionsMenu.theme`

- `scene/options_menu.tscn` assigns `OptionsMenu.theme` to the root `OptionsMenu` Control. Audio/Display title labels override font size 50, popup value labels override 30, and the audio grid overrides horizontal/vertical separation to 30/10.
- `scene/pause_menu/pause_menu.tscn` embeds `options_menu.tscn` and separately assigns `OptionsMenu.theme` to `SaveSettings`.
- `scene/out_scene/choose_menu/choose_menu.tscn` uses it for the character name and description labels.

### 3.3 Text StyleBox and scene override evidence

The following values are extracted from text `.tscn` files and are authoritative; they do not depend on compressed Theme extraction:

| Consumer | Source field | Exact value | Minimal Unity mapping |
| --- | --- | --- | --- |
| Options value popup | `StyleBoxTexture_4gmlt.texture` | `image/texture/hexagon_frame.png` | Sprite candidate; preserve source and verify border before `Image.Type.Sliced` |
| Options value popup | `texture_margin_left/top/right/bottom` | 2/2/2/2 | Sprite border candidate, not layout padding |
| Options value popup | `expand_margin_left` | 3 | Visual overflow metric; never fold into sprite border |
| Options value popup | `region_rect` | `Rect2(2, 0, 16, 20)` | Source region/atlas rect; record separately from border |
| In-scene timecoin panel | `StyleBoxTexture_pg1mb.texture` | `image/timecoin_ui_back.png` | Existing Sprite candidate; preserve Image sliced behavior |
| In-scene timecoin panel | `expand_margin_left/top/right/bottom` | 60/60/60/60 | Visual overflow metric; separate from content padding |
| Reward backgrounds | `reward_bg_style.tres.expand_margin_*` | 194/359/155/420 | Frame overflow metrics; do not treat as Unity sprite borders |
| Main menu Tip dialog | `tip.tres` StyleBoxTexture expand margins | 2048 on all sides | Dialog visual expansion metric |
| Craft result description | `ResultDescriptionMargin` margins | 18/18/18/18 | `RectOffset` content padding |
| Main menu guide / pause / choose | HBox/VBox separation | 48; pause/choose 20 | Container spacing metric |
| Main menu title/labels | Font sizes | title 144; menu labels 64; exit prompt 48 | `UiTextStyle.fontSize`, all player-visible text must use Silver in Unity |
| Options/pause/choose | Font sizes | 50, 30, 60, 40 as listed above | `UiTextStyle.fontSize` local override mask |
| Battle top HUD | Font sizes | era/process 24; deck/discard 64; end-turn/end-combat 48 | `TopHud`/button/text style IDs |
| Battle tooltip | Outline | font outline black, outline size 4 | `Outline` component values |
| Timecoin amount | Color/font size | black; font size 40 | `UiTextStyle.normalColor` and size |

`theme_override_fonts` in these scenes point to `fonts/ark-pixel-12px-proportional-zh_cn.otf` (UID `uid://dl3b53uhno64o`) except where a script/theme supplies it. The OTF hash is `7CA829A2A5D702E72AE46B6CADA4CA121E8E2ADD46B879431D89822A05FA7E0D`; its license is not verified and it must not be copied to Unity.

### 3.4 Runtime-created StyleBox/override consumers

These scripts are part of the requested dependency audit. They do not expose a serialised Theme and therefore must be migrated by typed style IDs rather than copied as an arbitrary dictionary:

| Script | Direct visual fields | Exact evidence |
| --- | --- | --- |
| `scene/in_scene/in_scene_modules/ui/CursorTooltipController.gd` | `StyleBoxFlat` is built from `tooltip_config`: background, border width/color, corner radius; panel margins 10/10/8/8; label uses `StyleBoxEmpty` normal | Config values intentionally remain unknown until the caller config is audited |
| `scene/in_scene/enermy/intent_presentation_modules/presenters/EnemyIntentStatusKeywordTooltipPresenter.gd` | dark panel, border 2, gold border, corner radius 6; margins L/R 12, T/B 10; title color is keyword data; description width is caller-supplied | `bg_color=(0.12,0.12,0.12,0.95)`, `border_color=(0.8,0.6,0.2,1)` |
| `scene/in_scene/rewards/presenters/CraftResultDescriptionPanelPresenter.gd` | same dark/gold panel values; default text color | background `(0.12,0.12,0.12,0.95)`, border 2, gold `(0.8,0.6,0.2,1)`, radius 6, text `(0.95,0.95,0.95,1)` |
| `scene/in_scene/hex_map_modules/presenters/SettlementRewardPresenter.gd` | reward tooltip panel, gold border, hover scale, label size/color and margins | bg `(0.1,0.08,0.04,0.88)`, border 2, `(1.0,0.78,0.18,1)`, radius 6; margins 10/10/6/6; font size default 18; label `(1.0,0.93,0.72,1)`; hover scale 1.08 for 0.12 s |
| `scene/in_scene/enermy/total_enemy_health_bar.gd` | panel, margin, title/info labels and generated health bar | bg `(0.06,0.07,0.1,0.88)`, border `(0.82,0.68,0.28,1)`, width 2, radius 10; margins 16/16/12/12; title 24, info 18; default font ARK Pixel |
| `scene/in_scene/victory/combat_victory_banner.gd` | banner panel | bg `(0.08,0.09,0.13,0.92)`, border `(0.95,0.82,0.28,1)`, width 3, radius 16 |
| `scene/in_scene/timeline/ui_modules/presenters/TimelineActionBlockPresenter.gd` | action block fill is data color; border 2 black when group visual is disabled, otherwise 0; enemy overlay is a material | No static fill color; action identity remains outside theme |
| `scene/in_scene/timeline/ui_modules/grid/TimelineGridBuilder.gd` | grid cell fill is caller color, border width 0; h/v spacing is caller value | No static fill color; theme must provide a typed timeline-cell style |
| `scene/shared/ui/combat_cartoon_ui.gd` / `scene/out_scene/cartoon_ui.gd` | top panel/clock/ring/point assets and layout, no Godot Theme fields | stable scene assets and coordinates are not generated by a theme binder |

## 4. Godot field-to-uGUI mapping contract

The mapping below is the minimum typed contract. `UNKNOWN` means the value must be filled by ResourceLoader/Inspector evidence; it is not permission to choose an approximation.

| Godot field | Unity typed destination | Rule |
| --- | --- | --- |
| `default_font`, `theme_override_fonts/*` | `UiTextStyle.font` | Unity uses the approved Silver Font; ARK Pixel remains local-only reference |
| `font_size`, `*_font_size` | `UiTextStyle.fontSize` | Integer metric with a local override mask |
| `font_color`, `font_outline_color`, keyword colors | `UiTextStyle.normalColor/outlineColor` | Per-state values; no color strings as runtime contract |
| `StyleBoxTexture.texture` | `UiFrameStyle.backgroundSprite` | Existing asset or manually approved import only |
| `texture_margin_*` | Sprite border / `Image.Type.Sliced` border | Validate non-zero border and source texture dimensions |
| `region_rect` | `UiFrameStyle.sourceRegion` | Keep independent from sprite border |
| `content_margin_*`, container margins | `UiLayoutMetrics.padding` (`RectOffset`) | Affects child layout, not sprite border |
| `expand_margin_*` | `UiLayoutMetrics.visualOverflow` (`Vector4`) | Affects visual footprint; never merge into padding or sprite border |
| `bg_color` | `UiFrameStyle.fillColor` | Applies to a saved `Image`/panel node |
| `border_width_*`, `border_color` | `UiFrameStyle.border` (`Vector4`) and `borderColor` | Use a saved frame/border component; warn if the prefab has no border target |
| `corner_radius_*` | `UiFrameStyle.cornerRadius` | Metric is preserved; validator warns if the target sprite/material cannot express it |
| `shadow_color`, `shadow_size`, `shadow_offset` | `UiDecorationStyle.shadow` | Bind only to saved `Shadow`/outline components |
| `normal/hover/pressed/disabled/focus` | `UiButtonStyle.states` / `ColorBlock` + `SpriteState` | Focus is a first-class state; do not collapse it into hover |
| Scene `theme_override_*` | `UiThemeScope.localOverride` with a typed mask | Merge only specified fields; do not clone the whole Theme |

Recommended first-slice IDs (stable enum, not arbitrary strings): `Panel`, `PrimaryButton`, `SecondaryButton`, `DangerButton`, `TopHud`, `OverworldNode`, `ConfirmationDialog`, `CardEffectFrame`, `PlayerActionFrame`, `EnemyIntentFrame`, `TimelineCell`, `Tooltip`.

## 5. Approved versus local-only assets

### Approved for Unity player text

`unity/Assets/_Project/Resources/Fonts/Silver.ttf` is the only approved player-visible font. The file is 3,740,480 bytes, SHA-256 `7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1`, GUID `35b5b371d76876b4f8b26cd2376eb08c`. Attribution is in `unity/Assets/_Project/Resources/Fonts/Silver-ATTRIBUTION.txt`, source `https://poppyworks.itch.io/silver`, CC BY 4.0. The source condition requiring direct contact with Poppy Works above USD 100,000 total spend or earnings remains in force and must be rechecked before public release.

### Local-only or unverified references

- ARK Pixel OTF is `REJECT` for Unity import: no verified license/source in the repository and it conflicts with the Silver requirement.
- `image/black rect.png`, `image/texture/hexagon_frame.png`, `image/gold rect.png`, `image/pole.png`, and other Godot UI images have no verified public license in the project. They remain source references only; no new Unity copy is authorized by this audit.
- Existing Unity clock copies are byte-identical local copies, not a new license grant: `clock_noring.png` SHA-256 `871A9CA3B1AB44965B89D81DFEECF4DF5EA08D68295B8C283EC019C48335225E`, `ring.png` `A5F2D81981DAF2F50213A8F244E6176C2B1D6DCD3F374AF617D4EDAC924A37FD`, `point.png` `9C9000C24F0A4630C9498A4C00716652A4EB1A9A9FB30DAC5DB7D6B3601A0FB9`. Their Unity importer `spriteBorder` is currently all zero and `spriteMode` is 0, so a 9-slice claim requires a separate, approved sprite asset/importer decision.
- Existing MainMenu button PNGs are also local copies with unverified artwork licensing and currently use `TextureImporter.spriteMode: 0` and zero sprite border. They are not evidence of a valid 9-slice frame.

## 6. Trusted compressed-resource extraction procedure

The main agent must perform this after the ownership gate, in a disposable copy of the Godot project or a read-only project mount:

1. Hash `MainMenu.theme`, `OptionsMenu.theme` and their `.depren` companions before loading. Do not open the original resource for save.
2. Use a minimal, temporary Godot Editor/ResourceLoader harness to call `ResourceLoader.load(path, "Theme", ResourceLoader.CACHE_MODE_IGNORE)`. Enumerate `get_type_list()` and each type's `get_color_list`, `get_constant_list`, `get_font_list`, `get_font_size_list`, `get_icon_list` and `get_stylebox_list` values.
3. For every returned StyleBox, enumerate its `get_property_list()` and read only documented properties. Record resource class (`StyleBoxTexture`, `StyleBoxFlat`, etc.), texture UID/path, margins, expand margins, borders, corner radii, shadows and state-specific values. Emit JSON/CSV with source path, UID, field, value and extraction tool/version.
4. Cross-check the same resource in the Godot Inspector on the isolated copy. Any Inspector/ResourceLoader disagreement is `BLOCKED_FOR_REVIEW`, not a guessed value.
5. Hash the source files again and prove unchanged bytes. Delete the disposable harness/copy after the report is preserved; never reserialize a Godot resource as part of Unity migration.

Until this procedure produces an auditable export, all compressed Theme state fields are `UNKNOWN`. The `.depren` files are not trusted fallbacks.

## 7. First-slice validation cases

The Unity Theme validator and EditMode tests should cover:

- missing `TimeKeyUiTheme`, missing `UiStyleId`, missing font, missing sprite and null target node;
- Silver font GUID and material consistency on visible text;
- invalid/zero 9-slice border, texture region outside source bounds, and content/expand margin confusion;
- every state (`normal`, `hover`, `pressed`, `selected/focus`, `disabled`) resolves without silently falling back to a different style ID;
- local override merges only its mask and is idempotent across repeated `OnEnable`/`Rebind`;
- saved `ThemeScope` and binder never instantiate stable UI tree nodes;
- first-slice consumer coverage for `Panel`, `PrimaryButton`, `SecondaryButton`, `DangerButton`, `TopHud`, `OverworldNode`, `ConfirmationDialog`, `CardEffectFrame`, `PlayerActionFrame`, `EnemyIntentFrame`, `TimelineCell`, and `Tooltip`;
- long Chinese strings using Silver do not clip at 1280x720, 1920x1080, 2560x1080 or after dynamic resize.

## 8. Ownership return

Only `docs/migration/unity-3d/03-workstreams/agents/reports/ui-theme-godot-audit.md` was created by this agent. No Godot resource, Unity file, Scene/Prefab, prompt, intake, evidence, Git state or process was modified. The main agent may now use this report to freeze the typed uGUI theme contract and implement the first Editor-only slice.
