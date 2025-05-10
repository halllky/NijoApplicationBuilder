chcp 65001
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく
 
@rem これはXMLスキーマ定義編集UIのデバッグ開始コマンドです。 
@rem デバッグは Windows Forms ではなくホットリロードがある vite で行います。 
 
@echo off 
setlocal 
 
set "NIJO_ROOT=%~dp0.." 
set "BACKEDN_PROJECT=%NIJO_ROOT%\Nijo" 
set "BACKEDN_EXE=%BACKEDN_PROJECT%\bin\Debug\net9.0\nijo.exe" 
set "TEMPLATE_PROJECT=%NIJO_ROOT%\Nijo.ApplicationTemplate.Ver1" 
set "FRONTEND_ROOT=%TEMPLATE_PROJECT%\react" 
 
dotnet build %BACKEDN_PROJECT% 
 
start %BACKEDN_EXE% run-ui-service %TEMPLATE_PROJECT% --port 8081 
start npm run dev --prefix %FRONTEND_ROOT% 
start http://localhost:5173/nijo-ui/ 
