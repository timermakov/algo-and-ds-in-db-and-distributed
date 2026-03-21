@echo off
setlocal
if not exist report\artifacts mkdir report\artifacts
dotnet run --project benchmarks\Hw1.Benchmarks\Hw1.Benchmarks.csproj -c Release -- --exporters json csv markdown
echo BenchmarkDotNet artifacts are in benchmarks\Hw1.Benchmarks\BenchmarkDotNet.Artifacts
endlocal
