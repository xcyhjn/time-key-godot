# 文件名: CardDataPool.gd
# 功能: 卡牌数据池管理系统 - 按时代组织卡牌ID，提供快速查询接口
# 位置: gd_db/CardDataPool.gd

class_name CardDataPool
extends Node

## ==========================================
## ★ 单例访问器
## ==========================================

static var instance: CardDataPool = null

static func get_instance() -> CardDataPool:
	if instance == null:
		# 尝试从场景中查找现有实例
		var root = Engine.get_main_loop().root
		instance = root.find_child("CardDataPool", true, false) as CardDataPool
		
		# 如果不存在，创建新实例
		if instance == null:
			instance = CardDataPool.new()
			instance.name = "CardDataPool"
			root.add_child(instance)
			print("✅ CardDataPool 单例已创建")
	
	return instance

## ==========================================
## ★ 数据存储结构
## ==========================================

# 时代 → 卡牌ID列表 映射表
var era_card_map: Dictionary = {}  # 键: int, 值: Array[String] (但GDScript不支持嵌套类型声明)

# 卡牌ID → 完整元数据 缓存
var card_metadata_cache: Dictionary = {}

# 已加载的卡牌ID集合 (用于快速查找)
var loaded_card_ids: Array[String] = []

# JSON数据目录路径
var card_data_dir: String = "res://card_data/"

# 数据状态标志
var is_data_loaded: bool = false

## ==========================================
## ★ 核心数据加载方法
## ==========================================

## 初始化数据池 (惰性加载)
func initialize() -> void:
	if is_data_loaded:
		return
	
	print("🃏 CardDataPool: 开始加载卡牌数据...")
	_load_card_data_from_directory()
	is_data_loaded = true
	print("✅ CardDataPool: 数据加载完成, 共加载 %d 张卡牌" % loaded_card_ids.size())

## 从目录加载所有卡牌JSON数据
func _load_card_data_from_directory() -> void:
	# 清空现有数据
	era_card_map.clear()
	card_metadata_cache.clear()
	loaded_card_ids.clear()
	
	# 打开数据目录
	var dir = DirAccess.open(card_data_dir)
	if dir == null:
		push_error("❌ CardDataPool: 无法打开卡牌数据目录: %s" % card_data_dir)
		return
	
	# 扫描所有JSON文件
	dir.list_dir_begin()
	var file_name = dir.get_next()
	var loaded_count = 0
	var skipped_count = 0
	
	while file_name != "":
		if file_name.ends_with(".json"):
			var card_id = file_name.get_basename()
			var metadata = _load_card_metadata(card_id)
			
			if not metadata.is_empty():
				# ★ 重要：检查卡牌是否包含必需字段
				if not metadata.has("id") or not metadata.has("时代"):
					print("⚠️ CardDataPool: 跳过卡牌 %s - 缺少必需字段 (id或时代)" % card_id)
					skipped_count += 1
				else:
					_register_card_metadata(card_id, metadata)
					loaded_count += 1
			else:
				print("⚠️ CardDataPool: 跳过卡牌 %s - JSON解析失败或元数据为空" % card_id)
				skipped_count += 1
		
		file_name = dir.get_next()
	
	dir.list_dir_end()
	
	print("📊 CardDataPool: 成功加载 %d 张卡牌, 跳过 %d 张卡牌, 时代分布如下:" % [loaded_count, skipped_count])
	for era in era_card_map.keys():
		print("  时代 %d: %d 张卡牌" % [era, era_card_map[era].size()])

## 加载单个卡牌的JSON元数据
func _load_card_metadata(card_id: String) -> Dictionary:
	var json_path = card_data_dir + card_id + ".json"
	
	if not FileAccess.file_exists(json_path):
		push_warning("⚠️ CardDataPool: JSON文件不存在: %s" % json_path)
		return {}
	
	# 读取JSON文件
	var file = FileAccess.open(json_path, FileAccess.READ)
	var json_string = file.get_as_text()
	file.close()
	
	# 解析JSON
	var json = JSON.new()
	var error = json.parse(json_string)
	
	if error != OK:
		push_error("❌ CardDataPool: JSON解析失败: %s, 错误: %s" % [json_path, json.get_error_message()])
		return {}
	
	var metadata = json.data as Dictionary
	
	# ★ 验证必需字段：如果缺少id或时代，返回空字典让上层跳过
	# 注意：这里不设置默认值，让_register_card_metadata处理跳过逻辑
	if not metadata.has("id"):
		metadata["id"] = card_id  # 使用文件名作为id，但上层仍会检查并可能跳过
	
	return metadata

