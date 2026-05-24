```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method              | Job  | WarmupCount | Case           | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
|-------------------- |----- |------------ |--------------- |-----------:|----------:|---------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
| **Memory_AndQuery**     | **Cold** | **0**           | **Synthetic_2000** |   **674.2 μs** |  **54.96 μs** | **28.75 μs** |  **1.00** |    **0.06** | **281.2500** |  **88.8672** |       **-** | **1728.95 KB** |        **1.00** |
| DiskMmap_AndQuery   | Cold | 0           | Synthetic_2000 | 1,068.1 μs |  30.00 μs | 15.69 μs |  1.59 |    0.07 | 359.3750 | 166.6667 |       - |  2212.7 KB |        1.28 |
| Memory_NearAdjQuery | Cold | 0           | Synthetic_2000 |   322.6 μs |  25.47 μs | 11.31 μs |  0.48 |    0.02 | 120.6055 |  32.2266 |       - |  742.43 KB |        0.43 |
| Memory_Bm25Top10    | Cold | 0           | Synthetic_2000 | 1,802.4 μs |  19.06 μs |  8.46 μs |  2.68 |    0.11 | 336.8056 |  97.2222 | 27.7778 | 1994.88 KB |        1.15 |
|                     |      |             |                |            |           |          |       |         |          |          |         |            |             |
| Memory_AndQuery     | Warm | 3           | Synthetic_2000 |   690.9 μs |  26.54 μs | 11.78 μs |  1.00 |    0.02 | 281.2500 |  88.8672 |       - | 1728.95 KB |        1.00 |
| DiskMmap_AndQuery   | Warm | 3           | Synthetic_2000 | 1,090.8 μs |  62.99 μs | 32.94 μs |  1.58 |    0.05 | 359.3750 | 166.6667 |       - |  2212.7 KB |        1.28 |
| Memory_NearAdjQuery | Warm | 3           | Synthetic_2000 |   355.1 μs |  37.66 μs | 16.72 μs |  0.51 |    0.02 | 120.6055 |  31.7383 |       - |  742.43 KB |        0.43 |
| Memory_Bm25Top10    | Warm | 3           | Synthetic_2000 | 1,836.6 μs | 135.32 μs | 60.08 μs |  2.66 |    0.09 | 336.8056 |  97.2222 | 27.7778 | 1994.88 KB |        1.15 |
|                     |      |             |                |            |           |          |       |         |          |          |         |            |             |
| **Memory_AndQuery**     | **Cold** | **0**           | **Wikipedia_5000** |         **NA** |        **NA** |       **NA** |     **?** |       **?** |       **NA** |       **NA** |      **NA** |         **NA** |           **?** |
| DiskMmap_AndQuery   | Cold | 0           | Wikipedia_5000 |         NA |        NA |       NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| Memory_NearAdjQuery | Cold | 0           | Wikipedia_5000 |   760.0 μs |  18.55 μs |  9.70 μs |     ? |       ? | 139.6484 |  39.0625 |       - |  953.27 KB |           ? |
| Memory_Bm25Top10    | Cold | 0           | Wikipedia_5000 | 4,234.3 μs |  85.10 μs | 37.78 μs |     ? |       ? | 640.6250 | 179.6875 |       - | 4249.17 KB |           ? |
|                     |      |             |                |            |           |          |       |         |          |          |         |            |             |
| Memory_AndQuery     | Warm | 3           | Wikipedia_5000 |         NA |        NA |       NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| DiskMmap_AndQuery   | Warm | 3           | Wikipedia_5000 |         NA |        NA |       NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| Memory_NearAdjQuery | Warm | 3           | Wikipedia_5000 |   754.0 μs |  23.59 μs | 12.34 μs |     ? |       ? | 139.6484 |  39.0625 |       - |  953.27 KB |           ? |
| Memory_Bm25Top10    | Warm | 3           | Wikipedia_5000 | 4,279.9 μs | 116.80 μs | 61.09 μs |     ? |       ? | 640.6250 | 179.6875 |       - | 4249.17 KB |           ? |

Benchmarks with issues:
  IndexQueryBenchmarks.Memory_AndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  IndexQueryBenchmarks.DiskMmap_AndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  IndexQueryBenchmarks.Memory_AndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
  IndexQueryBenchmarks.DiskMmap_AndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
