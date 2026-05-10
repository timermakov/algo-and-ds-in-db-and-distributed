# Отчёт по HW3: ANN-бенчмарк

## Пресет
- `fine`

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
| hnsw | `{"ef_construction": 100, "ef_search": 160, "m": 16}` | 0.9761 | 1710±14 | 0.5847±0.0049 | 20.25 | 153.37 |
| hnsw | `{"ef_construction": 100, "ef_search": 128, "m": 16}` | 0.9657 | 2029±22 | 0.4929±0.0053 | 21.84 | 153.37 |
| hnsw | `{"ef_construction": 100, "ef_search": 96, "m": 16}` | 0.9474 | 2376±302 | 0.4220±0.0553 | 20.12 | 153.37 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}` | 0.4878 | 5417±152 | 0.1846±0.0052 | 16.33 | 6.43 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 24, "pq_bits": 8}` | 0.4841 | 6634±296 | 0.1508±0.0067 | 16.31 | 6.43 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 16, "pq_bits": 8}` | 0.4751 | 8692±277 | 0.1151±0.0037 | 16.19 | 6.43 |
| lsh | `{"nbits": 2048}` | 0.6328 | 787±105 | 1.2751±0.1766 | 1.21 | 18.21 |
| lsh | `{"nbits": 1024}` | 0.5056 | 2006±26 | 0.4985±0.0066 | 0.39 | 9.10 |

### Лучшие конфигурации

- **lsh**: `{"nbits": 2048}` | recall=0.6328, qps=787±105, size=18.21MB, build=1.21s (fallback: maximize recall then qps)
- **hnsw**: `{"ef_construction": 100, "ef_search": 96, "m": 16}` | recall=0.9474, qps=2376±302, size=153.37MB, build=20.12s (recall>=0.8, then max qps)
- **ivfpq**: `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}` | recall=0.4878, qps=5417±152, size=6.43MB, build=16.33s (fallback: maximize recall then qps)


## Интерпретация

- LSH: выбрана `{"nbits": 2048}`; recall=0.6328, qps=787±105, size=18.21MB, build=1.21s.
- HNSW: выбрана `{"ef_construction": 100, "ef_search": 96, "m": 16}`; recall=0.9474, qps=2376±302, size=153.37MB, build=20.12s.
- IVFPQ: выбрана `{"m_pq": 48, "nlist": 1024, "nprobe": 32, "pq_bits": 8}`; recall=0.4878, qps=5417±152, size=6.43MB, build=16.33s.
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


![График trade-off](artifacts/tradeoff_fine.png)
