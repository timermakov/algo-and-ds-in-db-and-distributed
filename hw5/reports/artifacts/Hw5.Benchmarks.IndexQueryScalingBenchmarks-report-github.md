```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method            | Job  | WarmupCount | Case            | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0      | Gen1     | Gen2     | Allocated | Alloc Ratio |
|------------------ |----- |------------ |---------------- |------------:|------------:|------------:|------:|--------:|----------:|---------:|---------:|----------:|------------:|
| **Memory_AndQuery**   | **Cold** | **0**           | **Synthetic_10000** | **15,853.7 μs** | **5,042.88 μs** | **2,239.07 μs** |  **1.02** |    **0.19** | **1375.0000** | **468.7500** | **156.2500** |   **9.44 MB** |        **1.00** |
| DiskMmap_AndQuery | Cold | 0           | Synthetic_10000 | 13,684.6 μs | 1,629.73 μs |   723.61 μs |  0.88 |    0.12 | 1781.2500 | 656.2500 | 187.5000 |  11.78 MB |        1.25 |
|                   |      |             |                 |             |             |             |       |         |           |          |          |           |             |
| Memory_AndQuery   | Warm | 3           | Synthetic_10000 |  9,132.7 μs |   523.91 μs |   274.01 μs |  1.00 |    0.04 | 1375.0000 | 468.7500 | 156.2500 |   9.44 MB |        1.00 |
| DiskMmap_AndQuery | Warm | 3           | Synthetic_10000 | 13,595.6 μs |   610.03 μs |   270.86 μs |  1.49 |    0.05 | 1781.2500 | 625.0000 | 187.5000 |  11.78 MB |        1.25 |
|                   |      |             |                 |             |             |             |       |         |           |          |          |           |             |
| **Memory_AndQuery**   | **Cold** | **0**           | **Synthetic_2000**  |    **597.4 μs** |    **34.62 μs** |    **18.10 μs** |  **1.00** |    **0.04** |  **281.2500** |  **88.8672** |        **-** |   **1.69 MB** |        **1.00** |
| DiskMmap_AndQuery | Cold | 0           | Synthetic_2000  |  1,033.7 μs |    79.67 μs |    35.38 μs |  1.73 |    0.07 |  359.3750 | 165.1786 |        - |   2.16 MB |        1.28 |
|                   |      |             |                 |             |             |             |       |         |           |          |          |           |             |
| Memory_AndQuery   | Warm | 3           | Synthetic_2000  |    611.5 μs |    42.41 μs |    22.18 μs |  1.00 |    0.05 |  281.2500 |  88.8672 |        - |   1.69 MB |        1.00 |
| DiskMmap_AndQuery | Warm | 3           | Synthetic_2000  |  1,255.5 μs |   468.94 μs |   245.27 μs |  2.06 |    0.39 |  360.4167 | 166.6667 |        - |   2.16 MB |        1.28 |
|                   |      |             |                 |             |             |             |       |         |           |          |          |           |             |
| **Memory_AndQuery**   | **Cold** | **0**           | **Wikipedia_5000**  |          **NA** |          **NA** |          **NA** |     **?** |       **?** |        **NA** |       **NA** |       **NA** |        **NA** |           **?** |
| DiskMmap_AndQuery | Cold | 0           | Wikipedia_5000  |          NA |          NA |          NA |     ? |       ? |        NA |       NA |       NA |        NA |           ? |
|                   |      |             |                 |             |             |             |       |         |           |          |          |           |             |
| Memory_AndQuery   | Warm | 3           | Wikipedia_5000  |          NA |          NA |          NA |     ? |       ? |        NA |       NA |       NA |        NA |           ? |
| DiskMmap_AndQuery | Warm | 3           | Wikipedia_5000  |          NA |          NA |          NA |     ? |       ? |        NA |       NA |       NA |        NA |           ? |

Benchmarks with issues:
  IndexQueryScalingBenchmarks.Memory_AndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  IndexQueryScalingBenchmarks.DiskMmap_AndQuery: Cold(IterationCount=8, LaunchCount=1, WarmupCount=0) [Case=Wikipedia_5000]
  IndexQueryScalingBenchmarks.Memory_AndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
  IndexQueryScalingBenchmarks.DiskMmap_AndQuery: Warm(IterationCount=8, LaunchCount=1, WarmupCount=3) [Case=Wikipedia_5000]
