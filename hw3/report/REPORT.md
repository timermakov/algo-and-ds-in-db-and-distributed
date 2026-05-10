# Отчёт по HW3: ANN-бенчмарк

## Пресет
- `final`

## Гипотезы
- HNSW даст максимальный Recall при большем размере индекса.
- IVF+PQ даст лучшую компактность и высокий QPS, но с потерей Recall.
- LSH даст быстрый build и хороший компромисс по скорости/памяти.

## Критерии сравнения (явно)
- Основные метрики: `Recall@100`, `QPS`, `latency_ms`, `build_s`, `size_mb`, `CV(QPS)`.
- Правило выбора: сначала ограничение `Recall@100 >= 0.8` (если достижимо), затем максимум `QPS`, далее минимум `size_mb`, затем минимум `build_s`.

## Результаты

### Агрегированные метрики

| Алгоритм | Конфигурация | Recall@100 | QPS | Latency ms | Build s | Size MB | CV(QPS) |
|---|---|---:|---:|---:|---:|---:|---:|
| flat | `{"type": "IndexFlatL2"}` | 1.0000 | 2514 | 0.3979 | 0.00 | 146.48 | 0.0215 |
| hnsw | `{"ef_construction": 100, "ef_search": 128, "m": 16}` | 0.9657 | 2038 | 0.4907 | 20.16 | 153.37 | 0.0060 |
| ivfpq | `{"m_pq": 48, "nlist": 1024, "nprobe": 24, "pq_bits": 8}` | 0.4841 | 6519 | 0.1534 | 18.10 | 6.43 | 0.0183 |
| lsh | `{"nbits": 1024}` | 0.5056 | 1988 | 0.5034 | 0.61 | 9.10 | 0.0297 |

### Лучшие конфигурации

- **lsh**: `{"nbits": 1024}` | recall=0.5056, qps=1988, size=9.10MB, build=0.61s (fallback: maximize recall then qps)
- **hnsw**: `{"ef_construction": 100, "ef_search": 128, "m": 16}` | recall=0.9657, qps=2038, size=153.37MB, build=20.16s (recall>=0.8, then max qps)
- **ivfpq**: `{"m_pq": 48, "nlist": 1024, "nprobe": 24, "pq_bits": 8}` | recall=0.4841, qps=6519, size=6.43MB, build=18.10s (fallback: maximize recall then qps)


## Интерпретация

- LSH: выбрана `{"nbits": 1024}`; recall=0.5056, qps=1988, size=9.10MB, build=0.61s.
- HNSW: выбрана `{"ef_construction": 100, "ef_search": 128, "m": 16}`; recall=0.9657, qps=2038, size=153.37MB, build=20.16s.
- IVFPQ: выбрана `{"m_pq": 48, "nlist": 1024, "nprobe": 24, "pq_bits": 8}`; recall=0.4841, qps=6519, size=6.43MB, build=18.10s.
- Итоговый победитель определяется по явному критерию: recall-ограничение -> максимум QPS -> минимум размера и времени сборки.

## Пояснение по этапам coarse/fine/final

Этапы `coarse -> fine -> final` нужны для сужения области параметров и выбора рабочей конфигурации, а не для обязательного монотонного улучшения всех метрик на каждом шаге.

## Сравнение

`IndexFlatL2` в этой работе используется как exact baseline верхней границы качества (`Recall=1.0`).  
Его роль — эталон точности, а не лучший практический индекс по всем критериям.

В рабочих задачах индекс выбирают по многокритериальному компромиссу: recall, QPS, latency, размер, время сборки, и ANN-индексы нужны именно для снижения ресурсоёмкости и ускорения ответа при приемлемой полноте поиска.

На `N=50000` HNSW по показателям рядом с `flat` по QPS и размеру индекса - по моему мнению, связано с тем, что граф HNSW хранит дополнительные рёбра/уровни поверх векторов и даёт random-access нагрузку, тогда как `flat` использует плотные векторизованные вычисления. Это нормально для данного N и выбранных параметров `efSearch`.


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

# Trade-offs
![График trade-off](artifacts/tradeoff_final.png)
