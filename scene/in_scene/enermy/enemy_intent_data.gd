class_name EnemyIntentData
extends RefCounted

# ==========================================
# 脚本名称: enemy_intent_data.gd
# 功能概述:
# - 这是“敌人意图展示系统”的核心数据模型文件。
# - 它不负责显示，也不负责结算，只负责承载一份“已经被解析好的敌人意图”。
# - 地图 hover、时间轴 hover、tooltip 显示、目标范围高亮、意图失效清理，都应该围绕这份数据进行。
#
# 数据来源:
# - 来源于单个敌方/中立/友方单位(例如 village、祭坛等)提供的意图接口：
#   - get_intent_description()
#   - get_intent_effect_range()
#   - get_intent_target_center_coord()
#   - can_generate_intent()
#   - get_intent_invalid_reason()
#   - get_intent_target_affiliation()
#   - does_intent_include_self()
# - 同时也会从 TimelineAction 中补充时间轴占位信息。
#
# 本文件主要做什么:
# - 统一保存一份意图在“地图侧”和“时间轴侧”都需要用到的数据。
# - 为展示控制器提供稳定的数据结构，避免地图系统与时间轴系统重复计算。
#
# 本文件不做什么:
# - 不做地图高亮
# - 不做时间轴方块放大/波纹
# - 不做 tooltip 实际显示
# - 不做敌人 AI 结算
#
# 数据将传递到哪里:
# - 传递给 EnemyIntentResolver 做进一步补全
# - 传递给未来的 EnemyIntentPresentationController 做地图/时间轴双向联动显示
# ==========================================


## source_node
## 作用:
## - 意图的“施法者 / 发出者”节点引用。
## - 地图 hover 时，要高亮谁、tooltip 锚点贴在谁右边，都靠它。
## - 回合中如果这个节点被摧毁、失效、移动或换目标，意图也要随之重新判定。
##
## 怎么用:
## - 地图表现层用它来高亮敌人本体。
## - tooltip 系统用它来计算提示框位置。
## - 失效重判时，用它回查敌人当前是否仍然存在。
##
## 范例:
## - 一个 village 实例
## - 一个祭坛 landform 实例
var source_node: Node = null


## source_coord
## 作用:
## - 施法者所在的六边形坐标(Vector2i)。
## - 主要用于地图层定位、范围换算、调试日志以及“意图是否覆盖自身”的判定。
##
## 怎么用:
## - 地图高亮系统可用它快速找到对应 stack。
## - 若 target_center_coord 为空，也可以用它作为兜底参考。
##
## 范例:
## - Vector2i(2, -1)
var source_coord: Vector2i = Vector2i.ZERO


## description
## 作用:
## - 敌人意图的详细文本描述，直接供 tooltip 使用。
## - 与 TimelineAction.action_data["效果"] 的文字含义保持一致，但这里要求更详细。
##
## 怎么用:
## - hover 地图敌人时显示
## - hover 时间轴敌人意图时也显示
## - 若意图无效，正常显示本描述，并在下方追加“无可用目标”
##
## 范例:
## - "扩张1：向相邻空地扩建1格"
## - "祭祀：为自身与周围6格回复10点生命"
var description: String = ""


## target_center_coord
## 作用:
## - 这次意图的“目标中心格”。
## - 之后所有 effect_range 偏移，都围绕它做展开。
## - 它可能为 null，代表当前没有合法目标中心。
##
## 怎么用:
## - 用作 effect_range 的展开原点。
## - 地图层可把它视作“意图重点落点”。
##
## 范例:
## - Vector2i(3, -1)
## - null（没有目标）
var target_center_coord: Variant = null


## target_coords
## 作用:
## - 最终被 effect_range 解析出来的、真正受影响的地图格子列表。
## - 这是地图波纹高亮的核心数据来源。
##
## 怎么用:
## - 地图高亮系统对其中所有坐标施加“目标波纹高亮”。
## - 若为空，则表示当前没有实际作用范围。
##
## 范例:
## - [Vector2i(3, -1)]
## - [Vector2i(2, -1), Vector2i(3, -1), Vector2i(3, -2), ...]
var target_coords: Array[Vector2i] = []


