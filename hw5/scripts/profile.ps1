# CPU profile (Speedscope + nettrace) for Hw5.ProfileRunner tight loop.
# Usage: .\scripts\profile.ps1 -Mode and|mmap|bm25 -Seconds 25

param(
    [ValidateSet("and", "mmap", "bm25")]
    [string]$Mode = "and",
    [int]$Docs = 2000,
    [int]$Iterations = 3000,
    [int]$Seconds = 25
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$dll = Join-Path $root "tools\Hw5.ProfileRunner\bin\Release\net10.0\Hw5.ProfileRunner.dll"
if (-not (Test-Path $dll)) {
    Write-Host "Building ProfileRunner..."
    & dotnet build tools\Hw5.ProfileRunner\Hw5.ProfileRunner.csproj -c Release -v:q
}

$outDir = Join-Path $root "reports\profiles"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$base = Join-Path $outDir "hw5-query-$Mode"
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
    "--mode", $Mode,
    "--docs", "$Docs",
    "--iterations", "$Iterations"
)

Write-Host "Collecting -> $trace (mode=$Mode, docs=$Docs, iterations=$Iterations, ~${Seconds}s wall)"
& dotnet @collectArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ((Test-Path "$base.speedscope.json") -and ($speedscope -ne "$base.speedscope.json")) {
    Copy-Item -Force "$base.speedscope.json" $speedscope
}

& dotnet trace report $trace topN -n 25 | Out-File -Encoding utf8 $topN
Add-Content -Encoding utf8 $topN "`n--- inclusive ---`n"
& dotnet trace report $trace topN -n 25 --inclusive | Add-Content -Encoding utf8 $topN

Write-Host "Flame graph: $speedscope -> https://speedscope.app/"
Write-Host "NetTrace: $trace"
Write-Host "topN: $topN"
