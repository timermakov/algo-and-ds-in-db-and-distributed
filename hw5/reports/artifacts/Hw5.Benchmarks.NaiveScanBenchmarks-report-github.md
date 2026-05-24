```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method          | Job  | WarmupCount | Case          | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|---------------- |----- |------------ |-------------- |----------:|----------:|----------:|------:|--------:|---------:|--------:|----------:|------------:|
| **IndexedAndQuery** | **Cold** | **0**           | **Synthetic_128** |  **46.21 μs** |  **7.642 μs** |  **3.997 μs** |  **1.01** |    **0.11** |  **27.0386** |  **1.7090** |  **165.7 KB** |        **1.00** |
| NaiveAndQuery   | Cold | 0           | Synthetic_128 | 151.94 μs | 18.199 μs |  9.518 μs |  3.31 |    0.32 |  33.9355 |  3.1738 | 209.23 KB |        1.26 |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| IndexedAndQuery | Warm | 3           | Synthetic_128 |  48.00 μs |  4.759 μs |  2.489 μs |  1.00 |    0.07 |  27.0386 |  1.7090 |  165.7 KB |        1.00 |
| NaiveAndQuery   | Warm | 3           | Synthetic_128 | 149.59 μs |  2.744 μs |  1.219 μs |  3.12 |    0.15 |  33.9355 |  3.1738 | 209.23 KB |        1.26 |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| **IndexedAndQuery** | **Cold** | **0**           | **Synthetic_512** | **169.74 μs** | **17.763 μs** |  **7.887 μs** |  **1.00** |    **0.06** |  **88.6230** | **18.7988** | **544.16 KB** |        **1.00** |
| NaiveAndQuery   | Cold | 0           | Synthetic_512 | 697.57 μs | 20.556 μs | 10.751 μs |  4.12 |    0.18 | 116.2109 | 33.2031 | 714.91 KB |        1.31 |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| IndexedAndQuery | Warm | 3           | Synthetic_512 | 171.66 μs | 11.443 μs |  5.985 μs |  1.00 |    0.05 |  88.6230 | 18.7988 | 544.16 KB |        1.00 |
| NaiveAndQuery   | Warm | 3           | Synthetic_512 | 714.52 μs | 67.330 μs | 29.895 μs |  4.17 |    0.21 | 116.2109 | 33.2031 | 714.91 KB |        1.31 |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| **IndexedAndQuery** | **Cold** | **0**           | **Wikipedia_128** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |       **NA** |      **NA** |        **NA** |           **?** |
| NaiveAndQuery   | Cold | 0           | Wikipedia_128 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| IndexedAndQuery | Warm | 3           | Wikipedia_128 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |
| NaiveAndQuery   | Warm | 3           | Wikipedia_128 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| **IndexedAndQuery** | **Cold** | **0**           | **Wikipedia_512** |        **NA** |        **NA** |        **NA** |     **?** |       **?** |       **NA** |      **NA** |        **NA** |           **?** |
| NaiveAndQuery   | Cold | 0           | Wikipedia_512 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |
|                 |      |             |               |           |           |           |       |         |          |         |           |             |
| IndexedAndQuery | Warm | 3           | Wikipedia_512 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |
| NaiveAndQuery   | Warm | 3           | Wikipedia_512 |        NA |        NA |        NA |     ? |       ? |       NA |      NA |        NA |           ? |

Benchmarks with issues:
  NaiveScanBenchmarks.IndexedAndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_128]
  NaiveScanBenchmarks.NaiveAndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_128]
  NaiveScanBenchmarks.IndexedAndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_128]
  NaiveScanBenchmarks.NaiveAndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_128]
  NaiveScanBenchmarks.IndexedAndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_512]
  NaiveScanBenchmarks.NaiveAndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_512]
  NaiveScanBenchmarks.IndexedAndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_512]
  NaiveScanBenchmarks.NaiveAndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_512]
