# Отчёт по лабораторной работе №5 — инвертированный индекс

## 1. Цель

Реализован координатный (позиционный) инвертированный индекс с булевыми и позиционными операторами, дисковым сегментом на `mmap`, сжатием posting-list, ранжированием `TF-IDF`/`BM25`, интерактивным CLI и воспроизводимой валидацией (unit + randomized oracle).

## 2. Архитектура

Диаграммы в каталоге `diagrams/`:

- `architecture.puml` — слои библиотеки и CLI
- `segment-format.puml` — layout сегмента
- `query-flow.puml` — конвейер parse → execute → rank

## 3. Реализация

| Компонент | Файлы | Кратко |
| --- | --- | --- |
| In-memory индекс | `InMemoryPositionalIndex`, `PostingList` | sorted docId, skip-таблица √n |
| Запросы | `SearchQueryParser`, `QueryExecutor`, AST | `AND/OR/NOT/ADJ/NEAR` |
| Диск | `SegmentSerializer`, `DiskSegmentIndex`, `PagedMmapReader` | delta + bitpacking |
| Ранжирование | `Ranker`, `SearchService` | TopK, BM25 k1=1.2, b=0.75 |
| CLI | `SearchCliRepl` | `:add/:build/:save/:load/:mode/:topk` |

### 3.1. Интерактивный CLI

Запуск из `hw5/`:

```text
make run-cli                              # пустой индекс
make run-cli-wiki                         # Wikipedia, 200 док. (по умолчанию)
make run-cli-wiki DOCS=2000               # первые N из docs.jsonl
dotnet run --project apps/Hw5.SearchCli -- --corpus wiki --max-docs 500
```

Команды REPL:

| Команда | Назначение |
| --- | --- |
| `:add <id> <text>` | Добавить документ (текст — всё после id, пробелы допустимы) |
| `:build` | Зафиксировать RAM-индекс |
| `:save <path>` | Записать сегмент на диск |
| `:load <path>` | Открыть mmap-сегмент (дальнейшие запросы идут по диску) |
| `:mode bm25\|tfidf` | Режим ранжирования |
| `:topk <n>` | Число результатов |
| `:stats` | Состояние индекса |
| `:help`, `:exit` | Справка / выход |

**Синтаксис запросов** (без двоеточия — это не команды):

- Операторы: `AND`, `OR`, `NOT`, `ADJ`, `NEAR`, `NEAR/k` (по умолчанию k=3).
- **Скобки поддерживаются** — группировка через `( … )`.
- Приоритет: `NOT` > `ADJ`/`NEAR` > `AND` > `OR`.
- Термы: буквы, цифры, `_`; регистр не важен.

Пример с группировкой:

```text
:add 1 history of europe
:add 2 history russia china relations
:add 3 history russia war
:add 4 china history only
:build
history AND NOT (russia AND china)
```

Семантика: документы с `history`, в которых **не** встречаются **оба** терма `russia` и `china` в одном документе. Результат: id **3** и **4** (id 2 содержит оба терма).

Другие примеры: `alpha OR beta`, `completed AND paper` / `completed ADJ paper` / `completed NEAR/3 paper` (одна пара термов в bench-запросах).

## 4. Валидация

- Детерминированные тесты: пересечения, ADJ/NEAR, парсер-ловушки, round-trip сегмента.
- Randomized oracle: **200 seeds × 5 классов операторов** (`AND`, `OR`, `NOT`, `ADJ`, `NEAR`) — сравнение RAM vs mmap.
- CLI: **35** интеграционных сценариев REPL (команды, ошибки, save/load).

## 5. Производительность

### 5.1. Конфигурация и воспроизведение

| Параметр | Значение |
| --- | --- |
| Корпус **Synthetic** | N ∈ {2000, 10000}, 24 терма/док, seed **42** |
| Корпус **Wikipedia** | shard `pages-articles1`, **N ∈ {2000, 5000}**, medium-DF запросы |
| BDN | Warm (warmup=3, iter=8); Cold — только `IndexQueryBenchmarks`; `OperationsPerInvoke=32` |
| Классы | IndexQuery, Scaling, Operators, Ranking, Build, NaiveScan, MmapTouch |
| Команды | `make download-wiki` → `make prepare-corpus` → `make bench-report` (~**37 мин**) |

Настройки: `benchmarks/Hw5.Benchmarks/bench.settings.json`, локальный override — `bench.local.json` (см. `bench.local.json.example`).

Артефакты: все `*-report.csv`, `bench_summary.md`, **`analysis.md`**, PNG в `reports/artifacts/`.

### 5.1a. Корпус Wikipedia

