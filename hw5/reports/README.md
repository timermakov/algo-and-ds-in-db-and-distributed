# Каталог отчётов HW5

## Содержимое

| Путь | Описание |
| --- | --- |
| `REPORT.md` | Основной текст отчёта (архитектура, валидация, бенчмарки, выводы) |
| `artifacts/` | CSV/HTML BDN, `bench_summary.md`, PNG-графики, `compression_stats.json` |
| `profiles/` | Speedscope / nettrace / topN (локально, в `.gitignore`) |

## Воспроизведение бенчмарков

Из каталога `hw5/` (CMD + GNU Make):

```text
make bench              # полный BDN (2000 док, 8 итераций Warm)
make bench-collect      # копия CSV в reports/artifacts/
make compression-stats  # compression_stats.json
make graphs             # plot_bench.py → PNG + bench_summary.md
make bench-report       # bench + collect + compression + graphs
make bench-smoke        # быстрый smoke (128 док, 1 итерация)
```

Настройки корпуса: `benchmarks/Hw5.Benchmarks/bench.settings.json`.

Требования для графиков: Python 3 + `matplotlib` (`pip install matplotlib`).

## Профилирование

```text
make profile-run      # плотный цикл без dotnet-trace (оценка wall-time)
make profile-trace    # hw5-query-loop.nettrace + .speedscope.json + topN.txt
```

Открыть flame graph: [speedscope.app](https://www.speedscope.app/) → файл `reports/profiles/hw5-query-loop.speedscope.json` (режим Left Heavy).

Сводка hotspots: `reports/profiles/README.md`.

## Диаграммы

Исходники PlantUML в `../diagrams/`. PNG в отчёт не обязателен — схемы читаются из `.puml` в IDE/онлайн-рендерере.
