# HW3: ANN-бенчмарк (FAISS, arXiv embeddings)

Проект сравнивает ANN-индексы на векторных эмбеддингах:
- LSH (`faiss.IndexLSH`)
- HNSW (`faiss.IndexHNSWFlat`)
- IVF+PQ (`faiss.IndexIVFPQ`)
- IVF Flat (`faiss.IndexIVFFlat`)

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
В рамках текущей сессии выполнен бенчмарк на выборке:
- `N=100000` индексируемых векторов,
- `Q=10000` query-векторов,
- `k=100`.
- `seed=42`,
- число повторов:
  - `coarse/fine`: `3` (+warmup),
  - `final`: `5` (+warmup).

Единый размер датасета (`N=100000`, `Q=10000`) используется для `coarse`, `fine` и `final`, чтобы сравнение было корректным и сопоставимым.

## Критерии сравнения

- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.
- Правило выбора лучшей конфигурации:
  1. Сначала фильтрация по `Recall@100 >= 0.8` (если достижимо),
  2. затем максимум `QPS`,
  3. затем минимум `size_mb`,
  4. затем минимум `build_s`.

Этапы `coarse -> fine -> final` нужны для сужения области параметров и выбора рабочей конфигурации, а не для обязательного монотонного улучшения всех метрик на каждом шаге.

Команды:
- `python scripts/prepare_data.py --corpus-size 100000 --query-size 10000 --scan-limit 100000`
- `python scripts/build_ground_truth.py --top-k 100`
- `make bench-coarse`
- `make bench-fine`
- `make bench-final` (или `make bench`)
- `make bench-ivfpq` (отдельный прогон IVF+PQ)
- `make bench-ivf-flat` (отдельный прогон IVF Flat)
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

Ниже приведены актуальные результаты `final`-этапа (`N=100000`) с 95% CI.

| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB |
|---|---|---:|---:|---:|---:|---:|
| LSH | `{"nbits": 2048}` | 0.6195 | 401 | 2.4908 | 1.71 | 30.41 |
| HNSW | `{"ef_construction": 200, "ef_search": 128, "m": 16}` | 0.9652 | 1581 | 0.6326 | 95.56 | 306.73 |
| IVF+PQ | `{"m_pq": 192, "nlist": 512, "nprobe": 512, "pq_bits": 8}` | 0.8617 | 70 | 14.3658 | 32.95 | 21.33 |
| IVF Flat | `{"nlist": 1024, "nprobe": 64}` | 0.9467 | 633 | 1.5796 | 6.89 | 296.74 |

Ключевое условие для защиты выполнено: `IVF+PQ Recall@100 >= 0.85` (получено `0.8617`).

## Интерпретация

- Максимальный `Recall@100` в `final` среди выбранных best-конфигов у HNSW (`0.9652`), при высокой цене памяти и build-time.
- `IVF Flat` даёт очень высокий recall (`0.9467`) и хороший QPS, но индекс большой (`~296.74 MB`).
- `IVF+PQ` после тюнинга даёт целевой recall (`0.8617`) и радикально меньший размер (`21.33 MB`), но заметно ниже QPS.
- `LSH` остаётся самым лёгким по build-time, но уступает по качеству.

## Важная оговорка

Результаты выше получены на `100000` векторах и `10000` query.  
Для повторного эксперимента:
1. `make clean`
2. `make prepare` (или `python scripts/prepare_data.py --corpus-size ... --query-size ... --scan-limit ...`)
3. `make ground-truth`
4. `make bench-coarse && make bench-fine && make bench-final`
5. `make report-coarse && make report-fine && make report-final`
