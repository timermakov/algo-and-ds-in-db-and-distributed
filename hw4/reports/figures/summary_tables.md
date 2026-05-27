# Сводка бенчмарков (Operation API)

- CSV: Hw4.Benchmarks.OperationClearBenchmarks-report.csv, Hw4.Benchmarks.OperationEnumerateBenchmarks-report.csv, Hw4.Benchmarks.OperationGetBenchmarks-report.csv, Hw4.Benchmarks.OperationMergeBenchmarks-report.csv, Hw4.Benchmarks.OperationPutBenchmarks-report.csv, Hw4.Benchmarks.OperationSizeBenchmarks-report.csv
- Параметры: **keySpace 16384**, **opsPerInvocation 65536**, **threadCounts 1…16**, warmup **5**, **20** итераций (`bench.settings.json`)
- Ops per invocation: **65536** (из bench.settings.json или `HW4_OPS_PER_INVOCATION`)
- Baseline: **ConcurrentDictionary** (.NET) — эталон промышленной concurrent-map.
- Предыдущий прогон (архив): [`figures - old/summary_tables.md`](../figures%20-%20old/summary_tables.md) — keySpace **4096**, T до **64**.

## Матрица Mean (ms), Threads=1

| Операция  | Custom   | ConcurrentDict | Custom/CD |
|-----------|----------|----------------|-----------|
| Put       | 7.790000 | 6.609000       | 1.179x    |
| Get       | 5.276000 | 6.750000       | 0.782x    |
| Size      | 0.479400 | 16.985200      | 0.028x    |
| Clear     | 0.005660 | 0.020103       | 0.282x    |
| Merge     | 9.248000 | 12.392000      | 0.746x    |
| Enumerate | 4.624000 | 1.809000       | 2.556x    |

## Mean (ms) по числу потоков

### Put

| Реализация        | T=1    | T=2     | T=4     | T=8     | T=16    |
|-------------------|--------|---------|---------|---------|---------|
| Hw4.ConcurrentMap | 7.7900 | 11.3400 | 11.7870 | 16.7540 | 23.4280 |
| ConcurrentDict    | 6.6090 | 6.3320  | 5.1090  | 5.1230  | 5.0410  |

### Get

| Реализация        | T=1    | T=2    | T=4    | T=8    | T=16   |
|-------------------|--------|--------|--------|--------|--------|
| Hw4.ConcurrentMap | 5.2760 | 2.9950 | 2.1010 | 1.8130 | 1.7020 |
| ConcurrentDict    | 6.7500 | 2.5740 | 1.9830 | 1.6240 | 1.5860 |

### Size

| Реализация        | T=1     | T=2     | T=4     | T=8     | T=16    |
|-------------------|---------|---------|---------|---------|---------|
| Hw4.ConcurrentMap | 0.4794  | 0.7227  | 0.8683  | 1.0650  | 1.5606  |
| ConcurrentDict    | 16.9852 | 23.4299 | 30.3869 | 27.8912 | 27.6167 |

### Clear

| Реализация        | T=1    |
|-------------------|--------|
| Hw4.ConcurrentMap | 0.0057 |
| ConcurrentDict    | 0.0201 |

### Merge

| Реализация        | T=1     | T=2     | T=4     | T=8     | T=16    |
|-------------------|---------|---------|---------|---------|---------|
| Hw4.ConcurrentMap | 9.2480  | 11.0050 | 11.3440 | 19.8580 | 19.9510 |
| ConcurrentDict    | 12.3920 | 10.8930 | 10.4240 | 10.9520 | 11.6260 |

### Enumerate

| Реализация        | T=1    | T=2    | T=4     | T=8     | T=16    |
|-------------------|--------|--------|---------|---------|---------|
| Hw4.ConcurrentMap | 4.6240 | 4.7020 | 14.3660 | 15.8200 | 22.7280 |
| ConcurrentDict    | 1.8090 | 2.0240 | 4.6120  | 3.1900  | 4.1390  |

## Графики

- `Put_impl_comparison.png`
- `Put_threads_scaling.png`
- `Get_impl_comparison.png`
- `Get_threads_scaling.png`
- `Size_impl_comparison.png`
- `Size_threads_scaling.png`
- `Clear_impl_comparison.png`
- `Merge_impl_comparison.png`
- `Merge_threads_scaling.png`
- `Enumerate_impl_comparison.png`
- `Enumerate_threads_scaling.png`
- `dashboard_operations.png`
- `dashboard_operations_peak_threads.png`
- `operations_threads_scaling.png`
- `operations_custom_latency.png`
- `operations_custom_vs_baseline.png`
- `operations_throughput.png`