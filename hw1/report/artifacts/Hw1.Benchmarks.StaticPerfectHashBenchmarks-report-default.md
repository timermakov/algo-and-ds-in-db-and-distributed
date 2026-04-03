
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Stable : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Stable  InvocationCount=1  IterationCount=40  
LaunchCount=1  UnrollFactor=1  WarmupCount=15  

 Method            | N      | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------------ |------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
 **LookupPerfectHash** | **10000**  |  **53.28 ns** |  **3.133 ns** |  **5.404 ns** |  **53.27 ns** |  **4.85** |    **0.66** |         **-** |          **NA** |
 LookupDictionary  | 10000  |  11.09 ns |  0.640 ns |  1.103 ns |  10.76 ns |  1.01 |    0.14 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **12915**  |  **59.48 ns** |  **3.697 ns** |  **6.572 ns** |  **58.99 ns** |  **4.36** |    **0.49** |         **-** |          **NA** |
 LookupDictionary  | 12915  |  13.66 ns |  0.237 ns |  0.414 ns |  13.70 ns |  1.00 |    0.04 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **16681**  |  **51.15 ns** |  **2.700 ns** |  **4.799 ns** |  **48.83 ns** |  **3.30** |    **0.36** |         **-** |          **NA** |
 LookupDictionary  | 16681  |  15.55 ns |  0.552 ns |  0.922 ns |  15.56 ns |  1.00 |    0.08 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **21544**  |  **49.78 ns** |  **0.883 ns** |  **1.569 ns** |  **49.54 ns** |  **2.94** |    **0.23** |         **-** |          **NA** |
 LookupDictionary  | 21544  |  17.01 ns |  0.758 ns |  1.308 ns |  16.93 ns |  1.01 |    0.11 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **27826**  |  **58.19 ns** |  **3.060 ns** |  **5.359 ns** |  **57.34 ns** |  **3.69** |    **0.58** |         **-** |          **NA** |
 LookupDictionary  | 27826  |  16.06 ns |  1.280 ns |  2.275 ns |  14.92 ns |  1.02 |    0.19 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **35938**  |  **58.67 ns** |  **1.906 ns** |  **3.236 ns** |  **57.80 ns** |  **3.37** |    **0.23** |         **-** |          **NA** |
 LookupDictionary  | 35938  |  17.45 ns |  0.470 ns |  0.785 ns |  17.30 ns |  1.00 |    0.06 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **46416**  |  **64.41 ns** |  **2.439 ns** |  **4.206 ns** |  **64.72 ns** |  **4.23** |    **0.41** |         **-** |          **NA** |
 LookupDictionary  | 46416  |  15.32 ns |  0.669 ns |  1.153 ns |  14.94 ns |  1.01 |    0.10 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **59948**  |  **97.77 ns** |  **9.128 ns** | **15.988 ns** |  **94.97 ns** |  **5.40** |    **1.04** |         **-** |          **NA** |
 LookupDictionary  | 59948  |  18.34 ns |  1.227 ns |  2.180 ns |  17.42 ns |  1.01 |    0.16 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **77426**  | **106.38 ns** | **15.423 ns** | **27.414 ns** |  **92.69 ns** |  **6.07** |    **1.64** |         **-** |          **NA** |
 LookupDictionary  | 77426  |  17.67 ns |  0.957 ns |  1.677 ns |  17.33 ns |  1.01 |    0.13 |         - |          NA |
                   |        |           |           |           |           |       |         |           |             |
 **LookupPerfectHash** | **100000** | **127.24 ns** | **13.217 ns** | **23.149 ns** | **122.02 ns** |  **6.11** |    **1.16** |         **-** |          **NA** |
 LookupDictionary  | 100000 |  20.89 ns |  0.719 ns |  1.259 ns |  20.88 ns |  1.00 |    0.08 |         - |          NA |
