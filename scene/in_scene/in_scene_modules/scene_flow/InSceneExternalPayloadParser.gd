class_name InSceneExternalPayloadParser
extends RefCounted


## InSceneExternalPayloadParser 只解析局外传入的战斗 payload 文本。
## 当前兼容格式为："battle_normal <map_seed>"、"battle_elite <map_seed>"、"boss_stage <map_seed>"。


func parse(payload: Variant) -> Dictionary:
	var result: Dictionary = {
		"raw_payload": payload,
		"payload_text": "",
		"battle_tag": "",
		"map_seed": "",
		"has_payload": false,
	}

	if payload == null:
		return result

	var payload_text: String = str(payload).strip_edges()
	if payload_text == "":
		return result

	result["payload_text"] = payload_text
	result["has_payload"] = true

	var first_space_index: int = payload_text.find(" ")
	if first_space_index == -1:
		result["battle_tag"] = payload_text
	else:
		result["battle_tag"] = payload_text.substr(0, first_space_index)
		result["map_seed"] = payload_text.substr(first_space_index + 1).strip_edges()

	return result
