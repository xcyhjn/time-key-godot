# 音频系统说明

这份文档说明项目当前的音频结构、资源路径、信号触发时机和 `SoundManager` 常用 API。新增音效或 BGM 前，先确认这里的 key 和文件路径。

## 系统结构

```text
音频资源文件
├─ audio/bgm/
└─ audio/sfx/

SoundManager（autoload 单例）
├─ BGM 播放器 x2
│  ├─ 使用 Music 总线
│  └─ 支持交叉淡入淡出
├─ SFX 播放器池 x6
│  ├─ 使用 SFX 总线
│  └─ 支持复用播放
└─ 音量控制
   └─ 通过 AudioServer 调整总线音量

音频总线布局
├─ Master
├─ Music
└─ SFX
```

## 涉及文件

| 文件 | 用途 |
| --- | --- |
| `scene/global/sound_manager.gd` | `SoundManager` autoload，音频系统核心。 |
| `scene/pause_menu/Volumn/slider_master_volume.gd` | 主音量滑块。 |
| `scene/pause_menu/Volumn/slider_music_volume.gd` | 音乐音量滑块。 |
| `scene/pause_menu/Volumn/slider_sfx_volume.gd` | 音效音量滑块。 |
| `audio/bgm/` | BGM 文件目录。 |
| `audio/sfx/` | 音效文件目录。 |
| `project.godot` | 已注册 `SoundManager` autoload。 |

## 初次配置音频总线

在 Godot 编辑器中：

1. 打开底部 **Audio** 面板。
2. 点击 **Add Bus**，新增两条总线：
   - `Music`
   - `SFX`
3. 保留默认 `Master` 总线。
4. 确认结构为：

```text
Master
├─ Music
└─ SFX
```

Godot 会把总线布局保存到 `default_bus_layout.tres`。

## 音频资源路径

BGM 文件放在：

```text
audio/bgm/
  main_menu.ogg
  battle.ogg
  battle_boss.ogg
```

音效文件放在：

```text
audio/sfx/
  clock_tick.ogg
  choose_layer.ogg
  walk_dim.ogg
  draw_card.ogg
  shuffle.ogg
  confirm_timeline.ogg
  tile_effect.ogg
  combat_win.ogg
  game_win.ogg
  game_over.ogg
```

资源就位后，`SoundManager` 会按配置路径自动加载。缺失文件会输出 warning，但不会让游戏崩溃。

## 音效触发时机

| 序号 | 时机 | 触发方式 | 音效 key | 说明 |
| --- | --- | --- | --- | --- |
| 1 | 主菜单点击新游戏 | 显式调用 `SoundManager.play_sfx("clock_tick")` | `clock_tick` | 来自 `main_menu.gd`。 |
| 2 | 进入选角层 | `Global.choose(type)` | `choose_layer` | 来自 `out_scene_map_exp.gd`。 |
| 3 | 进入局内 dim 遮罩期间 | `Global.dim_in` | `walk_dim` | 由局外进入房间流程触发。 |
| 4 | 抽牌 | `Signal_Bus.card_drawn` | `draw_card` | 来自 `in_scene.gd::attempt_draw_cards()`。 |
| 5 | 牌库为空后洗牌 | `Signal_Bus.deck_shuffled` | `shuffle` | 来自 `in_scene.gd::shuffle_card()`。 |
| 6 | 放置玩家时间轴行动 | `Signal_Bus.timeline_action_added` | `confirm_timeline` | 来自 `TimelineManager.gd::place_action()`。 |
| 7 | 地块效果或交互 | `Signal_Bus.tile_selected` | `tile_damage` | 统一地块交互音效。 |
| 8 | 单场战斗胜利 | `Signal_Bus.combat_victory_triggered` | `combat_win` | 循环播放，结算离开时停止。 |
| 9 | 整局胜利 | `Signal_Bus.victory_triggered` | `game_win` | 来自胜利界面。 |
| 10 | 整局失败 | `Signal_Bus.defeat_triggered` | `game_over` | 来自失败界面。 |

## 返回场景时停止循环音效

| 场景 | 触发位置 | 调用 |
| --- | --- | --- |
| 局内结算返回局外 | `in_scene.gd::_return_to_out_scene()` | `SoundManager.stop_looping_sfx()` |
| 游戏结束返回主菜单 | `game_over.gd::_on_menu_btn_button_down()` | `SoundManager.stop_looping_sfx()` |
| 整局胜利返回主菜单 | `game_win_screen.gd::_on_btn_main_menu_button_down()` | `SoundManager.stop_looping_sfx()` |

## BGM 切换时机

| 场景 | BGM key | 调用方式 |
| --- | --- | --- |
| 主菜单 | `main_menu` | `main_menu.gd::_ready()` 自动调用。 |
| 局外地图 | `main_menu` | 从局内返回局外时自动调用 `SoundManager.play_bgm_main_menu()`。 |
| 普通或精英战斗 | `in_game` | `in_scene.gd` 首回合启动时根据 battle tag 自动选择。 |
| Boss 战斗 | `battle_boss` | 带 `boss_stage` tag 进入时自动调用。 |

BGM 切换规则：

- 局外进入局内时，`Global.dim_in` 触发后局外 BGM 渐弱，局内首回合切入对应战斗 BGM。
- 局内回到局外时，停止战斗 BGM 并切回 `main_menu`。
- 战斗胜利或失败时，立即停止战斗 BGM。

BGM 通过 `finished` 信号自动循环，不需要在音频文件中单独设置 loop 属性。

## SoundManager API 速查

```gdscript
# BGM 控制
SoundManager.play_bgm("main_menu")
SoundManager.play_bgm("in_game")
SoundManager.play_bgm_main_menu()
SoundManager.play_bgm_in_game()
SoundManager.stop_bgm()

# SFX 控制
SoundManager.play_sfx("clock_tick")
SoundManager.play_sfx("draw_card", true)
SoundManager.play_looping_sfx("combat_win")
SoundManager.stop_looping_sfx()
SoundManager.stop_looping_sfx(1.0)

# 音量控制，v 范围为 0.0 到 1.0
SoundManager.set_volume(SoundManager.Bus.MASTER, v)
SoundManager.set_volume(SoundManager.Bus.MUSIC, v)
SoundManager.set_volume(SoundManager.Bus.SFX, v)
```

## 添加新音效

1. 把音频文件放进 `audio/sfx/`。
2. 在 `sound_manager.gd` 的 `SFX_PATHS` 字典中新增一行：

```gdscript
new_sfx = "res://audio/sfx/new_sfx.ogg",
```

3. 在 `_connect_signals()` 中连接对应信号。
4. 在回调里播放音效：

```gdscript
func _on_some_event() -> void:
    play_sfx("new_sfx")
```

## 添加新 BGM

流程和新增音效相同，但要写入 `BGM_PATHS`，并通过 `play_bgm("key")` 播放。

## 调整播放器数量和淡入淡出

- 想让更多音效同时播放，调整 `sfx_audio_player_count`，当前默认值是 6。
- 想调整 BGM 淡入淡出时长，调整 `music_fade_duration`，当前默认值是 1.0 秒。

## 注意事项

- 如果没有创建 `Music` 或 `SFX` 总线，音频仍会通过 `Master` 播放，但独立音量控制会失效，并输出 warning。
- 音频文件缺失时，`SoundManager` 会输出 warning，不会崩溃。
- Dialogic 插件自带音频通道，目前独立于 `SoundManager`。如果后续要统一管理，可以另开一轮整合。
- `dim_in` 信号由 `Global` autoload 发出，调试场景切换音频时要确认 dim 过渡确实 emit 了该信号。
