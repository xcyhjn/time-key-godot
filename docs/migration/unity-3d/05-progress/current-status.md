# Unity 3D 迁移当前状态

> 状态：Wave 02B1 已完成并推送；Wave 02B2A Prompt 已审查待执行
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。Wave 02B1 已把原版 `lighting` 卡牌从素材、手牌状态、3D 目标范围、时间轴合法性预览、确认放置接到既有 360° 棋盘与结算闭环；局外 Godot 内容没有改动。

## 当前切片

Slice 01 和 Wave 02A 的既有契约继续成立。Wave 02B1 新增：原 `lighting.png`/`behide.png`、单卡底部手牌 idle/hover/selected/cancel、`TimelineGrid.CanPlace`、`CardPlaySession`、3D axial 范围投影、时间轴 valid/invalid 预览、确认放置和原有 seed 731 结算。

终验结果：Unity EditMode `31/31`、PlayMode `15/15`；Harness 生成 12 张集成截图并成功构建 Windows Player；实际 Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。人工检查覆盖 1920×1080、1280×720、2560×1080 与 0/90/180/270 四向镜头，没有发现卡面变形、裁切、关键 UI 遮挡或预览漂移。

当前画面仍是技术垂直切片，不是最终战斗 UI：顶部英文状态栏、单卡手牌、固定单目标和程序化时间轴外观仍需后续按 Godot 原项目逐项替换。现阶段的有效结论是交互、坐标、真实堆叠、输入隔离和原卡面接入已经可复用，而不是视觉迁移已经完成。

范围决策不变：接下来只扩展局内卡牌、敌人、建筑和胜负；局外流程保持 Godot 现状。实际代码审查发现一次完成七类效果会把 `clear`、普通 action 和领域棋盘状态耦合过早，因此下一单元收紧为 Wave 02B2A：七张真实 fixture/schema 全解析，但只把 `lighting` 和 `earthquake` 接入可玩闭环。`earthquake +2` 必须让每个目标柱真实增加两个 `0.32` block，并同步 collider、顶面 bounds、occupant anchor 和四向选择。

Git 状态：2026-08-01 已成功把 `5c6492a` 与 `c5ec5bc` 推送至 `origin/unity_7.31`，并创建 Wave 02B2A 交接检查点 `7a2ed96`。本轮结束前应把交接检查点和本状态记录一起推送；MIG-012 的 TLS 校验警告仍保留，未修改用户级 Git/GCM 配置。

下一位接手 AI 应直接进入仓库，完整读取并执行 `00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md` 的“主 Prompt”。该 Prompt 已包含五份所有权互斥的 Agent 任务、三道依赖门禁、测试/截图/build/Git 完成定义；不得重跑已关闭的 Wave 00/01/02A/02B1。

## 分支与工作区保护

- 当前集成分支：`unity_7.31`，跟踪 `origin/unity_7.31`。
- 用户原有四个未提交文件保持未暂存：`default_bus_layout.tres`、默认合成配方资源、`color_BG.gdshader`、`game_over.gdshader`。
- 主智能体只精确暂存 Wave 02B1 的迁移文档、Unity 代码/素材/测试与结构化证据；原始日志、Library 和 build 不入库。

## 用户决策

当前实现没有产品或环境决策阻塞。用户已决定先完成局内战斗，局外保持原状。旧 Godot CFG 是否兼容、素材发布授权和最终平台在相关波次进入前再决策。
