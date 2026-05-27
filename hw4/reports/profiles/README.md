# Профилирование

```text
make profile-put     # → hw4-put.speedscope.json, hw4-put-topN.txt
make profile-merge
make profile-get
```

Открыть `hw4-put.speedscope.json` в [speedscope.app](https://www.speedscope.app/) (режим Left Heavy).

Параметры цикла: `bench.settings.json` (keySpace), 30 с, 8 потоков — см. `scripts/profile.ps1`.
