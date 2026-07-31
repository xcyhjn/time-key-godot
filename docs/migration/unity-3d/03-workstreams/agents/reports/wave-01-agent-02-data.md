# Wave 01 Agent 02 执行报告：真实卡牌数据适配

> 状态：代理实现完成，主智能体修正并由 Unity 复验通过
> 负责人：Agent 02
> 最后验证日期：2026-07-31
> 证据来源：冻结 Adapter 契约、`card_data/lighting.json`、SHA-256 校验、Unity 随附 Roslyn 编译结果

## 改动

- `unity/Assets/_Project/Content/Cards/lighting.json`：逐字节复制首切片真实 fixture，保留中文描述字段和稳定拼写 `lighting`。
- `unity/Assets/_Project/Runtime/Infrastructure/TimeKey.Infrastructure.asmdef`：建立只依赖 `TimeKey.Domain` 的运行时适配程序集。
- `unity/Assets/_Project/Runtime/Infrastructure/CardJsonAdapter.cs`：使用 Unity `JsonUtility` 解析结构化 JSON，将 ASCII 字段映射到 Domain；未映射中文字段由解析器忽略但继续保留在原始 fixture 中。
- `unity/Assets/_Project/Tests/Infrastructure/TimeKey.Tests.Infrastructure.asmdef`：建立独立 Editor 测试程序集，只引用 Domain、Infrastructure 与 Unity Test Assemblies。
- `unity/Assets/_Project/Tests/Infrastructure/CardJsonAdapterTests.cs`：覆盖真实 fixture、未映射中文字段、缺失稳定 ID、未知 effect 以及空占位/非法字符/空行 shape。

## 证据

1. `Get-FileHash` 得到 Unity fixture SHA-256：`7DEC6590C409E3A5C9BB2E83B8E833FC1E6D9106EF5706E223F909D1E763FB0B`，与源文件和冻结契约一致。
2. `git diff --no-index -- card_data/lighting.json unity/Assets/_Project/Content/Cards/lighting.json` 退出码为 0，证明两份 fixture 字节内容一致。
3. 两个新增 asmdef 均通过 PowerShell `ConvertFrom-Json` 校验。
4. 使用 Unity `6000.4.10f1` 随附 `NetCoreRuntime/dotnet.exe` 和 `DotNetSdkRoslyn/csc.dll`，以 `/warnaserror+` 依次编译 `TimeKey.Domain.dll`、`TimeKey.Infrastructure.dll`、`TimeKey.Tests.Infrastructure.dll`，退出码为 0。
5. 静态编译引用 Unity 的 netstandard 2.1 reference assemblies、`UnityEngine.CoreModule.dll`、`UnityEngine.JSONSerializeModule.dll`、内置 `nunit.framework.dll` 与 Unity 提供的 netfx `mscorlib` compatibility shim；没有使用外部下载依赖。
6. `git diff --check` 对三个实现所有权目录无输出、退出码为 0。
7. 未执行暂存、提交、push、分支切换，也未写入四个独占范围之外的仓库路径。

## 代理交回时未执行项

- Agent 交回时 Unity Test Runner 与真实 `JsonUtility` 进程内解析尚未执行；这些限制已在下方“主智能体集成复验与修正”中关闭。

## 风险

- 首切片 adapter 仅接受冻结范围内的 `damage` effect；后续卡牌中的 `recover`、`built`、`poison`、`clear` 等类型必须通过契约变更后再扩展，当前会显式失败。
- shape 解析接受逗号分隔的 `0/1` 行，要求至少一个占位，并归一化到最小占位原点。源数据中用于 clear 特例的全零 shape 当前会被拒绝，符合首切片非目标，但后续迁移必须在 effect 语义层单独处理。
- `front_image` 被 DTO 接收但不进入 Domain；来源不明图片仍未复制，符合授权边界。

## 建议下一步

1. Unity 许可证可用后由主智能体运行 Infrastructure EditMode 测试并保留 XML 与 log，确认 `JsonUtility` 的真实进程内行为。
2. Presentation 通过 `CardJsonAdapter.Parse(TextAsset.text)` 获取 `CardDefinition`，不自行解析 JSON 或重复字段校验。
3. 扩展更多卡牌前先冻结 effect 类型和 clear 特例契约，再按真实 fixture 分批增加适配测试。

## 主智能体集成复验与修正

Agent 交回后，主智能体用 Unity 6000.4.10f1 的真实 `JsonUtility` 执行 adapter 测试；最终 EditMode XML 总计 19/19 通过。集成审查发现 Godot 形状解析把全零 shape 视为单格占位，且真实数据可能用逗号、空格或换行分隔，因此把 adapter 修正为同样的单格 fallback 并接受这些分隔符，测试同步覆盖。该修正替代了本报告“全零 shape 会拒绝”的早期风险判断，没有扩展 effect 类型。
