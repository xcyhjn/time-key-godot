class_name TimelineVisualConfig
extends Resource

## TimelineVisualConfig 只保存时间轴 UI 的纯视觉静态参数。
## 它不创建行动块，不读写 TimelineManager，也不决定拖拽放置或敌人意图规则。


@export_group("网格表现")
@export var grid_cell_default_color: Color = Color(0.4, 0.4, 0.4, 0.6)
@export var grid_cell_hover_color: Color = Color(0.5, 0.5, 0.5, 0.75)

@export_group("展开表现")
@export var expanded_scale: Vector2 = Vector2(1.5, 1.5)
@export var anim_duration: float = 0.3

@export_group("敌人意图表现")
@export var enemy_intent_timeline_shader: Shader
@export var enemy_intent_preview_scale: Vector2 = Vector2(1.08, 1.08)
@export var enemy_intent_removal_scale: Vector2 = Vector2(0.92, 0.92)
@export var enemy_intent_removal_duration: float = 0.28
@export var enemy_intent_preview_z_index: int = 50
@export var enemy_intent_pulse_speed: float = 3.0
@export var enemy_intent_pulse_min_alpha: float = 0.15
@export var enemy_intent_pulse_max_alpha: float = 0.75
@export var enemy_intent_stripe_color: Color = Color(1.0, 0.85, 0.12, 1.0)
@export var enemy_intent_stripe_speed: float = 3.8
@export var enemy_intent_stripe_density: float = 12.0
@export var enemy_intent_stripe_width: float = 0.12
@export_range(0.0, 1.0, 0.01) var enemy_intent_stripe_strength: float = 0.75

@export_group("移除动画表现")
@export var action_removal_drop_distance: float = 14.0
@export var action_removal_fade_color: Color = Color(0.28, 0.28, 0.28, 0.0)

@export_group("行动整体轮廓")
@export var action_group_visual_enabled: bool = true
@export var action_group_outline_width: float = 3.0
@export var action_group_outline_color: Color = Color(0.03, 0.03, 0.03, 0.95)
@export var action_group_internal_seam_width: float = 1.0
@export var action_group_internal_seam_color: Color = Color(1.0, 1.0, 1.0, 0.14)

@export_group("遮罩表现")
@export var mask_color: Color = Color(0.75, 0.75, 0.75, 0.6)
@export var mask_layer: int = 100
