@echo off
setlocal
cd /d "%~dp0.."
if not exist data\raw mkdir data\raw
set URL=https://dumps.wikimedia.org/enwiki/latest/enwiki-latest-pages-articles1.xml-p1p41242.bz2
set OUT=data\raw\enwiki-pages-articles1.xml-p1p41242.bz2
if exist "%OUT%" (
  echo Already exists: %OUT%
  exit /b 0
)
echo Downloading %URL%
curl -L -o "%OUT%" "%URL%"
if errorlevel 1 exit /b 1
echo Saved %OUT%
