@echo off
setlocal
if "%~1"=="" (
  echo Usage: tools\profile-cpu.cmd ^<pid^>
  exit /B 1
)
if not exist report\artifacts mkdir report\artifacts
dotnet-trace collect --process-id %1 --format NetTrace --output report\artifacts\cpu-trace.nettrace
endlocal
