SHELL = cmd.exe
.SHELLFLAGS = /S /C

SOLUTION = Hw5.sln
DOTNET = dotnet
BENCH_FILTER = *Benchmarks*

.PHONY: check-sdk restore build test bench-build bench bench-smoke bench-kill bench-collect bench-report graphs compression-stats download-wiki prepare-corpus run-cli run-cli-wiki profile-help profile-run profile-trace clean

check-sdk:
	@powershell -NoProfile -Command "$$v = & '$(DOTNET)' --version; if ($$v -notmatch '^10\.') { Write-Host '.NET 10 SDK required'; exit 1 }"

restore: check-sdk
	$(DOTNET) restore $(SOLUTION)

build: check-sdk
	$(DOTNET) build $(SOLUTION) -c Release

test: build
	$(DOTNET) test $(SOLUTION) -c Release --no-build

download-wiki:
	tools\download-wiki.cmd

prepare-corpus: build
	tools\prepare-corpus.cmd

prepare-corpus-queries: build
	tools\prepare-corpus.cmd --queries-only

prepare-corpus-queries-no-stopwords: build
	tools\prepare-corpus.cmd --queries-only --no-stopwords

prepare-corpus-articles: build
	tools\prepare-corpus.cmd --to-articles-only

bench-build:
	$(DOTNET) restore benchmarks\Hw5.Benchmarks\Hw5.Benchmarks.csproj
	$(DOTNET) build benchmarks\Hw5.Benchmarks\Hw5.Benchmarks.csproj -c Release --no-restore -v:q

bench-kill:
	-taskkill /F /IM Hw5.Benchmarks.exe /T >nul 2>&1
	-taskkill /F /IM testhost.exe /T >nul 2>&1

bench: bench-kill bench-build
	@set HW5_BENCH_SMOKE=
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "$(BENCH_FILTER)"

bench-collect:
	tools\collect-bench.cmd

compression-stats: build
	$(DOTNET) run -c Release --project tools\Hw5.ProfileRunner --no-build -- --compression-stats --docs 2000

graphs:
	python scripts\plot_bench.py

bench-report: bench bench-collect compression-stats graphs

bench-wiki: bench-kill bench-build
	@set HW5_BENCH_SMOKE=
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*IndexQueryBenchmarks*"
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*OperatorBenchmarks*"
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*IndexQueryScalingBenchmarks*"
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*IndexBuildBenchmarks*"
	$(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "*RankingBenchmarks*"
	$(MAKE) bench-collect graphs

bench-smoke: bench-kill bench-build
	set HW5_BENCH_SMOKE=1&& $(DOTNET) run -c Release --project benchmarks\Hw5.Benchmarks --no-build -- --filter "$(BENCH_FILTER)"

run-cli: build
	$(DOTNET) run -c Release --project apps\Hw5.SearchCli --no-build --

run-cli-wiki: build
	if not exist data\processed\articles\*.json (echo Run: make prepare-corpus & exit /b 1)
	$(DOTNET) run -c Release --project apps\Hw5.SearchCli --no-build -- --corpus wiki --max-docs $(if $(DOCS),$(DOCS),200)

profile-help:
	@echo make profile-run MODE=and^|mmap^|bm25
	@echo make profile-trace MODE=and^|mmap^|bm25

profile-run: build
	$(DOTNET) run -c Release --project tools\Hw5.ProfileRunner --no-build -- --mode $(if $(MODE),$(MODE),and) --docs 2000 --iterations 3000

profile-trace: build
	if not exist reports\profiles mkdir reports\profiles
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts\profile.ps1 -Mode $(if $(MODE),$(MODE),and)

clean:
	$(DOTNET) clean $(SOLUTION) -c Release
