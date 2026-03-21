@echo off
setlocal
if "%~1"=="" (
  echo Usage: tools\profile-cpu.cmd ^<pid^>
  exit /B 1
)
dotnet-trace collect --process-id %1 --format Speedscope --output report\artifacts\cpu-trace.nettrace
endlocal
