# Refactor.Gas

字节码栈机，将基础值与修饰符编译成指令流并解释执行，支持 Float/Double 精度。

---

相比直接硬编码或类似硬编码的委托，这套设计的核心收益是“灵活性/可组合性”：

- 公式结构可数据化
- 同一套 Attribute + Slot 的机制可复用到大量属性与修饰符组合，避免散落的特化实现
- 支持按需组合与热替换：只需替换公式与 slot 输入，无需替换调用点

## 使用

### 构建公式

```csharp
using var builder = Formulas.CreateFloat();
builder.LoadBase();
builder.LoadSlot(0);
builder.Add();
builder.LoadSlot(1);
builder.Multiply();
var formula = builder.Build();
```

### Preset + Attribute

```csharp
var formula = FormulaPresets.Float.Wow();

using var attr = new Attribute<FormulaFloat, float>(@base: 10f, formula: formula);
attr.SetSlot(0, 1f);
attr.SetSlot(1, 2f);
attr.SetSlot(2, 3f);
attr.SetSlot(3, 4f);

var value = attr.Value;
```

## 基准测试

> 环境：Windows 11，.NET 8，BenchmarkDotNet v0.14.0  

本仓库基准的典型结论（同一台机器、同一套实现的量级参考）：

- 短公式：字节码解释执行与接口链同量级。
- 长公式：字节码解释执行优于接口链。