# コード生成エンジン
このフォルダには、スキーマ定義から実際のソースコードを生成するためのエンジン実装が含まれています。

## 主な責務
- スキーマ定義（集約）から各種プロジェクトのソースコードを自動生成
- C#（クラスライブラリ、ASP.NET Core WebAPI、ユニットテスト）とTypeScript（React.js）のコード生成
- 複数の集約にまたがるファイルの管理
- ディレクトリ構造の自動セットアップ
- 生成されていないファイルの自動削除

## アーキテクチャ概要

### コア実行エンジン
- **`CodeRenderingContext`**: コード生成処理の中心となるコンテキストクラス
- **`IModel`**: スキーマ定義のルート集約の種類を表すインターフェース。モデルによりどのようなソースコードが生成されるかが大きく異なる。
- **`DirectorySetupper`**: ディレクトリ構造を直感的に設定するためのヘルパークラス

### ソースコードの抽象
- **`SourceFile`**: 生成される個別ソースファイルを表すクラス
- **`SourceFileByAggregate`**: ルート集約1つと対応する1つのファイルに複数の機能をレンダリングする場合に使用
- **`IMultiAggregateSourceFile`**: ルート集約複数から1つのファイルを生成する場合に使用

### コード生成オプション
- **`GeneratedProjectOptions`**: 生成されるプロジェクトの設定オプション
- **`CodeRenderingOptions`**: コード生成処理のオプション

### ソースコード内部に登場する変数の抽象
- **`Instance API`**: 生成されるソースコード上のインスタンス構造を抽象化
- **`ISchemaPathNode`**: スキーマ定義のパス構造を管理するインターフェース
- **`SchemaNodeIdentity`**: マッピング処理でスキーマノードを一意に識別

### C#の生テキストリテラル（Raw String Literal）文法の支援
- **`TemplateTextHelper`**: 条件分岐や反復処理を含むテンプレート生成を支援

#### 分岐処理の記法
何らかの条件に基づいて特定のソースをレンダリングするか否かが分かれる場合は [TemplateTextHelper](./TemplateTextHelper.cs) で定義されている `If()` , `.ElseIf()` , `.Else()` を使用してください。
また、それらのソースは、C#のテンプレートリテラルの構文の末尾の `"""` の位置とインデントを合わせて下さい。

OK

```cs
string RenderXXX() {
    return $$"""
        // ここに何らかのソースをレンダリングする
        {{If(/* 条件A */, () => $$"""
            // 条件Aを満たした場合のみレンダリングされるソース
        """).ElseIf(/* 条件B */, () => $$"""
            // 条件Bを満たした場合のみレンダリングされるソース
        """).Else(() => $$"""
            // 条件A, B いずれも満たさない場合のみレンダリングされるソース
        """)}}
        """;
}
```

NG

```cs
string RenderXXX() {
    return $$"""
        // ここに何らかのソースをレンダリングする
            {{If(/* 条件A */, () => $$"""
            // 条件Aを満たした場合のみレンダリングされるソース
            """).ElseIf(/* 条件B */, () => $$"""
            // 条件Bを満たした場合のみレンダリングされるソース
            """).Else(() => $$"""
            // 条件A, B いずれも満たさない場合のみレンダリングされるソース
            """)}}
        """;
}
```

#### 反復処理の記法
何らかの条件に基づいて特定のソースを反復処理する場合は `.SelectTextTemplate` メソッドを使ってください。
また、それらのソースは、C#のテンプレートリテラルの構文の末尾の `"""` の位置とインデントを合わせて下さい。

OK

```cs
string RenderXXX() {
    var array = Enumerable.Range(0, 4).Select(_ => "あ");
    return $$"""
        // ここに何らかのソースをレンダリングする
        {{array.SelectTextTemplate((_, i) => $$"""
            // {{i + 1}}番目の要素です。
        """)}}
        """;
}
```

NG

```cs
string RenderXXX() {
    var array = Enumerable.Range(0, 4).Select(_ => "あ");
    return $$"""
        // ここに何らかのソースをレンダリングする
            {{array.SelectTextTemplate((_, i) => $$"""
                // {{i + 1}}番目の要素です。
            """)}}
        """;
}
```

#### ソースコードレンダリング処理の入れ子の記法
ソースコードレンダリング処理の中で他のソースコードレンダリング処理を呼び出す際は、呼び出す側で `WithIndent` を用いてインデントを合わせます。
`WithIndent` の第2引数には、その行のインデントのサイズと等しい数の半角スペースを渡してください。

OK

```cs
string RenderXXX(object root) {
    return $$"""
        {{GetSomeChildren(root).SelectTextTemplate((x, i) => $$"""
          {{WithIndent(RenderTypeScriptObjectTypeRecursively(x), "  ")}}
        """)}}
        """;

    string RenderTypeScriptObjectTypeRecursively(object obj) {
        return $$"""
            {
            {{GetSomeChildren(obj).SelectTextTemplate(x => $$"""
              {{WithIndent(RenderTypeScriptObjectTypeRecursively(x), "  ")}}
            """)}}
            }
            """;
    }
}
```
