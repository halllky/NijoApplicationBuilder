chcp 65001 
@rem ↑dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく 
 
@echo off 
setlocal 
 
@rem nijo.exeを最新化する。nijo.exeを滅多に編集することが無い場合は不要 ここから 
set "NIJO_ROOT=%~dp0..\" 
dotnet build %NIJO_ROOT%\Nijo\Nijo.csproj -c Debug 
@rem nijo.exeを最新化する。nijo.exeを滅多に編集することが無い場合は不要 ここまで 
 
@rem 実行 
set "NIJO_EXE=%NIJO_ROOT%\Nijo\bin\Debug\net9.0\nijo.exe" 
%NIJO_EXE% %* 
