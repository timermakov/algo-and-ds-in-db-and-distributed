@echo off
setlocal
if "%~1"=="" (
  echo Usage: tools\profile-memory.cmd ^<pid^>
  exit /B 1
)
dotnet-gcdump collect --process-id %1 --output report\artifacts\memory.gcdump
endlocal
