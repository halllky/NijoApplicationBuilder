chcp 65001 
@rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく 
 
@echo off 
setlocal 
 
@rem nijo.exeを最新化する。nijo.exeを滅多に編集することが無い場合は不要 ここから 
set "NIJO_ROOT=%~dp0..\" 
dotnet build %NIJO_ROOT%\Nijo\Nijo.csproj -c Debug 
if not "%errorlevel%"=="0" ( 
  @echo nijo.exeの最新化でエラーが発生しました。 
  exit /b 1 
) 
@rem nijo.exeを最新化する。nijo.exeを滅多に編集することが無い場合は不要 ここまで 
 
@rem 実行 
set "NIJO_EXE=%NIJO_ROOT%\Nijo\bin\Debug\net9.0\nijo.exe" 
pushd %~dp0 
%NIJO_EXE% %* 
if not "%errorlevel%"=="0" ( 
  @echo nijo.exeの実行でエラーが発生しました。 
  popd
  exit /b 1 
) 
popd 
