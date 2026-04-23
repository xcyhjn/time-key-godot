extends landform

func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 10
	super._init(
	"blockhouse", 
	["res://image/enermy/blockhouse/blockhouse.png"], 
	["res://image/enermy/background/broken_trees.png"], 
	{"chance": 0.5},
	location_in,
	false,
	battle_in)
	if location_in == Vector2i(-100, -100):
		return
	Attitude = Attitude_Pool.Enemy
	# 设置时间占位形状为1x2
	willing_pool = {tree_harvest_will : tree_harvest_done,
					material_prepare_will : material_prepare_done,
					build_will : build_done}
	set_timeline_shape("011")


func get_possible_coords(coord : Vector2i, tile_info : Dictionary) -> bool:
	if landform_rules.keys().has("require_height") and !landform_rules["require_height"].has(tile_info[coord]["height"]) :
		return false
	if landform_rules.keys().has("require_terrain") and !landform_rules["require_terrain"].has(tile_info[coord]["terrain"]):
		return false
	if !tile_info[coord].has("landform")  or tile_info[coord]["landform"] != null:
		return false
	neighbors = get_neighbor_coords(coord)
	var bool_buffer = false
	for neighbor in neighbors:
		# ★★★ 核心修复：必须先检查地图字典中是否存在这个邻居坐标！★★★
		if tile_info.has(neighbor):
			# 只有确定坐标存在，才能安全地访问 tile_info[neighbor]
			if tile_info[neighbor].has("landform") and tile_info[neighbor]["landform"] != null and tile_info[neighbor]["landform"].landform_name == "forest":
				bool_buffer = true
				break # 优化：只要找到周围哪怕一格有森林，就直接跳出循环，节省性能
				
	return bool_buffer

func Behavior(Step, info_in, Other, beha):
	if beha != -1:
		willing = beha
	var rng = RandomNumberGenerator.new()
	if will:
		willing_pool.values()[willing].call()
		will = false
		if willing < willing_pool.size():
			willing += 1
		else:
			willing = 0
	else:
		willing_pool.keys()[willing].call()
		will = true
	

func tree_harvest_will():
	pass

func tree_harvest_done():
	pass

func material_prepare_will():
	pass


func material_prepare_done():
	pass

func build_will():
	pass
	
func build_done():
	heal(Max_Blood / 2.0)
