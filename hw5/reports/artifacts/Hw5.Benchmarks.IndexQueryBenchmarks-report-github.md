```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method              | Job  | WarmupCount | Case           | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
|-------------------- |----- |------------ |--------------- |-----------:|------------:|----------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
| **Memory_AndQuery**     | **Cold** | **0**           | **Synthetic_2000** | **1,117.4 μs** |   **426.11 μs** | **189.20 μs** |  **1.02** |    **0.23** | **281.2500** |  **88.5417** |       **-** | **1728.95 KB** |        **1.00** |
| DiskMmap_AndQuery   | Cold | 0           | Synthetic_2000 | 1,596.7 μs |   675.49 μs | 353.29 μs |  1.46 |    0.38 | 357.6389 | 166.6667 |       - |  2212.7 KB |        1.28 |
| Memory_NearAdjQuery | Cold | 0           | Synthetic_2000 |   402.9 μs |    44.34 μs |  23.19 μs |  0.37 |    0.06 | 120.6055 |  32.2266 |       - |  742.43 KB |        0.43 |
| Memory_Bm25Top10    | Cold | 0           | Synthetic_2000 | 1,887.6 μs |   364.27 μs | 161.74 μs |  1.73 |    0.30 | 336.8056 |  97.2222 | 27.7778 | 1994.88 KB |        1.15 |
|                     |      |             |                |            |             |           |       |         |          |          |         |            |             |
| Memory_AndQuery     | Warm | 3           | Synthetic_2000 |   834.1 μs |   154.65 μs |  68.66 μs |  1.01 |    0.11 | 281.2500 |  89.2857 |       - | 1728.95 KB |        1.00 |
| DiskMmap_AndQuery   | Warm | 3           | Synthetic_2000 | 1,717.6 μs |   202.63 μs | 105.98 μs |  2.07 |    0.19 | 359.3750 | 165.6250 |       - |  2212.7 KB |        1.28 |
| Memory_NearAdjQuery | Warm | 3           | Synthetic_2000 |   579.1 μs |   110.80 μs |  49.19 μs |  0.70 |    0.08 | 120.6055 |  31.7383 |       - |  742.43 KB |        0.43 |
| Memory_Bm25Top10    | Warm | 3           | Synthetic_2000 | 4,022.2 μs | 1,109.91 μs | 580.50 μs |  4.85 |    0.75 | 322.9167 |  83.3333 | 20.8333 | 1994.82 KB |        1.15 |
|                     |      |             |                |            |             |           |       |         |          |          |         |            |             |
| **Memory_AndQuery**     | **Cold** | **0**           | **Wikipedia_2000** |   **281.8 μs** |    **95.18 μs** |  **42.26 μs** |  **1.02** |    **0.19** |  **36.6211** |   **4.3945** |       **-** |     **227 KB** |        **1.00** |
| DiskMmap_AndQuery   | Cold | 0           | Wikipedia_2000 |   432.3 μs |    45.56 μs |  23.83 μs |  1.56 |    0.21 |  52.2461 |   6.8359 |       - |  321.79 KB |        1.42 |
| Memory_NearAdjQuery | Cold | 0           | Wikipedia_2000 |   141.6 μs |    29.99 μs |  15.69 μs |  0.51 |    0.08 |  25.6348 |   1.9531 |       - |  157.56 KB |        0.69 |
| Memory_Bm25Top10    | Cold | 0           | Wikipedia_2000 |   428.2 μs |   114.83 μs |  60.06 μs |  1.55 |    0.28 |  45.8984 |   4.8828 |       - |  281.52 KB |        1.24 |
|                     |      |             |                |            |             |           |       |         |          |          |         |            |             |
| Memory_AndQuery     | Warm | 3           | Wikipedia_2000 |   223.3 μs |    24.57 μs |   8.76 μs |  1.00 |    0.05 |  36.6211 |   4.3945 |       - |     227 KB |        1.00 |
| DiskMmap_AndQuery   | Warm | 3           | Wikipedia_2000 |   334.0 μs |    22.60 μs |  11.82 μs |  1.50 |    0.08 |  52.2461 |   6.8359 |       - |  321.79 KB |        1.42 |
| Memory_NearAdjQuery | Warm | 3           | Wikipedia_2000 |   119.3 μs |    19.23 μs |  10.06 μs |  0.53 |    0.05 |  25.6348 |   1.9531 |       - |  157.56 KB |        0.69 |
| Memory_Bm25Top10    | Warm | 3           | Wikipedia_2000 |   272.4 μs |    45.03 μs |  23.55 μs |  1.22 |    0.11 |  45.8984 |   4.8828 |       - |  281.52 KB |        1.24 |
