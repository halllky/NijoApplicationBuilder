@rem ソースコード自動生成のかけなおし 
call %~dp0run-nijo-exe.bat generate 
if not "%errorlevel%"=="0" ( 
  @echo nijo.exeの実行でエラーが発生しました。 
  goto END_ERR
) 
 
@rem C#コンパイルエラーチェック 
dotnet build %~dp0 
if not "%errorlevel%"=="0" ( 
  @echo ソースコード自動生成後の .NET Core のビルドでエラーが発生しました。 
  goto END_ERR
) 
 
@rem TypeScriptエラーチェック 
call %~dp0tsc.bat generate 
if not "%errorlevel%"=="0" ( 
  @echo ソースコード自動生成後の TypeScript でエラーが発生しました。 
  goto END_ERR
) 
 
@echo ★★★ 正常終了しました ★★★ 
exit /b 0 
 
:END_ERR 
@echo ★★★ エラーが発生しました ★★★ 
exit /b 1 
 