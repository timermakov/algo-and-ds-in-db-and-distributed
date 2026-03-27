@echo off
setlocal
if "%~1"=="" (
  echo Usage: profile-async.cmd ^<pid^>
  exit /b 1
)
if not exist report\artifacts mkdir report\artifacts
dotnet-counters collect --process-id %1 --format csv --output report\artifacts\async-counters.csv --duration 00:00:30
dotnet-trace collect --process-id %1 --format NetTrace --duration 00:00:30 --output report\artifacts\async-trace.nettrace
endlocal
