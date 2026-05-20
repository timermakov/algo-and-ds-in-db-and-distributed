# HW5 - Инвертированный индекс

Рабочая реализация на `.NET 10`: позиционный индекс, запросы, mmap-сегменты, ранжирование, CLI, бенчмарки.

## Структура

- `src/Hw5.SearchIndex` — библиотека (in-memory + disk/mmap + parser + ranking)
- `tests/Hw5.SearchIndex.Tests` — xUnit (unit, randomized oracle, CLI)
- `benchmarks/Hw5.Benchmarks` — BenchmarkDotNet (Warm/Cold jobs)
- `apps/Hw5.SearchCli` — интерактивный REPL
- `tools/Hw5.ProfileRunner` — плотный цикл запросов без BDN
- `reports/` — отчёт `REPORT.md`
- `diagrams/` — PlantUML

## Команды (CMD)

Из каталога `hw5/`:

```text
make restore
make build
make test
make bench-smoke
make bench
make bench-report    # bench + CSV/PNG в reports/artifacts/
make graphs          # только пересборка графиков из CSV
make run-cli
make profile-run
make profile-trace   # speedscope + nettrace в reports/profiles/
```

## Функциональность

- позиционный in-memory индекс и skip-переходы;
- парсер `Sprache` + AST (`AND/OR/NOT/ADJ/NEAR`);
- сегмент на диске, mmap, delta + bitpacking;
- ранжирование `TF-IDF` / `BM25`, TopK;
- REPL: `:add`, `:build`, `:save`, `:load`, `:mode`, `:topk`, `:stats`.

Подробности и таблицы — в `reports/REPORT.md`.
