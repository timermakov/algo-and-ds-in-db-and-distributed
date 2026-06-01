# Bench queries

## `wiki-bench-suite.json`

Machine-readable suite: **9 term pairs** (high / mid / low DF × 3 pairs).

Per pair `(termA, termB)` the same terms for AND, OR, ADJ, NEAR; NOT uses `termA AND NOT exclude`.

`OperatorBenchmarks` rotates through all queries of each operator per invoke.

## `wiki-bench-queries.txt`

Human-readable listing with `AND\t`, `OR\t`, … prefixes.

## Regenerate

```text
make prepare-corpus-queries
```

## Corpus layout

See `../processed/README.md` — one file per article under `articles/<pageId>.json`.
