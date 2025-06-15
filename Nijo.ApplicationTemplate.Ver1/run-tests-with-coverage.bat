chcp 65001 
 
@echo off 
setlocal 
 
REM グローバルツールとしてreportgeneratorがインストールされているか確認 
where reportgenerator >nul 2>nul 
if %ERRORLEVEL% neq 0 ( 
    echo reportgeneratorツールがインストールされていません。以下のコマンドでインストールしてください。 
    echo dotnet tool install -g dotnet-reportgenerator-globaltool 
    exit /b 1 
) 
 
echo テストの実行とカバレッジレポート生成を開始します... 
 
REM テスト実行用のディレクトリを作成 
if not exist TestResults mkdir TestResults 
 
REM テストを実行してカバレッジ情報を収集 
dotnet test --collect:"XPlat Code Coverage" --results-directory:TestResults 
 
REM レポートディレクトリを作成 
if not exist TestResults.coveragereport mkdir TestResults.coveragereport 
 
REM カバレッジレポートを生成 
echo カバレッジレポートを生成しています... 
reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"TestResults.coveragereport" -reporttypes:Html 
 
REM レポートを開く 
echo レポートを開いています... 
start "" "TestResults.coveragereport\index.html" 
 
echo 完了しました。  
