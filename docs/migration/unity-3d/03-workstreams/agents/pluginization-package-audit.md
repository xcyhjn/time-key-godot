# Agent Prompt: pluginization-package-audit

## 单一目标

审计当前 `manifest.json`、`packages-lock.json`、全部 asmdef，并只用 Unity 官方文档评估 UI Toolkit、Input System、Cinemachine、Addressables、Timeline 的版本、许可证、依赖、收益、性能/视觉影响和完整移除成本；不安装候选。

## 独占写入路径

- `docs/migration/unity-3d/03-workstreams/agents/reports/pluginization-package-audit.md`

## 禁止路径

- `unity/Packages/**`、全部 asmdef、`unity/ProjectSettings/**`
- `unity/Assets/**`、正式 Scene/Prefab、候选账本、共享文档和最终证据
- Gate 0 intake 和其他 Agent 报告

## 输入

- `unity/Packages/manifest.json`、`unity/Packages/packages-lock.json`
- `unity/Assets/_Project/**/*.asmdef`
- Unity 版本 `6000.4.10f1`
- Unity 6000.0/Unity 6 官方 Manual、Package 文档、Unity Companion License

## 输出

报告逐候选记录：官方 URL、检索日期、作者/发布者、Unity 6 官方版本、许可证、商用/再分发条件、依赖、二进制风险、SHA-256（未下载写 `N/A - not downloaded`）、导入路径、真实收益、性能/视觉影响、加入步骤、移除/回滚步骤、`ACCEPT/TRIAL/DEFER/REJECT`。另列当前 direct/transitive package 与 asmdef 依赖方向。

## 非目标

不下载、不执行、不导入、不改 manifest/lock/asmdef/ProjectSettings，不推荐无具体来源的第三方工具，不把 Timeline 用作玩法提交源，不迁移现有 Input/Camera/uGUI。

## 只读检查命令

```powershell
Get-Content unity/Packages/manifest.json
Get-Content unity/Packages/packages-lock.json
rg --files unity/Assets/_Project -g '*.asmdef'
git diff -- unity/Packages unity/Assets/_Project -- '*.asmdef'
```

## 停止条件

官方来源无法确认则标记 `UNVERIFIED`；需要下载、安装或写独占报告外路径时停止。你不是唯一在工程中工作的 Agent；不得回滚或覆盖他人改动。
