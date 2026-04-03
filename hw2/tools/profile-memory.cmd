@echo off
setlocal
if "%~1"=="" (
  echo Usage: profile-memory.cmd ^<pid^>
  exit /b 1
)
if not exist report\artifacts mkdir report\artifacts
dotnet-gcdump collect --process-id %1 --output report\artifacts\memory.gcdump
endlocal
