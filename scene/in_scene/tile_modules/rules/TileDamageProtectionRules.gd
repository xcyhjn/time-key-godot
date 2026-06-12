extends RefCounted

## TileDamageProtectionRules 只负责判断 Tile 受击时 protected 副状态是否吸收伤害。
## 它不扣血、不发信号、不播放受击表现，也不处理死亡、贴图切换或子类 locked 规则。


func resolve_damage(current_state_vice: int, protected_flag: int) -> Dictionary:
	var is_protected: bool = (current_state_vice & protected_flag) != 0
	if is_protected:
		return {
			"absorbed": true,
			"state_vice": current_state_vice & ~protected_flag
		}
	return {
		"absorbed": false,
		"state_vice": current_state_vice
	}
