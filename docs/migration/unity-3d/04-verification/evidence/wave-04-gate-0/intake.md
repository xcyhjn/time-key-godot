# Wave 04 Gate 0 Intake

> 状态：已验证，Gate 0 资料冻结
> 负责人：主智能体
> 最后验证日期：2026-08-04
> 证据来源：`git status --porcelain=v1 -uall`、Unity/Godot/Player 进程查询、Wave 03 Gate D XML/JSON/PNG、音频源文件和 Prompt 规定的现有账本

## 结论

当前分支为 `unity_7.31`，HEAD 为 `150e7f2199213530c590c530717b0986a2dad9d6`，与
`origin/unity_7.31` 的 ahead/behind 为 `0/0`，staged 为 `0`。Wave 03 Overworld Gate A-D
继承证据仍可解析，Gate D 不是 Wave 04 阻塞。进入 Wave 04 的结论为 `GO`（本地迁移验证）；
公开发布仍受 MIG-005 授权项限制，MIG-002/MIG-003 保持透明 no-effect/safe-skip。

## 工作区与所有权

### 进程和锁

Gate 0 首次查询发现 Unity Hub 的四个子进程和 Unity Licensing Client；没有 Unity Editor、
Godot、TimeKey Player 或其他项目写入进程。按用户要求已终止这五个进程，复核结果为空。
`.git/index.lock` 和 `unity/Temp/UnityLockfile` 均不存在。当前 Codex 树只有 `/root`，没有待交回
的子 Agent；主智能体取得本阶段新增路径所有权。

### 继承 dirty/untracked 保护

完整 `git status --porcelain=v1 -uall` 计数为：tracked dirty `145`、untracked `317`、staged `0`。
tracked dirty 根目录计数为 `docs=137`、`unity=3`、`scene=1`、`shaders=2`、`.gitignore=1`、
`default_bus_layout.tres=1`。所有这些都在本阶段前已存在，禁止回退、stash、清理、覆盖、暂存或提交。

untracked 的完整 inventory 已重新查询；`build-player/` 下 300 项是继承构建/运行时文件，保留
但禁止提交；除此之外的 17 项 docs 路径为：

```text
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_SHELL_AND_SCENE_FLOW_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_CONTENT_AND_EXPERIENCE_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_DECOUPLING_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECT_FRAME_STABILITY_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_ERA_CLOCK_ANIMATION_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_ERA_CLOCK_CLOSEOUT_AND_OVERWORLD_GATE_B_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_OVERWORLD_MOVEMENT_AND_UI_THEME_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_PLUGINIZATION_AND_ASSET_TOOLING_PROMPT.md
docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_REMAINING_CARDS_PROMPT.md
docs/migration/unity-3d/01-assessment/overworld-movement-and-ui-theme-gap-analysis.md
docs/migration/unity-3d/01-assessment/pluginization-and-tooling-assessment.md
docs/migration/unity-3d/03-workstreams/agents/prompt-review-pluginization-and-asset-tooling.md
docs/migration/unity-3d/03-workstreams/agents/reports/era-clock-current-progress-sync.md
docs/migration/unity-3d/04-verification/evidence/era-clock-animation-gate-0/fresh-agent-intake.md
docs/migration/unity-3d/06-maintenance/local-artifact-policy.md
docs/migration/unity-3d/06-maintenance/plugin-and-asset-tooling-guide.md
docs/migration/unity-3d/06-maintenance/ui-theme-resource-guide.md
```

本阶段不触碰上述继承路径；Wave 04 证据使用 `evidence/wave-04-gate-{0,a,b,c,d,e}/` 独立根。

## 既有目标文件快照

本阶段可能由主智能体后续集成的既有文件在写入前快照如下。任何变化必须在对应 Gate 报告中重新
记录 SHA-256，并且只能暂存本阶段 hunk：

