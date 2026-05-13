# HW5 - Инвертированный индекс

Каркас первой фазы для лабораторной 5 (`.NET 10`).

## Структура

- `src/Hw5.SearchIndex` - основная библиотека (заглушка на этапе M1)
- `tests/Hw5.SearchIndex.Tests` - проект тестов xUnit
- `benchmarks/Hw5.Benchmarks` - проект запуска BenchmarkDotNet
- `apps/Hw5.SearchCli` - консольная оболочка CLI
- `tools/Hw5.ProfileRunner` - оболочка для профилирования
- `reports/` - каркас отчета и заметки по артефактам
- `diagrams/` - заглушки под PlantUML/C4/BPMN

## Команды (CMD)

Из каталога `hw5/`:

```text
make restore
make build
make test
make bench-build
make bench
make run-cli
make profile-run
```

Прямые аналоги:

```text
dotnet restore Hw5.sln
dotnet build Hw5.sln -c Release
dotnet test Hw5.sln -c Release
```

Полная реализация функциональности (индекс/запросы/сжатие/ранжирование/содержимое отчета) осознанно перенесена на следующие фазы.
