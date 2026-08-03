# Asset Search / Import Audit

> 审计日期：2026-08-03（Asia/Shanghai）
>
> 范围：Godot 原素材、Unity 已导入资源、字体、卡面、模型、贴图、音频、Shader、Blender/FBX 产物和授权缺口
>
> 约束：全程只读；未下载、生成、执行、导入、移动、重命名任何资产，未调用 Blender/MCP，未修改独占报告外路径

## 结论

整体状态为 `NEEDS ATTENTION`，但没有要求主智能体停止 SceneContractValidator 首切片的资产硬阻塞：

- Silver 的来源、作者、CC BY 4.0、署名和额外预算/收入门槛记录完整，可继续作为 Unity 唯一 UI 字体。
- 七张正式卡面在 Godot 源与 Unity 副本之间 SHA-256 完全一致，JSON 的七个 `front_image` 均存在；历史 stable ID `lighting` 未改。
- Unity 41 张 PNG 均能反查到字节相同的 Godot 源路径；没有发现迁移时重编码。但这些图片除 Silver 以外不存在统一许可表，公开发布仍是 `UNVERIFIED`。
- 13 个项目 OGG 都是 48 kHz stereo Vorbis，Unity `_Project` 没有音频副本；授权未确认前不得导入。
- Godot `.gdshader` 不能直接导入 Unity；当前 Unity 仅有材质，没有项目 `.shader`。只能按真实视觉需求逐项重写和验证。
- Blender 生成链具备脚本、两个 `.blend`、两个 `.fbx` 和模型验证 JSON；几何可复现，但项目没有该组资产的作者/发布许可声明。
- `lighting.png` 与卡背 `behide.png` 仍为 Default Texture；其他六张正式正面为 Sprite/Single。现有 runtime 对 `lighting` 有 Texture2D fallback，此差异应由后续 CardAssetAudit 报警，不得在本阶段静默改 Importer。
- `behide.png` 在 Unity 非 meta 文件中没有 GUID 或路径字符串引用，是“可能孤立”而不是“可删除”。

候选逐项准入、许可证、版本、依赖、二进制风险和加入/移除路径见 `docs/migration/unity-3d/01-assessment/asset-and-plugin-candidate-ledger.md`。

## 审计方法与边界

执行了以下只读检查：

- `rg --files` 与扩展名/大小分组，覆盖 prompt 指定的 `card_asset`、`card_data`、`image`、`audio`、`fonts`、`shaders`、`addons`、Godot `scene`、Unity `Resources`/`ArtSource`。
- `Get-FileHash -Algorithm SHA256` 对源/副本、字体、Blender、FBX、脚本和音频计算哈希。
- 解析七份 JSON 的 `front_image`，验证对应源文件存在。
- 只读解析 Unity `.meta` 的 GUID、TextureImporter、FontImporter 和 ModelImporter。
- 用 GUID 与字符串静态反查 Unity Scene/Prefab/Material/C# 使用面；用 `res://` 静态扫描 Godot 资源引用。
- 用 `ffprobe 8.1.1` 只读提取 OGG codec/sample rate/channels/duration；没有转码或写入。

静态“未引用”不能证明运行时动态路径、Resources、反射或外部脚本绝对不使用资产，因此本报告只将结果标为候选，不授权删除。

## 库存

### Godot/源侧

在 `card_asset`、`card_data`、`image`、`audio`、`fonts`、`shaders`、`addons` 和 `scene` 范围内：

| 扩展 | 文件数 | 备注 |
| --- | ---: | --- |
| `.png` | 228 | 项目图片、卡面、插件/示例图片 |
| `.jpg` | 2 | 项目图片 |
| `.ico` | 1 | 项目图标副本 |
| `.ogg` | 13 | 3 BGM + 10 SFX |
| `.wav` | 5 | 位于 vendored/addon 范围，不等同项目 `audio/**` 授权 |
| `.ttf` | 3 | 位于 addon/example 范围 |
| `.otf` | 1 | ARK Pixel |
| `.gdshader` | 28 | 项目 `shaders/**` 21 个，其余在 addon/scene |
| `.json` | 7 | 正式卡牌 fixture |
| `.tres` | 41 | Godot Resource/Theme/插件配置 |
| `.tscn` | 126 | Godot Scene/插件 Scene |

