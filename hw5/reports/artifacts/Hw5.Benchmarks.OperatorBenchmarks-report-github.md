```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method    | Job  | WarmupCount | Case           | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0     | Gen1     | Gen2    | Allocated  | Alloc Ratio |
|---------- |----- |------------ |--------------- |------------:|------------:|------------:|------:|--------:|---------:|---------:|--------:|-----------:|------------:|
| **AndQuery**  | **Cold** | **0**           | **Synthetic_2000** |    **672.1 μs** |   **144.74 μs** |    **75.70 μs** |  **1.01** |    **0.15** | **281.2500** |  **89.5833** |       **-** | **1728.95 KB** |        **1.00** |
| OrQuery   | Cold | 0           | Synthetic_2000 |  1,870.2 μs |   256.21 μs |   113.76 μs |  2.81 |    0.33 | 304.6875 |  85.9375 | 23.4375 | 1818.47 KB |        1.05 |
| NotQuery  | Cold | 0           | Synthetic_2000 |    348.8 μs |    24.14 μs |    12.63 μs |  0.52 |    0.06 | 101.0742 |  24.4141 |       - |  621.77 KB |        0.36 |
| AdjQuery  | Cold | 0           | Synthetic_2000 |    281.4 μs |    24.98 μs |    13.07 μs |  0.42 |    0.05 | 102.5391 |  27.8320 |       - |  630.35 KB |        0.36 |
| NearQuery | Cold | 0           | Synthetic_2000 |    448.6 μs |   135.58 μs |    70.91 μs |  0.67 |    0.12 | 120.1172 |  32.2266 |       - |  742.43 KB |        0.43 |
|           |      |             |                |             |             |             |       |         |          |          |         |            |             |
| AndQuery  | Warm | 3           | Synthetic_2000 |    702.3 μs |    90.99 μs |    40.40 μs |  1.00 |    0.08 | 281.2500 |  88.8672 |       - | 1728.95 KB |        1.00 |
| OrQuery   | Warm | 3           | Synthetic_2000 |  1,323.9 μs |   186.60 μs |    82.85 μs |  1.89 |    0.15 | 310.0962 |  98.5577 | 28.8462 | 1818.48 KB |        1.05 |
| NotQuery  | Warm | 3           | Synthetic_2000 |    221.6 μs |    17.49 μs |     7.77 μs |  0.32 |    0.02 |  97.6563 |  23.4375 |       - |  621.77 KB |        0.36 |
| AdjQuery  | Warm | 3           | Synthetic_2000 |    295.7 μs |    32.17 μs |    16.82 μs |  0.42 |    0.03 | 102.5391 |  27.8320 |       - |  630.35 KB |        0.36 |
| NearQuery | Warm | 3           | Synthetic_2000 |    329.6 μs |    31.45 μs |    16.45 μs |  0.47 |    0.03 | 120.6055 |  31.7383 |       - |  742.43 KB |        0.43 |
|           |      |             |                |             |             |             |       |         |          |          |         |            |             |
| **AndQuery**  | **Cold** | **0**           | **Wikipedia_5000** |          **NA** |          **NA** |          **NA** |     **?** |       **?** |       **NA** |       **NA** |      **NA** |         **NA** |           **?** |
| OrQuery   | Cold | 0           | Wikipedia_5000 |  4,060.4 μs |   462.09 μs |   205.17 μs |     ? |       ? | 609.3750 | 171.8750 |       - | 4034.96 KB |           ? |
| NotQuery  | Cold | 0           | Wikipedia_5000 |    899.2 μs |   102.66 μs |    53.69 μs |     ? |       ? | 158.2031 |  42.9688 |       - | 1269.68 KB |           ? |
| AdjQuery  | Cold | 0           | Wikipedia_5000 |          NA |          NA |          NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| NearQuery | Cold | 0           | Wikipedia_5000 |  1,098.9 μs |   816.10 μs |   362.35 μs |     ? |       ? | 139.4231 |  38.4615 |       - |  953.27 KB |           ? |
|           |      |             |                |             |             |             |       |         |          |          |         |            |             |
| AndQuery  | Warm | 3           | Wikipedia_5000 |          NA |          NA |          NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| OrQuery   | Warm | 3           | Wikipedia_5000 | 13,089.1 μs | 4,487.38 μs | 2,346.98 μs |     ? |       ? | 604.1667 | 166.6667 |       - | 4034.96 KB |           ? |
| NotQuery  | Warm | 3           | Wikipedia_5000 |  2,050.5 μs |   899.74 μs |   470.58 μs |     ? |       ? | 156.2500 |  42.9688 |       - | 1269.68 KB |           ? |
| AdjQuery  | Warm | 3           | Wikipedia_5000 |          NA |          NA |          NA |     ? |       ? |       NA |       NA |      NA |         NA |           ? |
| NearQuery | Warm | 3           | Wikipedia_5000 |  1,115.6 μs |   368.93 μs |   192.96 μs |     ? |       ? | 138.0208 |  39.0625 |       - |  953.27 KB |           ? |

Benchmarks with issues:
  OperatorBenchmarks.AndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  OperatorBenchmarks.AdjQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  OperatorBenchmarks.AndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
  OperatorBenchmarks.AdjQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
