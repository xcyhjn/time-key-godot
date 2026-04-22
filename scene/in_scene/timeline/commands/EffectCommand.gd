# ==========================================
# 脚本名称: EffectCommand.gd
# 功能概述: 效果命令的抽象基类，规范化所有战斗交互行为（伤害、治疗、Buff等）。
# ------------------------------------------
# 【数据接收】
# - 来源: EffectProcessor 在解析卡牌/敌人行为时直接赋值。
# - 内容: source (释放者节点), target_tile (目标地块节点), hex_map (战场地图引用)。
# ------------------------------------------
# 【数据处理】
# - 逻辑: 仅提供基础数据结构和标准的执行接口 execute()。
# ------------------------------------------
# 【数据发送】
# - 目标: 提供给继承它的具体命令类（如 DamageCommand）。
# - 时机: 当具体的命令子类在 EffectProcessor 的队列中被调度时。
# ==========================================
class_name EffectCommand
extends RefCounted

var source: Node
# ★ 核心改动：将其变为数组，接收 AOE 范围内的所有地块
var target_tiles: Array[Area2D] = []
var hex_map: Node2D # 指向 battle 实例

# 虚函数，所有具体效果必须实现
func execute(tree: SceneTree) -> void:
	pass
