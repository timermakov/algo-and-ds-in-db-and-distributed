```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Stable : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Stable  InvocationCount=1  IterationCount=40  
LaunchCount=1  UnrollFactor=1  WarmupCount=15  

```
| Method            | N      | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |------- |----------:|---------:|----------:|----------:|------:|--------:|----------:|------------:|
| **LookupPerfectHash** | **10000**  |  **35.39 ns** | **1.621 ns** |  **2.838 ns** |  **34.39 ns** |  **2.76** |    **0.31** |         **-** |          **NA** |
| LookupDictionary  | 10000  |  12.90 ns | 0.583 ns |  1.021 ns |  12.83 ns |  1.01 |    0.11 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **12915**  |  **36.13 ns** | **0.610 ns** |  **0.985 ns** |  **35.93 ns** |  **2.69** |    **0.13** |         **-** |          **NA** |
| LookupDictionary  | 12915  |  13.46 ns | 0.329 ns |  0.549 ns |  13.38 ns |  1.00 |    0.06 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **16681**  |  **39.96 ns** | **3.588 ns** |  **5.895 ns** |  **37.23 ns** |  **2.59** |    **0.39** |         **-** |          **NA** |
| LookupDictionary  | 16681  |  15.43 ns | 0.347 ns |  0.608 ns |  15.40 ns |  1.00 |    0.05 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **21544**  |  **42.56 ns** | **1.066 ns** |  **1.896 ns** |  **42.29 ns** |  **2.49** |    **0.14** |         **-** |          **NA** |
| LookupDictionary  | 21544  |  17.14 ns | 0.377 ns |  0.670 ns |  17.06 ns |  1.00 |    0.05 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **27826**  |  **47.61 ns** | **1.814 ns** |  **3.129 ns** |  **47.26 ns** |  **2.78** |    **0.38** |         **-** |          **NA** |
| LookupDictionary  | 27826  |  17.37 ns | 1.167 ns |  2.012 ns |  17.82 ns |  1.01 |    0.17 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **35938**  |  **57.59 ns** | **4.602 ns** |  **7.561 ns** |  **56.57 ns** |  **2.78** |    **0.59** |         **-** |          **NA** |
| LookupDictionary  | 35938  |  21.45 ns | 2.548 ns |  4.326 ns |  20.17 ns |  1.03 |    0.27 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **46416**  |  **67.04 ns** | **2.148 ns** |  **3.589 ns** |  **66.82 ns** |  **3.73** |    **0.53** |         **-** |          **NA** |
| LookupDictionary  | 46416  |  18.28 ns | 1.425 ns |  2.420 ns |  18.33 ns |  1.02 |    0.19 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **59948**  |  **94.82 ns** | **8.764 ns** | **15.118 ns** |  **90.24 ns** |  **5.02** |    **0.89** |         **-** |          **NA** |
| LookupDictionary  | 59948  |  19.01 ns | 0.937 ns |  1.591 ns |  19.00 ns |  1.01 |    0.12 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **77426**  | **101.50 ns** | **5.309 ns** |  **8.420 ns** | **100.47 ns** |  **5.10** |    **0.50** |         **-** |          **NA** |
| LookupDictionary  | 77426  |  19.95 ns | 0.683 ns |  1.123 ns |  19.78 ns |  1.00 |    0.08 |         - |          NA |
|                   |        |           |          |           |           |       |         |           |             |
| **LookupPerfectHash** | **100000** | **108.79 ns** | **4.943 ns** |  **8.258 ns** | **106.36 ns** |  **4.97** |    **0.55** |         **-** |          **NA** |
| LookupDictionary  | 100000 |  22.03 ns | 1.050 ns |  1.867 ns |  21.65 ns |  1.01 |    0.12 |         - |          NA |
