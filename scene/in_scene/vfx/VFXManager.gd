# ==========================================
# 脚本名称: VFXManager.gd
# 功能概述: 全局特效与动画表现管理器，实现逻辑计算与视觉表现的彻底解耦。
# ------------------------------------------
# 【数据接收】
# - 来源: EffectCommand 的具体子类（如 DamageCommand 等业务逻辑脚本）。
# - 内容: 特效名称 (vfx_name)、目标位置 (target_pos) 以及 SceneTree。
# ------------------------------------------
# 【数据处理】
# - 逻辑: 根据传入的名称实例化对应的特效预制体，放置在目标位置，并结合全局游戏速度 (Global.speed_index) 管理动画的播放时长。
# ------------------------------------------
# 【数据发送】
# - 目标: 调用该方法的 EffectCommand 协程。
# - 时机: 在特效/动画完全播放结束时，解除 await 阻塞，通知战斗逻辑继续往下执行。
# ==========================================
class_name VFXManager
extends Node

const DEFAULT_TILE_VFX_REGISTRY: TileVFXRegistry = preload("res://scene/in_scene/vfx/default_tile_vfx_registry.tres")
const DEFAULT_HURT_VFX_PROFILE: HurtVFXProfile = preload("res://scene/in_scene/vfx/default_hurt_vfx_profile.tres")
const HURT_VFX_TWEEN_META: StringName = &"_hurt_vfx_tween"
const HURT_VFX_POSITION_META: StringName = &"_hurt_vfx_original_position"
const HURT_VFX_MODULATE_META: StringName = &"_hurt_vfx_original_modulate"
const PIXEL_DISSOLVE_TWEEN_META: StringName = &"_pixel_dissolve_vfx_tween"

## 播放特效并阻塞，直到特效播放完成
static func play_vfx(vfx_name: String, target_pos: Vector2, tree: SceneTree) -> void:
	# 假设你以后会有个特效预制体字典
	# var vfx_scene = preload("res://vfx/slash.tscn")
	
	
	# 这里模拟一个通用的受击特效表现（例如目标闪烁或震动）
	# 实际开发中，你可以实例化特效节点，并 await 特效的 finished 信号
	var s = Global.get_anim_speed() if Global.has_method("get_anim_speed") else 1.0
	
	# 模拟动画时间（解耦的好处就是，以后你改这里的动画，不需要去改卡牌逻辑代码）
	await tree.create_timer(0.3 * s).timeout

# ==========================================
# ★ 地形专用动画管理
# ==========================================

## 播放通用受伤反馈：目标图标变暗变红并左右抖动，播放完成后恢复原始状态。
## 说明:
## - 优先使用 landform.tex，也兼容直接传入 Sprite2D/Node2D。
## - 不创建持久节点；动画结束时会恢复 modulate 和 position，避免残留。
static func play_hurt_vfx(target: Node, tree: SceneTree, profile: HurtVFXProfile = null) -> Tween:
	var sprite: Node2D = _resolve_hurt_vfx_target(target)
	if not is_instance_valid(sprite) or not is_instance_valid(tree):
		return null

	var active_profile: HurtVFXProfile = profile if profile != null else DEFAULT_HURT_VFX_PROFILE
	var speed_scale: float = _get_vfx_time_scale(active_profile.use_global_anim_speed)
	_clear_hurt_vfx_target(sprite)

	var original_position: Vector2 = sprite.position
	var original_modulate: Color = sprite.modulate
	var hurt_modulate: Color = _build_hurt_modulate(original_modulate, active_profile)

	sprite.set_meta(HURT_VFX_POSITION_META, original_position)
	sprite.set_meta(HURT_VFX_MODULATE_META, original_modulate)

	var tw: Tween = tree.create_tween().set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	sprite.set_meta(HURT_VFX_TWEEN_META, tw)
	tw.tween_property(sprite, "modulate", hurt_modulate, active_profile.shake_step_duration * speed_scale)
	for i in range(active_profile.shake_count):
		var direction: float = 1.0 if i % 2 == 0 else -1.0
		tw.tween_property(sprite, "position:x", original_position.x + active_profile.shake_distance * direction, active_profile.shake_step_duration * speed_scale)
		tw.tween_property(sprite, "position:x", original_position.x - active_profile.shake_distance * direction, active_profile.shake_step_duration * speed_scale)

	tw.tween_property(sprite, "position", original_position, active_profile.shake_step_duration * speed_scale)
	tw.parallel().tween_property(sprite, "modulate", original_modulate, active_profile.recover_duration * speed_scale)
	tw.finished.connect(_finish_hurt_vfx_target.bind(sprite), CONNECT_ONE_SHOT)
	return tw


