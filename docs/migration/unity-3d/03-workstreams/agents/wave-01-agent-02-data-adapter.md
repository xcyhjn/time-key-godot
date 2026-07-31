# Wave 01 Agent 02：真实卡牌数据适配

> 状态：已审查并执行完成
> 负责人：Agent 02
> 最后验证日期：2026-07-31
> 证据来源：数据迁移契约与 `lighting.json` 样本

## 角色与单一目标

把原始 `lighting.json` 作为只读 fixture 接入 Domain，并验证稳定 ID、效果、范围与形状。你不是仓库里唯一的工作者；不得回退或覆盖他人改动。

## 必读

- `docs/migration/unity-3d/01-assessment/source-inventory.md`
- `docs/migration/unity-3d/02-architecture/data-and-save-migration.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `card_data/lighting.json`

可用 `codebase-migrate` 核对 fixture 保真、`Verification & Quality Assurance` 形成解析证据；不需要 UI/3D skill。

## 独占所有权

- `unity/Assets/_Project/Runtime/Infrastructure/**`
- `unity/Assets/_Project/Content/Cards/**`
- `unity/Assets/_Project/Tests/Infrastructure/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-02-data.md`

禁止修改其他路径，尤其是 Domain、Presentation、共享文档、工程包和 Godot 源文件。

## 契约、步骤与停止条件

1. 等待 `TimeKey.Domain` API 可用。
2. 原样复制 `lighting.json`，校验 SHA-256 为 `7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B`。
3. 建立 `TimeKey.Infrastructure` asmdef 与最小 JSON adapter；允许忽略未映射中文描述字段。
4. 在独立 `Tests/Infrastructure` EditMode 程序集中验证真实 fixture、未映射中文字段、缺失稳定 ID、未知 effect 和无效 shape。
5. 解析必须得到 `lighting`、damage 100、真实 range 与单格 shape；错误输入显式失败。
6. 写独占报告。

非目标：旧 CFG、所有卡牌批迁移、场景/UI、规则实现。Domain 契约不满足或必须越权时停止并报告。

## 验收与 Git

主智能体运行 EditMode 解析测试及：

```powershell
Get-FileHash .\unity\Assets\_Project\Content\Cards\lighting.json -Algorithm SHA256
```

无截图或性能责任。不得切分支、合并、push、commit、暂存或回退他人改动。报告格式：改动、证据、失败项、风险、建议下一步。
