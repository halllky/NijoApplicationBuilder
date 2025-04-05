@rem ソースコード自動生成のかけなおし 
call %~dp0run-nijo-exe.bat generate 
if not "%errorlevel%"=="0" ( 
  @echo nijo.exeの実行でエラーが発生しました。 
  exit /b 1 
) 
 
@rem C#コンパイルエラーチェック 
dotnet build %~dp0 
if not "%errorlevel%"=="0" ( 
  @echo ソースコード自動生成後の .NET Core のビルドでエラーが発生しました。 
  exit /b 1 
) 
 
@rem TypeScriptエラーチェック 
call %~dp0tsc.bat generate 
if not "%errorlevel%"=="0" ( 
  @echo ソースコード自動生成後の TypeScript でエラーが発生しました。 
  exit /b 1 
) 
 