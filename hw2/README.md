# HW2

## Части задания
1) Geo KD-tree для точек `Lat/Lng`
2) Вставка объектов `Insert`
3) Пространственный поиск:
   - `SearchRadius` (поиск в радиусе)
   - `SearchKNearest` (k ближайших)

## Запуск
`make restore`
`make build`
`make test`
`make bench`
`make bench-collect`
`make report`

## Бенчмаркинг
Бенчмарки настроены через `StableBenchmarkConfig` в проекте `benchmarks/Hw2.Benchmarks`.
Текущая конфигурация использует `LaunchCount=1`, `WarmupCount=15`, `IterationCount=40`.
В benchmark-методах применяются батчи операций (`OperationsPerInvoke`) для снижения накладных расходов таймера.
Измерения делаются на логарифмической сетке `N`, а также по параметрам `RadiusMeters` и `K`.
После `make bench-collect` формируется `report/artifacts/benchmark_quality.md` со значениями `Mean`, `StdDev` и `CV`.

## Профайлинг
CPU-трассировка `make profile-cpu PID=<pid>`.
Дамп managed-кучи `make profile-memory PID=<pid>`.
Async-профилирование `make profile-async PID=<pid>` с сохранением `async-counters.csv` и `async-trace.nettrace`.
Flame graph `make profile-flamegraph PID=<pid>`, после чего `report/artifacts/cpu-flamegraph.speedscope.json` можно открыть в [speedscope](https://www.speedscope.app).
Для полного набора артефактов `make profile-all PID=<pid>`.

[Report](report/REPORT.md)