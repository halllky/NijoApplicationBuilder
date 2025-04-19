# 文字コードをUTF-8に設定
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================="
Write-Host "DataPatternテスト詳細実行ツール（Cursor Agent用）"
Write-Host "===================================================="
Write-Host ""

# テスト結果出力ディレクトリ
$resultDir = Join-Path $PSScriptRoot "TestResults"
if (-not (Test-Path $resultDir)) {
    New-Item -ItemType Directory -Path $resultDir | Out-Null
}

# テストプロジェクトのビルド
Write-Host "ビルドを実行しています..."
dotnet build $PSScriptRoot\Nijo.IntegrationTest.csproj -c Debug | Out-File "$resultDir\Build_Results.txt"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "★★★ ビルド中にエラーが発生しました ★★★"
    Write-Host ""
    exit 1
}

Write-Host ""
Write-Host "ビルドが完了しました。"
Write-Host ""

# 引数の確認
if ($args.Count -eq 0) {
    Write-Host "全てのデータパターンテストを実行します。各パターン毎に結果ファイルを出力します。"
    Write-Host ""
    
    # DataPatternsディレクトリからXMLファイルを取得
    $xmlFiles = Get-ChildItem -Path (Join-Path $PSScriptRoot "DataPatterns") -Filter "*.xml"
    
    foreach ($xmlFile in $xmlFiles) {
        $patternName = [System.IO.Path]::GetFileNameWithoutExtension($xmlFile.Name)
        Write-Host "========================================"
        Write-Host "データパターン '$patternName' のテストを実行中..."
        Write-Host "========================================"
        
        # 環境変数にテストケースを設定
        $env:TEST_CASE = $patternName
        
        # 個別のパターンに対してテストを実行
        $testFilter = "FullyQualifiedName~Nijo.IntegrationTest.DataPatternTest.コンパイルエラーチェック"
        $resultFileName = "DataPatternTest_${patternName}_Results.trx"
        
        Write-Host "テストフィルター: $testFilter"
        Write-Host "結果ファイル: $resultFileName"
        Write-Host ""
        
        $testResult = dotnet test $PSScriptRoot\Nijo.IntegrationTest.csproj --filter "$testFilter" -v detailed --logger "console;verbosity=detailed" --logger "trx;LogFileName=$resultFileName" --results-directory $resultDir
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "★ '$patternName' のテスト実行中にエラーが発生しました"
        } else {
            Write-Host "★ '$patternName' のテストが正常に完了しました"
        }
        Write-Host ""
    }
    
    Write-Host "========================================"
    Write-Host "全てのデータパターンのテスト実行が完了しました"
    Write-Host "テスト結果は以下のディレクトリに保存されました:"
    Write-Host $resultDir
    Write-Host "========================================"
} else {
    $patternName = $args[0]
    Write-Host "データパターン '$patternName' のテストを実行します。"
    
    # 環境変数にテストケースを設定
    $env:TEST_CASE = $patternName
    
    # テスト実行（詳細出力とログ記録）
    $testFilter = "FullyQualifiedName~Nijo.IntegrationTest.DataPatternTest.コンパイルエラーチェック"
    $resultFileName = "DataPatternTest_${patternName}_Results.trx"
    
    Write-Host "テストフィルター: $testFilter"
    Write-Host "結果ファイル: $resultFileName"
    Write-Host ""
    
    Write-Host "テストを実行しています（詳細モード）..."
    Write-Host ""
    
    $testResult = dotnet test $PSScriptRoot\Nijo.IntegrationTest.csproj --filter "$testFilter" -v detailed --logger "console;verbosity=detailed" --logger "trx;LogFileName=$resultFileName" --results-directory $resultDir
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "★★★ テスト実行中にエラーが発生しました ★★★"
        Write-Host ""
        Write-Host "テスト結果は以下のディレクトリに保存されました:"
        Write-Host $resultDir
        Write-Host "エラーの詳細は $resultDir\$resultFileName を確認してください。"
    } else {
        Write-Host ""
        Write-Host "★★★ テストが正常に完了しました ★★★"
        Write-Host ""
        Write-Host "詳細なテスト結果は以下のディレクトリに保存されました:"
        Write-Host $resultDir
    }
}
