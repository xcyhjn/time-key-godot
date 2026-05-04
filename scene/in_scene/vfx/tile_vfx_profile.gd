# 功能: 地块动画特效的通用表现配置资源。
# 核心逻辑: 被 TileSpriteVFX 读取，用一份 .tres 统一控制窗口大小、偏移、缩放、层级、播放速度和自动清理策略。
class_name TileVFXProfile
extends Resource

@export_group("播放设置")
@export var auto_free_on_finish: bool = true
## 运行时强制关闭 SpriteFrames 循环，确保一次性动画能触发 animation_finished。
@export var force_non_loop: bool = true
@export var playback_speed_scale: float = 1.0

@export_group("地块窗口")
## 每个地块用于播放动画的固定窗口大小，动画帧会按这个窗口压缩。
@export var window_size: Vector2 = Vector2(128.0, 128.0)
## 窗口相对地块碰撞中心的位置偏移，y 越小越靠上。
@export var window_offset: Vector2 = Vector2(0.0, -20.0)
## 开启时强制拉伸到窗口大小；关闭时等比缩放进窗口。
@export var stretch_to_window: bool = true
## 特效层级，需高于地块和建筑。
@export var vfx_z_index: int = 3500
