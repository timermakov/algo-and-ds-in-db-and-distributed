# Каталог отчётов HW5

## Содержимое

| Путь | Описание |
| --- | --- |
| `REPORT.md` | Основной текст отчёта (архитектура, валидация, бенчмарки, выводы) |
| `artifacts/bench_summary.md` | Таблица Warm по всем BDN-классам |
| `artifacts/analysis.md` | Глубокий разбор гипотез и ограничений |
| `artifacts/*.png` | Графики (scaling, operators, CV, alloc, …) |
| `profiles/` | Speedscope / nettrace (локально, в `.gitignore`) |

## Воспроизведение бенчмарков

Из каталога `hw5/`:

```text
make bench-report       # полный прогон ~25–37 мин → artifacts/
make bench-smoke        # быстрая проверка (~4 мин)
make graphs             # пересборка PNG из CSV
make prepare-corpus-queries              # wiki-запросы (со stopwords)
make prepare-corpus-queries-no-stopwords # A/B без stopwords
```

Конфиг: `benchmarks/Hw5.Benchmarks/bench.settings.json` — Synthetic N∈{2000,10000}, Wiki N=5000.

## Профилирование

```text
make profile-trace MODE=and    # или mmap, bm25
```

См. `profiles/README.md`.

## Диаграммы

PlantUML в `../diagrams/`.
