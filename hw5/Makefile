SHELL = cmd.exe
.SHELLFLAGS = /S /C

SOLUTION = Hw5.sln
DOTNET = dotnet

.PHONY: check-sdk restore build test bench-build bench bench-smoke bench-kill bench-collect bench-report graphs compression-stats run-cli profile-help profile-run profile-trace clean

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

bench-kill:
	-taskkill /F /IM Hw5.Benchmarks.exe /T >nul 2>&1
	-taskkill /F /IM testhost.exe /T >nul 2>&1

bench: bench-kill bench-build
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*IndexQuery*"

bench-collect:
	tools\collect-bench.cmd

compression-stats: build
	$(DOTNET) run -c Release --project tools\Hw5.ProfileRunner --no-build -- --compression-stats --docs 2000

graphs:
	python scripts\plot_bench.py

bench-report: bench bench-collect compression-stats graphs

bench-smoke: bench-kill bench-build
	set HW5_BENCH_SMOKE=1&& $(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*IndexQuery*"

run-cli: build
	$(DOTNET) run -c Release --project apps\Hw5.SearchCli --no-build --

profile-help:
	@echo make profile-run    - tight query loop without BDN
	@echo make profile-trace  - speedscope + nettrace in reports\profiles\

profile-run: build
	$(DOTNET) run -c Release --project tools\Hw5.ProfileRunner --no-build -- --docs 2000 --iterations 3000

profile-trace: build
	if not exist reports\profiles mkdir reports\profiles
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts\profile.ps1

clean:
	$(DOTNET) clean $(SOLUTION) -c Release
