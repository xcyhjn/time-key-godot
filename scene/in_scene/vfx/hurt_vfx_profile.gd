# 功能: 通用受伤反馈动画配置资源。
# 核心逻辑: VFXManager.play_hurt_vfx() 读取该配置，临时让目标图标变暗变红并左右抖动，结束后恢复原始颜色和位置。
class_name HurtVFXProfile
extends Resource

@export_group("颜色反馈")
## 受击瞬间叠加到目标图标上的颜色。alpha 越高，原图越偏向该颜色。
@export var hurt_color: Color = Color(0.95, 0.08, 0.05, 1.0)
## 受击颜色与原颜色的混合强度。
@export_range(0.0, 1.0, 0.01) var hurt_color_blend: float = 0.75
## 受击时整体压暗倍率，越小越暗。
@export_range(0.0, 1.0, 0.01) var darken_factor: float = 0.55

@export_group("抖动反馈")
## 左右抖动幅度，单位为像素。
@export var shake_distance: float = 10.0
## 完整左右抖动次数。
@export_range(1, 12, 1) var shake_count: int = 3
## 每次半段抖动耗时。
@export var shake_step_duration: float = 0.035
## 颜色恢复耗时。
@export var recover_duration: float = 0.12

@export_group("时间缩放")
## 开启后乘以 Global.get_anim_speed()，跟随项目整体动画速度。
@export var use_global_anim_speed: bool = true
