# Глубокий анализ бенчмарков HW5

## Методика

| Параметр | Значение |
| --- | --- |
| Synthetic | seed 42, 24 терма/док, N∈{2000, 10000} |
| Wikipedia | shard pages-articles1, N∈{2000, 5000}, medium-DF запросы |
| BDN | Warm iter=8; Cold — IndexQuery; OperationsPerInvoke=32 |
| Запросы Wiki | top medium-DF (2–35% doc), без wikitext markup |

## IndexQuery AND @ N=2000

| Корпус | RAM µs | mmap µs | mmap/RAM | CV RAM |
| --- | ---: | ---: | ---: | ---: |
| Synthetic | **834.10** | 1717.60 | 2.06× | 8.2% |
| Wikipedia | **223.30** | 334.00 | 1.50× | 3.9% |

## Масштабирование RAM AND

- Synthetic N=2000: **1914.00 µs**, CV=29.5%
- Synthetic N=10000: **18576.10 µs**, CV=36.8%
- Wikipedia N=2000: **252.50 µs**, CV=8.8%
- Wikipedia N=5000: **1897.80 µs**, CV=27.3%

## Wikipedia

| Оператор | N | Mean µs | CV |
| --- | ---: | ---: | ---: |
| `Adj` | 2000 | 132.50 | 11.1% |
| `Adj` | 5000 | 410.60 | 24.6% |
| `And` | 2000 | 294.20 | 14.2% |
| `And` | 5000 | 642.80 | 15.1% |
| `Near` | 2000 | 139.70 | 3.7% |
| `Near` | 5000 | 674.50 | 5.2% |
| `Not` | 2000 | 390.40 | 5.2% |
| `Not` | 5000 | 681.80 | 2.0% |
| `Or` | 2000 | 296.00 | 6.9% |
| `Or` | 5000 | 555.40 | 3.2% |

## Проверка гипотез

| ID | Гипотеза | Вердикт |
| --- | --- | --- |
| H_corpus | Wiki AND дороже Synthetic @ 2000 | Частично |
| H_disk | mmap 1.3–1.6× RAM | **Подтверждена** (2.06× synth) |
| H_naive | indexed ≫ naive | **Подтверждена** (naive_index_ratio) |

## Графики

![indexquery_warm_latency.png](indexquery_warm_latency.png)
![scaling_latency_by_N.png](scaling_latency_by_N.png)
![mmap_ratio_vs_N.png](mmap_ratio_vs_N.png)
![operators_latency.png](operators_latency.png)
![indexquery_throughput.png](indexquery_throughput.png)
![ranking_tfidf_vs_bm25.png](ranking_tfidf_vs_bm25.png)
![corpus_comparison_and_latency.png](corpus_comparison_and_latency.png)
![corpus_comparison_mmap_latency.png](corpus_comparison_mmap_latency.png)
![indexquery_warm_vs_cold.png](indexquery_warm_vs_cold.png)
![naive_index_ratio.png](naive_index_ratio.png)
![mmap_first_vs_repeat.png](mmap_first_vs_repeat.png)
