@echo off
setlocal

set MODE=%~1
if "%MODE%"=="" set MODE=filehash
set N=%~2
if "%N%"=="" set N=100000

set RUNNER_DLL=tools\Hw1.ProfileRunner\bin\Release\net10.0\Hw1.ProfileRunner.dll
if not exist report\artifacts mkdir report\artifacts

if not exist "%RUNNER_DLL%" (
  echo Profile runner dll not found. Building release profile runner...
  dotnet build tools\Hw1.ProfileRunner\Hw1.ProfileRunner.csproj -c Release
  if errorlevel 1 exit /b 1
)

for /f %%p in ('powershell -NoProfile -Command "$p = Start-Process dotnet -ArgumentList '\"%RUNNER_DLL%\" --mode \"%MODE%\" --seconds 600 --n %N%' -PassThru; $p.Id"') do set PID=%%p
if "%PID%"=="" (
  echo Failed to start hotpath process.
  exit /b 1
)

echo Started hotpath process PID=%PID%
timeout /t 2 >nul
dotnet-gcdump collect --process-id %PID% --output report\artifacts\memory-hotpath-%MODE%.gcdump
if errorlevel 1 (
  echo Failed to collect gcdump from PID=%PID%.
  exit /b 1
)

powershell -NoProfile -Command "$proc = Get-Process -Id %PID% -ErrorAction SilentlyContinue; if ($null -ne $proc) { $proc.WaitForExit() }"
echo Done. Memory dump: report\artifacts\memory-hotpath-%MODE%.gcdump
endlocal & exit /b 0
