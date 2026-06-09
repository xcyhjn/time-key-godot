# 文件名: timecoin_ui.gd
# 功能: 时间币 UI 交互与动画反馈脚本
# 设计原则: 响应全局数据变更，提供丰富的视觉反馈，严格防动画冲突
extends PanelContainer

const TimecoinGlobalBridgeScript = preload("res://scene/in_scene/timecoin_ui_modules/bridges/TimecoinGlobalBridge.gd")

## ==========================================
## 节点引用 (必须在场景中正确连接)
## ==========================================

# 沙漏图标节点
@onready var hourglass_icon: TextureRect = $HorizontalLayout/HourglassIcon

# 时间币数量文本节点
@onready var amount_label: Label = $HorizontalLayout/TimecoinAmount

# UI 容器根节点 (用于整体动画)
@onready var ui_container: PanelContainer = self


## ==========================================
## 动画参数配置 (可在检查器中调整)
## ==========================================

# 获取货币动画参数
@export_group("获取动画 (Gain Animation)")
@export var gain_scale_amount: float = 1.25  # 放大比例
@export var gain_scale_duration: float = 0.3  # 缩放持续时间
@export var gain_shake_amount: float = 5.0  # 抖动幅度
@export var gain_shake_duration: float = 0.5  # 抖动持续时间
@export var gain_elasticity: float = 0.8  # 弹性系数 (0-1, 越高越弹)

# 消耗货币动画参数
@export_group("消耗动画 (Consume Animation)")
@export var consume_shake_amount: float = 8.0  # 消耗时的抖动幅度
@export var consume_shake_duration: float = 0.2  # 消耗抖动持续时间
@export var consume_color_tint: Color = Color(1.0, 0.3, 0.3, 0.5)  # 消耗时的红色半透明覆盖

# 动画防冲突参数
@export_group("动画防冲突 (Anti-Conflict)")
@export var max_concurrent_tweens: int = 3  # 最大同时运行的Tween数量


## ==========================================
## 运行时状态变量
## ==========================================

# 当前活跃的 Tween 实例 (用于防冲突管理)
var active_tweens: Array[Tween] = []

# 原始位置和缩放缓存 (用于动画重置)
var original_position: Vector2
var original_scale: Vector2 = Vector2.ONE

# 原始图标颜色缓存
var original_icon_color: Color

# 当前是否正在播放持续震动
var is_shaking: bool = false

# 动画队列 (用于处理快速连续操作)
var animation_queue: Array[Dictionary] = []

var _timecoin_global_bridge = null


## ==========================================
## 生命周期方法
## ==========================================

func _get_timecoin_global_bridge():
	if _timecoin_global_bridge == null:
		_timecoin_global_bridge = TimecoinGlobalBridgeScript.new()
	return _timecoin_global_bridge


# 验证节点引用是否有效
func _validate_node_references() -> void:
	# 如果 % 引用失败，尝试通过路径查找
	if not hourglass_icon:
		hourglass_icon = get_node("HorizontalLayout/HourglassIcon")
		if hourglass_icon:
			print("[TimecoinUI] 通过路径找到 hourglass_icon")
	
	if not amount_label:
		amount_label = get_node("HorizontalLayout/TimecoinAmount")
		if amount_label:
			print("[TimecoinUI] 通过路径找到 amount_label")
	
	# 如果仍然未找到，记录错误
	if not hourglass_icon:
		push_error("[TimecoinUI] 无法找到沙漏图标节点，请确保场景中有名为 'HourglassIcon' 的 TextureRect 节点")
	
	if not amount_label:
		push_error("[TimecoinUI] 无法找到时间币数量标签，请确保场景中有名为 'TimecoinAmount' 的 Label 节点")

func _ready() -> void:
	# 确保节点引用有效
	_validate_node_references()
	
	# 缓存原始状态
	original_position = position
	original_scale = scale
	
	if hourglass_icon:
		original_icon_color = hourglass_icon.modulate
	else:
		original_icon_color = Color.WHITE
		push_warning("[TimecoinUI] hourglass_icon 未找到，图标动画将无法播放")
	
	if not amount_label:
		push_warning("[TimecoinUI] amount_label 未找到，时间币数量将无法显示")
	
	# 连接全局时间币信号
	_connect_global_signals()
	
	# 初始化显示：
	# 这里绝不能直接写死成 0，否则会把 _connect_global_signals()
	# 中已经同步到的真实全局数值重新覆盖掉。
	# 因此改为显式从 GlobalTimecoin 读取一次当前值。
	_refresh_display_from_global()
	# 再延迟一帧补一次兜底刷新，防止极端情况下 Autoload 与 UI 初始化时序交错。
	call_deferred("_refresh_display_from_global")
	
	print("[TimecoinUI] UI 已初始化，原始位置: %s, 原始缩放: %s" % [original_position, original_scale])


