# Профилирование HW5 (локальные артефакты)

## Режимы (`MODE=and|mmap|bm25`)

| Команда | Цикл | Запрос (по умолчанию) |
| --- | --- | --- |
| `make profile-run MODE=and` | `QueryExecutor` in-memory | `alpha AND beta` |
| `make profile-run MODE=mmap` | `QueryExecutor` на mmap-сегменте | `alpha AND beta` |
| `make profile-run MODE=bm25` | `SearchService` BM25 Top10 | `alpha OR beta` |

Трассировка: `make profile-trace MODE=and` (или `mmap`, `bm25`) → отдельные файлы `hw5-query-{mode}.*`.

## Файлы (пример для `MODE=and`)

| Файл | Назначение |
| --- | --- |
| `hw5-query-and.nettrace` | сырой trace |
| `hw5-query-and.speedscope.json` | flame graph → [speedscope.app](https://www.speedscope.app/) |
| `hw5-query-and-topN.txt` | top-25 exclusive/inclusive |

## Hotspot → гипотеза

| Режим | Ожидаемый hotspot | Улучшение |
| --- | --- | --- |
| `and` | `MatchSet.And`, skip-merge | block-max WAND, меньше копий posting |
| `mmap` | bitpack decode, `PagedMmapReader` | zero-copy decode, prefetch сегмента |
| `bm25` | `Ranker.ComputeBm25`, `ToArray`, GC | кэш IDF при Seal, `ArrayPool` |

Повторить: `make profile-trace MODE=bm25` из каталога `hw5/`.
