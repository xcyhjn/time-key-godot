extends landform

func _init(location_in : Vector2i,battle_in):
	damage_rate = 0.5
	Max_Blood = 100
	if location_in == Vector2i(-100, -100):
		return
	var name_buffer = "snowpeak" if battle_in.map_data[location_in]["height"] > 5 else "forest"
	
	super._init(
	name_buffer, 
	["res://image/enermy/background/snowpeak.png",
	 "res://image/enermy/background/forest1.png",
	 "res://image/enermy/background/forest0.png"], 
	["res://image/enermy/background/broken_trees.png"], 
	{"chance": 0.5},
	location_in,
	false,
	battle_in)
	Attitude = Attitude_Pool.Middle
	# 设置时间占位形状为1x2
	set_timeline_shape("011")
		
func _add_landform_sprite(parent: Node2D, coord: Vector2, height: int, current_step_h: float, tile_scale : float) -> void:
	var tex: Texture2D = null
	if State_Main == Main_State_Pool.Broken:
		if landform_damaged_tex != null and not landform_damaged_tex.is_empty():
			tex = landform_damaged_tex[0]

	if tex == null:
		if landform_tex != null and not landform_tex.is_empty():
			tex = landform_tex[0] if height > 6 else landform_tex[1]

	if tex == null:
		return

	var lf_sprite = Sprite2D.new()
	# 使用唯一名称避免命名冲突
	lf_sprite.name = "LandformSprite_%s_%s" % [coord.x, coord.y]
	lf_sprite.texture = tex
	
	# ★ 致命漏洞修复1：必须把生成的 Sprite 赋值给 self.tex
	# 否则受到伤害变成废墟时，系统找不到节点换贴图！
	self.tex = lf_sprite
	
	lf_sprite.centered = false
	lf_sprite.scale = Vector2(tile_scale, tile_scale)
	# 移除硬编码的z_index，让父容器控制渲染层级
	# lf_sprite.z_index = 999
	
	var top_y = -((height - 1) * current_step_h)
	var tex_size = tex.get_size()

	# 额外上抬
	var extra_lift = 70.0 * tile_scale

	lf_sprite.position = Vector2(
		-tex_size.x * 0.5 * tile_scale,
		top_y - tex_size.y * tile_scale - extra_lift
	)
	
	var world_position = parent.global_position + lf_sprite.position
	
	# ★ 致命漏洞修复2：必须发射 CreateBar 信号！！
	# 你的原代码里把这一行删掉了，导致中立地形永远不会生成血条！
	owner_battle.emit_signal("CreateBar", self, Attitude, world_position.x, world_position.y)

	parent.add_child(lf_sprite)

func tex_picker():
	return landform_tex[0] if owner_battle.map_data[location]["height"] > 6 else landform_tex[1]
