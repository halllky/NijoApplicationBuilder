# Nijo プロジェクト
このプロジェクトは、スキーマ定義（nijo.xml）からソースコードを自動生成するプロジェクトです。
このプロジェクトが担うのはあくまでソース生成のみであり、生成されたあとのプロジェクトは、このプロジェクトを一切参照しません。

- [Nijo プロジェクト](#nijo-プロジェクト)
  - [Nijo プロジェクトを用いた開発の概要](#nijo-プロジェクトを用いた開発の概要)
  - [プロジェクト構成](#プロジェクト構成)
  - [コーディング規約](#コーディング規約)
    - [.editorconfig](#editorconfig)
    - [ソースコードレンダリング処理](#ソースコードレンダリング処理)
      - [メソッド名](#メソッド名)
      - [分岐処理の記法](#分岐処理の記法)
      - [反復処理の記法](#反復処理の記法)
      - [ソースコードレンダリング処理の入れ子の記法](#ソースコードレンダリング処理の入れ子の記法)
  - [デバッグ](#デバッグ)


## Nijo プロジェクトを用いた開発の概要
主な操作は [GeneratedProject](./GeneratedProject.cs) クラスのメソッドを通して実行されます。

1. 開発の最も最初のタイミングのみ、開発者は、プロジェクトのテンプレートを作成します。 [標準のアプリケーションテンプレート](../Nijo.ApplicationTemplate.Ver1/README.md) を用いるのが通常ですが、それ以外の任意のテンプレートを用いても構いません。
2. 以降のサイクルは開発のすべてのフェーズで頻繁に繰り返されます。
   1. 開発者は、要件定義や外部設計などに基づき、スキーマ定義(nijo.xml)を記載します。
   2. 開発者は、nijo.exeによるソースコードの自動生成を開始します。
      1. nijo.exeは、スキーマ定義(nijo.xml)を読み取り、 [SchemaParsingContext](./SchemaParsing/SchemaParseContext.cs) を通して、スキーマ定義の妥当性を検証します。この時点では、不正なスキーマ定義が存在しえます。例えば、循環参照、存在しない定義名、存在しない外部参照定義などが含まれる可能性があります。
      2. 検証が通った場合、nijo.exeは、 [CodeRenderingContext](./CodeGenerating/CodeRenderingContext.cs) を通して、ソースコード生成処理を実行します。ソースコードは、最初に作成されたプロジェクトテンプレートの `__AutoGenerate` という名前のフォルダの中に作成されます。この時点では、不正なスキーマ定義は全て排除されています。ソースコードの自動生成処理（主に [IModel](./CodeGenerating/IModel.cs) を継承するクラスとその関連モジュール）は、スキーマ定義が全て正であることを前提として処理を実行できます。
   3. 開発者は、生成されたあとのソースコードを利用しながら、外部仕様を満たすための残りの処理を自身の手で記述していきます。


## プロジェクト構成
- [`CodeGenerating/`](./CodeGenerating/): ソースコード生成処理の実装を容易にするための各種機能を提供します。
- [`SchemaParsing/`](./SchemaParsing/): nijo.xmlのXML要素の解析に関する機能を提供します。
- [`Models/`](./Models/): モデル定義。nijo.xmlのルート要素直下のXML要素は、その種類によりどういったソースコードが生成されるかが異なります。例えば、永続化されるべきデータを表したモデル(DataModel)からは、RDBMSのテーブル定義や、Entity Framework Core のDbContextの設定が生成されます。例えば、画面や帳票に表示されるべきデータを表したモデル(QueryModel)からは、検索条件のオブジェクト定義や、検索処理が生成されます。このフォルダにはモデル毎にそのモデルからどういったソースコードが生成されるべきかを定義する処理が書かれています。
- [`ImmutableSchema/`](./ImmutableSchema/): XMLスキーマの解析が通った後の、XMLスキーマが確定していることを前提とした各種クラス。コードの自動生成ではここにある各種クラスが利用されます。
- [`ValueMemberTypes/`](./ValueMemberTypes/): 値型メンバーの型定義。単語型、整数型、日付型、日付時刻型など。
- [`Parts/`](./Parts/): スキーマ定義XMLに由来するモジュールではなく、生成後のソースに由来するモジュール。C#のORMであるEntity Framework CoreのDbContext, Node.js の.envファイル、NUnitのテスト定義など。
- [`Util.DotnetEx/`](./Util.DotnetEx/): .NET関連のユーティリティ。ここにある機能はコード自動生成処理でのみ参照されます。


## コーディング規約

### .editorconfig
ファイルの改行コードや `{` の位置などのフォーマットは [.editorconfig](../.editorconfig) に準拠します。
人間がソース修正を行なう場合は必ず .editorconfig によるオートフォーマットが効くエディタを使用します。

### ソースコードレンダリング処理
ソースコードレンダリング処理とは、C#の生文字列リテラル(Raw String Literal)( `$$"""` ～ `"""` )を用いて複数行にわたるソースコードレンダリングを行うメソッドを指します。
このプロジェクトにおけるソースコードレンダリング処理は以下のルールに準拠します。

#### メソッド名
`Render` から始まるメソッド名を持ちます。

#### 分岐処理の記法
何らかの条件に基づいて特定のソースをレンダリングするか否かが分かれる場合は [TemplateTextHelper](./CodeGenerating/TemplateTextHelper.cs) で定義されている `If()` , `.ElseIf()` , `.Else()` を使用してください。
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

## デバッグ
Nijo.csprojプロジェクトのソースを修正した際は [run-for-cursor-agent.ps1](../Nijo.IntegrationTest/run-for-cursor-agent.ps1) を実行しエラーが出ないことを確認してください。
