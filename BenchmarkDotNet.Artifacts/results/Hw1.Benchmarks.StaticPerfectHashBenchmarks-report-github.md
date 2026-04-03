```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8117/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Stable : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Stable  InvocationCount=1  IterationCount=40  
LaunchCount=1  UnrollFactor=1  WarmupCount=15  

```
| Method            | N      | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |------- |----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| **LookupPerfectHash** | **10000**  |  **50.71 ns** |  **2.547 ns** |  **4.324 ns** |  **49.73 ns** |  **3.73** |    **0.58** |         **-** |          **NA** |
| LookupDictionary  | 10000  |  13.85 ns |  1.113 ns |  1.949 ns |  12.91 ns |  1.02 |    0.20 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **12915**  |  **57.19 ns** |  **1.715 ns** |  **2.769 ns** |  **57.27 ns** |  **3.24** |    **0.26** |         **-** |          **NA** |
| LookupDictionary  | 12915  |  17.73 ns |  0.672 ns |  1.123 ns |  17.84 ns |  1.00 |    0.09 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **16681**  |  **67.53 ns** |  **5.982 ns** | **10.318 ns** |  **65.12 ns** |  **3.04** |    **0.57** |         **-** |          **NA** |
| LookupDictionary  | 16681  |  22.51 ns |  1.605 ns |  2.853 ns |  21.39 ns |  1.01 |    0.17 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **21544**  |  **73.77 ns** | **10.297 ns** | **18.034 ns** |  **67.60 ns** |  **3.56** |    **0.90** |         **-** |          **NA** |
| LookupDictionary  | 21544  |  20.84 ns |  0.937 ns |  1.565 ns |  20.55 ns |  1.01 |    0.10 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **27826**  |  **70.71 ns** |  **4.947 ns** |  **8.400 ns** |  **69.26 ns** |  **4.04** |    **0.67** |         **-** |          **NA** |
| LookupDictionary  | 27826  |  17.74 ns |  1.268 ns |  2.253 ns |  17.40 ns |  1.01 |    0.17 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **35938**  |  **66.65 ns** |  **2.812 ns** |  **4.620 ns** |  **66.82 ns** |  **2.99** |    **0.26** |         **-** |          **NA** |
| LookupDictionary  | 35938  |  22.33 ns |  0.769 ns |  1.264 ns |  22.24 ns |  1.00 |    0.08 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **46416**  |  **90.61 ns** |  **8.227 ns** | **14.191 ns** |  **85.84 ns** |  **5.15** |    **0.97** |         **-** |          **NA** |
| LookupDictionary  | 46416  |  17.78 ns |  1.134 ns |  1.955 ns |  17.80 ns |  1.01 |    0.15 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **59948**  |  **87.95 ns** |  **3.563 ns** |  **6.145 ns** |  **86.93 ns** |  **5.03** |    **0.44** |         **-** |          **NA** |
| LookupDictionary  | 59948  |  17.55 ns |  0.558 ns |  0.933 ns |  17.70 ns |  1.00 |    0.08 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **77426**  | **103.76 ns** |  **3.902 ns** |  **6.300 ns** | **101.75 ns** |  **6.04** |    **0.50** |         **-** |          **NA** |
| LookupDictionary  | 77426  |  17.23 ns |  0.562 ns |  0.970 ns |  17.38 ns |  1.00 |    0.08 |         - |          NA |
|                   |        |           |           |           |           |       |         |           |             |
| **LookupPerfectHash** | **100000** | **135.01 ns** |  **9.448 ns** | **16.548 ns** | **133.88 ns** |  **5.71** |    **0.82** |         **-** |          **NA** |
| LookupDictionary  | 100000 |  23.78 ns |  1.154 ns |  1.929 ns |  23.19 ns |  1.01 |    0.11 |         - |          NA |
