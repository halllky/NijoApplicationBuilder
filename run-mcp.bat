@echo off
setlocal

@rem cursorエージェントがVer1プロジェクトのデバッグ等をできるようにするためのMCPサーバーを立ち上げる。
@rem VSCode起動時に人が手動でターミナルでこのbatを叩く必要あり。

cd /d "%~dp0"

echo McpServerを起動しています...
cd Nijo.ApplicationTemplate.Ver1\McpServer
start dotnet run

if not "%errorlevel%"=="0" (
    echo エラーが発生しました。
    exit /b 1
)

@REM echo ブラウザでSwagger UIを開いています...
@REM timeout /t 3 > nul
@REM start http://localhost:5001/swagger

echo 完了しました。
echo 以下のエンドポイントが利用可能です:
echo - GET  http://localhost:5001/api/process/status
echo - POST http://localhost:5001/api/process/start
echo - POST http://localhost:5001/api/process/rebuild
echo - POST http://localhost:5001/api/process/stop

endlocal 