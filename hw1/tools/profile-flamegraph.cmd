@echo off
setlocal
if "%~1"=="" (
  echo Usage: tools\profile-flamegraph.cmd ^<pid^>
  exit /B 1
)

if not exist report\artifacts mkdir report\artifacts
dotnet-trace collect --process-id %1 --format Speedscope --output report\artifacts\cpu-flamegraph.speedscope.json
echo Open report\artifacts\cpu-flamegraph.speedscope.json in https://www.speedscope.app and select "Left Heavy" to inspect flame graph.
endlocal
