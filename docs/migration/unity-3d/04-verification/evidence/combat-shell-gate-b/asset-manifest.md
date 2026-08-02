# Combat Shell Gate B asset manifest

> Status: local migration source verified; public redistribution authorization remains open under MIG-005.

| Godot source | Unity copy | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| `image/BG.png` | `Resources/Art/Battle/Background/BG.png` | 1678x962 | `2362BDF6226B3D203D8B8EC1FDA946BCFDA0984F626A70AA5F02EA7CF3C234F3` |
| `image/titleBG.png` | `Resources/Art/Battle/Background/titleBG.png` | 1344x768 | `9BAB726A018DE26B259696D9956D5654C9970CF2282E0EE8152079186BA2F409` |
| `image/outscene_block/out-bg_sea.png` | `Resources/Art/Battle/Background/out-bg_sea.png` | 256x256 | `3226E113FA2EB03B85E89287BB25CA24F97E40AD351980A6B49BF0D5B939AE3F` |
| `image/outscene_block/out-bg_shallow_layer.png` | `Resources/Art/Battle/Background/out-bg_shallow_layer.png` | 512x512 | `D145DE8AF231FDC08F4702E954106EFE4BB1D5B51EEBE622E3B161F719B58FB3` |

Each Unity copy has the same hash as its Godot source. Authoring reads the repository through `TIMEKEY_REPOSITORY_ROOT`; it never writes the source images.

All four textures use sRGB, point filtering, no mipmaps, max size 2048 and normal compression. `BG` and `titleBG` clamp; sea and shallow layers repeat. The sea material is opaque. The shallow material uses transparent URP blending, disables depth writes and is the only transparent world layer. Six saved renderers are used by the background Prefab: sea, shallow and four horizon panels.

The repository has no root project or image license that proves public redistribution rights for these four images. No conflicting license was found. Their status is therefore unchanged from MIG-005: permitted here only for local migration and verification; release packaging remains blocked until the project owner supplies provenance and redistribution authorization.
