# Отчёт по лабораторной работе №1

## Постановка задачи

В работе реализованы три структуры данных на C# под .NET 10, а также проведено сравнение с базовыми решениями для операций вставки и поиска. Источником численных результатов являются файлы `Hw1.Benchmarks.FileBucketHashBenchmarks-report.csv`, `Hw1.Benchmarks.StaticPerfectHashBenchmarks-report.csv` и `Hw1.Benchmarks.TextLshBenchmarks-report.csv` из каталога `hw1/report/artifacts`.

## Методика измерений

Замеры выполнены BenchmarkDotNet в конфигурации `Job-ZDPOZY` при рантайме `.NET 10.0` и JIT `RyuJit`. Для каждого теста использованы `WarmupCount = 5` и `IterationCount = 20`. В таблицах зафиксированы поля `Mean`, `Error`, `StdDev`, `Ratio`, `Allocated` и `Alloc Ratio` в том виде, в котором они записаны в исходных CSV.

## Результаты FileBucketHash

В этом блоке сравниваются `InsertFileHash` и `InsertDictionary` для двух размеров входа.

| Method | N | Mean | Error | StdDev | Median | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| InsertFileHash | 10000 | 61.725 μs | 42.8517 μs | 49.3481 μs | 28.700 μs | 36.75 | 32.69 | 0 B | NA |
| InsertDictionary | 10000 | 1.856 μs | 0.5328 μs | 0.5701 μs | 1.800 μs | 1.10 | 0.51 | 0 B | NA |
| InsertFileHash | 100000 | 107.090 μs | 31.3502 μs | 36.1029 μs | 109.400 μs | 19.88 | 19.62 | 0 B | NA |
| InsertDictionary | 100000 | 7.205 μs | 2.4294 μs | 2.7003 μs | 7.800 μs | 1.34 | 1.35 | 0 B | NA |

![График средней задержки FileBucketHash](artifacts/filebucket_latency.png)

По данным таблицы средняя задержка `InsertFileHash` выше, чем у `InsertDictionary`, в обоих измерениях при `N = 10000` и `N = 100000`.

## Результаты StaticPerfectHash

В этом блоке сравниваются `LookupPerfectHash` и `LookupDictionary` для двух размеров входа.

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| LookupPerfectHash | 10000 | 9.616 μs | 1.406 μs | 1.563 μs | 1.73 | 0.57 | 64 B | NA |
| LookupDictionary | 10000 | 5.990 μs | 1.443 μs | 1.662 μs | 1.08 | 0.43 | 0 B | NA |
| LookupPerfectHash | 100000 | 11.111 μs | 2.065 μs | 2.295 μs | 1.55 | 0.42 | 80 B | NA |
| LookupDictionary | 100000 | 7.421 μs | 1.309 μs | 1.455 μs | 1.03 | 0.27 | 0 B | NA |

![График средней задержки StaticPerfectHash](artifacts/perfecthash_latency.png)

По данным таблицы средняя задержка `LookupPerfectHash` выше, чем у `LookupDictionary`, в двух измеренных точках.

## Результаты TextLSH

В этом блоке сравниваются `QueryLsh` и `QueryFullScan` по времени и выделениям памяти.

| Method | N | Mean | Error | StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| QueryLsh | 1000 | 222.9 μs | 93.55 μs | 107.73 μs | 0.37 | 0.18 | 14.17 KB | 1.24 |
| QueryFullScan | 1000 | 605.8 μs | 38.26 μs | 42.52 μs | 1.00 | 0.10 | 11.44 KB | 1.00 |
| QueryLsh | 10000 | 3,251.0 μs | 1,656.01 μs | 1,840.65 μs | 0.66 | 0.37 | 11.38 KB | 0.14 |
| QueryFullScan | 10000 | 4,918.1 μs | 225.25 μs | 250.36 μs | 1.00 | 0.07 | 82.62 KB | 1.00 |

![График средней задержки TextLSH](artifacts/textlsh_latency.png)

При `N = 1000` и `N = 10000` значение `Mean` для `QueryLsh` ниже, чем для `QueryFullScan`. По памяти наблюдается разный характер на двух размерах набора, при `N = 1000` `QueryLsh` имеет `Alloc Ratio = 1.24`, а при `N = 10000` `Alloc Ratio = 0.14`.

## Итог

Отчёт сформирован на основе трёх CSV из каталога `artifacts` без добавления внешних чисел. Для каждой таблицы построен отдельный график средней задержки с интервалами `Mean ± StdDev` и вставлен в документ. Текущие результаты показывают преимущество `Dictionary` над файловой хэш-таблицей и static perfect hash в измеренных сценариях, а также преимущество LSH по времени запроса относительно полного перебора для двух рассмотренных значений `N`.

## Графики

График для сравнения `InsertFileHash` и `InsertDictionary`.

![FileBucketHash](artifacts/filebucket_latency.png)

График для сравнения `LookupPerfectHash` и `LookupDictionary`.

![StaticPerfectHash](artifacts/perfecthash_latency.png)

График для сравнения `QueryLsh` и `QueryFullScan`.

![TextLSH](artifacts/textlsh_latency.png)
