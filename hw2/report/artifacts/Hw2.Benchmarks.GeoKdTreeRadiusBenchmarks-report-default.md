
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8039/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Stable : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Stable  InvocationCount=1  IterationCount=40  
LaunchCount=1  UnrollFactor=1  WarmupCount=15  

 Method              | N     | RadiusMeters | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------- |------ |------------- |------------:|----------:|----------:|------------:|------:|--------:|----------:|------------:|
 **QueryKdTreeRadius**   | **10000** | **100**          |    **14.91 μs** |  **0.996 μs** |  **1.580 μs** |    **15.25 μs** |  **0.04** |    **0.00** |     **216 B** |          **NA** |
 QueryFullScanRadius | 10000 | 100          |   343.21 μs |  8.953 μs | 15.914 μs |   343.51 μs |  1.00 |    0.06 |         - |          NA |
                     |       |              |             |           |           |             |       |         |           |             |
 **QueryKdTreeRadius**   | **10000** | **5000**         |    **14.58 μs** |  **2.568 μs** |  **4.498 μs** |    **16.21 μs** |  **0.05** |    **0.01** |     **216 B** |          **NA** |
 QueryFullScanRadius | 10000 | 5000         |   315.34 μs |  8.893 μs | 15.808 μs |   314.62 μs |  1.00 |    0.07 |         - |          NA |
                     |       |              |             |           |           |             |       |         |           |             |
 **QueryKdTreeRadius**   | **21544** | **100**          |    **16.83 μs** |  **3.270 μs** |  **5.813 μs** |    **13.72 μs** |  **0.02** |    **0.01** |     **248 B** |          **NA** |
 QueryFullScanRadius | 21544 | 100          |   708.80 μs | 43.408 μs | 77.158 μs |   685.25 μs |  1.01 |    0.15 |         - |          NA |
                     |       |              |             |           |           |             |       |         |           |             |
 **QueryKdTreeRadius**   | **21544** | **5000**         |    **17.67 μs** |  **1.536 μs** |  **2.650 μs** |    **18.49 μs** |  **0.03** |    **0.00** |     **248 B** |          **NA** |
 QueryFullScanRadius | 21544 | 5000         |   661.92 μs | 35.167 μs | 61.593 μs |   646.44 μs |  1.01 |    0.13 |         - |          NA |
                     |       |              |             |           |           |             |       |         |           |             |
 **QueryKdTreeRadius**   | **35938** | **100**          |    **28.77 μs** |  **0.808 μs** |  **1.437 μs** |    **29.10 μs** |  **0.03** |    **0.00** |     **400 B** |          **NA** |
 QueryFullScanRadius | 35938 | 100          | 1,115.21 μs | 42.474 μs | 75.497 μs | 1,103.32 μs |  1.00 |    0.09 |         - |          NA |
                     |       |              |             |           |           |             |       |         |           |             |
 **QueryKdTreeRadius**   | **35938** | **5000**         |    **30.60 μs** |  **4.670 μs** |  **8.301 μs** |    **30.68 μs** |  **0.03** |    **0.01** |     **400 B** |          **NA** |
 QueryFullScanRadius | 35938 | 5000         | 1,032.23 μs | 33.143 μs | 56.280 μs | 1,019.33 μs |  1.00 |    0.08 |         - |          NA |
