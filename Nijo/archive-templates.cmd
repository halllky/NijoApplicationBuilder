@echo off

@REM アプリケーションテンプレートをzip化してビルド後ディレクトリに配置する。
@REM このスクリプトはビルド成功後に実行される。
@REM - 第1引数: MSBuildの $(ProjectDir)
@REM - 第2引数: MSBuildの $(OutDir)

set PROJECT_DIR=%1
set OUT_DIR=%PROJECT_DIR%%2

set ZIP_PATH=%OUT_DIR%templates.zip
set UNZIP_PATH=%OUT_DIR%ApplicationTemplates
set TEMPLATE_DIR=%PROJECT_DIR%..\Nijo.ApplicationTemplates

@REM -------------------------------------------
echo:
echo アプリケーションテンプレートの同梱を開始します。
echo TEMPLATE_DIR: %TEMPLATE_DIR%
echo ZIP_PATH:     %ZIP_PATH%
echo UNZIP_PATH:   %UNZIP_PATH%
echo:

@REM zip化
pushd %TEMPLATE_DIR%
git archive --output="%ZIP_PATH%" HEAD
popd

@REM 現在の解凍後ディレクトリを削除
del /s /q "%UNZIP_PATH%" >nul

@REM zipを解凍
call powershell -command "Expand-Archive -Force %ZIP_PATH% %UNZIP_PATH%"

@REM zipを削除
del /q "%ZIP_PATH%"

echo:
exit /b
