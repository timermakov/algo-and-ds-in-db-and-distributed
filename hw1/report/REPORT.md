# HW1 Report

## Progress
- Bootstrapped `hw1` solution on `.NET 10` target framework.
- Created projects for algorithms, tests, and benchmarks.
- Added `Makefile` with SDK version gate and standard commands.
- Added concise `README.md` with assignment parts and run instructions.
- Implemented file-based hash table with fixed buckets on `MemoryMappedFile`.
- Added API operations: `Insert`, `Update`, `Delete`, `TryGet`, plus bucket warmup.
- Implemented static perfect hash index with two-level collision-free construction.
- Perfect hash API includes only `Build` and `TryGet`.
- Implemented text MinHash + LSH index with `BuildIndex`, `AddDocument`, candidate lookup, and full-scan baseline.
- Added randomized and deterministic functional tests for all three algorithms.
- Added BenchmarkDotNet suite for all algorithms with baseline comparisons and shuffled keys each iteration.
- Added profiling scripts for CPU (`dotnet-trace`) and memory (`dotnet-gcdump`).

## Next
- Run test and benchmark suites on machine with .NET 10 SDK installed.
- Export benchmark tables/graphs into `report/artifacts`.
