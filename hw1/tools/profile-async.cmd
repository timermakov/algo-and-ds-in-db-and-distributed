@echo off
setlocal
if "%~1"=="" (
  echo Usage: tools\profile-async.cmd ^<pid^>
  exit /B 1
)

if not exist report\artifacts mkdir report\artifacts
dotnet-counters collect --process-id %1 --duration 00:00:30 --format csv --output report\artifacts\async-counters.csv --counters System.Runtime[threadpool-queue-length,threadpool-thread-count,monitor-lock-contention-count,alloc-rate]
dotnet-trace collect --process-id %1 --duration 00:00:30 --format NetTrace --output report\artifacts\async-trace.nettrace
endlocal
