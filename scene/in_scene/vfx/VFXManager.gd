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
	
	GameLogger.debug("播放特效: %s 在位置: %s" % [vfx_name, target_pos], "VFXManager")
	
	# 这里模拟一个通用的受击特效表现（例如目标闪烁或震动）
	# 实际开发中，你可以实例化特效节点，并 await 特效的 finished 信号
	var s = Global.get_anim_speed() if Global.has_method("get_anim_speed") else 1.0
	
	# 模拟动画时间（解耦的好处就是，以后你改这里的动画，不需要去改卡牌逻辑代码）
	await tree.create_timer(0.3 * s).timeout
