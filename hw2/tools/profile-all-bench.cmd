@echo off
setlocal

set FILTER=%~1
if "%FILTER%"=="" set FILTER=*GeoKdTree*

call tools\profile-cpu-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

call tools\profile-memory-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

call tools\profile-async-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

call tools\profile-flamegraph-bench.cmd "%FILTER%"
if errorlevel 1 exit /b 1

echo Bench profiling bundle completed for filter: %FILTER%
endlocal
