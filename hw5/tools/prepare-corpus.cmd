@echo off
setlocal
cd /d "%~dp0.."
dotnet build tools\Hw5.CorpusPrep\Hw5.CorpusPrep.csproj -c Release -v:q
if errorlevel 1 exit /b 1
dotnet run -c Release --project tools\Hw5.CorpusPrep --no-build -- %*
