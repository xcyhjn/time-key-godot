class_name ShopPricingConfig
extends Resource

## ShopPricingConfig 只保存商店经济定价的静态参数。
## 它不计算价格，不消费时间币，也不生成或购买商品。


@export var base_price: int = 50
@export var slot_price_step: int = 5
@export var price_increment: int = 25
@export var refresh_base_cost: int = 50
@export var upgrade_base_cost: int = 100
