# Wave 02B Agent 01：原卡牌素材接入

> 状态：已生成，待接手主智能体审查后启动
> 负责人：Wave 02B Agent 01
> 最后验证日期：2026-07-31
> 证据来源：Godot 卡牌实跑、`card_asset/lighting.png`、Wave 02B1 契约

## 角色与单一目标

把原版 `lighting` 卡面按原始字节导入 Unity 专用卡牌资源目录，并交付可审计的尺寸、哈希、导入建议和视觉证据。你不是仓库唯一工作者，不得回退或覆盖任何他人改动。

## 必读与能力

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_COMBAT_PROMPT.md`
- `docs/migration/unity-3d/01-assessment/source-inventory.md`
- `docs/migration/unity-3d/01-assessment/godot-baseline-evidence.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- `card_asset/lighting.png`
- `image/behide.png`（原牌背；历史拼写保留）

若可用，完整读取 `asset-audit` skill 后使用它核对来源、重复项、尺寸与授权状态。当前任务禁止调用 Blender MCP、图像生成或重绘；用户要求沿用原素材和样式。

## 独占写入范围

- `unity/Assets/_Project/Resources/Art/Battle/Cards/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b-card-art-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-01-card-art.md`

禁止修改 Godot 源文件、`Content/Cards/lighting.json`、任何 C#、scene、asmdef、meta 之外的工程设置、共享文档和其他 Agent 路径。`.meta` 可由本 Agent 在独占目录内生成；不得为生成 `.meta` 启动会改写全工程的 Unity 导入。

## 冻结输入/输出

- 输入：`card_asset/lighting.png`，预期 `1135×1590`，SHA-256 `DD27CCC0F0A9982286958D130E73E106DF309B1A116DA68139E30FA398ACE3DC`。
- 输出：`Resources/Art/Battle/Cards/lighting.png` 的精确字节副本；同时复制原 `image/behide.png` 为 `Resources/Art/Battle/Cards/behide.png`，保留历史文件名并分别记录哈希。
- 输出清单至少记录源/目标哈希、像素尺寸、alpha 情况、建议 Unity Texture Type、Filter Mode 和 Max Size。
- 不把生成后的 Unity 压缩纹理哈希伪装成源文件哈希。

## 实现与验收

1. 复制前后计算 SHA-256；不改变编码、尺寸、裁切或颜色。
2. 视觉证据应能清楚看到完整卡框、雷击画面、中文标题/描述和四边，不得裁掉透明/白边后冒充原图。
3. 若没有安全的独立 Unity 导入方式，只记录建议，由主智能体统一导入；不得启动编辑器污染共享文件。
4. 运行 `git diff --check` 限定本 Agent 路径，写独占报告。

非目标：其他六张卡、卡背重制、字体替换、UI 代码、材质、动画、压缩优化。遇到源哈希不符、授权信息冲突或必须写非拥有路径时停止写入并报告主智能体。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash 或回退他人改动。回报格式：改动文件、哈希/尺寸、视觉证据、未执行项、风险、交给主智能体的导入建议。