## ==========================================
## 信号连接与数据响应
## ==========================================

# 连接到全局时间币数据中心
func _connect_global_signals() -> void:
	# 尝试通过不同方式获取 GlobalTimecoin 实例
	var timecoin_singleton = _get_timecoin_singleton()
	
	if timecoin_singleton:
		# 连接更新信号
		if timecoin_singleton.has_signal("timecoin_updated"):
			timecoin_singleton.timecoin_updated.connect(_on_timecoin_updated)
			print("[TimecoinUI] 成功连接到 GlobalTimecoin.timecoin_updated 信号")
		else:
			push_warning("[TimecoinUI] GlobalTimecoin 没有 timecoin_updated 信号")
		
		# 连接不足警告信号
		if timecoin_singleton.has_signal("timecoin_insufficient"):
			timecoin_singleton.timecoin_insufficient.connect(_on_timecoin_insufficient)
			print("[TimecoinUI] 成功连接到 GlobalTimecoin.timecoin_insufficient 信号")
		
		# 初始同步显示
		if timecoin_singleton.has_method("get_timecoins"):
			var initial_amount = timecoin_singleton.get_timecoins()
			_update_display(initial_amount)
	else:
		push_error("[TimecoinUI] 无法找到 GlobalTimecoin 单例，请确保已添加到项目设置的 Autoload 中")


## 主动从 GlobalTimecoin 拉取一次当前值并刷新标签。
## 这个函数的意义是把“初始显示”与“信号更新”拆开：
## - 信号负责后续增减变化
## - 这里负责场景刚打开时立刻显示真实库存
func _refresh_display_from_global() -> void:
	var timecoin_singleton = _get_timecoin_singleton()
	if timecoin_singleton and timecoin_singleton.has_method("get_timecoins"):
		_update_display(timecoin_singleton.get_timecoins())


# 尝试获取时间币单例的多种方式
func _get_timecoin_singleton() -> Node:
	var timecoin_singleton: Node = _get_timecoin_global_bridge().find_timecoin_singleton(self)
	if timecoin_singleton == null:
		push_warning("[TimecoinUI] 无法找到 GlobalTimecoin 单例，请确保已添加到项目设置的 Autoload 中或场景中")
	return timecoin_singleton


# 时间币更新信号处理
func _on_timecoin_updated(current_amount: int, delta: int) -> void:
	print("[TimecoinUI] 时间币更新: 当前 %d, 变动 %d" % [current_amount, delta])
	
	# 更新显示
	_update_display(current_amount)
	
	# 根据变动类型播放动画
	if delta > 0:
		# 获取货币 - 播放丰富动画
		_play_gain_animation(delta)
	elif delta < 0:
		# 消耗货币 - 播放简洁动画
		_play_consume_animation(abs(delta))


# 时间币不足警告信号处理
func _on_timecoin_insufficient(requested: int, available: int) -> void:
	print("[TimecoinUI] 时间币不足警告: 请求 %d, 可用 %d" % [requested, available])
	
	# 播放警告动画
	_play_warning_animation()


# 更新显示文本
func _update_display(amount: int) -> void:
	if amount_label:
		amount_label.text = str(amount)
		print("[TimecoinUI] 更新显示: %d" % amount)
	else:
		push_warning("[TimecoinUI] amount_label 未找到，无法更新显示")


## ==========================================
## 核心动画系统 (严格防冲突)
## ==========================================

