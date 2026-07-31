# Unity 目标架构

> 状态：首切片共享契约已冻结
> 负责人：主智能体
> 最后验证日期：2026-07-31
> 证据来源：可行性评估、Godot composition root、Unity 6000.4.10f1 本机包

## 决策摘要

- Unity 工程固定在仓库根目录 `unity/`，版本固定为 `6000.4.10f1`。
- 渲染使用 URP `17.4.0`；运行时界面统一使用 uGUI `2.0.0`。
- 首切片只建立实际有代码的 `Domain`、`Infrastructure`、`Presentation` 和测试程序集，不建立空的 Application 层。
- `Domain` 是无 `UnityEngine` 引用的纯 C#；MonoBehaviour 只负责输入、生命周期和表现适配。
- 运行时状态是普通实例，由场景 bootstrap 组合；不新增全局静态 singleton。

## 目录与程序集

```text
unity/
  Assets/_Project/
    Content/Cards/                 # 真实 JSON fixture
    Runtime/Domain/                # TimeKey.Domain，无 UnityEngine
    Runtime/Infrastructure/        # TimeKey.Infrastructure，解析/适配
    Runtime/Presentation/Battle/   # TimeKey.Presentation，3D 与 uGUI
    Editor/                        # 工程生成、截图和构建 harness
    Scenes/VerticalSlice/
    Tests/EditMode/
    Tests/PlayMode/
  Packages/
  ProjectSettings/
```

程序集契约：

| 程序集 | 允许引用 | 禁止职责 |
| --- | --- | --- |
| `TimeKey.Domain` | .NET 基础库 | UnityEngine、场景查找、资源加载、静态全局状态 |
| `TimeKey.Infrastructure` | Domain、UnityEngine | 玩法决策、画面布局 |
| `TimeKey.Presentation` | Domain、Infrastructure、UnityEngine、uGUI | 存档 schema 决策、隐式全局状态 |
| `TimeKey.Tests.EditMode` | Domain、Infrastructure、Test Framework | 依赖活动场景的断言 |
| `TimeKey.Tests.PlayMode` | Domain、Infrastructure、Presentation、Test Framework | 未固定随机种子的流程 |

## 生命周期与通信

首切片由 `VerticalSliceController` 作为场景 composition root，显式创建领域状态并把视图事件转为领域命令。代码生命周期使用普通 C# event；外部能力通过接口注入；UnityEvent 仅允许用于 Inspector 可配置的表现回调。本切片不需要跨场景 event bus。

运行顺序固定为：加载 fixture -> 构建状态 -> 选择卡牌 -> 选择世界目标 -> 放置时间轴 -> 按列/行结算 -> 更新 3D 目标与结构化快照。

## 配置与运行态

- 卡牌 JSON、固定敌人意图和地图 fixture 是只读静态配置。
- HP、选中项、时间轴占位和回合阶段是每次会话新建的运行态。
- ScriptableObject 后续可承载稳定编辑器配置，但不得作为跨测试共享的可变运行态。
- 所有随机入口接收显式 seed；首切片固定为 `731`。

## 质量约束

- 领域规则必须可在 EditMode 单独测试。
- 场景交互必须有 PlayMode 覆盖。
- Editor harness 必须能在 batchmode 构建场景、生成两个视口截图并构建 Windows Player。
- 任何允许差异或 Godot 已知缺陷都写入 parity matrix，不在迁移中静默修正。
