extends landform

var damage_token = 0.3

func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	if location_in == Vector2i(-100, -100):
		return
	willing_pool = {Only_will : Only_Done}
	
	super._init(
	"tower", 
	["res://image/enermy/tower/tower.png"], 
	["res://image/enermy/background/broken_trees.png"], 
	{"chance": 0.5},
	location_in,
	false,
	battle_in)
	Attitude = Attitude_Pool.Middle
	# 设置时间占位形状为1x2
	
func Only_will(tile_info : Dictionary):
	pass

func Only_Done(tile_info : Dictionary):
	take_damage(Max_Blood * 0.3)

func die() -> void:
	State_Main = Main_State_Pool.Broken
	
	damage_rate = 1.0
	
	# 【修复 3】既然已经用 tex_toggle() 切换了贴图，就不需要再通知 HexMap 重新生成整个视觉了
	# 注释掉下面这两行，彻底切断死循环路径
	# if owner_battle and owner_battle.has_method("add_landform_visual_at"):
	# 	owner_battle.add_landform_visual_at(location)
	
	# 【修复 4】删掉 free()！！！
	# 地貌变成“废墟”后，它依然是一个占据格子的实体，只是贴图变了。
	# 如果直接 free() 销毁内存，HexMap 去查这个格子时就会导致 Null 空指针崩溃！
	# free()

	# Broken 虽然不 queue_free，但它的战斗行为/敌人意图应当视作已失效。
	# 这里主动通知 HexMap 触发一次地形拓扑/意图重判。
	if is_instance_valid(owner_battle) and owner_battle.has_signal("tile_topology_changed"):
		owner_battle.tile_topology_changed.emit()
	call_deferred("free")
	HealthBar_free.emit()
	
func Behavior(Step, info_in, Other, beha, rng):
	# 如果村庄已经变成废墟，则不再执行任何行为
	if State_Main == Main_State_Pool.Broken:
		return
	print(step)
	if beha != -1:
		willing = beha
	if will:
		willing_pool.values()[willing].call(info_in)
		will = false
		if willing < willing_pool.size() - 1:
			willing += 1
		else:
			willing = 0
		print(willing_pool.values()[willing])
	else:
		willing_pool.keys()[willing].call(info_in)
		will = true
		print(willing_pool.keys()[willing])

func damaged_tex_picker():
	pass
