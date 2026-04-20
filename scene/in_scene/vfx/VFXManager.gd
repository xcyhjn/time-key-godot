class_name VFXManager
extends Node

## 播放特效并阻塞，直到特效播放完成
static func play_vfx(vfx_name: String, target_pos: Vector2, tree: SceneTree) -> void:
	# 假设你以后会有个特效预制体字典
	# var vfx_scene = preload("res://vfx/slash.tscn")
	
	GameLogger.debug("播放特效: %s 在位置: %s" % [vfx_name, target_pos], "VFXManager")
	
	# 这里模拟一个通用的受击特效表现（例如目标闪烁或震动）
	# 实际开发中，你可以实例化特效节点，并 await 特效的 finished 信号
	var s = Global.get_anim_speed() if Global.has_method("get_anim_speed") else 1.0
	
	# 模拟动画时间（解耦的好处就是，以后你改这里的动画，不需要去改卡牌逻辑代码）
	await tree.create_timer(0.3 * s).timeout
