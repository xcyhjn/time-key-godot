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
