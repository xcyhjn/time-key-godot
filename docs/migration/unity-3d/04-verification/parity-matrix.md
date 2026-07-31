# Godot / Unity 等价矩阵

> 状态：Godot 基线已完成，Unity Slice 01 待验证
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 实跑截图/日志、玩法等价契约、Unity 测试与截图待生成

| 行为 | Godot 基线 | Unity Slice 01 目标 | 判定 | 证据 |
| --- | --- | --- | --- | --- |
| 启动/场景加载 | 主场景 headless 退出码 0；GUI 可进入战斗 | batchmode 导入、scene validate、Player build | 待验证 | `evidence/godot-baseline/*.log` |
| 六边形战场 | 2D axial 地图，目标可选择 | 3D flat-top axial XZ，固定 7+ 格 | 待验证 | Unity 双视口截图待生成 |
| 卡牌数据 | `lighting.json`，稳定 ID `lighting` | 原样 fixture，SHA-256 相同 | 待验证 | hash + EditMode 待生成 |
| 交互顺序 | 选卡→世界目标→时间轴格 | 相同语义，可用点击完成 | 待验证 | PlayMode + 截图待生成 |
| 时间轴 | 12×3，按列再按行 | 相同，冲突/边界受测 | 待验证 | EditMode 待生成 |
| 雷击效果 | damage 100 | 10 HP 目标钳制为 0 | 待验证 | snapshot 待生成 |
| 敌人意图 | 可见占位；当前命令解析为空，建筑在时间轴后行动 | 显示固定意图并在玩家 action 后记录已处理 | 允许差异，待验证 | contract + snapshot 待生成 |
| 回合变化 | 空结束回合 1/8→2/8，时间币变化 | 非首切片目标 | 未实现 | `battle-turn-2.png` |
| 胜负/奖励/返回 | 可进入奖励与局外返回路径 | 非首切片目标 | 未实现 | source map |
| 视觉 | 2D 像素/UI；1920 基线且 1280 菜单可读 | 程序化 3D 白盒 + uGUI，两个视口可读 | 允许差异，待验证 | 双视口截图待生成 |

判定词只使用：`等价`、`允许差异`、`未实现`、`已知缺陷`、`待验证`。Unity 证据完成前不得把 Slice 01 标为完成。
