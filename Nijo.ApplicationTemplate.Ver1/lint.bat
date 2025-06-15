@echo off 
pushd %~dp0react 
call npm run lint 
popd 
