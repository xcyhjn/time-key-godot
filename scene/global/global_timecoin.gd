# 文件名: global_timecoin.gd
# 功能: 时间币全局数据中心 (Autoload单例)
# 设计原则: 单一数据源，负责时间币的数值统筹和计算逻辑
extends Node

## ==========================================
## 导出变量 - 可在检查器中动态调整
## ==========================================

# 空位转货币比例: 每个时间轴空位转化为多少时间币
@export var empty_slot_ratio: float = 1.0

# 调试模式: 启用时会在控制台输出详细日志
@export var debug_mode: bool = true


## ==========================================
## 核心变量 - 时间币数值状态
## ==========================================

# 当前时间币数量 (必须保持为整数)
var current_timecoins: int = 0


## ==========================================
## 信号机制 - 用于通知UI层数据变更
## ==========================================

# 时间币更新信号
# @param current_amount: 更新后的总数量
# @param delta: 本次变动的差值（正数代表获取，负数代表消耗）
signal timecoin_updated(current_amount: int, delta: int)

# 时间币不足警告信号（当尝试消耗但余额不足时触发）
signal timecoin_insufficient(requested: int, available: int)


## ==========================================
## 核心方法 - 时间币数值管理
## ==========================================

# 通过时间轴空位数获取时间币
# @param empty_slots_count: 时间轴中的空位数量（没有敌我意图的格子）
func add_from_timeline(empty_slots_count: int) -> void:
	if empty_slots_count <= 0:
		_log_warning("add_from_timeline: 空位数量必须为正数，传入值: %d" % empty_slots_count)
		return
	
	# 根据比例计算获取的时间币数量（向下取整保证整数）
	var timecoins_to_add: int = floori(empty_slots_count * empty_slot_ratio)
	
	if timecoins_to_add <= 0:
		_log_warning("add_from_timeline: 计算后的时间币数量为0，请检查empty_slot_ratio设置")
		return
	
	# 调用通用增加方法
	add_timecoins(timecoins_to_add)
	
	_log_info("add_from_timeline: 从 %d 个空位中获取 %d 时间币 (比例: %.2f)" % [
		empty_slots_count, timecoins_to_add, empty_slot_ratio
	])


# 通用增加时间币方法
# @param amount: 要增加的时间币数量（必须为正数）
func add_timecoins(amount: int) -> void:
	if amount <= 0:
		_log_warning("add_timecoins: 增加数量必须为正数，传入值: %d" % amount)
		return
	
	var previous_amount: int = current_timecoins
	current_timecoins += amount
	_sync_to_map_state()
	
	# 发出更新信号，delta为正数表示获取
	timecoin_updated.emit(current_timecoins, amount)
	
	_log_info("add_timecoins: 时间币增加 %d，从 %d 变为 %d" % [
		amount, previous_amount, current_timecoins
	])


# 消耗时间币方法
# @param amount: 要消耗的时间币数量（必须为正数）
# @return bool: 是否成功扣除（余额充足时返回true）
func consume_timecoins(amount: int) -> bool:
	if amount <= 0:
		_log_warning("consume_timecoins: 消耗数量必须为正数，传入值: %d" % amount)
		return false
	
	if current_timecoins < amount:
		# 余额不足，触发警告信号
		timecoin_insufficient.emit(amount, current_timecoins)
		
		_log_warning("consume_timecoins: 余额不足！请求 %d，当前余额 %d" % [
			amount, current_timecoins
		])
		return false
	
	var previous_amount: int = current_timecoins
	current_timecoins -= amount
	_sync_to_map_state()
	
	# 发出更新信号，delta为负数表示消耗
	timecoin_updated.emit(current_timecoins, -amount)
	
	_log_info("consume_timecoins: 时间币减少 %d，从 %d 变为 %d" % [
		amount, previous_amount, current_timecoins
	])
	return true


# 直接设置时间币数量（用于调试或初始化）
# @param new_amount: 要设置的新时间币数量（必须为非负数）
func set_timecoins(new_amount: int) -> void:
	if new_amount < 0:
		_log_warning("set_timecoins: 时间币数量不能为负数，传入值: %d" % new_amount)
		return
	
	var delta: int = new_amount - current_timecoins
	current_timecoins = new_amount
	_sync_to_map_state()
	
	if delta != 0:
		timecoin_updated.emit(current_timecoins, delta)
	
	_log_info("set_timecoins: 直接设置时间币为 %d (变动: %d)" % [new_amount, delta])


# 获取当前时间币数量
func get_timecoins() -> int:
	return current_timecoins


# 检查是否有足够的时间币
func has_enough_timecoins(amount: int) -> bool:
	return current_timecoins >= amount


## ==========================================
## 辅助方法 - 日志记录与调试
## ==========================================

func _log_info(message: String) -> void:
	if debug_mode:
		print("[GlobalTimecoin] %s" % message)

func _log_warning(message: String) -> void:
	if debug_mode:
		push_warning("[GlobalTimecoin] %s" % message)


## ==========================================
## 生命周期方法
## ==========================================

func _ready() -> void:
	_restore_from_map_state()
	_sync_to_map_state()
	_log_info("时间币全局数据中心已加载")
	_log_info("当前空位转货币比例: %.2f" % empty_slot_ratio)
	
	# 连接信号总线（如果存在）
	var signal_bus = get_node_or_null("/root/Signal_Bus")
	if signal_bus:
		# 连接到 SignalBus 的转发函数
		if signal_bus.has_method("emit_timecoin_updated"):
			timecoin_updated.connect(signal_bus.emit_timecoin_updated)
			_log_info("已连接 timecoin_updated 到 SignalBus")
		else:
			_log_warning("SignalBus 没有 emit_timecoin_updated 方法")
		
		if signal_bus.has_method("emit_timecoin_insufficient"):
			timecoin_insufficient.connect(signal_bus.emit_timecoin_insufficient)
			_log_info("已连接 timecoin_insufficient 到 SignalBus")
		else:
			_log_warning("SignalBus 没有 emit_timecoin_insufficient 方法")


## 把当前时间币同步到 MapState 里，作为切场保底快照。
func _sync_to_map_state() -> void:
	if MapState and MapState.has_method("set_saved_timecoins"):
		MapState.set_saved_timecoins(current_timecoins)


## 如果 GlobalTimecoin 因某些场景切换方式被重新初始化，
## 就从 MapState 恢复上一份已知的时间币数量。
func _restore_from_map_state() -> void:
	if not MapState or not MapState.has_method("get_saved_timecoins"):
		return

	var saved_amount := MapState.get_saved_timecoins()
	if saved_amount <= 0 and current_timecoins > 0:
		return

	current_timecoins = max(max(saved_amount, current_timecoins), 0)