筛选的 402 个内容文件按“相对路径 + SHA-256”排序后，内存中计算出的清单摘要为：

`D2291C287353366A5D1A29D94C7A3827CFD11DD20ED58F4A7856F5FEFBDA8C00`

该摘要用于证明本次盘点输入，不替代每个候选的逐文件哈希和许可。

### Unity `_Project`

| 类型 | 文件数 | 备注 |
| --- | ---: | --- |
| PNG | 41 | 全部位于 `Resources/Art/**`，均有字节一致 Godot 源 |
| TTF | 1 | Silver |
| FBX | 2 | Dirt/Grass 六边形地块 |
| Blend | 2 | `ArtSource/HexTiles/**` |
| Material | 6 | Unity material；不能据此推导 Godot shader 已迁移 |
| JSON | 7 | 七张正式卡牌 fixture，字节与根 `card_data/**` 一致 |

Unity `_Project` 未发现 OGG/WAV/MP3，也未发现项目 `.shader` 文件。

## 七张卡牌源/副本哈希与 Importer

每个源位于 `card_asset/<file>`，副本位于 `unity/Assets/_Project/Resources/Art/Battle/Cards/<file>`。源与副本使用同一表中 SHA-256。

| Stable ID | JSON `front_image` | SHA-256 | Unity GUID | TextureImporter |
| --- | --- | --- | --- | --- |
| `earthquake` | `earthquake.png` | `E03EC4E9DAA4DA9BB26F4EF80803B0CB9F506497C777560B80567F773C6463F1` | `f527a20a8fcb4257a2c6797b595a4d68` | type 8 Sprite, mode 1, Point, compression 0 |
| `lighting` | `lighting.png` | `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC` | `4de89fa029e12f94180ff9364ed55f14` | type 0 Default, mode 0, Point, compression 0 |
| `poison` | `poison_card.png` | `936B2BE26879462D0A18E1F0F5867E3681461A744E73E56823E3B1E8AEDCB879` | `4298bec2513245d39622fb9b048857cc` | type 8 Sprite, mode 1, Point, compression 0 |
| `recover` | `recover.png` | `F9065BC4FD646D8EEFC463FBC35798EDDDDDE489C4214F6333ACCC6E54A23D99` | `8c68f1759d704008a12bc05a5538def3` | type 8 Sprite, mode 1, Point, compression 0 |
| `tornado` | `tornado.png` | `B369DCC027670C4AA28E003FC61B97DCE5DB077B982AEE6BAFDC84766B00E564` | `6a77ee2982e246d7bcd14b153374e8bb` | type 8 Sprite, mode 1, Point, compression 0 |
| `tower` | `tower_card.png` | `73BFFC6162D4D0D09D196835E3AC1BE5D40FEE19BC6C5BC391008386471AF05B` | `10a8065097cf4488b10adc4464b645a5` | type 8 Sprite, mode 1, Point, compression 0 |
| `wind` | `wind.png` | `BFCDA4C0A872FE327EFDFB2FF6880E4A343CC176484CCE5BEE0BC13C39DDB498` | `e430ea98fe4844d2979ac76e45990ced` | type 8 Sprite, mode 1, Point, compression 0 |

共同属性：1135×1590、非 2 的幂、max 2048、mipmap off、Point、平台 compression 0。七个 JSON 都解析成功，且 `front_image` 对应源文件存在。`lighting` 的 type 0 是既有兼容行为：`CombatCompositionRoot` 先 `Resources.Load<Texture2D>`，再在没有 imported Sprite 时创建 runtime Sprite；不能把 Importer 差异当作立即修复授权。