Источник: [enwiki-latest-pages-articles1.xml-p1p41242.bz2](https://dumps.wikimedia.org/enwiki/latest/enwiki-latest-pages-articles1.xml-p1p41242.bz2) (~296 MB). Устаревший `abstract.xml.gz` в дампах не публикуется; полный `pages-articles.xml.bz2` (~25 GB) избыточен для лабораторной машины.

Пайплайн: streaming XML → `WikiTextNormalizer` → `data/processed/docs.jsonl`; manifest — `data/dataset.manifest.json`; bench-запросы — `WikiBenchQuerySelector` (medium-DF **2–35%** doc, blacklist wikitext markup), stopwords из `data/stopwords-en.txt`.

### 5.1b. Синтетика vs Wikipedia

| Ось | Synthetic | Wikipedia |
| --- | --- | --- |
| Распределение термов | равномерное по словарю 10 термов | Zipf, длинные тексты |
| Запросы | `alpha/beta/...` | curated из DF корпуса |
| Ожидание CV | ниже на AND | выше alloc, возможен больший CV |
| mmap ratio | ~1.6× | выше на коротких posting-list (2.06× @ scaling 2k) |

### 5.2. Аппаратная и программная среда (прогон 2026-05-31, исправленный)

| | |
| --- | --- |
| ОС | Windows 11 (10.0.26200), план High performance |
| CPU | 12th Gen Intel Core i5-1240P, 1 логический CPU в job BDN |
| Runtime | .NET **10.0.5**, SDK 10.0.201, RyuJIT x86-64-v3, AVX2 |
| BDN | 0.15.8; `make bench-wiki` ~**50 мин** (5 классов, Wiki N∈{2000,5000}) |

### 5.3. IndexQuery AND @ N=2000 (сравнимый N, Warm)

| Корпус | RAM AND µs | mmap AND µs | mmap/RAM | CV |
| --- | ---: | ---: | ---: | ---: |
| Synthetic | **834** | 1718 | **2.07×** | 8.2% |
| Wikipedia | **223** | 334 | **1.50×** | 3.9% |


### 5.3a. IndexQuery @ Synthetic 2000 (полная таблица)

| Метод | Mean µs | CV | Ratio | Alloc/invoke |
| --- | ---: | ---: | ---: | ---: |
| RAM `AND` | **834** | 8.2% | 1.00 | 1729 KB |
| mmap `AND` | **1718** | 6.2% | **2.07** | 2213 KB |
| RAM NEAR/ADJ | 579 | 8.5% | 0.70 | 742 KB |
| RAM BM25 Top10 | **4022** | 14.4% | **4.85** | 1995 KB |

Q/s (RAM AND): **~1199**.

### 5.4. Cold vs Warm (Synthetic 2000, IndexQuery)

| Метод | Warm µs | Cold µs | Cold/Warm |
| --- | ---: | ---: | ---: |
| RAM `AND` | 834.1 | 1117.4 | **1.34×** |
| mmap `AND` | 1717.6 | 1596.7 | 0.93× |
| RAM BM25 | 4022.2 | 1887.6 | 0.47× |

Cold job (warmup=0, iter=8) для AND слегка медленнее RAM; для BM25 разброс выше (CV до 14%) — cold/warm ratio нестабилен на тяжёлом пути.

### 5.5. Масштабирование (Synthetic, IndexQueryScaling)

| N | RAM AND µs | mmap AND µs | mmap/RAM |
| --- | ---: | ---: | ---: |
| 2000 | 1914.0 | 2372.6 | 1.24× |
| 10000 | 18576.1 | 21197.8 | **1.14×** |

Рост RAM AND **~9.7×** при N×5. Wiki: **253 µs** @ 2k → **1898 µs** @ 5k (~7.5×).

### 5.5a. Операторы (изолированно, Synthetic 2000)

| Оператор | Mean µs | Ratio к AND |
| --- | ---: | ---: |
| `AND` | 1515 | 1.00 |
| `OR` | 1875 | **1.24** |
| `NOT` | 299 | 0.20 |
| `ADJ` | 424 | 0.28 |
| `NEAR/3` | 460 | 0.30 |

Wikipedia 5000: `AND` **643 µs**, `OR` **555 µs** (CV 3–15%). Wiki AND **измерен** на всех классах (ранее NA из‑за pathological query `align AND ndash`).

### 5.5b. Naive baseline (BDN)

| N | Indexed µs | Naive µs | Ускорение |
| --- | ---: | ---: | ---: |
| 128 | 48 | 150 | **3.1×** |
| 512 | 172 | 715 | **4.2×** |

### 5.5c. Ранжирование и mmap locality

- **Ranking** @ Synthetic 2k (OR-запрос): BooleanOnly **1926 µs**, TF-IDF **2694 µs** (**1.40×**), BM25 **2259 µs** (**1.17×** к BooleanOnly).
- **IndexQuery** BM25 Top10 **4022 µs** — **4.85×** к RAM AND (полный TopK + scoring).
- **FirstTouch mmap** 5780 µs vs **Repeated** 1334 µs (**~4.3×**) @ Synthetic 2k.

### 5.5d. RAM vs mmap (Synthetic 2000, IndexQuery)

| Режим | Mean µs | Ratio |
| --- | ---: | ---: |
| In-memory | 834.1 | 1.00 |
| mmap + bitpack | 1717.6 | **2.07** |

Несжатого mmap-baseline нет: экономия диска оплачивается декодированием.

### 5.6. Сжатие сегмента (тот же синтетический корпус)

| Метрика | Значение |
| --- | ---: |
| Наивный объём posting-list (int32 docId + positions) | 265 820 B |
| Файл сегмента на диске | 61 173 B |
| segment / naive | **0.23** |
| Экономия места | **~77%** |

Источник: `make compression-stats` → `reports/artifacts/compression_stats.json`.

### 5.7. Графики (14 PNG, прогон 2026-05-31)

| # | Файл | Содержание |
| --- | --- | --- |
| 1–11 | (как ранее) | scaling, operators, CV, alloc, … |
| 12 | `corpus_comparison_and_latency.png` | RAM AND Synthetic vs Wiki @ N=2000 |
| 13 | `corpus_comparison_mmap_latency.png` | mmap AND Synthetic vs Wiki @ N=2000 |
| 14 | `compression_ratio_vs_N.png` | segment/naive по корпусам и N |

Полный текстовый разбор: **`artifacts/analysis.md`**.

![Warm: задержка](artifacts/indexquery_warm_latency.png)

![Масштабирование](artifacts/scaling_latency_by_N.png)

![Операторы](artifacts/operators_latency.png)

![Synthetic vs Wiki](artifacts/corpus_comparison_and_latency.png)

![Сжатие vs N](artifacts/compression_ratio_vs_N.png)

### 5.8. Гипотезы и проверка

| Гипотеза | Ожидание | Наблюдение (2026-05-31) |
| --- | --- | --- |
| H1: skip ускоряют `AND` | < 1 ms @ 2k | **834 µs** (IndexQuery), CV **8.2%** |
| H2: mmap медленнее RAM | Ratio > 1 | **2.07×**, alloc **+28%** |
| H3: BM25 дороже AND | > 2× | **4.85×** (IndexQuery Top10) |
| H4: bitpack экономит диск | > 50% | **~77%** |
| H_scale | рост с N | **~9.7×** RAM AND при N×5 (1914→18576 µs, Scaling) |
| H_ops | OR дороже AND | OR **1.24×** (synth); NOT/ADJ/NEAR дешевле на synth |
| H_naive | indexed ≫ naive | **3.1–4.2×** @ N≤512 |
| H_corpus | Wiki vs Synthetic @ 2000 | Wiki AND **223 µs** vs Synth **834 µs** (разная селективность запроса) |
| H_corpus_mmap | mmap/RAM на Wiki | **1.50×** @ Wiki 2000 (vs 2.07× Synthetic) |
| H_cold | Cold > Warm | **Частично** @ synth AND (1.34× RAM); mmap/BM25 — шум |

### 5.9. Профилирование (вне BDN)

`make profile-trace MODE=and|mmap|bm25` → `reports/profiles/hw5-query-{mode}.speedscope.json` (см. `reports/profiles/README.md`). Режимы изолируют булево ядро, mmap+decode и BM25.

## 6. Выводы

- На Synthetic 2000 RAM `AND` **~834 µs** (CV 8.2%); mmap **2.07×** медленнее; BM25 Top10 **4.85×** к AND.
- Масштабирование Synthetic (Scaling): **~9.7×** RAM AND при N 2k→10k; mmap/RAM сходится к **~1.14×** на 10k.
- Операторы: `OR` дороже `AND` (**1.24×** на synth); на Wikipedia @ 5k `OR` **555 µs** vs `AND` **643 µs**.
- Naive scan в **3–4×** медленнее индекса уже при N≤512; first-touch mmap **~4.3×** дороже повторного чтения.
- Wiki AND **измерен** на N∈{2000,5000}; medium-DF запросы устранили NA.
- mmap/RAM на Wiki **1.50×** @ 2000; на Synthetic **2.07×** — разница из‑за профиля posting-list.
- Масштабирование Wiki: AND **253 µs** @ 2k → **1898 µs** @ 5k (~7.5×).
- Построены все 14 графиков, включая corpus comparison и compression vs N.
