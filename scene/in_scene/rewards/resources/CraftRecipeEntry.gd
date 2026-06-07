class_name CraftRecipeEntry
extends Resource

## CraftRecipeEntry 只记录一条合成配方的静态数据。
## 它不检查卡牌是否存在，不创建卡牌，也不处理合成页状态。


@export var card_a_id: String = ""
@export var card_b_id: String = ""
@export var result_card_id: String = ""
