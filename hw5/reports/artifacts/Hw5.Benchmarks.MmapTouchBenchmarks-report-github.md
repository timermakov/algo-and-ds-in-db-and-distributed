```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i5-1240P 1.70GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Cold   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Warm   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

IterationCount=8  LaunchCount=1  

```
| Method                  | Job  | WarmupCount | Case           | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Allocated | Alloc Ratio |
|------------------------ |----- |------------ |--------------- |---------:|----------:|----------:|------:|--------:|---------:|---------:|----------:|------------:|
| FirstTouchMmap_AndQuery | Cold | 0           | Synthetic_2000 | 6.275 ms | 1.0458 ms | 0.5470 ms |  1.01 |    0.11 | 687.5000 | 281.2500 |    4.2 MB |        1.00 |
| RepeatedMmap_AndQuery   | Cold | 0           | Synthetic_2000 | 1.166 ms | 0.0462 ms | 0.0205 ms |  0.19 |    0.01 | 370.5357 | 183.0357 |   2.22 MB |        0.53 |
|                         |      |             |                |          |           |           |       |         |          |          |           |             |
| FirstTouchMmap_AndQuery | Warm | 3           | Synthetic_2000 | 5.780 ms | 0.2106 ms | 0.0935 ms |  1.00 |    0.02 | 695.3125 | 289.0625 |    4.2 MB |        1.00 |
| RepeatedMmap_AndQuery   | Warm | 3           | Synthetic_2000 | 1.334 ms | 0.3170 ms | 0.1408 ms |  0.23 |    0.02 | 369.3182 | 184.6591 |   2.22 MB |        0.53 |
