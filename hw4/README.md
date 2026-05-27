# Лабораторная 4 — потокобезопасная хеш-таблица

Структура: `Hw4.ConcurrentMap` (закрытая адресация), тесты xUnit (+ Microsoft Coyote), BenchmarkDotNet.

## Требования

- .NET SDK 10+
- Для графиков: Python 3 с `matplotlib` (опционально)
- Для Coyote CLI (rewrite): `dotnet tool restore` из каталога лаборатории (см. `.config/dotnet-tools.json`)

## Команды (CMD)

Из каталога `lab-4-concurrent-map`:

```
make restore
make build
make test
make test-quick
make stress
make coyote-test
make bench
make bench-smoke
make bench-full
make bench-report
make bench-kill
make graphs
make profile-help
make profile-put
```

Либо напрямую:

```
dotnet test Hw4.sln -c Release
dotnet test Hw4.sln -c Release --filter "FullyQualifiedName!~CoyoteConcurrencyTests&FullyQualifiedName!~ConcurrentHashTableStressTests"
dotnet test Hw4.sln -c Release --filter "Category=Coyote"
dotnet run -c Release --project benchmarks\Hw4.Benchmarks --no-build -- --filter "*Operation*Benchmarks*"

# Быстрая проверка BDN (Dry, T=1, не для графиков):
set HW4_BENCH_SMOKE=1&& dotnet run -c Release --project benchmarks\Hw4.Benchmarks --no-build -- --filter "*Operation*Benchmarks*"
```

- `make test-quick` (~секунды): только функциональные тесты, без Coyote и без stress (`make stress` или полный `make test` включают нагрузочные кейсы).
- `make stress`, `make coyote-test`

- `make bench` / `make bench-full` — шесть классов Operation* (Put, Get, Size, Clear, Merge, Enumerate), сравнение Custom vs `ConcurrentDictionary`; настройки из `bench.settings.json` (сейчас T=1…16, keySpace 16384, 65536 ops, warmup 5, 20 итераций).
- `make bench-smoke`: один Dry-замер чтобы проверить запуск.
- `make bench-report`: полный `bench` + `make graphs`.
- `make graphs`: CSV → `reports/figures/` (Custom / ConcurrentDict).
- `make bench-kill` - если `dotnet build` сообщает про блокировку DLL (`Hw4.ConcurrentMap.dll`).

Память включается флагом `--memory` после `--`.

**Профилирование:** `make profile-put` → `reports/profiles/hw4-put.speedscope.json` → [speedscope.app](https://www.speedscope.app/). Также `make profile-merge`, `make profile-get`.

`make coyote-test` задаёт `HW4_COYOTE_ITERATIONS=10` (можно изменить число или убрать `set …&&` чтобы брать только дефолт из кода `CoyoteConcurrencyTests`).

## Coyote (systematic testing)

Пакеты `Microsoft.Coyote` / `Microsoft.Coyote.Test` подключены к проекту тестов. По документации Coyote для полного контроля над планированием `Task` рекомендуется шаг `coyote rewrite` перед прогоном:

```
cd lab-4-concurrent-map
dotnet tool restore
dotnet build tests\Hw4.ConcurrentMap.Tests\Hw4.ConcurrentMap.Tests.csproj -c Release
dotnet tool run coyote rewrite tests\Hw4.ConcurrentMap.Tests\bin\Release\net10.0\Hw4.ConcurrentMap.Tests.dll
dotnet test Hw4.sln -c Release --no-build --filter "Category=Coyote"
```

Без rewrite тесты всё равно выполняются через `TestingEngine`, но покрытие перестановок может быть ограниченным.

## Конфигурация бенчмарков

Пример [`config/bench.settings.example.json`](config/bench.settings.example.json) 

Актуальный `benchmarks/Hw4.Benchmarks/bench.settings.json`.

Поля: `threadCounts`, `keySpace`, `opsPerInvocation`, `warmupCount`, `iterationCount`, `artifactDirectory`.
