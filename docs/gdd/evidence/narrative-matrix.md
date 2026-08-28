# 叙事矩阵

| 节点 | DESIGN_INTENT | 当前资源/入口 | 自然可见性 | 状态 |
| --- | --- | --- | --- | --- |
| 表层身份 | 外星先锋征服人类、追逐时之钥 | 本轮 UI 无完整台词 | 未见 | DESIGN_REFERENCE |
| 教学关 | 未来人类、启动时之钥、代行者被冻结 | 教程场景、Dialogic | 默认入口关闭；定向可见固定地图 | TARGETED_ONLY/BLOCKED |
| 核战争回溯 | 人类内斗核战，时代回原始 | 旧策划案 | 未见 | DESIGN_REFERENCE |
| 接纳 | 接受神的存在，获得特殊牌 | 旧策划案 | 未见 | DESIGN_REFERENCE |
| 反思 | 跳过神牌，获得反思点并觉醒 | 旧策划案 | 未见 | DESIGN_REFERENCE |
| 普通结局 | 三 Boss、金钥摧毁、银钥冻结、轮回 | GameWin 场景/清档逻辑 | 未到达 | UNVERIFIED |
| 真结局 | 回原点、神使、最终弑神 Boss | 旧策划案 | 未到达 | DESIGN_REFERENCE/PROPOSED |

## PROPOSED 状态

持久化 `tutorial_seen`、`reflection_points`、`awakened`、`ordinary_bosses_defeated`、`return_origin_unlocked`、`true_boss_defeated`。剧情/奖励提交必须携带唯一事件 ID，防止重复加载重复触发。

