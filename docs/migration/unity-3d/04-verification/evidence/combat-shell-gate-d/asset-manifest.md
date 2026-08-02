# Combat Shell Gate D asset manifest

> Status: repository-local source and copy integrity verified.

## New out-of-battle copies

All paths are relative to the repository root. Each Unity file is byte-identical to its Godot-side source.

| Repository-local source | Unity copy | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `image/outscene_block/out-block_desert.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_desert.png` | 512x512 | `E4F790176CC9E8BC1238998A11F5AB50F4CF92FB368D8EF81DDB0A5A0FF43E5C` |
| `image/outscene_block/out-block_forest.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_forest.png` | 512x512 | `6EF3E2793E0617BFB1EB3E95335F2ABC76E2D18C0C4134E32820F5B111D452F3` |
| `image/outscene_block/out-block_grass_dark.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_grass_dark.png` | 512x512 | `4A67353738B3865571C0414AF04155290BE1CE104E754E25E92B37D5F6934833` |
| `image/outscene_block/out-block_grass.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_grass.png` | 512x512 | `B3647A3881646562E985E31B93453C5BB47FF63E411A197F33E6F74416C945EE` |
| `image/outscene_block/out-block_mystery.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_mystery.png` | 512x512 | `236507973FF2B2E3032B62069BCA77BE3F918B20685F44A707A47FDD2B8E53EC` |
| `image/outscene_block/out-block_snow_mount.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_snow_mount.png` | 512x512 | `8579F9D903B3DEAECC4A8B612EAEE1B8F2138708D69552CEB0E55FFB740E82EC` |
| `image/outscene_block/out-block_snow.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_snow.png` | 512x512 | `B8DEF35F75F74B9B763EB8BB8A3DD930F8DB95A05742DA690E59861D0F56B2BD` |
| `image/outscene_block/out-block_stone.png` | `unity/Assets/_Project/Resources/Art/Shell/OutOfBattleShell/out-block_stone.png` | 512x512 | `4A9C88BC896C8951F9F9AE1F80BA3DE547AB74E62A8716519B14961AB8F7DBFC` |

The authoring command imports these files as single sprites with sRGB enabled, point filtering, clamp wrapping, no mipmaps, and uncompressed texture import. It requires `TIMEKEY_REPOSITORY_ROOT` and writes only the Unity copies.

## Reused assets

Gate D reuses, rather than recopies, the following assets:

| Purpose | Existing Unity asset | SHA-256 |
| --- | --- | --- |
| out-of-battle background | `unity/Assets/_Project/Resources/Art/Battle/Background/BG.png` | `2362BDF6226B3D203D8B8EC1FDA946BCFDA0984F626A70AA5F02EA7CF3C234F3` |
| Game Over background | `unity/Assets/_Project/Resources/Art/Battle/Background/titleBG.png` | `9BAB726A018DE26B259696D9956D5654C9970CF2282E0EE8152079186BA2F409` |
| all Gate D player-visible text | `unity/Assets/_Project/Resources/Fonts/Silver.ttf` | `7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1` |

The background source/copy checks are recorded in the Gate B `asset-manifest.md`. Silver attribution and release conditions are maintained in `docs/migration/unity-3d/06-maintenance/simplified-chinese-localization.md` and `unity/Assets/_Project/Resources/Fonts/Silver-ATTRIBUTION.txt`.

This manifest records repository-local provenance and file integrity only. It does not infer ownership or redistribution permission for the eight Godot-side tile images.
