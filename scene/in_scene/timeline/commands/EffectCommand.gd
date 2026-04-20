class_name EffectCommand
extends RefCounted

var source: Node
var target_tile: Area2D
var hex_map: Node2D # 指向 battle 实例

# 虚函数，所有具体效果必须实现
func execute(tree: SceneTree) -> void:
	pass
