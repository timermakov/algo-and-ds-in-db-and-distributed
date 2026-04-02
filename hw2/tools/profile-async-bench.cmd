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

echo Collecting async counters for benchmark filter: %FILTER%
dotnet-counters collect --format csv --duration 00:00:30 --output report\artifacts\async-bench-counters.csv -- dotnet "%BENCH_DLL%" --filter "%FILTER%"
if errorlevel 1 exit /b 1

echo Collecting async trace for benchmark filter: %FILTER%
dotnet-trace collect --format NetTrace --duration 00:00:30 --output report\artifacts\async-bench-trace.nettrace -- dotnet "%BENCH_DLL%" --filter "%FILTER%"
if errorlevel 1 exit /b 1

echo Done. Async artifacts:
echo   report\artifacts\async-bench-counters.csv
echo   report\artifacts\async-bench-trace.nettrace
endlocal & exit /b 0
