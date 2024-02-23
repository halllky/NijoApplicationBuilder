chcp 65001
@rem ↑ dotnetコマンド実行時に強制的に書き換えられてしまいnpmの標準入出力が化けるので先に書き換えておく

@echo off
setlocal

set "NIJO_ROOT=%~dp0"
set "PROJECT_ROOT=%NIJO_ROOT%自動テストで作成されたプロジェクト"

@rem gitでコミットされていない変更が1個以上ある場合は処理を中断する
for /f "delims=" %%i in ('git status --porcelain') do (
  echo コミットされていない変更があります。処理を中断します。
  pause
  exit /b 1
)

@rem リリースするバージョンの番号を入力する
set /p RELEASE_VERSION="Version?: "
if "%RELEASE_VERSION%"=="" (
  echo バージョンを指定してください。処理を中断します。
  pause
  exit /b 1
)

@rem gitで現在のリビジョンに指定されているタグの一覧を取得し、上記で指定されたバージョンと一致するタグが無い場合は処理を中断する
for /f "delims=" %%i in ('git describe --tags --exact-match') do (
  if "%%i"=="ver-%RELEASE_VERSION%" (
    goto :EXISTS_VERSION_TAG
  )
)
echo 現在のリビジョンに ver-%RELEASE_VERSION% タグが打たれていません。処理を中断します。
pause
exit /b 1

:EXISTS_VERSION_TAG

echo ビルドを開始します。
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=FOR_GITHUB_RELEASE_WINDOWS
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=FOR_GITHUB_RELEASE_OSX

@rem 圧縮
powershell /c "Compress-Archive -Path %NIJO_ROOT%Nijo\bin\Release\net8.0\publish-win\* -DestinationPath release-%RELEASE_VERSION%-win.zip"
powershell /c "Compress-Archive -Path %NIJO_ROOT%Nijo\bin\Release\net8.0\publish-osx\* -DestinationPath release-%RELEASE_VERSION%-osx.zip"

@rem 手作業でやらなければいけないことを表示
echo リリース %RELEASE_VERSION% を作成しました。

echo GitHubのReleaseページにアップロードしてください。

pause
