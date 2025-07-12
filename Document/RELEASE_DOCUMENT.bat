chcp 65001 
@echo off 
 
cd %~dp0 
 
call npm run docs:build 
exit /b 0 