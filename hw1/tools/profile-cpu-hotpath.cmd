@echo off
setlocal

set MODE=%~1
if "%MODE%"=="" set MODE=filehash
set DURATION=%~2
if "%DURATION%"=="" set DURATION=00:02:00
set N=%~3
if "%N%"=="" set N=100000

set RUNNER_DLL=tools\Hw1.ProfileRunner\bin\Release\net10.0\Hw1.ProfileRunner.dll
if not exist report\artifacts mkdir report\artifacts

if not exist "%RUNNER_DLL%" (
  echo Profile runner dll not found. Building release profile runner...
  dotnet build tools\Hw1.ProfileRunner\Hw1.ProfileRunner.csproj -c Release
  if errorlevel 1 exit /b 1
)

echo Collecting hotpath CPU trace: mode=%MODE% duration=%DURATION% n=%N%
dotnet-trace collect --format NetTrace --duration %DURATION% --output report\artifacts\cpu-hotpath-trace-%MODE%.nettrace -- dotnet "%RUNNER_DLL%" --mode "%MODE%" --seconds 600 --n %N%
if errorlevel 1 exit /b 1

echo Done. CPU trace: report\artifacts\cpu-hotpath-trace-%MODE%.nettrace
endlocal & exit /b 0
