# 文字コードをUTF-8に設定
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================="
Write-Host "DataPatternテスト詳細実行ツール"
Write-Host "===================================================="
Write-Host ""

# 引数の確認
if ($args.Count -eq 0) {
    $testFilter = "FullyQualifiedName~DataPatternTest"
    Write-Host "全てのデータパターンテストを実行します。"
} else {
    $testFilter = "FullyQualifiedName~DataPatternTest_$($args[0])"
    Write-Host "データパターン '$($args[0])' のテストを実行します。"
}
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
$testResult = dotnet test $PSScriptRoot\Nijo.IntegrationTest.csproj --filter $testFilter -v detailed --logger "console;verbosity=detailed" --logger "trx;LogFileName=DataPatternTest_Results.trx" --results-directory $resultDir

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

Write-Host ""
$openResults = Read-Host "テスト結果ファイルを開きますか？ (Y/N)"
if ($openResults -eq "Y" -or $openResults -eq "y") {
    Start-Process $resultDir
}

Read-Host "続行するには何かキーを押してください..." 