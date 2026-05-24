# Отчёт по HW3: ANN-бенчмарк

## Пресет
- `coarse`

## Гипотезы
- HNSW даст максимальный Recall при большем размере индекса.
- IVF+PQ даст лучшую компактность и высокий QPS, но с потерей Recall.
- LSH даст быстрый build и хороший компромисс по скорости/памяти.

## Критерии сравнения
- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.
- Правило выбора: сначала ограничение `Recall@100 >= 0.8` (если достижимо), затем максимум `QPS`, далее минимум `size_mb`, затем минимум `build_s`.

## Результаты

### Агрегированные метрики (с 95% доверительными интервалами)

| Алгоритм | Конфигурация | Recall@100 (CI) | QPS (CI) | Latency ms (CI) | Build s | Size MB |
|---|---|---:|---:|---:|---:|---:|
| hnsw | `{"ef_construction": 100, "ef_search": 128, "m": 16}` | 0.9657 | 2047±79 | 0.4887±0.0189 | 19.90 | 153.37 |
| hnsw | `{"ef_construction": 100, "ef_search": 128, "m": 8}` | 0.9187 | 2851±10 | 0.3507±0.0013 | 16.41 | 150.33 |
| hnsw | `{"ef_construction": 100, "ef_search": 64, "m": 16}` | 0.9081 | 3523±41 | 0.2838±0.0033 | 20.17 | 153.37 |
| hnsw | `{"ef_construction": 100, "ef_search": 64, "m": 8}` | 0.8303 | 4771±99 | 0.2096±0.0043 | 19.02 | 150.33 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}` | 0.4878 | 5368±68 | 0.1863±0.0024 | 16.45 | 6.43 |
| ivfpq | `{"m_pq": 48, "nlist": 512, "nprobe": 32, "pq_bits": 8}` | 0.4863 | 4743±694 | 0.2116±0.0312 | 14.72 | 4.92 |
| ivfpq | `{"m_pq": 48, "nlist": 512, "nprobe": 8, "pq_bits": 8}` | 0.4595 | 11584±649 | 0.0864±0.0049 | 15.12 | 4.92 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 8, "pq_bits": 8}` | 0.4438 | 12404±373 | 0.0806±0.0024 | 16.37 | 6.43 |
| lsh | `{"nbits": 2048}` | 0.6328 | 688±138 | 1.4627±0.2927 | 1.65 | 18.21 |
| lsh | `{"nbits": 1024}` | 0.5056 | 1666±53 | 0.6004±0.0190 | 0.45 | 9.10 |
| lsh | `{"nbits": 512}` | 0.3543 | 4574±101 | 0.2186±0.0049 | 0.24 | 4.55 |

### Лучшие конфигурации

- **lsh**: `{"nbits": 2048}` | recall=0.6328, qps=688±138, size=18.21MB, build=1.65s (fallback: maximize recall then qps)
- **hnsw**: `{"ef_construction": 100, "ef_search": 64, "m": 8}` | recall=0.8303, qps=4771±99, size=150.33MB, build=19.02s (recall>=0.8, then max qps)
- **ivfpq**: `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}` | recall=0.4878, qps=5368±68, size=6.43MB, build=16.45s (fallback: maximize recall then qps)


## Интерпретация

- LSH: выбрана `{"nbits": 2048}`; recall=0.6328, qps=688±138, size=18.21MB, build=1.65s.
- HNSW: выбрана `{"ef_construction": 100, "ef_search": 64, "m": 8}`; recall=0.8303, qps=4771±99, size=150.33MB, build=19.02s.
- IVFPQ: выбрана `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}`; recall=0.4878, qps=5368±68, size=6.43MB, build=16.45s.
- Итоговый победитель определяется по явному критерию: recall-ограничение -> максимум QPS -> минимум размера и времени сборки.
- Доверительные интервалы (95% CI) рассчитаны для n=3 повторов (t=4.303).

## Пояснение по этапам coarse/fine/final

Этапы `coarse -> fine -> final` нужны для сужения области параметров и выбора рабочей конфигурации, а не для обязательного монотонного улучшения всех метрик на каждом шаге.


## Графики влияния параметров

### HNSW: влияние параметра m
![HNSW m sweep](artifacts/hnsw_m_sweep.png)

### HNSW: влияние параметра efSearch
![HNSW ef sweep](artifacts/hnsw_ef_sweep.png)

### LSH: влияние параметра nbits
![LSH nbits sweep](artifacts/lsh_nbits_sweep.png)

### IVF+PQ: влияние параметра nlist
![IVFPQ nlist sweep](artifacts/ivfpq_nlist_sweep.png)

### IVF+PQ: влияние параметра nprobe
![IVFPQ nprobe sweep](artifacts/ivfpq_nprobe_sweep.png)


![График trade-off](artifacts/tradeoff_coarse.png)
