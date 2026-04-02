@echo off
setlocal
if "%~1"=="" (
  echo Usage: profile-flamegraph.cmd ^<pid^>
  exit /b 1
)
if not exist report\artifacts mkdir report\artifacts
tasklist /FI "PID eq %1" | findstr /R /C:" %1 " >nul
if errorlevel 1 (
  echo Process with PID %1 is not running.
  echo For benchmark profiling without wrapper use: tools\profile-flamegraph-bench.cmd
  exit /b 1
)
dotnet-trace collect --process-id %1 --format Speedscope --output report\artifacts\cpu-flamegraph.speedscope.json
endlocal
