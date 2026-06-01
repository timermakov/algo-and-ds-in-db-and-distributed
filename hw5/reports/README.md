# Каталог отчётов HW5

## Содержимое

| Путь | Описание |
| --- | --- |
| `REPORT.md` | Основной текст отчёта (архитектура, CLI, валидация, бенчмарки, выводы) |
| `artifacts/bench_summary.md` | Таблица Warm по всем BDN-классам (авто из CSV) |
| `artifacts/analysis.md` | Глубокий разбор гипотез и ограничений (авто из CSV) |
| `artifacts/*.png` | 14 графиков (scaling, operators, corpus comparison, …) |
| `profiles/` | Speedscope / nettrace (локально, в `.gitignore`) |

## CLI

Из `hw5/`:

```text
make run-cli
```

Строки с `:` — команды REPL; без `:` — поисковый запрос. Скобки `( … )` поддерживаются.

Пример:

```text
:add 1 history of europe
:add 2 history russia china relations
:build
history AND NOT (russia AND china)
```

Подробнее — §3.1 в `REPORT.md`.

## Воспроизведение бенчмарков

Из каталога `hw5/`:

```text
make download-wiki
make prepare-corpus
make bench-report       # полный прогон ~25–37 мин → artifacts/
make bench-wiki         # Wiki N∈{2000,5000}, 5 классов ~50 мин
make bench-smoke        # быстрая проверка (~4 мин)
make graphs             # пересборка PNG из CSV
make prepare-corpus-queries              # wiki-запросы (со stopwords)
make prepare-corpus-queries-no-stopwords # A/B без stopwords
```

Конфиг: `benchmarks/Hw5.Benchmarks/bench.settings.json` — Synthetic N∈{2000,10000}, Wiki N∈{2000,5000}.

## Профилирование

```text
make profile-trace MODE=and    # или mmap, bm25
```

См. `profiles/README.md`.

## Диаграммы

PlantUML в `../diagrams/`.
