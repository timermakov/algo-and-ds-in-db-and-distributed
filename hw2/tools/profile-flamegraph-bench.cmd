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

echo Collecting flamegraph for benchmark filter: %FILTER%
dotnet-trace collect --format Speedscope --output report\artifacts\cpu-flamegraph -- dotnet "%BENCH_DLL%" --filter "%FILTER%"

echo Done. Open report\artifacts\cpu-flamegraph.speedscope.json in speedscope.
endlocal
