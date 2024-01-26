@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト

@rem コード自動生成ツールを最新化
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

robocopy /s /NFL /NDL /NJH /NJS /nc /ns /np ^
  %NIJO_ROOT%Nijo\bin\Release\net8.0\win-x64\ApplicationTemplates ^
  %NIJO_ROOT%Nijo\bin\publish\ApplicationTemplates

@rem コード自動生成とビルドを実行
nijo fix %PROJECT_ROOT%

@rem コンパイルチェック
npm run tsc --prefix %PROJECT_ROOT%\react
dotnet build --project %PROJECT_ROOT%\webapi
