SHELL = cmd.exe
.SHELLFLAGS = /S /C

SOLUTION = Hw5.sln
DOTNET = dotnet

.PHONY: check-sdk restore build test bench-build bench run-cli profile-help profile-run clean

check-sdk:
	@powershell -NoProfile -Command "$$v = & '$(DOTNET)' --version; if ($$v -notmatch '^10\.') { Write-Host '.NET 10 SDK required'; exit 1 }"

restore: check-sdk
	$(DOTNET) restore $(SOLUTION)

build: check-sdk
	$(DOTNET) build $(SOLUTION) -c Release

test: build
	$(DOTNET) test $(SOLUTION) -c Release --no-build

bench-build:
	$(DOTNET) restore benchmarks\Hw5.Benchmarks\Hw5.Benchmarks.csproj
	$(DOTNET) build benchmarks\Hw5.Benchmarks\Hw5.Benchmarks.csproj -c Release --no-restore -v:q

bench: bench-build
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build --

run-cli: build
	$(DOTNET) run -c Release --project apps\Hw5.SearchCli --no-build --

profile-help:
	@echo make profile-run  - run placeholder profile tool

profile-run: build
	$(DOTNET) run -c Release --project tools\Hw5.ProfileRunner --no-build --

clean:
	$(DOTNET) clean $(SOLUTION) -c Release
