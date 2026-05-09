# 音频系统文档 (AudioSys)

## 一、架构概览

```
音频资源文件 (audio/bgm/, audio/sfx/)
        │
        ▼
SoundManager (autoload 单例)
  ├── BGM 播放器 ×2 (Music 总线, 交叉淡入淡出)
  │     └── play_bgm("main_menu"|"in_game")
  ├── SFX 播放器 ×6 (SFX 总线, 池化复用)
  │     └── play_sfx("clock_tick"|"draw_card"|...)
  └── 音量控制 → AudioServer 总线音量
        │
        ▼
    音频总线布局 (default_bus_layout.tres)
      ├── Master (索引 0)
      ├── Music  (索引 1)
      └── SFX    (索引 2)
```

## 二、涉及文件

| 文件 | 用途 |
|------|------|
| `scene/global/sound_manager.gd` | SoundManager autoload，音频系统核心 |
| `scene/pause_menu/Volumn/slider_master_volume.gd` | 主音量滑块，已连线 |
| `scene/pause_menu/Volumn/slider_music_volume.gd` | 音乐音量滑块，已连线 |
| `scene/pause_menu/Volumn/slider_sfx_volume.gd` | 音效音量滑块，已连线 |
| `audio/bgm/` | BGM 文件存放目录 |
| `audio/sfx/` | SFX 文件存放目录 |
| `project.godot` | 已注册 SoundManager 为 autoload |

## 三、编辑器操作（首次配置）

### 3.1 创建音频总线布局

在 Godot 编辑器中：

1. 打开 **音频总线 (Audio)** 面板（底部面板 → Audio 标签）
2. 点击 **添加总线 (Add Bus)** 按钮，添加两条总线：
   - 命名为 `Music`（索引 1）
   - 命名为 `SFX`（索引 2）
   - Master 是默认的总线（索引 0），无需创建
3. 总线结构应为：
   ```
   Master
   ├── Music
   └── SFX
   ```
4. 此时编辑器会生成 `default_bus_layout.tres`

### 3.2 放入音频文件

按以下路径放置音频文件（目前支持 .ogg、.mp3、.wav）：

```
audio/bgm/
  main_menu.ogg    # 主页面/局外地图 BGM
  battle.ogg       # 局内普通/精英战斗 BGM
  battle_boss.ogg  # 局内 BOSS 战斗 BGM

audio/sfx/
  clock_tick.ogg       # 进入场景时钟转动音效
  choose_layer.ogg     # 选角层厚重音效（塔风格）
  walk_dim.ogg         # 进入局内走路声
  draw_card.ogg        # 抽牌刷刷声
  shuffle.ogg          # 洗牌声
  confirm_timeline.ogg # 时间轴确定确认音效
  tile_effect.ogg      # 地块效果/雷击通用音效
  combat_win.ogg       # 战斗胜利小旋律
  game_win.ogg         # 整局胜利音效
  game_over.ogg        # 整局失败音效
```

音频文件就位后，SoundManager 会自动加载，无需修改代码。

## 四、音效-信号-时机对应表

按时间顺序排列：

| # | 时机 | 触发信号 | 音效 Key | 说明 |
|---|------|---------|---------|------|
| 1 | 主菜单点击"新游戏" | 显式调用 `SoundManager.play_sfx("clock_tick")` | `clock_tick` | `main_menu.gd:215` |
| 2 | 进入选角层 | `Global.choose(type)` | `choose_layer` | `out_scene_map_exp.gd:282` |
| 3 | 进入局内（dim 遮罩期间） | `Global.dim_in` | `walk_dim` | `out_scene_map_exp.gd` `_enter_room_logic()` 中 emit |
| 4 | 抽牌（按钮/自动） | `Signal_Bus.card_drawn` | `draw_card` | `in_scene.gd` `attempt_draw_cards()` 中 emit |
| 5 | 洗牌（牌库空时重洗） | `Signal_Bus.deck_shuffled` | `shuffle` | `in_scene.gd` `shuffle_card()` 中 emit |
| 6 | 时间轴放置/槽变蓝 | `Signal_Bus.timeline_action_added` (PLAYER 类型) | `confirm_timeline` | `TimelineManager.gd` `place_action()` 中 emit |
| 7 | 地块效果（选中/震动/雷击等） | `Signal_Bus.tile_selected` | `tile_damage` | 地块交互统一音效 |
| 8 | 战斗胜利 | `Signal_Bus.combat_victory_triggered` | `combat_win` (循环播放) | 单场战斗结算 |
| 9 | 整局胜利 | `Signal_Bus.victory_triggered` | `game_win` (循环播放) | game_win_screen.gd |
| 10 | 整局失败 | `Signal_Bus.defeat_triggered` | `game_over` (循环播放) | game_over.gd |

