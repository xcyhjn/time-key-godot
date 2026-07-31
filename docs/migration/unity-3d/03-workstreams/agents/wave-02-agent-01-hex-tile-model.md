# Wave 02 / Agent 01：六边形实体地块美术重建

> 状态：已审查，可执行
> 所有权：仅限本 Prompt 的独占路径
> 基线分支：`unity_7.31`

## 目标

为 Unity 局内战斗棋盘制作可从 360 度观察的低多边形六边形实体地块。造型必须有真实厚度、可逐层堆叠，并延续原项目像素地块的草地、土层、深色基座和少量植被语言；不得把原 PNG 直接当作水平纸片交付。

参考原素材：

- `image/inscene_block/grass.png`
- `image/inscene_block/dirt.png`
- 当前 Unity 棋盘截图：`docs/migration/unity-3d/04-verification/evidence/unity-slice-02-board/`

## 独占写入路径

- `unity/Assets/_Project/ArtSource/HexTiles/**`
- `unity/Assets/_Project/Resources/Art/Battle/Models/**`
- `docs/migration/unity-3d/04-verification/evidence/hex-tile-agent/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-02-agent-01-hex-tile-model.md`

不要修改 `Runtime/Presentation/**`、`Editor/**`、`Tests/**`、场景、ProjectSettings、共享迁移文档或 Godot 源码。你并非独自在仓库中工作；不得回退或覆盖他人的改动，并需适配执行期间出现的其他变更。

## 工具约束

- 用户已显式允许本子任务调用 Blender MCP；该权限仅限本 Prompt 的独占路径与六边形地块交付。
- 可以使用本机已存在的 Blender GUI/CLI 或编写可复现的 Blender Python 生成脚本；若 Blender 不可执行，保留可运行的生成脚本与明确阻塞报告，不要伪造 FBX/GLB。
- 不下载来源或授权不明的模型。若搜索外部底模，只能使用许可清晰且允许随游戏再分发的素材，并在报告中记录来源、作者、许可证和修改内容。
- 优先自建简单网格，避免引入与原项目风格不一致的写实资产。

## 冻结几何契约

- flat-top 六边形，外接半径 `0.94` Unity 单位。
- 单层实体高度 `0.32` Unity 单位；模型原点位于底面中心。
- 顶面、草边、土层、深色基座在侧视图中可区分。
- 草地与裸土地块至少各一个变体；两者轮廓和连接尺寸一致，可无缝逐层叠放。
- 低多边形、平面着色；单个变体目标不超过 2,000 三角形、4 个材质槽。
- 不在模型中烘焙相机方位、世界坐标或碰撞体。

## 交付与验证

1. 交付可复现源文件或生成脚本，以及 Unity 可原生导入的 FBX（优先）或经现有包验证可导入的格式。
2. 在 Blender 或其他本地查看器中生成至少正面斜视、背面斜视、两层堆叠三张 PNG；检查没有开口、倒法线、穿插或悬空。
3. 报告顶点/三角形/材质数、包围盒、原点、单位和导出参数。
4. 对输出执行 `git diff --check`；不得 stage、commit、push 或切分支。
5. 最终只报告独占路径内改动、验证证据与任何阻塞。

## 验收门禁

- 360 度观察时不是纸片。
- 两层模型按 `0.32` 间距叠放时无缝、无间隙。
- 风格可追溯到原草地/土地图，而非通用写实地块。
- 输出可由主智能体在 Unity 中接入，不要求修改现有棋盘控制器。
