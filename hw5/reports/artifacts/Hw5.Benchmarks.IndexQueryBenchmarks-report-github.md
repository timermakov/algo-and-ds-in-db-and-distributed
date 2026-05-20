```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

LaunchCount=1  

```
| Method              | Job  | IterationCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated | Alloc Ratio |
|-------------------- |----- |--------------- |------------ |-----------:|------------:|----------:|------:|--------:|---------:|---------:|--------:|----------:|------------:|
| Memory_AndQuery     | Cold | 4              | 0           | 1,072.6 μs | 2,208.88 μs | 341.83 μs |  1.07 |    0.42 | 281.2500 |  88.8672 |       - |   1.69 MB |        1.00 |
| DiskMmap_AndQuery   | Cold | 4              | 0           | 1,243.8 μs |    82.76 μs |  12.81 μs |  1.24 |    0.32 | 359.3750 | 166.0156 |       - |   2.16 MB |        1.28 |
| Memory_NearAdjQuery | Cold | 4              | 0           |   969.4 μs |   100.15 μs |  15.50 μs |  0.97 |    0.25 | 269.5313 |  91.7969 |       - |   1.62 MB |        0.96 |
| Memory_Bm25Top10    | Cold | 4              | 0           | 2,132.3 μs |   638.89 μs |  98.87 μs |  2.13 |    0.55 | 341.7969 |  97.6563 | 29.2969 |   1.97 MB |        1.17 |
|                     |      |                |             |            |             |           |       |         |          |          |         |           |             |
| Memory_AndQuery     | Warm | 8              | 3           |   811.9 μs |    47.02 μs |  24.59 μs |  1.00 |    0.04 | 281.2500 |  89.8438 |       - |   1.69 MB |        1.00 |
| DiskMmap_AndQuery   | Warm | 8              | 3           | 1,159.3 μs |    46.81 μs |  20.78 μs |  1.43 |    0.05 | 359.3750 | 166.0156 |       - |   2.16 MB |        1.28 |
| Memory_NearAdjQuery | Warm | 8              | 3           |   928.9 μs |   161.30 μs |  84.36 μs |  1.15 |    0.10 | 269.5313 |  91.7969 |       - |   1.62 MB |        0.96 |
| Memory_Bm25Top10    | Warm | 8              | 3           | 1,911.9 μs |   148.67 μs |  66.01 μs |  2.36 |    0.10 | 341.7969 |  97.6563 | 29.2969 |   1.97 MB |        1.17 |
