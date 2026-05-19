@echo off
setlocal
cd /d "%~dp0.."
if not exist reports\artifacts mkdir reports\artifacts
if exist BenchmarkDotNet.Artifacts\hw5\results\* copy /Y BenchmarkDotNet.Artifacts\hw5\results\* reports\artifacts\ >nul
if exist benchmarks\Hw5.Benchmarks\BenchmarkDotNet.Artifacts\hw5\results\* copy /Y benchmarks\Hw5.Benchmarks\BenchmarkDotNet.Artifacts\hw5\results\* reports\artifacts\ >nul
echo Artifacts copied to reports\artifacts