static func _resolve_hurt_vfx_target(target: Node) -> Node2D:
	if not is_instance_valid(target):
		return null
	if target is Sprite2D:
		return target as Sprite2D

	var texture_node: Variant = target.get("tex")
	if is_instance_valid(texture_node) and texture_node is Node2D:
		return texture_node as Node2D

	if target is Node2D:
		return target as Node2D
	return null


static func _build_hurt_modulate(original_modulate: Color, profile: HurtVFXProfile) -> Color:
	var darkened: Color = Color(
		original_modulate.r * profile.darken_factor,
		original_modulate.g * profile.darken_factor,
		original_modulate.b * profile.darken_factor,
		original_modulate.a
	)
	var hurt_color: Color = profile.hurt_color
	hurt_color.a = original_modulate.a
	return darkened.lerp(hurt_color, profile.hurt_color_blend)


static func _restore_hurt_vfx_target(target: Node2D, original_position: Vector2, original_modulate: Color) -> void:
	if not is_instance_valid(target):
		return
	target.position = original_position
	target.modulate = original_modulate


static func _finish_hurt_vfx_target(target: Node2D) -> void:
	if not is_instance_valid(target):
		return

	var original_position: Vector2 = target.get_meta(HURT_VFX_POSITION_META, target.position)
	var original_modulate: Color = target.get_meta(HURT_VFX_MODULATE_META, target.modulate)
	_restore_hurt_vfx_target(target, original_position, original_modulate)
	target.remove_meta(HURT_VFX_TWEEN_META)
	target.remove_meta(HURT_VFX_POSITION_META)
	target.remove_meta(HURT_VFX_MODULATE_META)


static func _clear_hurt_vfx_target(target: Node2D) -> void:
	if not is_instance_valid(target):
		return

	var old_tween: Variant = target.get_meta(HURT_VFX_TWEEN_META, null)
	var original_position: Vector2 = target.get_meta(HURT_VFX_POSITION_META, target.position)
	var original_modulate: Color = target.get_meta(HURT_VFX_MODULATE_META, target.modulate)
	if old_tween is Tween and is_instance_valid(old_tween):
		old_tween.kill()

	_restore_hurt_vfx_target(target, original_position, original_modulate)
	if target.has_meta(HURT_VFX_TWEEN_META):
		target.remove_meta(HURT_VFX_TWEEN_META)
	if target.has_meta(HURT_VFX_POSITION_META):
		target.remove_meta(HURT_VFX_POSITION_META)
	if target.has_meta(HURT_VFX_MODULATE_META):
		target.remove_meta(HURT_VFX_MODULATE_META)


static func _get_vfx_time_scale(use_global_anim_speed: bool) -> float:
	if use_global_anim_speed and Global and Global.has_method("get_anim_speed"):
		return maxf(float(Global.get_anim_speed()), 0.01)
	return 1.0


## 播放通用像素粒子入场。
## 核心逻辑: 复用 hex.gdshader 的 dissolve_blend，按 1 -> 0 倒放消融效果；目标可以是 landform、Sprite2D 或任意含 Sprite2D 子节点的 Node。
static func play_pixel_spawn_vfx(target: Node, tree: SceneTree, duration: float = 0.45, use_global_anim_speed: bool = true) -> Tween:
	return _play_pixel_dissolve_vfx(target, tree, 1.0, 0.0, duration, use_global_anim_speed)


## 播放通用像素粒子退场。
## 核心逻辑: 复用 hex.gdshader 的 dissolve_blend，按 0 -> 1 正向消融；调用方可 await 返回 Tween 的 finished 后再清理节点。
static func play_pixel_despawn_vfx(target: Node, tree: SceneTree, duration: float = 0.45, use_global_anim_speed: bool = true) -> Tween:
	return _play_pixel_dissolve_vfx(target, tree, 0.0, 1.0, duration, use_global_anim_speed)


