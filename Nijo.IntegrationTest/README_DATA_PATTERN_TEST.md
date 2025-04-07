# DataPatternテスト実行ガイド

このテストスイートは、`DataPatterns`フォルダ内のすべてのXMLファイルに対してテストを実行します。

## テスト実行方法

次の2つのバッチファイルが用意されています：

### 1. 通常テスト実行

```
run-data-pattern-tests.bat
```

このバッチファイルは以下を実行します：
- プロジェクトのビルド
- DataPatternTestの実行（標準出力モード）
- テスト結果の表示

### 2. 詳細テスト実行

```
run-data-pattern-tests-detailed.bat
```

このバッチファイルは以下を実行します：
- プロジェクトのビルド
- DataPatternTestの詳細実行（詳細出力モード）
- テスト結果をTRXファイルとして保存
- テスト結果フォルダを開くオプション

## テストの追加方法

新しいテストケースを追加するには、`DataPatterns`フォルダに新しいXMLファイルを追加するだけです。
テスト実行時に自動的に検出されます。

## テスト内容のカスタマイズ

`DataPatternTest.cs`ファイルの`TestXmlPattern`メソッド内に、XMLファイルに対する
テストロジックを実装してください。

```csharp
[Test]
[TestCaseSource(nameof(GetXmlFilePaths))]
public void TestXmlPattern(string xmlFilePath)
{
    // ファイル名を取得
    string fileName = Path.GetFileName(xmlFilePath);

    // ここにテストロジックを実装
    // 例: XMLの読み込み、解析、検証など

    Assert.Pass($"{fileName} のテストが完了しました");
}
```
