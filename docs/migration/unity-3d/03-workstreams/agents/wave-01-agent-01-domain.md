# Wave 01 Agent 01：纯 C# 领域规则

> 状态：已审查并执行完成
> 负责人：Agent 01
> 最后验证日期：2026-07-31
> 证据来源：共享契约与玩法等价契约

## 角色与单一目标

实现并测试首切片的纯 C# 卡牌、六边形坐标、12×3 时间轴、伤害结算、敌人意图记录与确定性快照。你不是仓库里唯一的工作者；不得回退或覆盖他人改动。

## 必读

- `docs/migration/unity-3d/01-assessment/gameplay-parity-contract.md`
- `docs/migration/unity-3d/02-architecture/target-architecture.md`
- `docs/migration/unity-3d/02-architecture/godot-to-unity-mapping.md`
- `docs/migration/unity-3d/03-workstreams/integration-contracts.md`
- Godot `addons/card-framework` 中时间轴/形状核心代码，仅用于行为核对。

可用 `codebase-migrate` 理解行为迁移、`Verification & Quality Assurance` 约束测试证据；不需要 Godot UI 或 3D skill，因为本角色禁止写表现层。

## 独占所有权

- `unity/Assets/_Project/Runtime/Domain/**`
- `unity/Assets/_Project/Tests/EditMode/**`
- `docs/migration/unity-3d/03-workstreams/agents/reports/wave-01-agent-01-domain.md`

禁止修改上述之外任何路径，尤其是共享文档、Infrastructure、Presentation、Packages、ProjectSettings、Godot 文件和用户脏文件。

## 输入/输出契约

输入和 snapshot 字段遵守 `integration-contracts.md`；Domain 不得引用 `UnityEngine`。稳定卡牌 ID 为 `lighting`，timeline 为 12×3，seed 为 731，目标 10 HP，damage 100 后为 0。敌人意图必须进入顺序记录，但不虚构完整建筑 AI。

## 实现与停止条件

1. 建立 `TimeKey.Domain` asmdef 与最小不可变/受控可变模型。
2. 写失败优先测试：边界、冲突、列/行顺序、单次执行、伤害钳制、快照。
3. 实现使测试通过的最少代码。
4. 写独占报告。

非目标：JSON 解析、MonoBehaviour、Unity scene、UI、旧档和完整卡牌。契约冲突或必须修改非拥有路径时立即停止，在报告中说明并通知主智能体。

## 验收命令

由主智能体在工程可编译后执行：

```powershell
& $Unity -projectPath .\unity -batchmode -runTests -testPlatform EditMode -testResults ..\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\editmode-results.xml -logFile ..\docs\migration\unity-3d\04-verification\evidence\unity-slice-01\editmode.log -quit
```

无截图或性能验收责任；要求测试无 UnityEngine 场景依赖且固定 seed 重跑结果一致。Git：不切分支、不合并、不 push、不 commit，不暂存或回退他人改动。

## 回报格式

报告依次列出：改动文件、测试/命令证据、失败项、风险、建议下一步。
