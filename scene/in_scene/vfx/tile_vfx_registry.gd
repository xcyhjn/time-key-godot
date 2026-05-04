# 功能: 地块特效注册表资源，统一管理特效名称到特效场景的映射。
# 核心逻辑: VFXManager 通过 get_scene() 按名称取 PackedScene；新增动画优先改 .tres 注册表，不需要新增播放代码。
class_name TileVFXRegistry
extends Resource

@export var default_profile: TileVFXProfile = preload("res://scene/in_scene/vfx/default_tile_vfx_profile.tres")
@export var tile_vfx_scenes: Dictionary = {}


func get_scene(vfx_name: StringName) -> PackedScene:
	var scene: Variant = tile_vfx_scenes.get(vfx_name, null)
	if scene == null:
		scene = tile_vfx_scenes.get(String(vfx_name), null)
	return scene as PackedScene
