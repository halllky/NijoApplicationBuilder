@echo off

chcp 65001
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト

@rem コード自動生成ツールを最新化
dotnet build %NIJO_ROOT%Nijo\Nijo.csproj

@rem デバッグ開始
%NIJO_ROOT%Nijo\bin\Debug\net8.0\nijo.exe debug %PROJECT_ROOT%
