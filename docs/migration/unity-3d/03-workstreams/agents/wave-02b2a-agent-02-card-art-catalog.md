# Wave 02B2A Agent 02：原卡面目录

> 状态：已审查；Wave A 可启动
> 负责人：Wave 02B2A Agent 02
> 最后验证日期：2026-08-01

## 角色与单一目标

把除 `lighting` 外的六张正式原卡面逐字节接入 Unity Resources，形成可审计的 stable ID -> 资源路径清单。只负责素材复制、导入设置和素材证据；不改卡图、不做 UI。你不是仓库唯一工作者，不得回退他人改动。

## 必读和能力

- `docs/migration/unity-3d/00-bootstrap/NEXT_STAGE_EFFECTS_PROMPT.md`
- `docs/migration/unity-3d/05-progress/known-issues.md` 的 MIG-005/MIG-014
- `card_data/*.json`
- `card_asset/*.png`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b-agent-01-card-art.md`

使用 `asset-audit` 检查尺寸、哈希和来源；使用 `Verification & Quality Assurance` 检查导入后像素与比例。禁止图像生成、修图、去棋盘格、Blender 和外部素材替换。

## 独占写入范围

- `unity/Assets/_Project/Resources/Art/Battle/Cards/**`
- `docs/migration/unity-3d/04-verification/evidence/wave-02b2a-card-art-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02b2a-agent-02-card-art-catalog.md`

禁止修改 JSON、Domain、Infrastructure、Presentation、Scenes、Editor、Packages、ProjectSettings、Godot 源、共享文档和其他 Agent 路径。

## 冻结映射

| stable ID | 源文件 | Unity 资源名 |
| --- | --- | --- |
| `lighting` | `lighting.png` | 已存在，仅读校验 |
| `earthquake` | `earthquake.png` | `earthquake` |
| `wind` | `wind.png` | `wind` |
| `recover` | `recover.png` | `recover` |
| `tower` | `tower_card.png` | `tower_card` |
| `poison` | `poison_card.png` | `poison_card` |
| `tornado` | `tornado.png` | `tornado` |

所有卡面源尺寸均应为 `1135x1590`。运行时必须保持比例；本 Agent 不创建裁切后的派生图。

## 实现步骤与验收

1. 读取 JSON 的 `front_image`，按表原名复制六张缺失 PNG；对已存在 lighting 只核验。Presentation 将使用 Agent 01 保留的 `front_image` 文件 stem 加载。
2. Unity import 设为 Sprite/UI 可读路径，避免压缩导致卡面文字不可辨；不得改源 PNG 字节。
3. 生成 manifest：源路径、目标路径、尺寸、字节数、SHA-256、stable ID 和导入结论。
4. 生成一张仅用于审查的七卡 contact sheet 或逐卡导入预览，不能替代原资源。
5. 验证每张图非空、比例一致、没有被拉伸或裁切；记录原素材烘焙棋盘格和授权仍未闭合。
6. 执行 `git diff --check` 并写独占报告。

不启动 Unity 图形实例；如需 import 元数据，先通知主智能体，由主智能体安排唯一 Unity 实例。源素材缺失、哈希在读取过程中变化或需要修图时停止该文件写入并报告。

## Git 与完成回报

不得暂存、commit、push、切分支、merge、stash。回报：文件映射、哈希、尺寸、预览路径、未满足项、授权风险和交给多卡 UI 的资源加载表。