### 回局外时淡出

| 场景 | 触发位置 | 调用 |
|------|---------|------|
| 局内结算返回局外 | `in_scene.gd` `_return_to_out_scene()` | `SoundManager.stop_looping_sfx()` |
| 游戏结束返回主菜单 | `game_over.gd` `_on_menu_btn_button_down()` | `SoundManager.stop_looping_sfx()` |
| 整局胜利返回主菜单 | `game_win_screen.gd` `_on_btn_main_menu_button_down()` | `SoundManager.stop_looping_sfx()` |

## 五、BGM 切换时机

| 场景 | BGM Key | 调用方式 |
|------|---------|---------|
| 主菜单 | `main_menu` | `main_menu.gd` `_ready()` 中自动调用 |
| 局外地图 | `main_menu`（复用） | 从局内返回局外时自动调用 `SoundManager.play_bgm_main_menu()` |
| 局内普通/精英 | `in_game` | `in_scene.gd` 首回合启动时根据 battle tag 自动选择 |
| 局内 BOSS | `battle_boss` | `boss_stage` tag 进入时自动调用 |

BGM 切换逻辑：
- 局外→局内：`Global.dim_in` 触发时局外 BGM 渐弱，局内首回合切入对应战斗 BGM
- 局内→局外：返回局外地图时停止战斗 BGM，切换回 `main_menu`
- 战斗胜利/失败：立即停止战斗 BGM

BGM 通过 `finished` 信号自动循环，无需在音频文件中设置 loop 属性。

## 六、SoundManager API 速查

```gdscript
# BGM 控制
SoundManager.play_bgm("main_menu")       # 按 key 播放 BGM（交叉淡入淡出）
SoundManager.play_bgm("in_game")         # 按 key 播放 BGM
SoundManager.play_bgm_main_menu()        # 快捷方法：播放主菜单 BGM
SoundManager.play_bgm_in_game()          # 快捷方法：播放局内 BGM
SoundManager.stop_bgm()                  # 停止 BGM（淡出）

# SFX 控制
SoundManager.play_sfx("clock_tick")              # 按 key 播放一次性音效
SoundManager.play_sfx("draw_card", true)         # 第二个参数为 true 时随机变调 (0.9~1.1)
SoundManager.play_looping_sfx("combat_win")      # 播放循环音效（胜利/失败等，可淡出）
SoundManager.stop_looping_sfx()                  # 淡出并停止当前循环音效（默认 0.5 秒）
SoundManager.stop_looping_sfx(1.0)               # 指定淡出时长

# 音量控制 (v: 0.0 ~ 1.0)
SoundManager.set_volume(SoundManager.Bus.MASTER, v)
SoundManager.set_volume(SoundManager.Bus.MUSIC, v)
SoundManager.set_volume(SoundManager.Bus.SFX, v)
```

## 七、扩展指南

### 添加新音效

1. 将音频文件放入 `audio/sfx/` 目录
2. 在 `sound_manager.gd` 的 `SFX_PATHS` 字典中添加一行：
   ```gdscript
   new_sfx = "res://audio/sfx/new_sfx.ogg",
   ```
3. 连接对应信号，在 `_connect_signals()` 中添加：
   ```gdscript
   SignalBus.some_signal.connect(_on_some_event)
   ```
4. 添加回调函数：
   ```gdscript
   func _on_some_event() -> void:
       play_sfx("new_sfx")
   ```

### 添加新 BGM

同上，但使用 `BGM_PATHS` 字典和 `play_bgm("key")` 方法。

### 调整音效播放器池大小

修改 `sfx_audio_player_count`（默认 6）。更大的池允许更多音效同时播放而不被截断。

### 调整 BGM 淡入淡出时长

修改 `music_fade_duration`（默认 1.0 秒）。

## 八、注意事项

- 如果音频总线 `Music` 或 `SFX` 未创建，音频仍会通过 Master 总线播放，但独立音量控制会失效并输出 warning。
- 音频文件未放入对应路径时，SoundManager 会在运行时输出 warning，不会崩溃。
- Dialogic 插件自带音频通道，目前独立于 SoundManager 运行，如需统一管理可在后续整合。
- `dim_in` 信号由 `Global` autoload 发出，需要确认该信号在 dim 过渡时确实被 emit。
