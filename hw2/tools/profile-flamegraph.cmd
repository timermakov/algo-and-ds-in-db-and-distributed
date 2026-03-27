@echo off
setlocal
if "%~1"=="" (
  echo Usage: profile-flamegraph.cmd ^<pid^>
  exit /b 1
)
if not exist report\artifacts mkdir report\artifacts
dotnet-trace collect --process-id %1 --format Speedscope --output report\artifacts\cpu-flamegraph.speedscope.json
endlocal
