# HW1

## Части задания
1) Hash table на файловой системе (insert, update, delete)
2) Perfect hash (build static index, lookup)
3) LSH для текстов (build index, add document, full scan duplicates)

## Запуск
`make restore`
`make build`
`make test`
`make bench`
`make bench-collect`
`make report`

## Бенчмаркинг
Бенчмарки настроены через `StableBenchmarkConfig` в проекте `benchmarks/Hw1.Benchmarks`.
Конфигурация повышенной повторяемости использует не менее 10 запусков (`LaunchCount=10`) и увеличенные warmup/measurement итерации.
Внутри benchmark-методов применяются батчи операций (`OperationsPerInvoke`), чтобы снизить шум таймера и накладных расходов инфраструктуры.
Для каждого benchmark-класса используется 10 логарифмических значений `N`.
После `make bench-collect` формируется `report/artifacts/benchmark_quality.md` с фактическими значениями `Mean`, `StdDev` и `CV` по всем точкам.

## Профайлинг
CPU-трассировка `make profile-cpu PID=<pid>`.
дамп managed-кучи `make profile-memory PID=<pid>`.
async-профилирование `make profile-async PID=<pid>` с сохранением `async-counters.csv` и `async-trace.nettrace`.
flame graph `make profile-flamegraph PID=<pid>`, после чего `report/artifacts/cpu-flamegraph.speedscope.json` в [speedscope](https://www.speedscope.app).
Для полного набора артефактов `make profile-all PID=<pid>`.