## 注册卡牌元数据到数据池
func _register_card_metadata(card_id: String, metadata: Dictionary) -> void:
	# ★ 检查必需字段：如果缺少id或时代，跳过注册
	if not metadata.has("id"):
		print("⚠️ CardDataPool: 跳过卡牌 %s - 缺少id字段" % card_id)
		return
	
	if not metadata.has("时代"):
		print("⚠️ CardDataPool: 跳过卡牌 %s - 缺少时代字段" % card_id)
		return
	
	# 缓存完整元数据
	card_metadata_cache[card_id] = metadata
	loaded_card_ids.append(card_id)
	
	# 提取时代信息 (支持字符串和整数格式)
	var era_value = 1  # 默认时代
	
	var era_field = metadata["时代"]
	if era_field is String:
		era_value = int(era_field)
	elif era_field is int:
		era_value = era_field
	elif era_field is float:
		era_value = int(era_field)
	
	# 确保时代在有效范围内 (1-6)
	era_value = clampi(era_value, 1, 6)
	
	# 添加到时代映射表
	if not era_card_map.has(era_value):
		era_card_map[era_value] = [] as Array[String]
	
	era_card_map[era_value].append(card_id)
	
	# 调试输出
	print("  注册卡牌: %s → 时代 %d" % [card_id, era_value])

## ==========================================
## ★ 核心查询接口
## ==========================================

## 获取指定时代的所有卡牌ID
func get_cards_by_era(era: int) -> Array[String]:
	if not is_data_loaded:
		initialize()
	
	# 确保时代在有效范围内
	var valid_era = clampi(era, 1, 6)
	
	if era_card_map.has(valid_era):
		return era_card_map[valid_era].duplicate() as Array[String]
	else:
		print("⚠️ CardDataPool: 时代 %d 没有卡牌，返回空数组" % valid_era)
		return [] as Array[String]

## 获取指定时代范围的卡牌ID
func get_cards_by_era_range(min_era: int, max_era: int) -> Array[String]:
	if not is_data_loaded:
		initialize()
	
	var result: Array[String] = []
	
	for era in range(min_era, max_era + 1):
		if era_card_map.has(era):
			result.append_array(era_card_map[era])
	
	return result

## 获取卡牌完整元数据
func get_card_metadata(card_id: String) -> Dictionary:
	if not is_data_loaded:
		initialize()
	
	if card_metadata_cache.has(card_id):
		return card_metadata_cache[card_id].duplicate()
	else:
		push_warning("⚠️ CardDataPool: 卡牌ID不存在: %s" % card_id)
		return {}

## 获取所有已加载卡牌ID
func get_all_card_ids() -> Array[String]:
	if not is_data_loaded:
		initialize()
	
	return loaded_card_ids.duplicate()

## 获取时代分布统计
func get_era_distribution() -> Dictionary:
	if not is_data_loaded:
		initialize()
	
	var distribution = {}
	
	for era in era_card_map.keys():
		distribution[era] = era_card_map[era].size()
	
	return distribution

## ==========================================
## ★ 数据验证与工具方法
## ==========================================

## 检查卡牌ID是否存在
func has_card(card_id: String) -> bool:
	if not is_data_loaded:
		initialize()
	
	return card_metadata_cache.has(card_id)

## 重新加载数据 (用于开发时热重载)
func reload_data() -> void:
	is_data_loaded = false
	initialize()
	print("🔄 CardDataPool: 数据已重新加载")

## 获取卡牌数量统计
func get_statistics() -> Dictionary:
	if not is_data_loaded:
		initialize()
	
	var stats = {
		"total_cards": loaded_card_ids.size(),
		"total_eras": era_card_map.keys().size(),
		"era_distribution": get_era_distribution()
	}
	
	return stats

## ==========================================
## ★ 单例生命周期管理
## ==========================================

func _ready() -> void:
	# 自动注册为单例
	if instance == null:
		instance = self
		print("✅ CardDataPool: 单例已注册")
