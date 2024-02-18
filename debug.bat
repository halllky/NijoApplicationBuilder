@echo off

chcp 65001
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト

@rem コード自動生成ツールを最新化
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem デバッグ開始
nijo debug %PROJECT_ROOT%