# 播放获取货币动画
func _play_gain_animation(amount_gained: int) -> void:
	# 清理旧动画，防止冲突
	_cleanup_active_tweens()
	
	# 重置到原始状态
	_reset_to_original_state()
	
	print("[TimecoinUI] 播放获取动画，获得 %d 时间币" % amount_gained)
	
	# 创建新的 Tween
	var tween = create_tween()
	tween.set_parallel(true)  # 并行执行多个动画
	
	# 1. 弹性缩放动画
	tween.tween_property(ui_container, "scale", 
		original_scale * gain_scale_amount, gain_scale_duration * 0.5
	).set_trans(Tween.TRANS_BACK).set_ease(Tween.EASE_OUT)
	
	tween.tween_property(ui_container, "scale", 
		original_scale, gain_scale_duration * 0.5
	).set_delay(gain_scale_duration * 0.5).set_trans(Tween.TRANS_ELASTIC).set_ease(Tween.EASE_OUT)
	
	# 2. 位置抖动动画
	_apply_shake_effect(tween, gain_shake_amount, gain_shake_duration)
	
	# 3. 图标高亮效果 (如果存在图标)
	if hourglass_icon:
		tween.tween_property(hourglass_icon, "modulate", 
			Color(1.5, 1.5, 1.0, 1.0), gain_scale_duration * 0.3
		).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_OUT)
		
		tween.tween_property(hourglass_icon, "modulate", 
			original_icon_color, gain_scale_duration * 0.7
		).set_delay(gain_scale_duration * 0.3).set_trans(Tween.TRANS_QUAD).set_ease(Tween.EASE_IN)
	
	# 记录活跃 Tween
	active_tweens.append(tween)
	
	# 动画结束时清理
	tween.finished.connect(_on_gain_animation_finished.bind(tween))


