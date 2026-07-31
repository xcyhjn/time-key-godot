# Unity 3D 迁移环境预检

> 状态：已验证（A0 完成，运行证据持续补充）
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：本机命令输出、`project.godot`、Git 工作树、Mem0（仅作历史上下文）

## 结论

**已验证事实**：当前工作目录是 `D:\godot\时之钥\时之钥`，当前分支是 `unity_7.31`，Godot 4.6.2 与 Unity 6000.4.10f1 均可从固定路径启动。仓库具备开始评估和本地 Unity 垂直切片的基础条件。

**决策**：迁移工程默认放在仓库内 `unity/`，与 Godot 源工程并存。评估门禁通过前只写迁移文档和基线证据，不创建 Unity 游戏逻辑。

## Git 与用户改动保护

**已验证事实**：

- 分支：`unity_7.31`；HEAD 为迁移启动文档提交，跟踪 `origin/unity_7.31`。
- 远端：`https://github.com/xcyhjn/time-key-godot.git`。
- Godot 参考基线：`ver1.1.3-new`。
- Git LFS 3.7.1 已安装；`git lfs ls-files` 当前为空。
- 最大受控文件约 4.8 MB（`log/game.log`），其后主要是 OGG、字体和 PNG；当前没有必须先引入 LFS 才能启动切片的文件。

以下 4 个文件在开工前已经修改，属于用户现有改动：

```text
default_bus_layout.tres
scene/in_scene/rewards/resources/default_craft_recipe_book.tres
shaders/color_BG.gdshader
shaders/game_over.gdshader
```

**决策**：不回滚、不覆盖、不格式化、不暂存、不提交上述文件；每个迁移检查点只使用精确路径暂存，并在提交前复核 `git diff --cached --name-only`。

## 引擎与批处理入口

| 项目 | 已验证值 | 可复制命令 |
| --- | --- | --- |
| Godot | `4.6.2.stable.official.71f334935` | `& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --version` |
| Godot 主场景 | `scene/game_start/game_start.tscn`（UID 由 `project.godot` 解析） | `& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --path . --headless --quit-after 10` |
| Unity | `6000.4.10f1` | `& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' -version -batchmode -quit` |

**已验证事实**：`project.godot` 声明 Godot 4.6、Forward Plus、设计视口 `1920 x 1080`、`canvas_items` 拉伸模式和 11 个 autoload：`Global`、`MapState`、`GlobalClock`、`Signal_Bus`、`GlobalDB`、`GlobalTimecoin`、`Dialogic`、`StatusDb`、`Saver`、`SceneLog`、`SoundManager`。

## 主机基线

| 项目 | 值 |
| --- | --- |
| 操作系统 | Windows 11 家庭中文版，10.0.26200，64 位 |
| CPU | Intel Core i7-14650HX，16 核 / 24 逻辑处理器 |
| 内存 | 约 15.7 GiB；预检时空闲不足 1 GiB |
| 独立 GPU | NVIDIA GeForce RTX 5060 Laptop GPU，驱动 32.0.16.1047 |
| 其他显示适配器 | Intel UHD Graphics、GameViewer/Todesk 虚拟显示适配器 |
| D 盘空间 | 约 60.4 GiB 可用 |

**风险**：预检时可用内存偏低，Unity 导入与 Godot 图形实例并行可能产生交换和超时。运行 Unity 批处理前关闭不需要的编辑器实例，并把性能数据标注为开发机基线而非目标设备指标。

## 源库存复核

排除 `addons/` 与 `.godot/` 后：

| 类别 | 数量 |
| --- | ---: |
| 文件 | 575 |
| GDScript | 295 个 / 28,165 行 |
| 场景 `.tscn` | 38 |
| Resource `.tres` | 16 |
| Godot shader | 24 |
| JSON | 7 |

## Mem0 与指令核对

**已验证事实**：Mem0 中与当前项目直接相关的稳定事实只有“项目根目录”“必须先评估”和“集成分支 `unity_7.31`”，均与本地文件一致。其他返回项属于不同 Unity 项目，未用于本项目决策。

**已验证事实**：仓库内未发现额外 `AGENTS.md`；本轮遵循用户在对话中提供的 `AGENTS.md`。

## 工具能力与限制

- 可用：Git/Git LFS、Godot console、Unity batchmode、Windows Computer Use、PowerShell、`rg`。
- Godot/Unity 图形窗口通过 Computer Use 采集实际渲染证据；headless 只作为结构门禁。
- 当前没有正式 Godot 自动化测试套件；不得将启动成功等同玩法正确。
- 网络与 Unity 包解析状态要在创建工程后再次验证。

## A0 复现命令

```powershell
git status --short --branch
git remote -v
git log -5 --oneline --decorate
git lfs env
git lfs ls-files
& 'D:\Godot_v4.6.2-stable_win64.exe\Godot_v4.6.2-stable_win64_console.exe' --version
& 'C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe' -version -batchmode -quit
```

## 待确认

- 正式目标平台尚无本地证据；首切片按 Windows 桌面开发基线进行，不形成发布平台承诺。
- 是否要求转换现有 Godot 存档尚无本地决策；首切片不实现旧档转换，但保留版本化适配边界。
- 第三方插件和现有素材授权需进入依赖替换矩阵；在授权确认前不向 Unity 批量复制插件内容。
