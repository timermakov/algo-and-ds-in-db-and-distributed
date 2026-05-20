# Профилирование HW5 (локальные артефакты)

Сгенерировано `make profile-trace` (ProfileRunner: 2000 док, 3000 итераций, запрос `alpha AND beta OR gamma NEAR/2 delta`).

## Файлы

| Файл | Назначение |
| --- | --- |
| `hw5-query-loop.nettrace` | сырой trace (`dotnet trace`) |
| `hw5-query-loop.speedscope.json` | flame graph для [speedscope.app](https://www.speedscope.app/) |
| `hw5-query-loop-topN.txt` | top-25 exclusive/inclusive |

## Top hotspots (exclusive, ориентир)

1. **GC / `Thread.PollGC`** (~26%) — давление аллокаций в горячем цикле.
2. **`List<int>.ToArray` / resize** (~23% incl.) — копии при материализации posting-list.
3. **`Ranker.ComputeBm25`** (~7% excl., ~14% incl.) — скоринг TopK.
4. **`BinarySearch` / сортировки** — работа с отсортированными docId.
5. **`MatchSet.And` / `Or` / `NearUnordered`** — булево ядро запроса.

## Гипотезы улучшений

- Убрать лишние `ToArray()` в `MatchSet` / ранжировании → `ArrayPool<int>` или span поверх mmap-буфера.
- Разделить «лёгкий» булевый путь и BM25: не строить полный `MatchSet`, если нужен только TopK по одному терму.
- Предвычислить IDF/длины документов при `Seal()` — меньше работы в `ComputeBm25` на запрос.
- Декодер bitpack: batch-read без промежуточных `List<int>`.

Повторить сбор: `make profile-trace` из `hw5/`.
