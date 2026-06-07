class_name ShopEraWeightConfig
extends Resource

## ShopEraWeightConfig 只保存商店按时代选卡的静态权重参数。
## 它不读取卡池，不生成卡牌，也不决定最终商品。


@export var weight_current_era: float = 0.85
@export var weight_previous_era: float = 0.05
@export var weight_next_era: float = 0.09
@export var weight_next_next_era: float = 0.01
