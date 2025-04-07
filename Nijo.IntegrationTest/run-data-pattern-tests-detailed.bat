@echo off
setlocal enabledelayedexpansion

echo ====================================================
echo DataPatternテスト詳細実行ツール
echo ====================================================
echo.

rem テストプロジェクトのビルド
echo ビルドを実行しています...
dotnet build %~dp0Nijo.IntegrationTest.csproj -c Debug

if %ERRORLEVEL% neq 0 (
    echo.
    echo ★★★ ビルド中にエラーが発生しました ★★★
    echo.
    pause
    exit /b 1
)

echo.
echo ビルドが完了しました。
echo.

rem テスト結果出力ディレクトリ
set RESULT_DIR=%~dp0TestResults
if not exist "%RESULT_DIR%" mkdir "%RESULT_DIR%"

rem テスト実行（詳細出力とログ記録）
echo テストを実行しています（詳細モード）...
echo.
dotnet test %~dp0Nijo.IntegrationTest.csproj --filter "FullyQualifiedName~DataPatternTest" -v detailed --logger "console;verbosity=detailed" --logger "trx;LogFileName=DataPatternTest_Results.trx" --results-directory "%RESULT_DIR%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ★★★ テスト実行中にエラーが発生しました ★★★
    echo.
    echo テスト結果は以下のディレクトリに保存されました:
    echo %RESULT_DIR%
) else (
    echo.
    echo ★★★ すべてのテストが正常に完了しました ★★★
    echo.
    echo 詳細なテスト結果は以下のディレクトリに保存されました:
    echo %RESULT_DIR%
)

echo.
echo テスト結果ファイルを開きますか？ (Y/N)
choice /C YN /M ">"

if %ERRORLEVEL% equ 1 (
    start "" "%RESULT_DIR%"
)

pause 