# 播放消耗货币动画
func _play_consume_animation(amount_spent: int) -> void:
	# 清理旧动画，防止冲突
	_cleanup_active_tweens()
	
	# 重置到原始状态
	_reset_to_original_state()
	
	print("[TimecoinUI] 播放消耗动画，花费 %d 时间币" % amount_spent)
	
	# 创建新的 Tween
	var tween = create_tween()
	
	# 1. 快速左右震动
	_apply_shake_effect(tween, consume_shake_amount, consume_shake_duration)
	
	# 2. 红色半透明覆盖 (短暂提示)
	if hourglass_icon:
		tween.tween_property(hourglass_icon, "modulate", 
			consume_color_tint, consume_shake_duration * 0.2
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		
		tween.tween_property(hourglass_icon, "modulate", 
			original_icon_color, consume_shake_duration * 0.8
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	
	# 记录活跃 Tween
	active_tweens.append(tween)
	
	# 动画结束时清理
	tween.finished.connect(_on_consume_animation_finished.bind(tween))


# 播放警告动画 (余额不足时)
func _play_warning_animation() -> void:
	# 清理旧动画
	_cleanup_active_tweens()
	
	# 重置状态
	_reset_to_original_state()
	
	print("[TimecoinUI] 播放余额不足警告动画")
	
	var tween = create_tween()
	tween.set_parallel(true)
	
	# 快速红色闪烁
	if hourglass_icon:
		tween.tween_property(hourglass_icon, "modulate", 
			Color(2.0, 0.2, 0.2, 1.0), 0.1
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		
		tween.chain().tween_property(hourglass_icon, "modulate", 
			original_icon_color, 0.1
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		
		# 重复两次
		tween.chain().tween_property(hourglass_icon, "modulate", 
			Color(2.0, 0.2, 0.2, 1.0), 0.1
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
		
		tween.chain().tween_property(hourglass_icon, "modulate", 
			original_icon_color, 0.1
		).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	
	# 轻微震动
	_apply_shake_effect(tween, 3.0, 0.4)
	
	active_tweens.append(tween)
	
	tween.finished.connect(_on_warning_animation_finished.bind(tween))


# 应用抖动效果到 Tween
func _apply_shake_effect(tween: Tween, shake_amount: float, duration: float) -> void:
	# 创建随机抖动序列
	var shake_points = 5
	for i in range(shake_points):
		var time_point = float(i) / shake_points * duration
		var shake_direction = Vector2(
			randf_range(-shake_amount, shake_amount),
			randf_range(-shake_amount * 0.3, shake_amount * 0.3)
		)
		
		tween.tween_property(ui_container, "position", 
			original_position + shake_direction, duration / shake_points * 0.8
		).set_delay(time_point).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)
	
	# 最终回到原始位置
	tween.tween_property(ui_container, "position", 
		original_position, duration / shake_points * 0.2
	).set_trans(Tween.TRANS_SINE).set_ease(Tween.EASE_IN_OUT)


# 清理活跃的 Tween 实例
func _cleanup_active_tweens() -> void:
	# 限制同时运行的 Tween 数量
	if active_tweens.size() >= max_concurrent_tweens:
		print("[TimecoinUI] 达到最大 Tween 数量限制 (%d)，清理旧动画" % max_concurrent_tweens)
		
		# 停止并移除最旧的 Tween
		var oldest_tween = active_tweens.pop_front()
		if oldest_tween and oldest_tween.is_valid():
			oldest_tween.kill()
	
	# 清理所有无效的 Tween 引用
	var valid_tweens: Array[Tween] = []
	for tween in active_tweens:
		if tween and tween.is_valid():
			valid_tweens.append(tween)
		else:
			print("[TimecoinUI] 清理无效的 Tween 引用")
	
	active_tweens = valid_tweens


# 重置到原始状态
func _reset_to_original_state() -> void:
	# 立即停止所有位置和缩放相关的补间
	for tween in active_tweens:
		if tween and tween.is_valid():
			tween.kill()
	
	# 立即重置到原始状态
	ui_container.position = original_position
	ui_container.scale = original_scale
	
	if hourglass_icon:
		hourglass_icon.modulate = original_icon_color
	
	print("[TimecoinUI] 已重置到原始状态")


## ==========================================
## 动画完成回调
## ==========================================

func _on_gain_animation_finished(tween: Tween) -> void:
	print("[TimecoinUI] 获取动画完成")
	_remove_tween_from_active(tween)

func _on_consume_animation_finished(tween: Tween) -> void:
	print("[TimecoinUI] 消耗动画完成")
	_remove_tween_from_active(tween)

func _on_warning_animation_finished(tween: Tween) -> void:
	print("[TimecoinUI] 警告动画完成")
	_remove_tween_from_active(tween)

# 从活跃列表移除 Tween
func _remove_tween_from_active(tween: Tween) -> void:
	var index = active_tweens.find(tween)
	if index != -1:
		active_tweens.remove_at(index)


## ==========================================
## 调试与测试方法
## ==========================================

# 手动触发获取动画 (用于调试)
func debug_trigger_gain(amount: int = 10) -> void:
	print("[TimecoinUI] 调试: 触发获取 %d 时间币" % amount)
	_play_gain_animation(amount)

# 手动触发消耗动画 (用于调试)
func debug_trigger_consume(amount: int = 5) -> void:
	print("[TimecoinUI] 调试: 触发消耗 %d 时间币" % amount)
	_play_consume_animation(amount)

# 手动触发警告动画 (用于调试)
func debug_trigger_warning() -> void:
	print("[TimecoinUI] 调试: 触发警告动画")
	_play_warning_animation()


## ==========================================
## Shader 控制功能 (可选)
## ==========================================

# 沙漏Shader材质引用
var hourglass_shader_material: ShaderMaterial = null

# 初始化Shader材质
func _initialize_shader_material() -> void:
	if not hourglass_icon:
		return
	
	# 检查是否已有材质
	if hourglass_icon.material == null:
		# 创建新的ShaderMaterial
		var new_material = ShaderMaterial.new()
		new_material.shader = preload("res://shaders/hourglass_shake.gdshader")
		hourglass_icon.material = new_material
		hourglass_shader_material = new_material
		print("[TimecoinUI] 已为沙漏图标创建Shader材质")
	elif hourglass_icon.material is ShaderMaterial:
		hourglass_shader_material = hourglass_icon.material
		print("[TimecoinUI] 已获取现有Shader材质")
	else:
		push_warning("[TimecoinUI] 沙漏图标已有材质，但不是ShaderMaterial")

# 开始沙漏震动效果
func start_hourglass_shake(intensity: float = 0.8, frequency: float = 20.0) -> void:
	if not hourglass_shader_material:
		_initialize_shader_material()
	
	if hourglass_shader_material:
		hourglass_shader_material.set_shader_parameter("shake_enabled", 1.0)
		hourglass_shader_material.set_shader_parameter("shake_intensity", intensity)
		hourglass_shader_material.set_shader_parameter("shake_frequency", frequency)
		print("[TimecoinUI] 已启动沙漏震动效果")

# 停止沙漏震动效果
func stop_hourglass_shake() -> void:
	if hourglass_shader_material:
		hourglass_shader_material.set_shader_parameter("shake_enabled", 0.0)
		print("[TimecoinUI] 已停止沙漏震动效果")

# 设置震动强度
func set_shake_intensity(intensity: float) -> void:
	if hourglass_shader_material:
		hourglass_shader_material.set_shader_parameter("shake_intensity", intensity)

# 设置震动频率
func set_shake_frequency(frequency: float) -> void:
	if hourglass_shader_material:
		hourglass_shader_material.set_shader_parameter("shake_frequency", frequency)
