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
make bench-smoke       # быстрая проверка (~4 мин)
make bench-report      # полный прогон ~25–37 мин → reports/artifacts/
make bench-wiki        # Wiki-focused ~50 мин (5 BDN-классов)
make download-wiki     # shard Wikipedia (~296 MB)
make prepare-corpus    # articles/ + bench-запросы
make prepare-corpus-queries              # только wiki-bench-queries.txt (со stopwords)
make prepare-corpus-queries-no-stopwords # bench-запросы без фильтра stopwords
make graphs            # только пересборка графиков из CSV
make run-cli
make run-cli-wiki              # Wikipedia, 200 док. (по умолчанию)
make run-cli-wiki DOCS=2000    # Wikipedia, первые N документов
make profile-run
make profile-trace   # speedscope + nettrace в reports/profiles/
```

## CLI (интерактивный поиск)

```text
make run-cli
```

| Команда | Действие |
| --- | --- |
| `:add <id> <text>` | Добавить документ |
| `:build` | Зафиксировать индекс |
| `:save <path>` / `:load <path>` | Сегмент на диск / mmap |
| `:mode bm25\|tfidf` | Ранжирование |
| `:topk <n>` | Число результатов |
| `:stats` | Статистика индекса |
| `:help` / `:exit` | Справка / выход |

**Запросы** (строка без `:`):

- Операторы: `AND`, `OR`, `NOT`, `ADJ`, `NEAR`, `NEAR/k`.
- **Скобки работают:** `(russia AND china)` группирует подзапрос.
- Приоритет: `NOT` > `ADJ`/`NEAR` > `AND` > `OR`.

Пример:

```text
:add 1 history russia china
:add 2 history russia war
:build
history AND NOT (russia AND china)
```

→ документ **2** (есть `history`, но нет пары `russia`+`china` в одном документе).

Wikipedia в CLI не подгружается автоматически — добавляйте документы через `:add` или загрузите свой сегмент `:load`.

Для Wikipedia из `docs.jsonl`:

```text
make run-cli-wiki              # 200 документов (по умолчанию)
make run-cli-wiki DOCS=2000      # первые 2000
```

Или напрямую: `dotnet run --project apps/Hw5.SearchCli -- --corpus wiki --max-docs 500`

## Функциональность

- позиционный in-memory индекс и skip-переходы;
- парсер `Sprache` + AST (`AND/OR/NOT/ADJ/NEAR`);
- сегмент на диске, mmap, delta + bitpacking;
- ранжирование `TF-IDF` / `BM25`, TopK;
- REPL: `:add`, `:build`, `:save`, `:load`, `:mode`, `:topk`, `:stats`.

Подробности и таблицы — в `reports/REPORT.md`.
