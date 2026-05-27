SHELL = cmd.exe

.SHELLFLAGS = /S /C



.PHONY: restore build bench-build test test-quick bench bench-smoke bench-full bench-report bench-kill stress coyote-test graphs profile-help profile-put profile-merge profile-get clean



restore:

	dotnet restore Hw4.sln



build:

	dotnet build Hw4.sln -c Release



# Только Hw4.ConcurrentMap → Hw4.Benchmarks — без Hw4.ConcurrentMap.Tests (нет копирования DLL в bin тестов, реже MSB3021 при залоченном testhost)

# Restore отдельно: при устаревшем obj/project.assets.json build без восстановления пакетов даёт CS0246 (BenchmarkDotNet).

bench-build:

	dotnet restore benchmarks\Hw4.Benchmarks\Hw4.Benchmarks.csproj

	dotnet build benchmarks\Hw4.Benchmarks\Hw4.Benchmarks.csproj -c Release --no-restore -v:q



test: build

	dotnet test Hw4.sln -c Release --no-build



# только functional-тесты: без Coyote и без многопоточного stress (~тысячи merge/put под нагрузкой)

test-quick: build

	dotnet test Hw4.sln -c Release --no-build --filter "FullyQualifiedName!~CoyoteConcurrencyTests&FullyQualifiedName!~ConcurrentHashTableStressTests"



stress: build

	dotnet test Hw4.sln -c Release --no-build --filter "Category=Stress"



coyote-test: build

	set HW4_COYOTE_ITERATIONS=10&& dotnet test Hw4.sln -c Release --no-build --filter "Category=Coyote"



# Завершить процессы, держащие Hw4.ConcurrentMap.dll / узел бенча (иначе MSB3021 при сборке решения).

bench-kill:

	-taskkill /F /IM Hw4.Benchmarks.exe /T >nul 2>&1

	-taskkill /F /IM testhost.exe /T >nul 2>&1

	-taskkill /F /IM vstest.console.exe /T >nul 2>&1



# Полный прогон: warmup + 12 итераций, threadCounts из bench.settings.json (долго, но стабильные Error/StdDev).

bench: bench-kill bench-build

	dotnet run -c Release --project benchmarks\Hw4.Benchmarks --no-build -- --filter "*Operation*Benchmarks*"



bench-full: bench



# Один Dry-замер на сценарий

bench-smoke: bench-kill bench-build

	set HW4_BENCH_SMOKE=1&& dotnet run -c Release --project benchmarks\Hw4.Benchmarks --no-build -- --filter "*Operation*Benchmarks*"


bench-report: bench graphs



graphs:

	python scripts\plot_bench.py



profile-help:

	@echo make profile-put    - 30s CPU loop Put  -^> reports\profiles\hw4-put.speedscope.json

	@echo make profile-merge  - 30s CPU loop Merge

	@echo make profile-get    - 30s CPU loop Get

	@echo Open Speedscope: https://speedscope.app/

profile-put: bench-build

	if not exist reports\profiles mkdir reports\profiles

	powershell -NoProfile -ExecutionPolicy Bypass -File scripts\profile.ps1 -Mode put

profile-merge: bench-build

	powershell -NoProfile -ExecutionPolicy Bypass -File scripts\profile.ps1 -Mode merge

profile-get: bench-build

	powershell -NoProfile -ExecutionPolicy Bypass -File scripts\profile.ps1 -Mode get



clean:

	dotnet clean Hw4.sln -c Release

