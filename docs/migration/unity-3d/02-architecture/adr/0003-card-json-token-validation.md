# ADR-0003：卡牌 JSON 异构 value 使用结构化 token 校验

> 状态：Accepted
> 日期：2026-08-01
> 负责人：主智能体

## 背景

七张 Godot 卡牌在同一 `effects[].value` 字段同时使用 JSON number 与 string。Wave 02B2A Agent 01 用 Unity `JsonUtility` 对同一源 JSON 做 common/numeric/string DTO 视图后发现：`JsonUtility` 会把错误 token 静默强制转换，numeric 的 `"100"`/`null` 可变成整数，clear 的 number `1` 可变成字符串。因此仅靠 DTO 视图无法满足“未知或错误 payload 显式失败”。

## 决策

- 固定 Unity 官方 registry 包 `com.unity.nuget.newtonsoft-json` `3.2.2`；该版本来自 Unity registry，包声明兼容 Unity 2018.4 及以上，且当前 Unity 6 工程可使用。
- 只用 `JObject`/`JArray`/`JToken.Type` 做原始 token 类型、字段存在性和 JSON 语法校验。
- 通过校验后继续使用现有 `JsonUtility` typed DTO 视图读取 common/numeric/string payload，不引入动态字典作为领域 API。
- 包许可证与来源随 Unity 官方包元数据保留；本阶段不引入其他 JSON 依赖。

## 影响

错误 numeric/string token、缺失 value 和 malformed JSON 现在显式抛出 `FormatException`；七张真实 fixture 的解析结果与原文件字节不变。Package/lock 变更由主智能体独占，后续 Agent 不得修改。
