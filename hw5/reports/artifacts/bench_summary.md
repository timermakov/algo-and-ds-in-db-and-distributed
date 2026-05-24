# Сводка бенчмарков HW5

## Warm — все классы

| Класс                       | Метод                   | Corpus    | N     | Mean µs    | CV    | Ratio |
|-----------------------------|-------------------------|-----------|-------|------------|-------|-------|
| IndexBuildBenchmarks        | NaivePostingBytes       | Synthetic | 2000  | 217.90     | 3.8%  |       |
| IndexBuildBenchmarks        | NaivePostingBytes       | Wikipedia | 5000  | 583318.80  | 4.8%  |       |
| IndexBuildBenchmarks        | SealAndWriteSegment     | Synthetic | 2000  | 5382.50    | 9.2%  |       |
| IndexBuildBenchmarks        | SealAndWriteSegment     | Wikipedia | 5000  | 3710093.90 | 5.1%  |       |
| IndexQueryBenchmarks        | DiskMmap_AndQuery       | Synthetic | 2000  | 1090.80    | 3.0%  | 1.58  |
| IndexQueryBenchmarks        | Memory_AndQuery         | Synthetic | 2000  | 690.90     | 1.7%  | 1.00  |
| IndexQueryBenchmarks        | Memory_Bm25Top10        | Synthetic | 2000  | 1836.60    | 3.3%  | 2.66  |
| IndexQueryBenchmarks        | Memory_Bm25Top10        | Wikipedia | 5000  | 4279.90    | 1.4%  |       |
| IndexQueryBenchmarks        | Memory_NearAdjQuery     | Synthetic | 2000  | 355.10     | 4.7%  | 0.51  |
| IndexQueryBenchmarks        | Memory_NearAdjQuery     | Wikipedia | 5000  | 754.00     | 1.6%  |       |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Synthetic | 10000 | 13595.60   | 2.0%  | 1.49  |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Synthetic | 2000  | 1255.50    | 19.5% | 2.06  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Synthetic | 10000 | 9132.70    | 3.0%  | 1.00  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Synthetic | 2000  | 611.50     | 3.6%  | 1.00  |
| MmapTouchBenchmarks         | FirstTouchMmap_AndQuery | Synthetic | 2000  | 5780.00    | 1.6%  | 1.00  |
| MmapTouchBenchmarks         | RepeatedMmap_AndQuery   | Synthetic | 2000  | 1334.00    | 10.6% | 0.23  |
| NaiveScanBenchmarks         | IndexedAndQuery         | Synthetic | 128   | 48.00      | 5.2%  | 1.00  |
| NaiveScanBenchmarks         | IndexedAndQuery         | Synthetic | 512   | 171.66     | 3.5%  | 1.00  |
| NaiveScanBenchmarks         | NaiveAndQuery           | Synthetic | 128   | 149.59     | 0.8%  | 3.12  |
| NaiveScanBenchmarks         | NaiveAndQuery           | Synthetic | 512   | 714.52     | 4.2%  | 4.17  |
| OperatorBenchmarks          | AdjQuery                | Synthetic | 2000  | 295.70     | 5.7%  | 0.42  |
| OperatorBenchmarks          | AndQuery                | Synthetic | 2000  | 702.30     | 5.8%  | 1.00  |
| OperatorBenchmarks          | NearQuery               | Synthetic | 2000  | 329.60     | 5.0%  | 0.47  |
| OperatorBenchmarks          | NearQuery               | Wikipedia | 5000  | 1115.60    | 17.3% |       |
| OperatorBenchmarks          | NotQuery                | Synthetic | 2000  | 221.60     | 3.5%  | 0.32  |
| OperatorBenchmarks          | NotQuery                | Wikipedia | 5000  | 2050.50    | 22.9% |       |
| OperatorBenchmarks          | OrQuery                 | Synthetic | 2000  | 1323.90    | 6.3%  | 1.89  |
| OperatorBenchmarks          | OrQuery                 | Wikipedia | 5000  | 13089.10   | 17.9% |       |
| RankingBenchmarks           | Bm25Top10               | Synthetic | 2000  | 2639.00    | 18.3% | 1.16  |
| RankingBenchmarks           | Bm25Top10               | Wikipedia | 5000  | 4877.00    | 3.6%  | 1.14  |
| RankingBenchmarks           | BooleanOnly             | Synthetic | 2000  | 2296.00    | 9.4%  | 1.01  |
| RankingBenchmarks           | BooleanOnly             | Wikipedia | 5000  | 4300.00    | 4.0%  | 1.00  |
| RankingBenchmarks           | TfIdfTop10              | Synthetic | 2000  | 2404.00    | 10.9% | 1.05  |
| RankingBenchmarks           | TfIdfTop10              | Wikipedia | 5000  | 4790.00    | 3.4%  | 1.12  |

## Графики

- `indexquery_warm_latency.png`
- `scaling_latency_by_N.png`
- `mmap_ratio_vs_N.png`
- `operators_latency.png`
- `cv_by_method.png`
- `alloc_ratio.png`
- `indexquery_throughput.png`
- `ranking_tfidf_vs_bm25.png`
- `indexquery_warm_vs_cold.png`
- `naive_index_ratio.png`
- `mmap_first_vs_repeat.png`