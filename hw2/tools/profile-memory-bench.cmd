@echo off
setlocal

set FILTER=%~1
if "%FILTER%"=="" set FILTER=*GeoKdTree*

set BENCH_DLL=benchmarks\Hw2.Benchmarks\bin\Release\net10.0\Hw2.Benchmarks.dll
if not exist report\artifacts mkdir report\artifacts

if not exist "%BENCH_DLL%" (
  echo Benchmark dll not found. Building release benchmark project...
  dotnet build benchmarks\Hw2.Benchmarks\Hw2.Benchmarks.csproj -c Release
  if errorlevel 1 exit /b 1
)

for /f %%p in ('powershell -NoProfile -Command "$p = Start-Process dotnet -ArgumentList '\"%BENCH_DLL%\" --filter \"%FILTER%\"' -PassThru; $p.Id"') do set PID=%%p
if "%PID%"=="" (
  echo Failed to start benchmark process.
  exit /b 1
)

echo Started benchmark process PID=%PID%
timeout /t 2 >nul
dotnet-gcdump collect --process-id %PID% --output report\artifacts\memory-bench.gcdump
if errorlevel 1 (
  echo Failed to collect gcdump from PID=%PID%.
  exit /b 1
)

powershell -NoProfile -Command "$proc = Get-Process -Id %PID% -ErrorAction SilentlyContinue; if ($null -ne $proc) { $proc.WaitForExit() }"
echo Done. Memory dump: report\artifacts\memory-bench.gcdump
endlocal & exit /b 0