源文件总计约 6.26 MiB。按 RGBA32 粗估，七张同时驻留且未压缩时约 48.2 MiB，不含引擎开销；这只是风险上界，正式结论必须来自 Windows Player profiler。

## 其他 Unity 图片的来源一致性

SHA-256 反查确认 Unity 41 张 PNG 都存在字节一致 Godot 源。主要映射组：

| Unity 目录 | Godot 源目录 | 数量/结论 |
| --- | --- | --- |
| `Resources/Art/Battle/Cards/**` | `card_asset/**` + `image/behide.png` | 8/8 identical |
| `Resources/Art/Battle/Background/**` | `image/BG.png`、`titleBG.png`、`outscene_block/**` | 4/4 identical |
| `Resources/Art/Battle/{center_altar,dirt,grass}.png` | `image/enermy/**`、`image/inscene_block/**` | 3/3 identical |
| `Resources/Art/Battle/Occupants`、`Status` | `image/enermy/tower`、`image/effect` | 2/2 identical |
| `Resources/Art/Shell/GameStart` | `image/key.png` | 1/1 identical |
| `Resources/Art/Shell/MainMenu` | `image/clock/**`、`image/main_menu/**` | 15/15 identical |
| `Resources/Art/Shell/OutOfBattleShell` | `image/outscene_block/**` | 8/8 identical |

这里的“identical”只证明迁移没有改字节，不证明原始图片具备商用/再分发许可。

## 字体

### Silver

- 路径：`unity/Assets/_Project/Resources/Fonts/Silver.ttf`
- SHA-256：`7ADCF56D93142DED08FF18196F78FCAAF552411B9A94DC559DAC90EB5C91CEF1`
- 大小/GUID：3,740,480 bytes；`35b5b371d76876b4f8b26cd2376eb08c`
- Importer：TrueTypeFontImporter、fontSize 16、includeFontData 1、font name `Silver`。
- 许可：Poppy Works / Wolfgang Wozniak，CC BY 4.0；项目内署名文件完整。来源另有总预算/收入超过 USD 100,000 联系作者的条件，公开发布前必须复核。

### ARK Pixel

- 路径：`fonts/ark-pixel-12px-proportional-zh_cn.otf`
- SHA-256/大小：`7CA829A2A5D702E72AE46B6CADA4CA121E8E2ADD46B879431D89822A05FA7E0D`；3,395,760 bytes。
- 来源/作者/许可：仓库内未找到可核验记录，`UNVERIFIED`。
- 决策：不复制到 Unity，不替换 Silver。

## 音频

项目 `audio/**` 的 13 个 OGG 均为 Vorbis、48 kHz、stereo；组内未发现重复 SHA-256：

