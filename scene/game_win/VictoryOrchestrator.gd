extends Node2D
class_name VictoryOrchestrator

# 节点引用
@onready var animation_player: AnimationPlayer = $AnimationRoot/AnimationPlayer
@onready var key_sprite: Sprite2D = $AnimationRoot/KeyAnchor/KeySprite

# 镜头控制参数
@export var target_zoom: float = 2.5
@export var zoom_duration: float = 1.5
@export var camera_move_duration: float = 1.2

# 内部状态
var main_camera: Camera2D = null
var dim_menu_instance: Node = null
var is_following: bool = false


func _ready() -> void:
	GameLogger.info("🎬 胜利演出控制器已加载", "VictoryOrchestrator")
	z_index = 2000  # 强制视觉置顶，超过地图上所有层級
	
	# 初始隐藏钥匙（如果存在）
	if is_instance_valid(key_sprite):
		key_sprite.modulate.a = 0.0
		GameLogger.debug("钥匙精灵透明度已归零", "VictoryOrchestrator")
	
	# 连接动画完成信号
	if is_instance_valid(animation_player):
		animation_player.animation_finished.connect(_on_animation_finished)
	else:
		GameLogger.error("未找到AnimationPlayer节点", "VictoryOrchestrator")


## 启动胜利演出（由project.gd调用）
func start_performance() -> void:
	GameLogger.info("🎬 开始胜利演出序列", "VictoryOrchestrator")
	
	# 定位到HexMap中心（世界坐标系原点）
	global_position = Vector2.ZERO
	GameLogger.debug("胜利演出已定位到世界坐标原点: " + str(global_position), "VictoryOrchestrator")
	
	# 1. 获取主相机引用
	main_camera = _find_main_camera()
	if not is_instance_valid(main_camera):
		GameLogger.warning("未找到主相机，跳过镜头动画", "VictoryOrchestrator")
		_play_victory_animation()
		return
	
	# 2. 执行镜头聚焦动画
	_start_camera_focus()


## 查找主相机（活动相机）
func _find_main_camera() -> Camera2D:
	# 方法1：获取视口活动相机
	var viewport_camera = get_viewport().get_camera_2d()
	if is_instance_valid(viewport_camera):
		GameLogger.debug("找到视口活动相机: " + viewport_camera.name, "VictoryOrchestrator")
		return viewport_camera
	
	# 方法2：查找场景中第一个Camera2D节点
	var cameras = get_tree().get_nodes_in_group("camera")
	if not cameras.is_empty():
		GameLogger.debug("通过'camera'组找到相机: " + cameras[0].name, "VictoryOrchestrator")
		return cameras[0] as Camera2D
	
	# 方法3：遍历根节点查找
	var root = get_tree().root
	for child in root.get_children():
		if child is Camera2D:
			GameLogger.debug("遍历找到相机: " + child.name, "VictoryOrchestrator")
			return child
	
	GameLogger.warning("未找到任何Camera2D节点", "VictoryOrchestrator")
	return null


## 启动镜头聚焦动画
func _start_camera_focus() -> void:
	if not is_instance_valid(main_camera):
		GameLogger.warning("相机无效，跳过镜头聚焦", "VictoryOrchestrator")
		_play_victory_animation()
		return
	
	GameLogger.info("📷 开始镜头聚焦动画", "VictoryOrchestrator")
	
	# 彻底接管相机控制权，防止原生脚本干扰
	if main_camera.has_method("set_process"):
		main_camera.set_process(false)
		main_camera.set_process_unhandled_input(false)
		GameLogger.debug("已挂起相机原生脚本处理逻辑", "VictoryOrchestrator")
	
	# 保存原始相机状态（以备恢复，虽然当前不恢复）
	var _original_zoom = main_camera.zoom
	var _original_position = main_camera.global_position
	
	# 计算目标位置（聚焦到钥匙位置或自身位置）
	var target_position = Vector2.ZERO
	if is_instance_valid(key_sprite):
		# 使用钥匙精灵的全局坐标
		target_position = key_sprite.global_position
		GameLogger.debug("镜头将聚焦到钥匙全局位置: " + str(target_position), "VictoryOrchestrator")
	else:
		# 聚焦到自身位置（世界坐标系原点）
		target_position = global_position
		GameLogger.debug("镜头将聚焦到胜利演出位置: " + str(target_position), "VictoryOrchestrator")
	
	# 缩短动画时长以增加流畅感
	var shorter_zoom_duration = zoom_duration * 0.7
	var shorter_move_duration = camera_move_duration * 0.7
	
	# 创建并行动画
	var tween = create_tween().set_parallel(true).set_trans(Tween.TRANS_CUBIC).set_ease(Tween.EASE_OUT)
	
	# 缩放动画
	tween.tween_property(main_camera, "zoom", Vector2(target_zoom, target_zoom), shorter_zoom_duration)
	
	# 移动动画（使用全局坐标）
	tween.tween_property(main_camera, "global_position", target_position, shorter_move_duration)
	
	# 动画完成后播放胜利动画（确保时序不冲突）
	tween.finished.connect(_play_victory_animation)


