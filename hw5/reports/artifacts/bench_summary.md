# Сводка бенчмарков HW5

## Warm — все классы

| Класс                       | Метод                   | Corpus    | N     | Mean µs    | CV    | Ratio |
|-----------------------------|-------------------------|-----------|-------|------------|-------|-------|
| IndexBuildBenchmarks        | NaivePostingBytes       | Synthetic | 2000  | 0.00       | 0.0%  |       |
| IndexBuildBenchmarks        | NaivePostingBytes       | Wikipedia | 2000  | 0.00       | 0.0%  |       |
| IndexBuildBenchmarks        | NaivePostingBytes       | Wikipedia | 5000  | 0.00       | 77.1% |       |
| IndexBuildBenchmarks        | SealAndWriteSegment     | Synthetic | 2000  | 15106.46   | 11.2% |       |
| IndexBuildBenchmarks        | SealAndWriteSegment     | Wikipedia | 2000  | 2522776.83 | 9.8%  |       |
| IndexBuildBenchmarks        | SealAndWriteSegment     | Wikipedia | 5000  | 5975730.79 | 7.7%  |       |
| IndexQueryBenchmarks        | DiskMmap_AndQuery       | Synthetic | 2000  | 1717.60    | 6.2%  | 2.07  |
| IndexQueryBenchmarks        | DiskMmap_AndQuery       | Wikipedia | 2000  | 334.00     | 3.5%  | 1.50  |
| IndexQueryBenchmarks        | Memory_AndQuery         | Synthetic | 2000  | 834.10     | 8.2%  | 1.01  |
| IndexQueryBenchmarks        | Memory_AndQuery         | Wikipedia | 2000  | 223.30     | 3.9%  | 1.00  |
| IndexQueryBenchmarks        | Memory_Bm25Top10        | Synthetic | 2000  | 4022.20    | 14.4% | 4.85  |
| IndexQueryBenchmarks        | Memory_Bm25Top10        | Wikipedia | 2000  | 272.40     | 8.6%  | 1.22  |
| IndexQueryBenchmarks        | Memory_NearAdjQuery     | Synthetic | 2000  | 579.10     | 8.5%  | 0.70  |
| IndexQueryBenchmarks        | Memory_NearAdjQuery     | Wikipedia | 2000  | 119.30     | 8.4%  | 0.53  |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Synthetic | 10000 | 21197.80   | 12.3% | 1.26  |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Synthetic | 2000  | 2372.60    | 38.7% | 1.33  |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Wikipedia | 2000  | 556.30     | 18.3% | 2.22  |
| IndexQueryScalingBenchmarks | DiskMmap_AndQuery       | Wikipedia | 5000  | 1342.00    | 25.0% | 0.77  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Synthetic | 10000 | 18576.10   | 36.8% | 1.11  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Synthetic | 2000  | 1914.00    | 29.5% | 1.07  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Wikipedia | 2000  | 252.50     | 8.8%  | 1.01  |
| IndexQueryScalingBenchmarks | Memory_AndQuery         | Wikipedia | 5000  | 1897.80    | 27.3% | 1.09  |
| MmapTouchBenchmarks         | FirstTouchMmap_AndQuery | Synthetic | 2000  | 5780.00    | 1.6%  | 1.00  |
| MmapTouchBenchmarks         | RepeatedMmap_AndQuery   | Synthetic | 2000  | 1334.00    | 10.6% | 0.23  |
| NaiveScanBenchmarks         | IndexedAndQuery         | Synthetic | 128   | 48.00      | 5.2%  | 1.00  |
| NaiveScanBenchmarks         | IndexedAndQuery         | Synthetic | 512   | 171.66     | 3.5%  | 1.00  |
| NaiveScanBenchmarks         | NaiveAndQuery           | Synthetic | 128   | 149.59     | 0.8%  | 3.12  |
| NaiveScanBenchmarks         | NaiveAndQuery           | Synthetic | 512   | 714.52     | 4.2%  | 4.17  |
| OperatorBenchmarks          | AdjQuery                | Synthetic | 2000  | 423.80     | 18.6% | 0.33  |
| OperatorBenchmarks          | AdjQuery                | Wikipedia | 2000  | 132.50     | 11.1% | 0.46  |
| OperatorBenchmarks          | AdjQuery                | Wikipedia | 5000  | 410.60     | 24.6% | 0.65  |
| OperatorBenchmarks          | AndQuery                | Synthetic | 2000  | 1515.40    | 45.7% | 1.17  |
| OperatorBenchmarks          | AndQuery                | Wikipedia | 2000  | 294.20     | 14.2% | 1.02  |
| OperatorBenchmarks          | AndQuery                | Wikipedia | 5000  | 642.80     | 15.1% | 1.02  |
| OperatorBenchmarks          | NearQuery               | Synthetic | 2000  | 460.40     | 12.2% | 0.36  |
| OperatorBenchmarks          | NearQuery               | Wikipedia | 2000  | 139.70     | 3.7%  | 0.48  |
| OperatorBenchmarks          | NearQuery               | Wikipedia | 5000  | 674.50     | 5.2%  | 1.07  |
| OperatorBenchmarks          | NotQuery                | Synthetic | 2000  | 298.60     | 9.6%  | 0.23  |
| OperatorBenchmarks          | NotQuery                | Wikipedia | 2000  | 390.40     | 5.2%  | 1.35  |
| OperatorBenchmarks          | NotQuery                | Wikipedia | 5000  | 681.80     | 2.0%  | 1.08  |
| OperatorBenchmarks          | OrQuery                 | Synthetic | 2000  | 1875.20    | 3.4%  | 1.45  |
| OperatorBenchmarks          | OrQuery                 | Wikipedia | 2000  | 296.00     | 6.9%  | 1.02  |
| OperatorBenchmarks          | OrQuery                 | Wikipedia | 5000  | 555.40     | 3.2%  | 0.88  |
| RankingBenchmarks           | Bm25Top10               | Synthetic | 2000  | 2258.80    | 8.1%  | 1.18  |
| RankingBenchmarks           | Bm25Top10               | Wikipedia | 2000  | 343.10     | 4.8%  | 1.20  |
| RankingBenchmarks           | Bm25Top10               | Wikipedia | 5000  | 1730.50    | 11.8% | 1.80  |
| RankingBenchmarks           | BooleanOnly             | Synthetic | 2000  | 1926.20    | 5.6%  | 1.00  |
| RankingBenchmarks           | BooleanOnly             | Wikipedia | 2000  | 291.00     | 14.2% | 1.02  |
| RankingBenchmarks           | BooleanOnly             | Wikipedia | 5000  | 966.30     | 8.1%  | 1.01  |
| RankingBenchmarks           | TfIdfTop10              | Synthetic | 2000  | 2693.90    | 12.3% | 1.40  |
| RankingBenchmarks           | TfIdfTop10              | Wikipedia | 2000  | 313.80     | 5.3%  | 1.10  |
| RankingBenchmarks           | TfIdfTop10              | Wikipedia | 5000  | 2218.30    | 27.8% | 2.31  |

## Графики

- `indexquery_warm_latency.png`
- `scaling_latency_by_N.png`
- `mmap_ratio_vs_N.png`
- `operators_latency.png`
- `cv_by_method.png`
- `alloc_ratio.png`
- `indexquery_throughput.png`
- `ranking_tfidf_vs_bm25.png`
- `corpus_comparison_and_latency.png`
- `corpus_comparison_mmap_latency.png`
- `indexquery_warm_vs_cold.png`
- `naive_index_ratio.png`
- `mmap_first_vs_repeat.png`
- `compression_ratio_vs_N.png`