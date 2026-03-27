@echo off
setlocal
if not exist report\artifacts mkdir report\artifacts
echo Preparing stable benchmark environment...
echo Please close browsers, IDE indexing, and other background-heavy workloads before running.
powercfg /S 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c >nul 2>&1
dotnet run --project benchmarks\Hw1.Benchmarks\Hw1.Benchmarks.csproj -c Release -- --filter * --exporters json csv markdown
if exist BenchmarkDotNet.Artifacts\results\* copy /Y BenchmarkDotNet.Artifacts\results\* report\artifacts\ >nul
if exist benchmarks\Hw1.Benchmarks\BenchmarkDotNet.Artifacts\results\* copy /Y benchmarks\Hw1.Benchmarks\BenchmarkDotNet.Artifacts\results\* report\artifacts\ >nul
python report\check_bench_quality.py
echo BenchmarkDotNet artifacts copied to report\artifacts
endlocal
