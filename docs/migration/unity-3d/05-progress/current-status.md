# Unity 3D 迁移当前状态

> 状态：Wave 02B1 已完成；Wave 02B2 最小效果切片待执行
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：评估门禁、共享契约、Godot 基线、Git 状态

## 结论

迁移结论保持 `CONDITIONAL GO`，总体难度 4/5。Wave 02B1 已把原版 `lighting` 卡牌从素材、手牌状态、3D 目标范围、时间轴合法性预览、确认放置接到既有 360° 棋盘与结算闭环；局外 Godot 内容没有改动。

## 当前切片

Slice 01 和 Wave 02A 的既有契约继续成立。Wave 02B1 新增：原 `lighting.png`/`behide.png`、单卡底部手牌 idle/hover/selected/cancel、`TimelineGrid.CanPlace`、`CardPlaySession`、3D axial 范围投影、时间轴 valid/invalid 预览、确认放置和原有 seed 731 结算。

终验结果：Unity EditMode `31/31`、PlayMode `15/15`；Harness 生成 12 张集成截图并成功构建 Windows Player；实际 Player 退出码 0 且日志含 `TIMEKEY_PLAYER_SMOKE_PASS`。人工检查覆盖 1920×1080、1280×720、2560×1080 与 0/90/180/270 四向镜头，没有发现卡面变形、裁切、关键 UI 遮挡或预览漂移。

范围决策不变：接下来只扩展局内卡牌、敌人、建筑和胜负；局外流程保持 Godot 现状。Wave 02B2 先做七张真实 fixture 的统一 schema 与每类效果的最小确定性 Domain 闭环，不提前接敌方/建筑行动或局外场景。

远端状态：Wave 02B1 实现检查点为 `5c6492a`。连续三次 push 因本机无法连接 GitHub 443 失败；没有 rebase、amend 或其他历史改写。网络恢复后先执行 `git push origin unity_7.31`。

随后完整读取并执行 `00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`，先冻结七卡 schema/效果验收，再按该文档的互斥所有权启动下一波 Agent；不得重跑已关闭的 Wave 00/01/02A/02B1。

```powershell
Get-Content -Raw 'docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md' |
    codex exec --cd 'D:\godot\时之钥\时之钥' --add-dir 'D:\timekey-unity-731' --sandbox workspace-write --ask-for-approval never -
```

## 分支与工作区保护

- 当前集成分支：`unity_7.31`，跟踪 `origin/unity_7.31`。
- 用户原有四个未提交文件保持未暂存：`default_bus_layout.tres`、默认合成配方资源、`color_BG.gdshader`、`game_over.gdshader`。
- 主智能体只精确暂存 Wave 02B1 的迁移文档、Unity 代码/素材/测试与结构化证据；原始日志、Library 和 build 不入库。

## 用户决策

当前实现没有产品或环境决策阻塞。用户已决定先完成局内战斗，局外保持原状。旧 Godot CFG 是否兼容、素材发布授权和最终平台在相关波次进入前再决策。