| 路径 | 字节 | SHA-256 |
| --- | ---: | --- |
| `unity/Assets/_Project/Scenes/Shell/Bootstrap.unity` | 20837 | `157580B498A82C24D463DB44E80F455FF33FA3EB86D8C59EDDC4FC58FDF87A30` |
| `unity/Assets/_Project/Prefabs/Shell/MainMenu.prefab` | 197153 | `1D20D258E3AC736AD0E6B4C64259560AC04ACAADEE7BC4115242900CF19D3AB7` |
| `unity/Assets/_Project/Runtime/Composition/SceneFlow/MainMenuSceneNavigation.cs` | 10477 | `98B96E23DF48EE2A9FCBA40EFE95F6A60027364E44BA3DC339693FDED7EFB7F8` |
| `unity/Assets/_Project/Runtime/Presentation/MainMenu/MainMenuPresenter.cs` | 23475 | `F1A931FA9D2078CCB305FCA533A36C8A09D11387BDE5320F833CBDCA35ECFCED` |
| `unity/Packages/manifest.json` | 1744 | `87470673344599BA64CAACE4CDB49A9077B4EA05161E1A61ADA17BC207226DB3` |
| `unity/Packages/packages-lock.json` | 10719 | `FC6171105BBAAE442AEEDD0844FEEA8FBE1451F67819420964E875A0BCF8501B` |
| `unity/ProjectSettings/ProjectSettings.asset` | 25346 | `49C8B763BBB7B7CDA30BDAD2BF4DEF208B5648C5F701A716CDD9334929247C0D` |
| `unity/ProjectSettings/URPProjectSettings.asset` | 461 | `C317FE22B9488785C0611F0642429922E5D324430D929BFFB41648130F30C218` |

`ProjectSettings.asset` 已有继承的 `ps4Passcode` dirty hunk；`URPProjectSettings.asset` 已有
继承的 material-version/setting-folder dirty hunk。本阶段不得覆盖或提交它们。

## Wave 03 权威证据复核

`docs/migration/unity-3d/04-verification/evidence/overworld-map-gate-d/` 的 XML 解析结果：

| 文件 | total | passed | failed | skipped | inconclusive | SHA-256 |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| `additive-results.xml` | 10 | 10 | 0 | 0 | 0 | `15891F44FD29F3428FA7D89D5E0EE6E9E4EBA92BDB9A3165A8DE92F1EE10FB08` |
| `editmode-full.xml` | 446 | 446 | 0 | 0 | 0 | `2A2047E3B3C90DFA3AC8084BB7116D6757D466245680F7180B5F7E5F56DFA0B6` |
| `playmode-full-d3d12.xml` | 130 | 130 | 0 | 0 | 0 | `D795CD1367AC2E158B6DA3DCE96A7104318B13C1EBE2AB6830DD53793279F338` |

`build-summary.json`、`delivery-summary.json`、`player-phase1-summary.json`、
`player-smoke-summary.json` 和 `visual-evidence-index.json` 全部 `ConvertFrom-Json` 成功。
历史 PNG、JSON、XML 不覆盖；本阶段只新增 Wave 04 evidence root。

## 工程入口与包契约

- Unity Editor：`C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe`，存在。
- Godot CLI：`D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe`，存在。
- `manifest.json`/lockfile 当前已锁定 URP `17.4.0`、uGUI `2.0.0`、Test Framework `1.6.0`、
  Newtonsoft `3.2.2`；`com.unity.modules.audio` 已是内置模块。Wave 04 不引入第三方音频/VFX/教程包。
- 现有程序集为 Domain、Application、Infrastructure、Presentation、Composition、Diagnostics、
  Editor 以及测试程序集；新增 Audio/Feedback 代码只进入现有 Presentation 和现有测试程序集，
  不修改 asmdef 或 Package/ProjectSettings。

## Gate 0 验收与硬阻塞

- [x] 分支、HEAD、远端、staged/dirty/untracked、锁和进程已复核。
- [x] Wave 03 Gate D XML/JSON/PNG/build/Player 证据仍可解析并保留。
- [x] 资产来源/授权 ledger 与四类 typed 表现契约已冻结。
- [x] 最多三个 Agent Prompt 已生成并通过互斥路径审查；主智能体保留共享 Scene/Prefab、设置、Composition、文档、证据和 Git 所有权。
- [x] Gate 0 定向红测：`contract-gap-results.xml` 为 `2 total / 0 passed / 2 failed / 0 skipped / 0 inconclusive`，SHA-256 `66F7AA6DEE421DA7731BD44EB289AF7532B93CA3CEC5A8D59FEE20B85372F116`。两条失败分别为 Audio foundation 与 Feedback/Tutorial typed runtime 缺口；Unity 日志记录 `Exiting with code 2 (Failed)`，随后写入进程复核为空。原始 `.log` 按本地 artifact 策略排除，不提交。

Gate 0 非硬阻塞：MIG-002 空 enemy command、MIG-003 Event safe-skip、MIG-005 公开授权未闭合、
MIG-020 无 draw-call recorder、旧 CFG 兼容和远端 TLS/push。只有分支/写入锁/权威证据失效、明确
授权禁止本地验证或 Unity/Godot/磁盘故障才可停止。

## 下一动作

按 Prompt 依赖顺序启动 Audio Agent、Combat Feedback Agent 和只读 Tutorial Parity Audit Agent；每波先
审查回报、集成和验证，再建立精确 Git 检查点。
