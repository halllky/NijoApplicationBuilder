# テスト実行手順

テストランナーには NUnit 、カバレッジ収集ツールには Coverlet を使用しています。
以下のコマンドでテストを実行すると [TestResults](./TestResults/) フォルダに結果が出力されます。

```cmd
dotnet test --collect:"XPlat Code Coverage"
```

この結果を閲覧するには `dotnet-reportgenerator-globaltool` がインストールされている必要があります。

```cmd
dotnet tool install -g dotnet-reportgenerator-globaltool
```

## 推奨：テストとレポート生成を同時実行

自動生成ファイルの警告を避けるため、以下のコマンドでテスト実行とレポート生成を連続して行うことを推奨します：

```cmd
dotnet test --collect:"XPlat Code Coverage" && reportgenerator -reports:"TestResults\*\coverage.cobertura.xml" -targetdir:"TestResults\coveragereport" -reporttypes:Html -filefilters:-*.g.cs
```

## 個別実行の場合

TestResultsフォルダ内の、ファイル更新時刻がテスト実施時刻になっているフォルダのGUIDを控えておき、
以下のコマンドを実行して結果をhtmlで閲覧することができます。

```cmd
cd このREADMEがあるフォルダ

reportgenerator ^
  -reports:"TestResults\{guid}\coverage.cobertura.xml" ^
  -targetdir:"TestResults\coveragereport" ^
  -reporttypes:Html ^
  -filefilters:-*.g.cs
```
