@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト

@rem コード自動生成ツールを最新化
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem デバッグ開始
nijo debug %PROJECT_ROOT%