## 播放胜利动画（钥匙上升、发光等）
func _play_victory_animation() -> void:
	GameLogger.info("✨ 播放胜利动画序列", "VictoryOrchestrator")
	
	# 启动动态相机跟随
	is_following = true
	GameLogger.debug("启动动态相机跟随", "VictoryOrchestrator")
	
	if is_instance_valid(animation_player):
		# 确保钥匙完全可见
		if is_instance_valid(key_sprite):
			key_sprite.modulate.a = 1.0
			GameLogger.debug("钥匙精灵已设为完全可见", "VictoryOrchestrator")
		# 播放预设的动画轨道
		animation_player.play("victory_rise")
		GameLogger.debug("开始播放 'victory_rise' 动画", "VictoryOrchestrator")
	else:
		GameLogger.warning("AnimationPlayer无效，跳过动画播放", "VictoryOrchestrator")
		# 如果没动画，直接进入下一步
		_on_animation_finished("")


## 动画完成回调
func _on_animation_finished(anim_name: String) -> void:
	GameLogger.info("✅ 胜利动画播放完成: " + anim_name, "VictoryOrchestrator")
	
	# 停止动态相机跟随
	is_following = false
	GameLogger.debug("停止动态相机跟随", "VictoryOrchestrator")
	
	# 延迟一帧避免竞争条件
	await get_tree().process_frame
	
	# 加载并显示渐暗菜单
	_show_dim_menu()


## 显示渐暗菜单并切换场景
func _show_dim_menu() -> void:
	GameLogger.info("🌙 开始渐暗转场", "VictoryOrchestrator")
	
	# 加载dim_menu场景
	var dim_scene = preload("res://scene/dim_menu/dim_menu.tscn")
	if not is_instance_valid(dim_scene):
		GameLogger.error("无法加载dim_menu场景", "VictoryOrchestrator")
		_switch_to_win_screen()
		return
	
	# 实例化并添加到场景树
	dim_menu_instance = dim_scene.instantiate()
	add_child(dim_menu_instance)
	
	# 调用渐暗方法（dim_menu的use(0)方法，0表示渐暗模式）
	if dim_menu_instance.has_method("use"):
		GameLogger.debug("调用dim_menu.use(0)开始渐暗", "VictoryOrchestrator")
		dim_menu_instance.use(0)  # 开始渐暗，不等待完成
		# 等待屏幕变黑（0.45秒后切换场景，防止闪烁）
		await get_tree().create_timer(0.45).timeout
		GameLogger.debug("屏幕已变黑，执行场景切换", "VictoryOrchestrator")
	else:
		GameLogger.warning("dim_menu没有use方法，直接切换场景", "VictoryOrchestrator")
		# 如果没有方法，等待固定时间
		await get_tree().create_timer(2.0).timeout
	
	# 切换到结算界面
	_switch_to_win_screen()


## 切换到胜利结算界面
func _switch_to_win_screen() -> void:
	GameLogger.info("🚪 切换到胜利结算界面", "VictoryOrchestrator")
	
	# 恢复相机控制权，确保相机在后续游戏状态中正常工作
	if is_instance_valid(main_camera) and main_camera.has_method("set_process"):
		main_camera.set_process(true)
		main_camera.set_process_unhandled_input(true)
		GameLogger.debug("已恢复相机原生脚本处理逻辑", "VictoryOrchestrator")
	
	# 清理资源
	if is_instance_valid(dim_menu_instance):
		dim_menu_instance.queue_free()
		dim_menu_instance = null
	
	# 注意：不再恢复相机缩放，让相机保持在放大聚焦状态
	# 由dim_menu黑屏遮盖后直接切换到下一个场景，避免视觉弹回
	
	# 切换场景
	if GlobalClock and GlobalClock.has_method("switch_to_scene"):
		GlobalClock.switch_to_scene("res://scenes/ui/game_win_screen.tscn")
	else:
		GameLogger.error("GlobalClock无效或缺少switch_to_scene方法", "VictoryOrchestrator")
		# 备用方案：直接切换场景
		var err = get_tree().change_scene_to_file("res://scenes/ui/game_win_screen.tscn")
		if err != OK:
			GameLogger.error("直接场景切换失败: " + str(err), "VictoryOrchestrator")
	
	# 清理自身
	queue_free()


## 动态相机跟随逻辑
func _process(_delta: float) -> void:
	if is_following and is_instance_valid(main_camera) and is_instance_valid(key_sprite):
		main_camera.global_position = key_sprite.global_position
