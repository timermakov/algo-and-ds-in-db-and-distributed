```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Warm  IterationCount=8  LaunchCount=1  
WarmupCount=3  

```
| Method              | Case           | Mean                  | Error                 | StdDev              | Gen0        | Gen1       | Gen2     | Allocated    |
|-------------------- |--------------- |----------------------:|----------------------:|--------------------:|------------:|-----------:|---------:|-------------:|
| **SealAndWriteSegment** | **Synthetic_2000** |    **15,106,460.5469 ns** |     **3,229,534.0900 ns** |   **1,689,108.7760 ns** |    **515.6250** |   **234.3750** | **140.6250** |    **2922095 B** |
| NaivePostingBytes   | Synthetic_2000 |             0.0000 ns |             0.0000 ns |           0.0000 ns |           - |          - |        - |            - |
| **SealAndWriteSegment** | **Wikipedia_2000** | **2,522,776,825.0000 ns** |   **470,866,932.1973 ns** | **246,272,510.3141 ns** |  **77000.0000** | **20000.0000** |        **-** |  **516872080 B** |
| NaivePostingBytes   | Wikipedia_2000 |             0.0000 ns |             0.0000 ns |           0.0000 ns |           - |          - |        - |            - |
| **SealAndWriteSegment** | **Wikipedia_5000** | **5,975,730,785.7143 ns** | **1,042,096,035.9032 ns** | **462,697,098.6833 ns** | **175000.0000** | **46000.0000** |        **-** | **1217197328 B** |
| NaivePostingBytes   | Wikipedia_5000 |             0.3464 ns |             0.6019 ns |           0.2672 ns |           - |          - |        - |            - |