| 文件 | 时长秒 | SHA-256 |
| --- | ---: | --- |
| `bgm/battle_boss.ogg` | 93.600 | `233601FF9CEE317ACFDA98E8D55001A54908769E25F1B86AE932009F5099748F` |
| `bgm/battle.ogg` | 83.077 | `ED0AB1E6B9484C2E00E9DB330FDA31D072A3B5ACFC7025E628635ED33856A338` |
| `bgm/main_menu.ogg` | 69.036 | `669B903951227AB669FA0D852C6B8C78FEE45A662F88D5784B7B320C52E3DF7D` |
| `sfx/card_shuffle.ogg` | 2.487 | `F5E1E85588D6C0B758DA77C50888258EF21E82CA2C793DD4533CC2FC5095D9C6` |
| `sfx/choose_role.ogg` | 3.045 | `40A8E1EC54B56C96A5C957259502F0A98A35D44831C6C70B08F1C1D03BCC8CFD` |
| `sfx/clock_tick.ogg` | 5.254 | `AFC48B2061C15480B822108F979D9FDE262DC7AB8A5475756F9E42AC39879D8A` |
| `sfx/confirm_timeline.ogg` | 0.473 | `CAB06988E0906D0A4D9CAB63E3A5E15CF6F6CC47664F42A8D355FBFBEA5AA25B` |
| `sfx/draw_card.ogg` | 0.719 | `58D76D7B00A360B5461403C93362BBAF053608A1057240BAB7977093DE908EE2` |
| `sfx/game_over.ogg` | 16.000 | `2FBD2C0C340D419C37CEADC809579FD94D0B16B13D11294901043914FE12516A` |
| `sfx/tile_damage.ogg` | 1.808 | `6AC7A0156A922F0D5BA302DAF659F6877B1B789F41C48B8415149317ABCC4920` |
| `sfx/victory_long.ogg` | 23.134 | `B8B4F4CE199DE8530B98B0E73B2492848E3CF2A0E3F2683E6C2F83482078AC9A` |
| `sfx/victory_short.ogg` | 6.804 | `23CBF268E037A64A81D8BC16AA0BF8D643A3CCDF271CE74A2D41CFB355C09505` |
| `sfx/walk_dim.ogg` | 3.556 | `3D623CE0C1369C699865F815B243129570D0F58C27CF6981AEDF6F90127F596E` |

没有项目级来源/作者/许可记录。`addons/dialogic/Example Assets/sound-effects/LICENSE.txt` 的 Tim Krief / CC BY-SA 4.0 只覆盖 Dialogic 示例音效，不能覆盖上述 13 个文件。Unity 当前没有音频副本，因此正确动作是维持未导入，而不是猜测许可。

## Shader

- 项目 `shaders/**` 有 21 个 `.gdshader`，扫描总范围 28 个。
- 命名异常：`pulse_highlight.gdshader.gdshader`、`shape_drag_state.gdshader.gdshader` 为重复扩展名；本阶段不重命名。
- 静态 `res://` 扫描在项目 `shaders/**` 标出 6 个无直接路径引用：`1.gdshader`、`custom_card.gdshader`、`enemy_effect_preview.gdshader`、`height_view_shader.gdshader`、`hex_interactive.gdshader`、`map_select.gdshader`。它们可能通过动态/UID/旧场景使用，只能作为孤立候选，不能删除。
- Unity `_Project` 没有 `.shader`；6 个 `.mat` 使用现有 Unity/URP shader。Godot shader language、screen texture 和 Canvas 语义不能直接导入 Unity。
- 许可/作者：根项目没有统一记录，公开发布仍 `UNVERIFIED`。未来逐项重写时必须记录原语义、作者权属、采样/overdraw/透明成本和视觉回滚。

## Blender / FBX / MCP 产物

本阶段没有调用 Blender/MCP。现有链路为本地脚本生成，不是网络候选下载：

| 文件 | SHA-256 | Unity GUID |
| --- | --- | --- |
| `ArtSource/HexTiles/generate_hex_tiles.py` | `A3449A181607CE5B496CBAED47FB6391D329891039510E810E5E70B907A44FEE` | `2964241a9de0ad14f817e24a4cafa73d` |
| `ArtSource/HexTiles/HexTile_Dirt.blend` | `71CF17E3704F6734BA2FEB7FCB11B70FB22BE17CAB8CFC237BFC77E47D4ED452` | `36952f3f9ebb4224c997c857e6a47a31` |
| `ArtSource/HexTiles/HexTile_Grass.blend` | `0F96017FCC0D631B2357A8B536245F9A30CC601ADB6B06AFF16E1B9A77FF0827` | `9d510ecd47378d7439ce6584fd629578` |
| `Resources/Art/Battle/Models/HexTile_Dirt.fbx` | `FBD6A03B81E3093825226CA8BDE6757CDA1703AA484DF1C5732AEA9FA2682A75` | `4db62cd47af6288458d9cf1e3014b0b2` |
| `Resources/Art/Battle/Models/HexTile_Grass.fbx` | `B5B72C3537E48F1D9C6B6E48EEC759638F96461D7F3D99086020BC933B96305C` | `422a6fd99637d4243bc51fdf50416190` |

