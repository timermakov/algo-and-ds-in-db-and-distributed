# Отчёт по лабораторной работе №1

## Цель и источник данных

В работе реализованы `FileBucketHash`, `StaticPerfectHash` и `TextLSH` на C# под `.NET 10`.
Все численные значения ниже взяты из текущих файлов в `hw1/report/artifacts`:
- `Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv`
- `Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv`
- `Hw1.Benchmarks.TextLshBenchmarks-report.csv`
- `benchmark_quality.md`

## Конфигурация измерений

Измерения выполняются через `StableBenchmarkConfig` с параметрами `LaunchCount=1`, `WarmupCount=15` и `IterationCount=40`. Во всех benchmark-методах используется пакетный запуск операций через `OperationsPerInvoke`. Для каждого семейства тестов используется 10 логарифмически распределённых значений `N`.

## Организация benchmark-сценариев

Для `FileBucketHash` измеряется путь обновления существующего ключа, для `StaticPerfectHash` измеряется поиск по статическому индексу, для `TextLSH` измеряется запрос к LSH-индексу и к full scan как базовому сравнению. Для всех серий сохраняются `Mean`, `StdDev`, `Ratio` и данные по памяти из BenchmarkDotNet.

## Таблицы результатов

### FileBucketHash

| Method | N | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| InsertFileHash | 10000 | 110.845 ns | 3.0050 ns | 5.1027 ns | 110.366 ns | 25.88 | 1.81 | 0 B | NA |
| InsertDictionary | 10000 | 4.295 ns | 0.1314 ns | 0.2301 ns | 4.314 ns | 1.00 | 0.08 | 0 B | NA |
| InsertFileHash | 100000 | 146.956 ns | 2.7643 ns | 4.7682 ns | 145.087 ns | 35.66 | 1.65 | 0 B | NA |
| InsertDictionary | 100000 | 4.126 ns | 0.0842 ns | 0.1430 ns | 4.088 ns | 1.00 | 0.05 | 0 B | NA |

### StaticPerfectHash

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| LookupPerfectHash | 10000 | 35.39 ns | 1.621 ns | 2.838 ns | 2.76 | 0.31 | 0 B | NA |
| LookupDictionary | 10000 | 12.90 ns | 0.583 ns | 1.021 ns | 1.01 | 0.11 | 0 B | NA |
| LookupPerfectHash | 100000 | 108.79 ns | 4.943 ns | 8.258 ns | 4.97 | 0.55 | 0 B | NA |
| LookupDictionary | 100000 | 22.03 ns | 1.050 ns | 1.867 ns | 1.01 | 0.12 | 0 B | NA |

### TextLSH

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| QueryLsh | 1000 | 28.718 μs | 1.7609 μs | 2.9902 μs | 0.53 | 0.06 | 19.48 KB | 1.72 |
| QueryFullScan | 1000 | 54.167 μs | 2.0247 μs | 3.3827 μs | 1.00 | 0.09 | 11.3 KB | 1.00 |
| QueryLsh | 10000 | 693.278 μs | 30.5748 μs | 53.5494 μs | 0.73 | 0.09 | 134.16 KB | 1.64 |
| QueryFullScan | 10000 | 955.971 μs | 59.7404 μs | 103.0493 μs | 1.01 | 0.15 | 81.86 KB | 1.00 |

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

Базовые PID-скрипты (`profile-cpu`, `profile-memory`, `profile-async`, `profile-flamegraph`) сохраняют артефакты в `report/artifacts`.
Добавлен hotpath-режим:
- `make profile-flamegraph-hotpath MODE=filehash DURATION=00:02:00 N=100000`
- `make profile-flamegraph-hotpath MODE=perfecthash DURATION=00:02:00 N=100000`
- `make profile-flamegraph-hotpath MODE=lsh DURATION=00:02:00 N=10000`

По актуальным артефактам hotpath (`cpu-flamegraph-hotpath-filehash.speedscope.json`) в основном времени доминируют `Hw1.Algorithms.FileHashing.FileBucketHashTable.Insert` и связанные `MemoryMappedViewAccessor.Write/Read`, что указывает на I/O и системные вызовы как основной bottleneck в file-backed hash table.

## Вывод

`FileBucketHash` по-прежнему медленный из-за работы с и baseline `Dictionary` остаётся быстрее по latency, а `TextLSH` в среднем быстрее `FullScan` на ряде точек, но требует больше памяти. Hotpath-профилирование дополнительно показало, что для file-backed структуры ключевым ограничением являются операции записи/чтения через memory-mapped I/O, а не чистая вычислительная часть хеширования.
