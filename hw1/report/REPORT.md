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

## Next
- Add baseline benchmarks and profiling scripts.
- Run full test suite on .NET 10 SDK environment.