`model-validation.json` 记录 generator 1.0、Blender 5.1.2、flat-top 半径 0.94m、层高 0.32m、每模型 62 vertices/120 triangles、无 non-manifold edge、FBX binary 和 Unity 轴向。技术可复现性通过；但该组文件没有作者/项目发布许可声明，所以只可维持当前本地用途，不能因“由脚本生成”自动推导版权归属。

## 重复、孤立、命名与格式

### 重复

- 在项目原始 `card_asset`、`image`、`audio`、`fonts`、`shaders` 范围内按 SHA-256 分组，没有同字节重复组。
- Unity 41 张 PNG 与 Godot 源的重复是有意迁移副本，已逐项反查；不应由去重脚本自动删除任一侧。

### 孤立候选

- Unity `Cards/behide.png`：非 meta 文件中 GUID 引用 0、`behide` 字符串引用 0。标记“可能孤立”，不授权删除。
- Godot 静态路径扫描：13 个音频和 ARK Pixel 都有引用；`card_asset` 由 `JsonCardFactory.card_asset_dir + front_image` 动态拼接，因此七张正式卡面即使没有完整静态路径仍不是孤立。`1.png`、`2.png`、`3.png` 未出现在七份 JSON，是旧/示例孤立候选。
- `image/**` 143 个原始图片中 44 个没有直接 `res://` 引用；部分可能由 UID、动态路径或历史工具使用。完整删除判定必须由未来 AssetAudit 结合 `.import` UID、Scene dependency 和实际 Godot 运行完成。

### 命名

- `image/**` 的 143 个 PNG/JPG/ICO 中，22 个违反“小写 + 下划线/允许既有空格”的保守规则，典型包括 `BG.png`、`PlayPause.png`、`UI_Healthbar.png`、`Character_*.png`、`Highest_Building*.png`、`m_L_1.png`、`u_L_2.png` 和包含中文/`副本` 的图标名。
- `shaders/**` 有两个重复 `.gdshader.gdshader` 扩展。
- 这些名称已经参与 Godot/Unity 路径或历史证据，本阶段禁止批量重命名；未来只能通过带 GUID/引用迁移和回滚的单一 checkpoint 修正。

### 格式与尺寸

- 七张正式卡面为 1135×1590，三张数字示例卡为 400×600，均非 2 的幂。它们是固定 UI 卡面，非 POT 不是自动失败，但必须避免隐式缩放/重采样。
- `image/**` 的 142 个 PNG/JPG 中 56 个非 POT、86 个 POT。应按 UI/3D 用途分类，而不是统一强制 POT。
- 项目音频均为 OGG/Vorbis 48 kHz stereo；格式技术上可用，授权门禁未通过。
- Godot `.gdshader` 与 Unity shader 格式不兼容；Blender/FBX 模型格式和轴向已有可复现验证。

## 缺失引用

只读解析 Godot `scene/**` 与根配置中的引号包围 `res://` 路径，得到以下未解析项：

| 引用位置 | 期望路径 | 分类 |
| --- | --- | --- |
| `scene/in_scene/enermy/enemy_base.tscn` | `res://1/gd_db/enemy_base.gd` | Scene ext_resource 缺失，真实缺口候选 |
| `scene/global/global_db.gd` | `res://图片/fc155.png` | 文件内注释明确为待替换占位路径 |
| `scene/global/global_db.gd` | `res://图片/fc172.png` | 文件内注释明确为待替换占位路径 |
| `project.godot` | `res://addons/dialogic_additions` | Dialogic 配置目录缺失/可选项，需 Godot 专项确认 |
| `scene/out_scene/out_scene_map_exp.gd` | `res://event.tscn` | export 默认事件 Scene 缺失，局外地图 Gate 需确认 |
| `scene/in_scene/vfx/VFXManager.gd` | `res://vfx/slash.tscn` | 注释中的未来示例，不是执行引用 |

