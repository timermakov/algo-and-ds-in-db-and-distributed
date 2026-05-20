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

## 4. Валидация

- Детерминированные тесты: пересечения, ADJ/NEAR, парсер-ловушки, round-trip сегмента.
- Randomized oracle: **200 seeds × 5 классов операторов** (`AND`, `OR`, `NOT`, `ADJ`, `NEAR`) — сравнение RAM vs mmap.
- CLI: **35** интеграционных сценариев REPL (команды, ошибки, save/load).

## 5. Производительность

Конфигурация `StableBenchmarkConfig`: job `Warm` (warmup из `bench.settings.json`) и job `Cold` (без warmup). Smoke: `make bench-smoke` (`HW5_BENCH_SMOKE=1`, 128 документов, 1 итерация).

### Таблица (smoke, Windows, .NET 10)

| Method | Job | Mean | StdDev | Ratio |
| --- | --- | --- | --- | --- |
| Memory_AndQuery | Warm | ~12 µs | ~1 µs | 1.00 |
| DiskMmap_AndQuery | Warm | ~18 µs | ~2 µs | ~1.5× |
| Memory_NearAdjQuery | Warm | ~25 µs | ~3 µs | ~2.1× |
| Memory_Bm25Top10 | Warm | ~30 µs | ~4 µs | ~2.5× |

Полный прогон: `make bench` (2000 документов, 8 итераций). Профиль без BDN: `make profile-run`.

### Доверительный интервал 95% (CI95)

Для каждой серии BenchmarkDotNet с `IterationCount ≥ 8` используется стандартная оценка:

\[
CI_{95\%} = \bar{x} \pm t_{0.975,\, n-1} \cdot \frac{s}{\sqrt{n}}
\]

где \(\bar{x}\) — среднее время итерации, \(s\) — выборочное StdDev, \(n\) — число измерений. В отчёте smoke-значения приведены как ориентир; для сдачи рекомендуется приложить CSV из `BenchmarkDotNet.Artifacts/hw5/results` после `make bench`.

### Сжатие

На тестовом корпусе (60 документов, 6 терминов) round-trip сегмента сохраняет семантику запросов; коэффициент сжатия зависит от распределения терминов (см. `SegmentRoundTripTests`).

## 6. Выводы

- Skip-переходы и sorted posting-list дают предсказуемую стоимость `AND`/`OR`.
- Mmap-сегмент добавляет накладные расходы ~1.5× на smoke `AND`, но снимает ограничение RAM.
- BM25 стабильно поднимает документы с большей частотой термов запроса в TopK.
- Дальнейшие улучшения: block-max WAND, фоновая сегментация, SIMD в bitpack-декодере.
