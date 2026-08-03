# Agent Prompt: asset-search-import-audit

## 单一目标

审计 Godot 原素材、Unity 已导入资产、字体、卡面、模型、贴图、音频、Shader、Blender/MCP 产物及授权缺口，并建立不下载外部二进制的插件/资产候选账本。

## 独占写入路径

- `docs/migration/unity-3d/01-assessment/asset-and-plugin-candidate-ledger.md`
- `docs/migration/unity-3d/03-workstreams/agents/reports/asset-search-import-audit.md`

## 禁止路径

- `unity/Assets/**`、`unity/Packages/**`、全部 asmdef、`unity/ProjectSettings/**`
- Godot 原素材、正式 Scene/Prefab、共享维护/进度文档、最终视觉证据
- Gate 0 intake 和其他 Agent 报告

## 输入

- `card_asset/**`、`card_data/**`、`image/**`、`audio/**`、`fonts/**`、`shaders/**`、`addons/**`
- `unity/Assets/_Project/Resources/**`、`ArtSource/**` 及 `.meta`
- `docs/migration/unity-3d/01-assessment/dependency-replacement-matrix.md`
- Silver attribution 与既有 hash/授权记录

## 输出

账本中每项包含：名称/类别、状态、来源 URL、作者、检索日期、版本、许可证与全文 URL、商用/再分发、Unity 兼容性、依赖、二进制风险、SHA-256 或未下载说明、导入路径/Importer/GUID、性能、视觉影响、加入路径、移除/回滚、拒绝原因。报告包含源/副本 hash、重复/孤立/命名/格式/授权缺口和 CardAssetAudit 后续建议。

## 非目标

不下载、不生成、不执行、不导入、不移动/重命名资产，不调用 Blender/MCP，不声称未证实授权，不修正稳定 ID `lighting`，不替换 Silver 或原卡面。

## 只读检查命令

```powershell
rg --files card_asset card_data image audio fonts shaders addons unity/Assets/_Project/Resources unity/Assets/_Project/ArtSource
Get-FileHash <source> -Algorithm SHA256
rg -n -i "license|copyright|author|source|授权|来源" . --glob '*.md' --glob '*.txt'
git status --porcelain=v1 -uall
```

## 停止条件

缺少许可证/来源即记录 `UNVERIFIED` 或 `REJECT`；需要联网下载、调用 Blender/MCP、改资产或写独占路径外文件时停止。你不是唯一在工程中工作的 Agent；不得回滚或覆盖他人改动。