static func _play_pixel_dissolve_vfx(target: Node, tree: SceneTree, from_value: float, to_value: float, duration: float, use_global_anim_speed: bool) -> Tween:
	if not is_instance_valid(target) or not is_instance_valid(tree):
		return null

	var items: Array[CanvasItem] = _collect_pixel_vfx_items(target)
	if items.is_empty():
		return null

	var speed_scale: float = _get_vfx_time_scale(use_global_anim_speed)
	var safe_duration: float = maxf(duration * speed_scale, 0.01)
	var tw: Tween = tree.create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)

	for item in items:
		_clear_pixel_dissolve_item(item)
		item.set_meta(PIXEL_DISSOLVE_TWEEN_META, tw)
		if item.material != null:
			item.set_instance_shader_parameter("dissolve_blend", from_value)
			tw.tween_method(
				_set_pixel_dissolve_blend.bind(item),
				from_value,
				to_value,
				safe_duration
			)
		else:
			var from_alpha: float = 0.0 if from_value >= 1.0 else item.modulate.a
			var to_alpha: float = 0.0 if to_value >= 1.0 else 1.0
			item.modulate.a = from_alpha
			tw.tween_property(item, "modulate:a", to_alpha, safe_duration)

	tw.finished.connect(_finish_pixel_dissolve_items.bind(items), CONNECT_ONE_SHOT)
	return tw


static func _collect_pixel_vfx_items(target: Node) -> Array[CanvasItem]:
	var items: Array[CanvasItem] = []

	var texture_node: Variant = target.get("tex") if is_instance_valid(target) else null
	if is_instance_valid(texture_node) and texture_node is CanvasItem:
		items.append(texture_node as CanvasItem)

	if target is Sprite2D or target is TextureRect:
		var direct_item := target as CanvasItem
		if not items.has(direct_item):
			items.append(direct_item)

	_collect_child_pixel_vfx_items(target, items)
	return items


static func _collect_child_pixel_vfx_items(node: Node, items: Array[CanvasItem]) -> void:
	for child in node.get_children():
		if child is Sprite2D or child is TextureRect:
			var item := child as CanvasItem
			if not items.has(item):
				items.append(item)
		_collect_child_pixel_vfx_items(child, items)


static func _clear_pixel_dissolve_item(item: CanvasItem) -> void:
	if not is_instance_valid(item):
		return

	var old_tween: Variant = item.get_meta(PIXEL_DISSOLVE_TWEEN_META, null)
	if old_tween is Tween and is_instance_valid(old_tween):
		old_tween.kill()
	if item.has_meta(PIXEL_DISSOLVE_TWEEN_META):
		item.remove_meta(PIXEL_DISSOLVE_TWEEN_META)


static func _set_pixel_dissolve_blend(value: float, item: CanvasItem) -> void:
	if is_instance_valid(item) and item.material != null:
		item.set_instance_shader_parameter("dissolve_blend", value)


static func _finish_pixel_dissolve_items(items: Array[CanvasItem]) -> void:
	for item in items:
		if is_instance_valid(item) and item.has_meta(PIXEL_DISSOLVE_TWEEN_META):
			item.remove_meta(PIXEL_DISSOLVE_TWEEN_META)

## 播放挂在地块 stack 下的通用序列帧特效。
## 说明:
## - vfx_name 通过 TileVFXRegistry 注册表找到具体场景。
## - target_stack 只提供挂载节点和碰撞中心；窗口、偏移、速度、层级等由 TileSpriteVFX/Profile 管理。
## - 该函数不 await，适合和治疗、伤害等数值结算并行播放。
static func play_tile_vfx(vfx_name: StringName, target_stack: Area2D, tree: SceneTree, profile: TileVFXProfile = null) -> Node2D:
	if not is_instance_valid(target_stack) or not is_instance_valid(tree):
		return null

	var effect_scene: PackedScene = get_tile_vfx_scene(vfx_name)
	return play_tile_vfx_scene(effect_scene, target_stack, tree, vfx_name, profile)


