# NijoApplicationBuilder の開発者に向けた記述


## 主要なプロジェクト構成
[README.md](./README.md) を参照してください。


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
Nijo.csprojプロジェクトのソースを修正した際は以下いずれかからユニットテスト(Nijo.IntegrationTestプロジェクト)を実行しエラーが出ないことを確認してください。
- Visual Studio のテストエクスプローラー
- [run-for-cursor-agent.ps1](../Nijo.IntegrationTest/run-for-cursor-agent.ps1)
