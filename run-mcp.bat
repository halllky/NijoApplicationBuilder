@echo off
setlocal

@rem cursor�G�[�W�F���g��Ver1�v���W�F�N�g�̃f�o�b�O�����ł���悤�ɂ��邽�߂�MCP�T�[�o�[�𗧂��グ��B
@rem VSCode�N�����ɐl���蓮�Ń^�[�~�i���ł���bat��@���K�v����B

cd /d "%~dp0"

echo McpServer���N�����Ă��܂�...
cd Nijo.ApplicationTemplate.Ver1\McpServer
start dotnet run

if not "%errorlevel%"=="0" (
    echo �G���[���������܂����B
    exit /b 1
)

@REM echo �u���E�U��Swagger UI���J���Ă��܂�...
@REM timeout /t 3 > nul
@REM start http://localhost:5001/swagger

echo �������܂����B
echo �ȉ��̃G���h�|�C���g�����p�\�ł�:
echo - GET  http://localhost:5001/api/process/status
echo - POST http://localhost:5001/api/process/start
echo - POST http://localhost:5001/api/process/rebuild
echo - POST http://localhost:5001/api/process/stop

endlocal 