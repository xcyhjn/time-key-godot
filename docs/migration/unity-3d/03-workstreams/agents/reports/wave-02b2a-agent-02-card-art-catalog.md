# Wave 02B2A Agent 02 原卡面目录报告

> 状态：已完成，待主智能体执行唯一 Unity 导入/渲染门禁
> 负责人：Wave 02B2A Agent 02
> 最后验证日期：2026-08-01
> 证据来源：`asset-audit`、`Verification & Quality Assurance`、结构化 JSON 映射、双次 SHA-256/尺寸扫描、完整 contact sheet 人工检查

## 结论

除只读的 `lighting` 外，冻结映射中的六张原卡面已从 `card_asset/` 逐字节复制到 Unity Resources。七张目标卡面均存在、非空、尺寸为 `1135x1590`、源目标 SHA-256 完全一致；没有重编码、修图、裁切、缩放、去棋盘格或外部素材替换。

六张新增卡的 `.meta` 已静态设置为 Single Sprite/UI、关闭 mipmap、Point、Clamp、Max Size 2048 和无纹理压缩。既有 `lighting.png.meta` 当前仍是 Default Texture 而不是 Sprite/UI；Agent Prompt 要求对 `lighting` 只读，因此本 Agent 未修改，已经通知主智能体在唯一 Unity 实例阶段统一裁决。

## 实际改动

- 运行时资源：`earthquake.png`、`wind.png`、`recover.png`、`tower_card.png`、`poison_card.png`、`tornado.png` 及六份对应 `.meta`。
- 独占证据：`docs/migration/unity-3d/04-verification/evidence/wave-02b2a-card-art-agent/manifest.md`、`seven-card-contact-sheet.png`。
- 独占报告：本文。
- 未修改 `lighting.png`、`lighting.png.meta`、JSON、Domain、Infrastructure、Presentation、Scenes、Editor、Packages、ProjectSettings、Godot 源、共享文档或四个受保护脏文件。

## 冻结资源加载表

| stable ID | `CardDefinition.FrontImage` 文件名 | Resources 文件 stem |
| --- | --- | --- |
| `lighting` | `lighting.png` | `lighting` |
| `earthquake` | `earthquake.png` | `earthquake` |
| `wind` | `wind.png` | `wind` |
| `recover` | `recover.png` | `recover` |
| `tower` | `tower_card.png` | `tower_card` |
| `poison` | `poison_card.png` | `poison_card` |
| `tornado` | `tornado.png` | `tornado` |

Presentation 应从 typed definition 保存的 `front_image` 去扩展名后加载，不应按 stable ID 特判 `tower_card` 或 `poison_card`。

## 审计结果

- 完整哈希、字节数、尺寸和目标路径见 `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-card-art-agent/manifest.md`。
- 七张 PNG 均为 24-bit RGB、无 alpha；统一纵横比约 `0.713836`。
- 通用 `asset-audit` 的 2 的幂尺寸和命名模板不适用于本次冻结历史卡面；项目明确要求保留 `1135x1590` 与原文件名，因此没有改名或缩放。
- 运行时副本不是 orphan：七份真实 JSON 的 `front_image` 均有一一对应目标；`lighting` 已被既有手牌加载，其他六张由本波 typed definition/后续 UI 按映射加载。
- 没有项目定义的卡图字节预算，因此不伪造“体积预算合规”结论。

## 视觉证据

`docs/migration/unity-3d/04-verification/evidence/wave-02b2a-card-art-agent/seven-card-contact-sheet.png` 已在原始分辨率 988x748 下人工查看。七张卡均完整显示四边、顶部时间轴 shape、中心插画、中文标题与描述；未见裁切、拉伸、空白资源、错位标签或不可辨识的主要文字。

contact sheet 从 Unity Resources 目标 PNG 生成，每张采用精确 `1/5` 比例 `227x318`；它只用于审查，不参与运行时。源卡面的灰白棋盘格角部是烘焙 RGB 像素，保持原样符合 MIG-014。

## 验证

1. 使用 `ConvertFrom-Json` 读取七份真实 JSON，逐项验证 `name` 和 `front_image` 与冻结映射一致：通过。
2. 复制前后两轮源 SHA-256 未变化；七组源/目标字节数与 SHA-256 一致：通过。
3. 七张目标文件 PNG 解码、尺寸、PixelFormat 和非空检查：通过。
4. 六份新增 importer 字段静态检查：`textureType=8`、`spriteMode=1`、`enableMipMap=0`、三平台 `textureCompression=0`：通过。
5. contact sheet 人工检查完整边框、比例、中文和插画：通过。
6. 六个新增 `.meta` GUID 在 `unity/Assets` 内均只出现一次：通过。
7. `git diff --check`：退出码 0；暂存区为空；当前分支仍为 `unity_7.31`。
8. Unity Editor/Player 实际导入与渲染：本 Agent 按禁止启动图形实例的约束未执行，交主智能体唯一实例门禁。

## 风险与交接

- MIG-005：原图片授权仍未闭合。本地迁移可继续，公开发布不能依据本报告放行。
- MIG-014：七张卡均为不透明 RGB，圆角外棋盘格已经烘焙，Unity 导入设置不能恢复不存在的透明度。
- `lighting.png.meta` 当前是 Default Texture（`textureType=0`、`spriteMode=0`），而六张新增资源是 Sprite/UI。主智能体应在唯一 Unity import 后确认加载 API 与最终 importer 一致，并补实际两卡 UI 渲染证据。
- Unity 首次读取手写 `.meta` 后可能补充序列化字段；只要 GUID 和冻结设置不变，属于预期导入变更，但应由主智能体审查。

## Git 边界

本 Agent 没有执行 branch、stash、stage、commit、push 或 merge。最终路径状态只包含本报告、独占证据目录和 `unity/Assets/_Project/Resources/Art/Battle/Cards/` 下的六组新增 PNG/`.meta`；未发现 Agent 02 越权写入。四个保护文件仍保持任务开始时已有的 modified/unstaged 状态，暂存区为空；共享工作区的并行改动未被回退。
