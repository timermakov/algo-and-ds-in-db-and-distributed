# CPU profile (Speedscope) — tight loop without BenchmarkDotNet.
# Usage: .\scripts\profile.ps1 -Mode put -Threads 8 -Seconds 30

param(
    [ValidateSet("put", "merge", "get")]
    [string]$Mode = "put",
    [int]$Threads = 8,
    [int]$Seconds = 30
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$dll = Join-Path $root "benchmarks\Hw4.Benchmarks\bin\Release\net10.0\Hw4.Benchmarks.dll"
if (-not (Test-Path $dll)) {
    Write-Host "Building benchmarks..."
    & make bench-build
}

$outDir = Join-Path $root "reports\profiles"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$base = Join-Path $outDir "hw4-$Mode"
$trace = "$base.nettrace"
$speedscope = "$base.speedscope.json"
$topN = "$base-topN.txt"

$collectArgs = @(
    "trace", "collect",
    "-o", $trace,
    "--format", "Speedscope",
    "--profile", "dotnet-sampled-thread-time",
    "--",
    "dotnet", "exec", $dll,
    "--hw4-profile-loop", $Mode,
    "--threads", "$Threads",
    "--seconds", "$Seconds"
)

Write-Host "Collecting -> $trace ($Mode, threads=$Threads, ${Seconds}s)"
& dotnet @collectArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& dotnet trace report $trace topN -n 25 | Out-File -Encoding utf8 $topN
Add-Content -Encoding utf8 $topN "`n--- inclusive ---`n"
& dotnet trace report $trace topN -n 25 --inclusive | Add-Content -Encoding utf8 $topN

Write-Host "Flame graph: $speedscope -> https://speedscope.app/"
Write-Host "topN: $topN"
