@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト

@rem コード自動生成ツールを最新化
rmdir /s /q %NIJO_ROOT%Nijo\bin\Debug\net7.0\win-x64\ApplicationTemplates
rmdir /s /q %NIJO_ROOT%Nijo\bin\publish\ApplicationTemplates
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem コード自動生成とビルドを実行
nijo fix %PROJECT_ROOT%
npm run tsc --prefix %PROJECT_ROOT%\react
dotnet build --project %PROJECT_ROOT%\webapi
