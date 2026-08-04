# Wave 04 资产来源与授权账本

> 状态：Gate 0 冻结；仅本地迁移验证
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：Godot `audio/`、`scene/global/sound_manager.gd`、`asset-and-plugin-candidate-ledger.md`、ffprobe 与 SHA-256

## 判定规则

`ACCEPT` 只表示已核实的当前用途；未知来源的原项目素材使用 `LOCAL_DEV_ONLY`，Dialogic 示例
默认 `HOLD`。本阶段不声称公开发布许可完成，不修改源 OGG、字体、卡面或 shader。

## 项目音频清单

所有以下源文件保持 Godot 原字节，Unity 导入目标拟为 `Assets/_Project/Audio/Wave04/`；
具体 Import Settings 和 Unity 导入后 hash 在 Gate A 报告补录。

| stable_id | source_path | author/license/commercial/redistribution | bytes | sha256 | duration/s | channels/rate | status |
| --- | --- | --- | ---: | --- | ---: | --- | --- |
| bgm.main_menu | `audio/bgm/main_menu.ogg` | 项目来源未核实；unknown/unknown | 2225619 | `669B903951227AB669FA0D852C6B8C78FEE45A662F88D5784B7B320C52E3DF7D` | 69.035708 | 2 / 48000 | LOCAL_DEV_ONLY |
| bgm.battle | `audio/bgm/battle.ogg` | 项目来源未核实；unknown/unknown | 3363588 | `ED0AB1E6B9484C2E00E9DB330FDA31D072A3B5ACFC7025E628635ED33856A338` | 83.076917 | 2 / 48000 | LOCAL_DEV_ONLY |
| bgm.battle_boss | `audio/bgm/battle_boss.ogg` | 项目来源未核实；unknown/unknown | 3790498 | `233601FF9CEE317ACFDA98E8D55001A54908769E25F1B86AE932009F5099748F` | 93.599750 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.card_shuffle | `audio/sfx/card_shuffle.ogg` | 项目来源未核实；unknown/unknown | 105327 | `F5E1E85588D6C0B758DA77C50888258EF21E82CA2C793DD4533CC2FC5095D9C6` | 2.486604 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.choose_role | `audio/sfx/choose_role.ogg` | 项目来源未核实；unknown/unknown | 126832 | `40A8E1EC54B56C96A5C957259502F0A98A35D44831C6C70B08F1C1D03BCC8CFD` | 3.044646 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.clock_tick | `audio/sfx/clock_tick.ogg` | 项目来源未核实；unknown/unknown | 208940 | `AFC48B2061C15480B822108F979D9FDE262DC7AB8A5475756F9E42AC39879D8A` | 5.254458 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.confirm_timeline | `audio/sfx/confirm_timeline.ogg` | 项目来源未核实；unknown/unknown | 22123 | `CAB06988E0906D0A4D9CAB63E3A5E15CF6F6CC47664F42A8D355FBFBEA5AA25B` | 0.473208 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.draw_card | `audio/sfx/draw_card.ogg` | 项目来源未核实；unknown/unknown | 33184 | `58D76D7B00A360B5461403C93362BBAF053608A1057240BAB7977093DE908EE2` | 0.718750 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.game_over | `audio/sfx/game_over.ogg` | 项目来源未核实；unknown/unknown | 650889 | `2FBD2C0C340D419C37CEADC809579FD94D0B16B13D11294901043914FE12516A` | 16.000000 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.tile_damage | `audio/sfx/tile_damage.ogg` | 项目来源未核实；unknown/unknown | 70959 | `6AC7A0156A922F0D5BA302DAF659F6877B1B789F41C48B8415149317ABCC4920` | 1.808042 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.victory_long | `audio/sfx/victory_long.ogg` | 项目来源未核实；unknown/unknown | 914123 | `B8B4F4CE199DE8530B98B0E73B2492848E3CF2A0E3F2683E6C2F83482078AC9A` | 23.133937 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.victory_short | `audio/sfx/victory_short.ogg` | 项目来源未核实；unknown/unknown | 279638 | `23CBF268E037A64A81D8BC16AA0BF8D643A3CCDF271CE74A2D41CFB355C09505` | 6.803562 | 2 / 48000 | LOCAL_DEV_ONLY |
| sfx.walk_dim | `audio/sfx/walk_dim.ogg` | 项目来源未核实；unknown/unknown | 139954 | `3D623CE0C1369C699865F815B243129570D0F58C27CF6981AEDF6F90127F596E` | 3.556250 | 2 / 48000 | LOCAL_DEV_ONLY |

Godot 的 `SoundManager` 映射、双 BGM source、6 个 one-shot source、1 个 looping source、
`Music`/`SFX` bus 和 `Master`/`Music`/`SFX` 音量语义是 Wave 04 Audio contract 的参考实现。

## 明确 HOLD/ACCEPT 项

| stable_id | 来源 | 授权状态 | 处理 |
| --- | --- | --- | --- |
| dialogic.example.typing1..5 | `addons/dialogic/Example Assets/sound-effects/*.wav` | 示例素材许可证与项目发布授权未闭合 | HOLD；不复制、不进入正式 catalog |
| font.silver | Unity 已继承 Silver 资源 | CC BY 4.0，归属与 USD 100,000 upstream budget/earnings 条件见现有 ledger | ACCEPT（当前本地/已登记用途）；保持 Silver Font + matching renderer material |
| Godot 卡面/海洋 tile/背景图 | 原项目 `图片/`、`scene/` 与已接入 Unity 副本 | 项目来源与公开发布授权未核实 | LOCAL_DEV_ONLY；不修图、不宣称发布 |
| Godot/Dialogic shader | `scene/tutorial/tutorial_mask_layer.gdshader` 与其他 canvas shader | 来源/发布范围未闭合 | LOCAL_DEV_ONLY；只做行为参考，正式 Unity shader 需独立证据 |

任何外部候选必须先写入 `06-maintenance/plugin-and-asset-tooling-guide.md` 规定的候选记录，
通过版本、许可证、依赖、hash、视觉和可移除验证后才可改正式工程；本 Wave 不需要联网候选。
