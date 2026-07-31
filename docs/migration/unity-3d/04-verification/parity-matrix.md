# Godot / Unity 等价矩阵

> 状态：Slice 01 + Wave 02A + Wave 02B1 已验证
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：Godot 实跑截图/日志、玩法等价契约、Unity 测试与截图待生成

| 行为 | Godot 基线 | Unity 当前实现 | 判定 | 证据 |
| --- | --- | --- | --- | --- |
| 启动/场景加载 | 主场景 headless 退出码 0；GUI 可进入战斗 | batchmode 导入、scene validate、Player build/启动均为 0 | 等价 | Godot logs；`evidence/unity-slice-01/verification-summary.md` |
| 六边形战场 | 2D axial 地图，目标可选择 | 3D flat-top axial XZ，19 格且四向可选 | 允许差异 | `evidence/unity-slice-02-board/board-yaw-*.png` |
| 高度 | 2D 绘制偏移表现高度 | 每层 0.32 的独立 FBX mesh 与 collider 实体堆叠 | 允许差异 | PlayMode + `hex-tile-agent/two-layer-stack.png` |
| 镜头检视 | 局内主要缩放，无 3D 环绕 | 透视 360° 旋转、俯仰、缩放、平移 | 允许差异 | 0/90/180/270 PNG + PlayMode |
| 卡牌数据 | `lighting.json`，稳定 ID `lighting` | 原样 fixture，SHA-256 相同 | 等价 | hash + EditMode XML |
| 卡牌素材 | `lighting.png` 与 `behide.png` 原卡面/牌背 | 原文件逐字节复制；运行时保持 `1135×1590` 卡面比例 | 等价 | `evidence/wave-02b-card-art-agent/manifest.md` + PlayMode |
| 手牌状态 | 底部卡牌 idle/hover/selected；右键取消 | 原 `125×175` 语义、hover 抬升/放大、selected 抬升 `80 px`/`1.5x`、右键/Escape 取消 | 等价 | `evidence/unity-slice-02b1/card-*.png` + PlayMode |
| 交互顺序 | 选卡→世界目标→时间轴格 | 相同语义，可用点击/公共控制器路径完成 | 等价 | PlayMode XML + 截图 |
| 世界范围预览 | `effect_range` axial offset 投影到地图 | 相同 offset 投影到 3D 实体格；四向镜头不改变坐标集合，缺失格不生成幽灵格 | 等价 | `target-range-yaw-*.png` + PlayMode |
| 时间轴合法性预览 | hover/拖放时显示可放置性，确认后才占格 | `CanPlace`/preview 无副作用；valid 绿、invalid 红，Commit 后才写入 | 等价 | `timeline-valid/invalid-1920x1080.png` + EditMode/PlayMode |
| 时间轴 | 12×3，按列再按行 | 相同，冲突/边界受测 | 等价 | EditMode XML |
| 雷击效果 | damage 100 | 10 HP 目标钳制为 0 | 等价 | snapshot 测试 + resolved PNG |
| 敌人意图 | 可见占位；当前命令解析为空，建筑在时间轴后行动 | 固定意图可见，并在玩家 action 后记录“已处理”，不新增伤害 | 允许差异 | contract + snapshot 测试 |
| 回合变化 | 空结束回合 1/8→2/8，时间币变化 | 非首切片目标 | 未实现 | `battle-turn-2.png` |
| 胜负/奖励/返回 | 可进入奖励与局外返回路径 | 非首切片目标 | 未实现 | source map |
| 视觉 | 2D 像素/UI；1920 基线且 1280 菜单可读 | Blender 低多边形地块 + 原建筑 billboard + uGUI，1920 与 1280 均可读 | 允许差异 | 5 张 Unity PNG + 3 张 Blender PNG |

判定词只使用：`等价`、`允许差异`、`未实现`、`已知缺陷`、`待验证`。Slice 01、Wave 02A 和 Wave 02B1 范围已关闭；七卡效果、敌方/建筑行动与局内闭环留给 02B2-02B4，局外保持原状。
