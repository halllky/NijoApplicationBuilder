# 文字コードをUTF-8に設定
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================="
Write-Host "DataPatternテスト詳細実行ツール"
Write-Host "===================================================="
Write-Host ""

# 引数の確認
if ($args.Count -eq 0) {
    $testFilter = "Category=DataPattern"
    Write-Host "全てのデータパターンテストを実行します。"
} else {
    $testFilter = "Name~TestXmlPattern"
    Write-Host "データパターン '$($args[0])' のテストを実行します。"
}
Write-Host "テストフィルター: $testFilter"
Write-Host ""

# テストプロジェクトのビルド
Write-Host "ビルドを実行しています..."
$buildResult = dotnet build $PSScriptRoot\Nijo.IntegrationTest.csproj -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "★★★ ビルド中にエラーが発生しました ★★★"
    Write-Host ""
    Read-Host "続行するには何かキーを押してください..."
    exit 1
}

Write-Host ""
Write-Host "ビルドが完了しました。"
Write-Host ""

# テスト結果出力ディレクトリ
$resultDir = Join-Path $PSScriptRoot "TestResults"
if (-not (Test-Path $resultDir)) {
    New-Item -ItemType Directory -Path $resultDir | Out-Null
}

# テスト実行（詳細出力とログ記録）
Write-Host "テストを実行しています（詳細モード）..."
Write-Host ""

# テストケースの引数を環境変数として設定
if ($args.Count -gt 0) {
    $env:TEST_CASE = $args[0]
}

$testResult = dotnet test $PSScriptRoot\Nijo.IntegrationTest.csproj --filter "$testFilter" -v detailed --logger "console;verbosity=detailed" --logger "trx;LogFileName=DataPatternTest_Results.trx" --results-directory $resultDir

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "★★★ テスト実行中にエラーが発生しました ★★★"
    Write-Host ""
    Write-Host "テスト結果は以下のディレクトリに保存されました:"
    Write-Host $resultDir
} else {
    Write-Host ""
    Write-Host "★★★ すべてのテストが正常に完了しました ★★★"
    Write-Host ""
    Write-Host "詳細なテスト結果は以下のディレクトリに保存されました:"
    Write-Host $resultDir
}
