# Unity 3D 迁移当前状态

> 状态：Wave 02B2A 已完成并推送；下一切片为 Wave 02B2B
> 负责人：主智能体
> 最后验证日期：2026-08-01
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。Wave 02B2A 已完成七卡 typed schema、七张原卡面接入，以及 `lighting` + `earthquake` 两卡垂直切片；局外 Godot 内容没有改动。

## 当前切片

Slice 01、Wave 02A 和 Wave 02B1 的既有契约继续成立。七张真实 fixture 现在统一解析为 `Damage/Elevation/Recover/Built/Poison/Clear` typed effects，保留 `FrontImage`、range、普通 shape 与 clear mask；官方 Newtonsoft JSON 包只用于 `JsonUtility` 无法可靠完成的 token 类型校验。

两卡手牌使用 JSON `front_image` 实际加载原 `lighting/earthquake` 卡面。`earthquake` 保存地图目标和两格时间轴 shape，结算时对当前存在的中心加六邻格各执行 `+2`：七个有效柱均从一层变为三层，每层是独立 mesh/renderer/collider，local Y 间距严格为 `0.32`；真实顶面和 occupant anchor 均上移 `0.64`，升高后 yaw 0/90/180/270 仍能选择同一格。

终验结果：Unity EditMode `67/67`、PlayMode `25/25`；Cards 子集 `9/9`、Terrain 子集 `3/3`；Harness 生成 11 张集成截图并成功构建 Windows Player；实际 Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。人工检查覆盖 1920×1080、1280×720、2560×1080 与四向镜头，没有发现卡面/时间轴裁切、关键 UI 遮挡、范围漂移、浮空 occupant、拉伸 mesh 或残留高亮。

当前画面仍是技术垂直切片，不是最终战斗 UI。下一单元收紧为 Wave 02B2B：只实现 `recover` 的 MaxHP 钳制、`poison` 的可累加层数，以及 `tower/built` 在空地创建一个 HP 100 occupant；Tower 自损、poison tick/传播、clear 和敌人完整行动继续留在后续波次。

Git 状态：Wave 02B2A 功能检查点 `cdb09ab` 已于 2026-08-01 推送至 `origin/unity_7.31`，本状态账本随后的文档检查点也已推送。MIG-012 的 TLS 校验警告仍保留，未修改用户级 Git/GCM 配置。

下一位接手 AI 应完整读取并执行 `00-bootstrap/NEXT_STAGE_EFFECTS_B_PROMPT.md` 的“主 Prompt”；不得重跑已关闭的 Wave 00/01/02A/02B1/02B2A。

## 分支与工作区保护

- 当前集成分支：`unity_7.31`，跟踪 `origin/unity_7.31`。
- 用户原有四个未提交文件保持未暂存：`default_bus_layout.tres`、默认合成配方资源、`color_BG.gdshader`、`game_over.gdshader`。
- 主智能体只精确暂存 Wave 02B2A 的迁移文档、Unity 代码/素材/测试与结构化证据；原始日志、Library 和 build 不入库。

## 用户决策

当前实现没有产品或环境决策阻塞。用户已决定先完成局内战斗，局外保持原状。旧 Godot CFG 是否兼容、素材发布授权和最终平台在相关波次进入前再决策。
