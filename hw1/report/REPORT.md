# Отчёт по лабораторной работе №1

## Цель и источник данных

В работе реализованы `FileBucketHash`, `StaticPerfectHash` и `TextLSH` на C# под `.NET 10`. Все численные значения в таблицах взяты только из файлов `Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv`, `Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv` и `Hw1.Benchmarks.TextLshBenchmarks-report.csv` в каталоге `hw1/report/artifacts`.

## Конфигурация измерений

Измерения выполняются через `StableBenchmarkConfig` с параметрами `LaunchCount=1`, `WarmupCount=15` и `IterationCount=40`. Во всех benchmark-методах используется пакетный запуск операций через `OperationsPerInvoke`. Для каждого семейства тестов используется 10 логарифмически распределённых значений `N`.

## Организация benchmark-сценариев

Для `FileBucketHash` измеряется путь обновления существующего ключа, для `StaticPerfectHash` измеряется поиск по статическому индексу, для `TextLSH` измеряется запрос к LSH-индексу и к full scan как базовому сравнению. Для всех серий сохраняются `Mean`, `StdDev`, `Ratio` и данные по памяти из BenchmarkDotNet.

## Таблицы результатов

### FileBucketHash

| Method | N | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| InsertFileHash | 10000 | 61.725 μs | 42.8517 μs | 49.3481 μs | 28.700 μs | 36.75 | 32.69 | 0 B | NA |
| InsertDictionary | 10000 | 1.856 μs | 0.5328 μs | 0.5701 μs | 1.800 μs | 1.10 | 0.51 | 0 B | NA |
| InsertFileHash | 100000 | 107.090 μs | 31.3502 μs | 36.1029 μs | 109.400 μs | 19.88 | 19.62 | 0 B | NA |
| InsertDictionary | 100000 | 7.205 μs | 2.4294 μs | 2.7003 μs | 7.800 μs | 1.34 | 1.35 | 0 B | NA |

### StaticPerfectHash

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| LookupPerfectHash | 10000 | 9.616 μs | 1.406 μs | 1.563 μs | 1.73 | 0.57 | 64 B | NA |
| LookupDictionary | 10000 | 5.990 μs | 1.443 μs | 1.662 μs | 1.08 | 0.43 | 0 B | NA |
| LookupPerfectHash | 100000 | 11.111 μs | 2.065 μs | 2.295 μs | 1.55 | 0.42 | 80 B | NA |
| LookupDictionary | 100000 | 7.421 μs | 1.309 μs | 1.455 μs | 1.03 | 0.27 | 0 B | NA |

### TextLSH

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| QueryLsh | 1000 | 222.9 μs | 93.55 μs | 107.73 μs | 0.37 | 0.18 | 14.17 KB | 1.24 |
| QueryFullScan | 1000 | 605.8 μs | 38.26 μs | 42.52 μs | 1.00 | 0.10 | 11.44 KB | 1.00 |
| QueryLsh | 10000 | 3,251.0 μs | 1,656.01 μs | 1,840.65 μs | 0.66 | 0.37 | 11.38 KB | 0.14 |
| QueryFullScan | 10000 | 4,918.1 μs | 225.25 μs | 250.36 μs | 1.00 | 0.07 | 82.62 KB | 1.00 |

## Графики по бенчмаркам

### FileBucketHash

![FileBucket latency](artifacts/filebucket_latency.png)
![FileBucket CV](artifacts/filebucket_cv.png)
![FileBucket speedup](artifacts/filebucket_speedup.png)
![FileBucket allocated](artifacts/filebucket_allocated.png)

График `Mean ± StdDev` показывает зависимость времени от `N` для `FileBucketHash` и базового `Dictionary`. График CV показывает относительную вариативность по тем же точкам. График speedup показывает отношение `Dictionary / FileBucketHash`, значения ниже единицы соответствуют более быстрому базовому `Dictionary`. График allocated фиксирует объём выделений памяти в тестируемых сценариях.

### StaticPerfectHash

![PerfectHash latency](artifacts/perfecthash_latency.png)
![PerfectHash CV](artifacts/perfecthash_cv.png)
![PerfectHash speedup](artifacts/perfecthash_speedup.png)
![PerfectHash allocated](artifacts/perfecthash_allocated.png)

График `Mean ± StdDev` показывает динамику времени для `StaticPerfectHash` и `Dictionary` при росте `N`. График CV дополняет сравнение относительным отклонением. График speedup отражает отношение `Dictionary / StaticPerfectHash`. График allocated показывает распределение выделений памяти между вариантами.

### TextLSH

![TextLSH latency](artifacts/textlsh_latency.png)
![TextLSH CV](artifacts/textlsh_cv.png)
![TextLSH speedup](artifacts/textlsh_speedup.png)
![TextLSH allocated](artifacts/textlsh_allocated.png)

График `Mean ± StdDev` показывает время `QueryLsh` и `QueryFullScan` в зависимости от `N`. График speedup построен как `FullScan / LSH`, значения выше единицы соответствуют ускорению LSH относительно полного перебора. График allocated показывает объём выделений памяти в обоих сценариях запроса.

## Профайлинг и FlameGraph

Для CPU трассировки используется `make profile-cpu PID=<pid>` с сохранением `cpu-trace.nettrace`. Для памяти используется `make profile-memory PID=<pid>` с сохранением `memory.gcdump`. Для async-профилирования используется `make profile-async PID=<pid>` с сохранением `async-counters.csv` и `async-trace.nettrace`. Для flame graph используется `make profile-flamegraph PID=<pid>` с сохранением `cpu-flamegraph.speedscope.json`, который открывается в `speedscope`. После `make bench-collect` автоматически формируется `benchmark_quality.md` с фактическими значениями `Mean`, `StdDev` и `CV` по всем точкам.

## Вывод

Пакет `hw1` содержит воспроизводимый набор benchmark- и profiling-сценариев для `FileBucketHash`, `StaticPerfectHash` и `TextLSH`. Команда `make bench-collect` формирует CSV/Markdown-артефакты и сводку `benchmark_quality.md`, команда `make report` обновляет графики в `report/artifacts`. Такой процесс позволяет последовательно сравнивать производительность и память по единой методике на фиксированной сетке `N`.
