# HW3: ANN-бенчмарк (FAISS, arXiv embeddings)

Проект сравнивает ANN-индексы на векторных эмбеддингах:
- LSH (`faiss.IndexLSH`)
- HNSW (`faiss.IndexHNSWFlat`)
- IVF+PQ (`faiss.IndexIVFPQ`)
- baseline exact (`faiss.IndexFlatL2`)

Считаемые метрики:
- `Recall@100`
- `QPS` и `latency_ms`
- `build_time_s`
- `index_size_mb`
- стабильность (`std`, `cv`)

## Запуск 

```bash
cd hw3
make install
make prepare
make ground-truth
make bench-coarse
make bench-fine
make bench
make report-coarse
make report-fine
make report-final
```

Полный прогон:

```bash
make all
```

## Метод
В рамках текущей сессии в соответствии с требованиями сделан бенчмарк на выборке:
- `N=50000` индексируемых векторов,
- `Q=10000` query-векторов,
- `k=100`.
- `seed=42` (зафиксирован),
- число повторов на конфигурацию: `3` (плюс warmup).

Единый размер датасета (`N=50000`, `Q=10000`) используется для `coarse`, `fine` и `final`, чтобы сравнение было корректным и сопоставимым.

## Критерии сравнения (явно)

- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.
- Правило выбора лучшей конфигурации:
  1. Сначала фильтрация по `Recall@100 >= 0.8` (если достижимо),
  2. затем максимум `QPS`,
  3. затем минимум `size_mb`,
  4. затем минимум `build_s`.

Этапы `coarse -> fine -> final` нужны для сужения области параметров и выбора рабочей конфигурации, а не для обязательного монотонного улучшения всех метрик на каждом шаге.

Команды:
- `python scripts/prepare_data.py --corpus-size 50000 --query-size 10000 --scan-limit 50000`
- `python scripts/build_ground_truth.py --top-k 100`
- `make bench-coarse`
- `make bench-fine`
- `make bench` (final)
- `make report-coarse`
- `make report-fine`
- `make report-final`

Артефакты:
- `report/artifacts/benchmark_coarse.csv`
- `report/artifacts/benchmark_fine.csv`
- `report/artifacts/benchmark_final.csv`
- `report/artifacts/summary_coarse.md`
- `report/artifacts/summary_fine.md`
- `report/artifacts/summary_final.md`
- `report/artifacts/tradeoff_coarse.png`
- `report/artifacts/tradeoff_fine.png`
- `report/artifacts/tradeoff_final.png`

## Результаты выполнения бенчмарка

Ниже приведены лучшие конфигурации из каждого этапа (`coarse`, `fine`, `final`).

### Coarse
| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB |
|---|---|---:|---:|---:|---:|---:|
| LSH | `nbits=2048` | 0.6328 | 688 | 1.4627 | 1.65 | 18.21 |
| HNSW | `M=8, efC=100, efS=64` | 0.8303 | 4771 | 0.2096 | 19.02 | 150.33 |
| IVFPQ | `nlist=1024, nprobe=32, m_pq=48, pq_bits=8` | 0.4878 | 5368 | 0.1863 | 16.45 | 6.43 |

### Fine

| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB |
|---|---|---:|---:|---:|---:|---:|
| LSH | `nbits=2048` | 0.6328 | 787 | 1.2751 | 1.21 | 18.21 |
| HNSW | `M=16, efC=100, efS=96` | 0.9474 | 2376 | 0.4220 | 20.12 | 153.37 |
| IVFPQ | `nlist=1024, nprobe=32, m_pq=48, pq_bits=8` | 0.4878 | 5417 | 0.1846 | 16.33 | 6.43 |

### Final

| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB |
|---|---|---:|---:|---:|---:|---:|
| LSH | `nbits=1024` | 0.5056 | 1988 | 0.5034 | 0.61 | 9.10 |
| HNSW | `M=16, efC=100, efS=128` | 0.9657 | 2038 | 0.4907 | 20.16 | 153.37 |
| IVFPQ | `nlist=1024, nprobe=24, m_pq=48, pq_bits=8` | 0.4841 | 6519 | 0.1534 | 18.10 | 6.43 |

## Интерпретация

- Лучший `Recall@100` стабильно у HNSW.
- На этой выборке `IVFPQ` даёт максимальный `QPS`, но уступает HNSW по recall.
- `LSH` показывает средний компромисс между скоростью и качеством.

## Важная оговорка

Результаты выше уже получены на `50000` векторах и `10000` query, что покрывает минимальное требование ТЗ по числу запросов.  
Для расширенного эксперимента (например, `N=200k` или `N=500k`) повторите:
1. `make clean`
2. `make prepare` (или `python scripts/prepare_data.py --corpus-size ... --query-size ...`)
3. `make ground-truth`
4. `make bench-coarse && make bench-fine && make bench`
5. `make report-coarse && make report-fine && make report-final`
