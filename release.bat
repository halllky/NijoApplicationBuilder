chcp 65001 
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく 
 
@echo off 
setlocal enabledelayedexpansion 
 
set "NIJO_ROOT=%~dp0" 
set "PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト" 
set "APP_TEMPLATE_DIR=%NIJO_ROOT%Nijo.ApplicationTemplate.Ver1" 
set "APP_TEMPLATE_ZIP=%NIJO_ROOT%temp_release\Nijo.ApplicationTemplate.Ver1.zip" 
 
@rem gitでコミットされていない変更が1個以上ある場合は確認。y以外は中断 
if not "%1"=="TEST" ( 
  for /f "delims=" %%i in ('git status --porcelain') do ( 
    choice /c yn /n /m "コミットされていない変更があります。リリースを続行しますか？" 
 
    if not "!errorlevel!"=="1" ( 
      @echo リリースを中断します。 
      exit /b 1 
    ) 
    @rem yが選択された場合は、これ以上確認する必要はないのでループを抜ける 
    goto :ChangesChecked 
  ) 
) 
:ChangesChecked 
 
@rem リリースするバージョンの番号を入力する 
if "%1"=="TEST" ( 
  set "RELEASE_VERSION=x.x.x" 
) else ( 
  set /p RELEASE_VERSION="Version?: " 
) 
if "%RELEASE_VERSION%"=="" ( 
    choice /c yn /n /m "バージョンが指定されていません。リリースを続行しますか？" 
    if not "!errorlevel!"=="1" ( 
      @echo リリースを中断します。 
      exit /b 1 
    ) 
  ) 
) 
 
@rem gitで現在のリビジョンに指定されているタグの一覧を取得し、 
@rem 上記で指定されたバージョンと一致するタグが無い場合は警告 
if not "%1"=="TEST" ( 
  for /f "delims=" %%i in ('git describe --tags --exact-match') do ( 
    if "%%i"=="ver-%RELEASE_VERSION%" ( 
      goto :EXISTS_VERSION_TAG 
    ) 
  ) 
  choice /c yn /n /m "現在のリビジョンに ver-%RELEASE_VERSION% タグが打たれていません。 リリースを続行しますか？" 
  if not "!errorlevel!"=="1" ( 
    @echo リリースを中断します。 
    exit /b 1 
  ) 
) 
 
:EXISTS_VERSION_TAG 
 
@echo アプリケーションテンプレートを圧縮します: %APP_TEMPLATE_ZIP% 
mkdir "%NIJO_ROOT%temp_release" 
if exist "%APP_TEMPLATE_ZIP%" ( 
  del "%APP_TEMPLATE_ZIP%" 
) 
git archive HEAD:Nijo.ApplicationTemplate.Ver1 --format=zip -o "%APP_TEMPLATE_ZIP%" 
if not exist "%APP_TEMPLATE_ZIP%" ( 
  @echo アプリケーションテンプレートの圧縮に失敗しました。 
  exit /b 1 
) 
 
@echo ビルドを開始します。  
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=FOR_GITHUB_RELEASE_WINDOWS 
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=FOR_GITHUB_RELEASE_OSX 
 
@rem 圧縮 
if not "%1"=="TEST" ( 
  powershell /c "Compress-Archive -Path %NIJO_ROOT%Nijo\bin\Release\net9.0\publish-win\* -DestinationPath release-%RELEASE_VERSION%-win.zip" 
  powershell /c "Compress-Archive -Path %NIJO_ROOT%Nijo\bin\Release\net9.0\publish-osx\* -DestinationPath release-%RELEASE_VERSION%-osx.zip" 
) 
 
@rem 手作業でやらなければいけないことを表示 
@echo リリース %RELEASE_VERSION% を作成しました。 
 
@echo GitHubのReleaseページにアップロードしてください。 
 