## 播放指定场景的地块序列帧特效，供少数不想进入注册表的临时效果复用。
static func play_tile_vfx_scene(effect_scene: PackedScene, target_stack: Area2D, tree: SceneTree, effect_key: StringName = &"tile_vfx", profile: TileVFXProfile = null) -> Node2D:
	if effect_scene == null or not is_instance_valid(target_stack) or not is_instance_valid(tree):
		return null

	var old_effect_name: String = "TileVFX_%s" % String(effect_key)
	var old_effect: Node = target_stack.get_node_or_null(old_effect_name)
	if is_instance_valid(old_effect):
		old_effect.queue_free()

	var effect: Node = effect_scene.instantiate()
	if not is_instance_valid(effect):
		return null
	if not (effect is Node2D):
		effect.queue_free()
		return null

	effect.name = old_effect_name
	var effect_node: Node2D = effect as Node2D
	target_stack.add_child(effect_node)

	var active_profile: TileVFXProfile = profile if profile != null else DEFAULT_TILE_VFX_REGISTRY.default_profile
	if effect_node.has_method("set_profile_if_empty"):
		effect_node.call("set_profile_if_empty", active_profile)

	var tile_anchor: Vector2 = get_tile_vfx_anchor(target_stack)
	if effect_node.has_method("play_at"):
		effect_node.call("play_at", tile_anchor)
	elif effect_node.has_method("play_once"):
		effect_node.position = tile_anchor
		effect_node.call("play_once")
	else:
		effect_node.position = tile_anchor

	return effect_node


## 获取注册表中的地块特效场景。新增动画时优先在 default_tile_vfx_registry.tres 增加映射。
static func get_tile_vfx_scene(vfx_name: StringName) -> PackedScene:
	if DEFAULT_TILE_VFX_REGISTRY == null:
		return null
	return DEFAULT_TILE_VFX_REGISTRY.get_scene(vfx_name)


## 读取地块碰撞中心作为特效锚点；缺失时回退到地块本地原点。
static func get_tile_vfx_anchor(target_stack: Area2D) -> Vector2:
	if not is_instance_valid(target_stack):
		return Vector2.ZERO

	var collision: Variant = target_stack.get_meta("collision_node") if target_stack.has_meta("collision_node") else null
	if is_instance_valid(collision) and collision is Node2D:
		return collision.position
	return Vector2.ZERO

## 1. 播放地块升降动画
## 返回 Tween 对象，以便调用方 (HexMap) 可以挂载 Tween_callback 来执行生成补块的逻辑
static func play_tile_elevation_vfx(moving_parts: Array, original_positions: Dictionary, y_offset: float, duration: float, trans_type: Tween.TransitionType, ease_type: Tween.EaseType, tree: SceneTree) -> Tween:
	var tw = tree.create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	
	for part in moving_parts:
		if is_instance_valid(part):
			var p_id = part.get_instance_id()
			var target_pos = original_positions[p_id] + Vector2(0, y_offset)
			
			# 阶段A: 震动蓄力 (左右小幅度摇晃)
			tw.tween_property(part, "position:x", original_positions[p_id].x + randf_range(-5.0, 5.0), 0.1)
			
			# 阶段B: 平滑位移
			tw.chain().tween_property(part, "position", target_pos, duration)\
				.set_trans(trans_type).set_ease(ease_type)
				
	return tw


## 2. 播放地块毁灭动画
## 使用 await 阻塞，等待全部光效和崩塌表现播完
static func play_tile_destruction_vfx(sprites: Array, tree: SceneTree) -> void:
	if sprites.is_empty() or not is_instance_valid(tree): 
		return
		
	var tw = tree.create_tween().set_parallel(true).set_pause_mode(Tween.TWEEN_PAUSE_PROCESS)
	
	for s in sprites:
		if is_instance_valid(s):
			# 阶段A: 剧烈左右抖动
			for i in range(5):
				tw.tween_property(s, "position:x", s.position.x + randf_range(-15.0, 15.0), 0.05)
				tw.chain().tween_property(s, "position:x", s.position.x, 0.05)
			
			# 阶段B: 像素化消融 Shader 渐变
			if s.material:
				# 使用 lambda 动态传参给 shader 的 dissolve_blend 进度
				tw.tween_method(func(v: float): 
					if is_instance_valid(s) and s.material:
						s.set_instance_shader_parameter("dissolve_blend", v)
				, 0.0, 1.0, 1.0)
				
	await tw.finished
