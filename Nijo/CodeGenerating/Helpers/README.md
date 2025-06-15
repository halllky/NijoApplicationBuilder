# CodeGenerating.Helpers
複雑な処理をレンダリングしやすくするためのヘルパーがある名前空間。
理論上はここにあるものを一切使わずともソースコードのレンダリングは可能ではある。

複雑な処理の例としては、ある型のクラスから別の型へのクラスの変換やマッピング。

## Instance API について
[Instance API.cs](./Instance%20API.cs) にあるインターフェースや拡張メソッド群。

生成後のソースコードに登場する変数やオブジェクトを直感的に記述できるようにすることで
オブジェクトのパスの複雑な生成を補助することを目的とする。

例えば以下のようなスキーマ定義があるとき、

```xml
<?xml version="1.0" encoding="utf-8" ?>

<自動テストで作成されたプロジェクト RootNamespace="MyApp.Core">
  <集約その1 Type="data-model" GenerateDefaultQueryModel="True">
    <集約ID Type="word" IsKey="True" />
    <集約名 Type="word" />
  </集約その1>
</自動テストで作成されたプロジェクト>
```

ソースコードを自動生成する方の処理をこのように直感的に書くことで、

```cs
var variable = new Variable("x");
var someModelObject = new EFCoreEntity(/* 集約その1のRootAggregate */);
var properties = variable.CreateProperties(someModelObject);

return $$"""
  {{properties.SelectTextTemplate(prop => $$"""
  {{prop.GetJoinedPathFromInstance(".")}};
  """)}}
  """;
```

このようなソースが生成される。

```cs
x.集約ID;
x.集約名;
```

この例ではきわめて単純なデータ構造で示したが、Childrenやref-toが絡む複雑なスキーマ定義で、より威力を発揮する。
