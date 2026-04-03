@echo off
setlocal

set MODE=%~1
if "%MODE%"=="" set MODE=filehash
set DURATION=%~2
if "%DURATION%"=="" set DURATION=00:00:30
set N=%~3
if "%N%"=="" set N=100000

set RUNNER_DLL=tools\Hw1.ProfileRunner\bin\Release\net10.0\Hw1.ProfileRunner.dll
if not exist report\artifacts mkdir report\artifacts

if not exist "%RUNNER_DLL%" (
  echo Profile runner dll not found. Building release profile runner...
  dotnet build tools\Hw1.ProfileRunner\Hw1.ProfileRunner.csproj -c Release
  if errorlevel 1 exit /b 1
)

echo Collecting hotpath async counters: mode=%MODE% duration=%DURATION% n=%N%
dotnet-counters collect --format csv --duration %DURATION% --output report\artifacts\async-hotpath-counters-%MODE%.csv -- dotnet "%RUNNER_DLL%" --mode "%MODE%" --seconds 600 --n %N%
if errorlevel 1 exit /b 1

echo Collecting hotpath async trace: mode=%MODE% duration=%DURATION% n=%N%
dotnet-trace collect --format NetTrace --duration %DURATION% --output report\artifacts\async-hotpath-trace-%MODE%.nettrace -- dotnet "%RUNNER_DLL%" --mode "%MODE%" --seconds 600 --n %N%
if errorlevel 1 exit /b 1

echo Done. Async artifacts:
echo   report\artifacts\async-hotpath-counters-%MODE%.csv
echo   report\artifacts\async-hotpath-trace-%MODE%.nettrace
endlocal & exit /b 0
