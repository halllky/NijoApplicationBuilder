chcp 65001
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく
 
@rem これはXMLスキーマ定義編集UIのビルドおよび発行コマンドです。 
@rem 以下3プロジェクトが絡んでいるので順番にビルドします。 
@rem * react: viteのビルドを行いhtml,css,jsを1つのhtmlファイルにまとめます。 
@rem * Nijo.csproj: バックエンド処理本体。これのビルド時にreactのバンドルファイルをexeに埋め込みます。 
@rem * Nijo.Ui.csproj: Windows Formsのexeをビルドします。Nijo.csprojをプロジェクト参照しています。 
 
@echo off 
setlocal 
 
set "NIJO_ROOT=%~dp0.." 
set "WINFORMS_PROJECT=%NIJO_ROOT%\Nijo.Ui" 
set "PUBLISH_FOLDER=%WINFORMS_PROJECT%\bin\Release\net9.0-windows\publish" 
set "FRONTEND_ROOT=%NIJO_ROOT%\Nijo.ApplicationTemplate.Ver1\react" 

call npm run build:nijo-ui --prefix %FRONTEND_ROOT% 
 
if not "%errorlevel%"=="0" ( 
  @echo ビルドに失敗しました。 
  exit /b 1 
) 
 
dotnet publish %WINFORMS_PROJECT% -p:PublishProfile=FolderProfile 
 
if not "%errorlevel%"=="0" ( 
  @echo ビルドに失敗しました。 
  exit /b 1 
) 
 
@echo ビルドおよび発行が完了しました。フォルダを開きます。 
%SystemRoot%\explorer.exe %PUBLISH_FOLDER% 
