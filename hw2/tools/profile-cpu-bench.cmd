@echo off
setlocal

set FILTER=%~1
if "%FILTER%"=="" set FILTER=*GeoKdTree*
set DURATION=%~2
if "%DURATION%"=="" set DURATION=00:00:45

set BENCH_DLL=benchmarks\Hw2.Benchmarks\bin\Release\net10.0\Hw2.Benchmarks.dll
if not exist report\artifacts mkdir report\artifacts

if not exist "%BENCH_DLL%" (
  echo Benchmark dll not found. Building release benchmark project...
  dotnet build benchmarks\Hw2.Benchmarks\Hw2.Benchmarks.csproj -c Release
  if errorlevel 1 exit /b 1
)

echo Collecting CPU trace for benchmark filter: %FILTER%
dotnet-trace collect --format NetTrace --duration %DURATION% --output report\artifacts\cpu-bench-trace.nettrace -- dotnet "%BENCH_DLL%" --filter "%FILTER%"
if errorlevel 1 exit /b 1

echo Done. CPU trace: report\artifacts\cpu-bench-trace.nettrace
endlocal & exit /b 0