## is_valid
## 作用:
## - 表示这份意图在“当前时刻”是否合法、可执行。
## - 合法与否不影响地图 hover 显示，但会影响时间轴意图是否生成/是否需要消失。
##
## 怎么用:
## - true: 正常生成时间轴意图，并显示正常颜色
## - false: 时间轴不生成，或已有时间轴意图进入“暗淡后消失”
##
## 范例:
## - true: 村庄旁边还有可扩张空地
## - false: 周围所有格子都被占了
var is_valid: bool = false


## invalid_reason
## 作用:
## - 当 is_valid == false 时，用来说明“为什么无效”的详细原因。
## - tooltip 下方追加红字时，优先显示它。
##
## 怎么用:
## - tooltip 主文案显示 description
## - tooltip 第二行红字显示 invalid_reason
##
## 范例:
## - "无可用目标"
## - "高度条件不满足"
## - "目标地块已被占据"
var invalid_reason: String = ""


## target_affiliation
## 作用:
## - 目标阵营标签，为未来支持敌方/中立/友方目标留下扩展口。
## - 当前版本可先使用字符串，后续再升级成枚举。
##
## 怎么用:
## - 地图展示层可根据阵营切换不同颜色方案。
## - 未来可以让中立目标、友方目标用不同视觉语义。
##
## 范例:
## - "enemy"
## - "neutral"
## - "ally"
var target_affiliation: String = "enemy"


## includes_self
## 作用:
## - 表示这次意图的作用范围是否覆盖施法者自己。
## - 这是“施法者高亮应该是白色还是红色”的关键判断条件。
##
## 怎么用:
## - false: 施法者白色高亮，目标地块红色波纹
## - true: 施法者红色高亮，目标地块仍然是红色波纹，但两者样式不同
##
## 范例:
## - 村庄扩张: false
## - 祭坛自身+周围回血: true
var includes_self: bool = false


## timeline_action
## 作用:
## - 与这份地图意图绑定的 TimelineAction 引用。
## - 时间轴 hover 到 action 时，可反查回地图侧的同一份意图数据。
##
## 怎么用:
## - 时间轴格子 hover 事件 -> 找到 timeline_action -> 找到 EnemyIntentData -> 地图联动显示
## - 若意图失效，可通过它定位时间轴上的占位格
##
## 范例:
## - 一个 type == ENEMY 的 TimelineAction
## - null（当前没有生成到时间轴）
var timeline_action: TimelineAction = null


## timeline_cells
## 作用:
## - 这份意图在时间轴上占据的所有绝对格子。
## - hover 时，要求“整组格子一起放大/一起上 shader”，就靠这组坐标来找 UI 方格。
##
## 怎么用:
## - 时间轴展示层根据这些格子统一加红色波纹与轻微放大
## - 意图失效时，根据这些格子播放“变暗后消失”
##
## 范例:
## - [Vector2i(3, 0), Vector2i(3, 1)]
## - [Vector2i(7, 2)]
var timeline_cells: Array[Vector2i] = []


## effect_range_raw
## 作用:
## - 保存敌人原始的 effect_range 配置。
## - 这不是最终作用格，而是“解析前的原始输入”，便于调试和复用卡牌解析逻辑。
##
## 怎么用:
## - EnemyIntentResolver 根据它复用卡牌的范围解析方式展开 target_coords
##
## 范例:
## - 1
## - ["0,0", "1,0", "0,1"]
var effect_range_raw: Variant = 0


## debug_label
## 作用:
## - 纯调试字符串，便于日志中一眼识别这份意图是谁、是否有效、目标是谁。
## - 不参与正式逻辑。
##
## 怎么用:
## - 打印日志或调试窗口
##
## 范例:
## - "[Village @(2,-1)] valid -> center=(3,-1)"
var debug_label: String = ""


func build_debug_label() -> void:
	var source_name = source_node.name if is_instance_valid(source_node) else "UnknownSource"
	var center_text = str(target_center_coord) if target_center_coord != null else "null"
	debug_label = "[%s @%s] %s -> center=%s" % [
		source_name,
		str(source_coord),
		"valid" if is_valid else "invalid",
		center_text
	]