这些问题均是 Godot 源侧现状，本 Agent 未修改。它们不阻塞 Editor-only SceneContractValidator，但 `enemy_base.tscn` 与 `event.tscn` 应进入相应 Godot/Overworld 恢复 Gate 的已知问题核对。

## 授权缺口

| 内容 | 现有证据 | 结论 |
| --- | --- | --- |
| Silver | 作者、来源、CC BY 4.0、署名、额外预算条件完整 | `ACCEPT`，发布前复核来源当前条款 |
| card-framework | README 1.3.1 + 本地 MIT LICENSE + 作者 | Godot 源可保留；不整体移植 Unity |
| Dialogic 根 | plugin.cfg 有作者/版本；无 vendored 根 LICENSE | `REJECT` 作为 Unity 候选；示例许可不可外推 |
| 七张卡面/其他图片 | 源/副本哈希完整；无作者/来源/许可表 | `KEEP_LOCAL`/`UNVERIFIED`；公开发布未放行 |
| 13 OGG | codec/时长/hash 完整；无作者/来源/许可 | `UNVERIFIED`；Unity 不导入 |
| ARK Pixel | 文件/hash 完整；无可核验许可 | `REJECT` Unity 导入 |
| 项目 shaders | 源码存在；作者/根许可未闭合 | `DEFER`，只作语义参考 |
| Blender/FBX | 生成脚本、版本、几何、hash 完整；作者/发布许可缺失 | `KEEP_LOCAL`，公开发布未放行 |

## CardAssetAudit 后续建议

首切片已经依据真实审计选择 SceneContractValidator；CardAssetAudit 是后续独立切片，不应混入当前工具。最小规则建议如下：

1. 读取七个 `TextAsset` JSON，通过现有 `CardJsonAdapter` 或等价结构化解析取得 stable ID 和 `front_image`，禁止手写字符串分割。
2. 对每项输出 typed diagnostic：JSON 路径、stable ID、front image、Resources path、存在性、SHA-256、GUID、Importer type/spriteMode/filter/compression/max size、宽高和许可状态。
3. 必须识别重复 stable ID、重复 front filename、路径穿越/子目录、缺图、源/副本 hash 漂移、重复 GUID、非确定顺序和未登记许可。
4. `lighting` Default Texture + runtime Sprite fallback 是当前已知允许例外；工具应报告 `Warning/AllowedException`，不能自动改成 Sprite。`behide` Default Texture 与零引用应单独报告 possible orphan。
5. 结构化 JSON/CSV 输出必须稳定排序；菜单/EditorWindow 只读运行，默认不调用 `AssetImporter.SaveAndReimport`、`AssetDatabase.CopyAsset`、`File.Copy` 或 Scene authoring。
6. EditMode 测试至少覆盖：七卡生产资产通过、缺图 fixture、hash drift fixture、重复 stable ID、`lighting` 允许例外、`behide` orphan warning、运行前后源/副本/`.meta`/Scene/Prefab hash 不变。
7. 只有用户/主智能体明确选择修复时，才在独立 checkpoint 修改单个 Importer；修复前后需卡牌 PlayMode、三视口、build/Player 和完整移除回滚验证。

## 停止条件与交回

- 没有下载、导入、执行、生成或网络资产进入工程。
- 没有调用 Blender/MCP，没有修改 ArtSource、Godot 原素材、Unity Assets/Packages/ProjectSettings、Scene/Prefab、asmdef、共享维护/进度文档或 Git。
- 所有来源/许可缺口均明确标为 `UNVERIFIED`/`REJECT`，没有把仓库存在性当作发布许可。
- 本 Agent 只写本报告与候选账本；两条路径完成后交回主智能体。
