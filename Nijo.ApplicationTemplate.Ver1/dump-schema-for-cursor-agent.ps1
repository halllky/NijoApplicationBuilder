# 文字コードをUTF-8に設定
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================="
Write-Host "スキーマダンプツール（Cursor Agent用）"
Write-Host "===================================================="
Write-Host ""

# カレントディレクトリの取得
$currentDir = Get-Location
Write-Host "カレントディレクトリ: $currentDir"
Write-Host ""

# 出力ファイルの設定
$outputDir = Join-Path $PSScriptRoot "SchemaOutput"
$outputFile = Join-Path $outputDir "schema_dump.md"

# 出力ディレクトリの作成（存在しない場合）
if (-not (Test-Path $outputDir)) {
    Write-Host "出力ディレクトリを作成しています: $outputDir"
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

# dump.mdが既に存在する場合は削除
if (Test-Path $outputFile) {
    Remove-Item $outputFile
}

# Nijoプロジェクトのパスを特定
$nijoProjectDir = Join-Path $PSScriptRoot ".." "Nijo"
if (-not (Test-Path $nijoProjectDir)) {
    Write-Host "エラー: Nijoプロジェクトが見つかりません: $nijoProjectDir" -ForegroundColor Red
    exit 1
}

Write-Host "Nijoプロジェクトパス: $nijoProjectDir"
Write-Host ""

# Nijoプロジェクトのビルド
Write-Host "Nijoプロジェクトをビルドしています..."
Write-Host ""

dotnet build $nijoProjectDir

# Nijoのdumpコマンドを実行
Write-Host "スキーマダンプを実行しています..."
Write-Host ""

try {    
    # 対象アプリケーションへのパス
    $targetAppPath = $PSScriptRoot
    
    # dumpコマンドの実行と結果の取得
    $dumpResult = nijo dump "$targetAppPath" | Out-String
    
    # 結果をファイルに保存
    $dumpResult | Out-File -FilePath $outputFile -Encoding utf8
    
    # カレントディレクトリを元に戻す
    Set-Location $currentDir
    
    Write-Host ""
    Write-Host "★★★ スキーマダンプが正常に完了しました ★★★"
    Write-Host ""
    Write-Host "ダンプ結果は以下のファイルに保存されました:"
    Write-Host $outputFile
} catch {
    # カレントディレクトリを元に戻す
    Set-Location $currentDir
    
    Write-Host ""
    Write-Host "★★★ スキーマダンプ中にエラーが発生しました ★★★" -ForegroundColor Red
    Write-Host ""
    Write-Host "エラー詳細: $_" -ForegroundColor Red
}

# スクリプト終了時に元のディレクトリに戻る
Set-Location $currentDir 