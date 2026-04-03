@echo off
setlocal

set MODE=%~1
if "%MODE%"=="" set MODE=filehash
set DURATION=%~2
if "%DURATION%"=="" set DURATION=00:02:00
set N=%~3
if "%N%"=="" set N=100000

call tools\profile-cpu-hotpath.cmd "%MODE%" "%DURATION%" "%N%"
if errorlevel 1 exit /b 1

call tools\profile-memory-hotpath.cmd "%MODE%" "%N%"
if errorlevel 1 exit /b 1

call tools\profile-async-hotpath.cmd "%MODE%" "00:00:30" "%N%"
if errorlevel 1 exit /b 1

call tools\profile-flamegraph-hotpath.cmd "%MODE%" "%DURATION%" "%N%"
if errorlevel 1 exit /b 1

echo Hotpath profiling bundle completed: mode=%MODE% duration=%DURATION% n=%N%
endlocal & exit /b 0
