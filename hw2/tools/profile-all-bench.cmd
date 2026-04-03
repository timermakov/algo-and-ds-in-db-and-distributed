@echo off
setlocal

set FILTER=%~1
if "%FILTER%"=="" set FILTER=*GeoKdTree*
set DURATION=%~2
if "%DURATION%"=="" set DURATION=00:00:45

call tools\profile-cpu-bench.cmd "%FILTER%" "%DURATION%"
if errorlevel 1 exit /b 1

call tools\profile-memory-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

call tools\profile-async-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

call tools\profile-flamegraph-bench.cmd "%FILTER%" "%DURATION%"
if errorlevel 1 exit /b 1

echo Bench profiling bundle completed for filter: %FILTER%
endlocal & exit /b 0
