```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method              | Job  | WarmupCount | Case           | Mean           | Error         | StdDev        | Gen0        | Gen1       | Allocated     |
|-------------------- |----- |------------ |--------------- |---------------:|--------------:|--------------:|------------:|-----------:|--------------:|
| **SealAndWriteSegment** | **Cold** | **0**           | **Synthetic_2000** |     **6,893.0 μs** |     **932.00 μs** |     **413.81 μs** |    **312.5000** |    **62.5000** |    **1957.67 KB** |
| NaivePostingBytes   | Cold | 0           | Synthetic_2000 |       254.7 μs |      86.30 μs |      45.14 μs |     40.5273 |     2.9297 |      250.4 KB |
| SealAndWriteSegment | Warm | 3           | Synthetic_2000 |     5,382.5 μs |   1,119.26 μs |     496.96 μs |    312.5000 |    70.3125 |    1957.09 KB |
| NaivePostingBytes   | Warm | 3           | Synthetic_2000 |       217.9 μs |      18.53 μs |       8.23 μs |     40.7715 |     2.9297 |      250.4 KB |
| **SealAndWriteSegment** | **Cold** | **0**           | **Wikipedia_5000** | **3,667,896.5 μs** | **165,780.21 μs** |  **73,607.44 μs** | **175000.0000** | **46000.0000** | **1186679.54 KB** |
| NaivePostingBytes   | Cold | 0           | Wikipedia_5000 |   539,291.2 μs |  71,036.60 μs |  37,153.51 μs |  24000.0000 |          - |  150404.44 KB |
| SealAndWriteSegment | Warm | 3           | Wikipedia_5000 | 3,710,093.9 μs | 362,995.11 μs | 189,853.46 μs | 175000.0000 | 46000.0000 | 1186679.28 KB |
| NaivePostingBytes   | Warm | 3           | Wikipedia_5000 |   583,318.8 μs |  53,338.56 μs |  27,897.10 μs |  24000.0000 |          - |  150404.44 KB |
