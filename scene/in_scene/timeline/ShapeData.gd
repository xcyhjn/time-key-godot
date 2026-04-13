# ShapeData.gd (Resource)
class_name ActionShape
extends Resource

# 用坐标偏移量表示形状，例如 L型：[Vector2i(0,0), Vector2i(0,1), Vector2i(1,1)]
@export var coords: Array[Vector2i] = []
@export var color: Color = Color.WHITE
@export var action_name: String = "Default Action"
