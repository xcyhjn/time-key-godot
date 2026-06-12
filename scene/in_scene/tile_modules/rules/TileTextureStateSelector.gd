extends RefCounted

## TileTextureStateSelector 只负责根据调用方传入的目标贴图组和当前贴图，判断是否需要切换贴图。
## 它不修改 Sprite2D、不读取场景树、不加载资源，也不处理死亡、血量、状态组件或子类贴图选择策略。


func should_switch_texture(current_texture: Texture2D, allowed_textures: Array[Texture2D]) -> bool:
	if allowed_textures.is_empty():
		return false
	return not allowed_textures.has(current_texture)
