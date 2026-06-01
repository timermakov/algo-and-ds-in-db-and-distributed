using Hw5.SearchIndex.Corpus;
using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Ranking;
using Hw5.SearchIndex.Searching;
using Hw5.SearchIndex.Storage;

namespace Hw5.SearchCli;

public sealed class SearchCliRepl : IDisposable
{
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly SearchService _searchService = new();
    private readonly InMemoryPositionalIndex _mutableIndex = new();
    private DiskSegmentIndex? _diskIndex;
    private bool _sealed;
    private int _topK = 10;
    private RankingMode _rankingMode = RankingMode.Bm25;

    public SearchCliRepl(TextReader input, TextWriter output)
    {
        _input = input;
        _output = output;
    }

    public void LoadWikiCorpus(string jsonlPath, int maxDocuments)
    {
        if (!WikipediaJsonlReader.IsAvailable(jsonlPath))
        {
            throw new FileNotFoundException(
                $"Wiki-корпус не найден: {jsonlPath}. Выполните: make prepare-corpus");
        }

        var loaded = 0;
        foreach (var record in WikipediaJsonlReader.ReadRecords(jsonlPath, maxDocuments))
        {
            _mutableIndex.AddDocument(new SearchDocument(record.Id, record.Text));
            loaded++;
        }

        if (loaded == 0)
        {
            throw new InvalidOperationException($"В {jsonlPath} нет документов для загрузки.");
        }

        _mutableIndex.Seal();
        _sealed = true;
        _output.WriteLine(
            $"Загружен Wikipedia-корпус: {loaded} документов (limit {maxDocuments}) из {jsonlPath}.");
    }

    public void Run()
    {
        _output.WriteLine("HW5 Search CLI. Введите :help для списка команд.");
        while (true)
        {
            _output.Write("> ");
            var line = _input.ReadLine();
            if (line is null)
            {
                break;
            }

            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith(':'))
            {
                if (!HandleCommand(trimmed))
                {
                    break;
                }

                continue;
            }

            ExecuteQuery(trimmed);
        }
    }

    public void Dispose()
    {
        _diskIndex?.Dispose();
    }

    private bool HandleCommand(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLowerInvariant();
        return command switch
        {
            ":help" => PrintHelp(),
            ":exit" => false,
            ":quit" => false,
            ":build" => SealIndex(),
            ":stats" => PrintStats(),
            ":add" => AddDocument(parts),
            ":save" => SaveSegment(parts),
            ":load" => LoadSegment(parts),
            ":topk" => SetTopK(parts),
            ":mode" => SetMode(parts),
            _ => UnknownCommand(command),
        };
    }

    private bool PrintHelp()
    {
        _output.WriteLine(":add <id> <text>   - добавить документ в RAM-индекс");
        _output.WriteLine(":build             - зафиксировать и отсортировать индекс");
        _output.WriteLine(":save <path>       - сохранить сегмент на диск");
        _output.WriteLine(":load <path>       - загрузить mmap-сегмент");
        _output.WriteLine(":mode bm25|tfidf   - переключить ранжирование");
        _output.WriteLine(":topk <n>          - задать число результатов");
        _output.WriteLine(":stats             - показать состояние");
        _output.WriteLine(":exit              - выйти");
        return true;
    }

    private bool SealIndex()
    {
        _mutableIndex.Seal();
        _sealed = true;
        _output.WriteLine("Индекс зафиксирован.");
        return true;
    }

    private bool PrintStats()
    {
        var active = GetActiveIndex();
        _output.WriteLine($"Документов: {active.DocumentCount}, терминов: {active.Terms.Count}, topK: {_topK}, mode: {_rankingMode}");
        return true;
    }

    private bool AddDocument(string[] parts)
    {
        if (parts.Length < 3)
        {
            _output.WriteLine("Ошибка: используйте :add <id> <text>.");
            return true;
        }

        if (!int.TryParse(parts[1], out var id) || id < 0)
        {
            _output.WriteLine("Ошибка: id должен быть неотрицательным целым.");
            return true;
        }

        var text = parts[2].Trim();
        if (text.Length == 0)
        {
            _output.WriteLine("Ошибка: пустой текст документа.");
            return true;
        }

        try
        {
            _mutableIndex.AddDocument(new SearchDocument(id, text));
            _sealed = false;
            _output.WriteLine($"Добавлен документ {id}.");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Ошибка добавления: {ex.Message}");
        }

        return true;
    }

    private bool SaveSegment(string[] parts)
    {
        if (parts.Length < 2)
        {
            _output.WriteLine("Ошибка: используйте :save <path>.");
            return true;
        }

        if (!_sealed)
        {
            _mutableIndex.Seal();
            _sealed = true;
        }

        try
        {
            SegmentSerializer.Write(parts[1], _mutableIndex);
            _output.WriteLine($"Сегмент сохранен: {parts[1]}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Ошибка сохранения: {ex.Message}");
        }

        return true;
    }

    private bool LoadSegment(string[] parts)
    {
        if (parts.Length < 2)
        {
            _output.WriteLine("Ошибка: используйте :load <path>.");
            return true;
        }

        try
        {
            _diskIndex?.Dispose();
            _diskIndex = new DiskSegmentIndex(parts[1]);
            _output.WriteLine($"Сегмент загружен: {parts[1]}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Ошибка загрузки: {ex.Message}");
        }

        return true;
    }

    private bool SetTopK(string[] parts)
    {
        if (parts.Length < 2 || !int.TryParse(parts[1], out var value) || value <= 0)
        {
            _output.WriteLine("Ошибка: используйте :topk <положительное целое>.");
            return true;
        }

        _topK = value;
        _output.WriteLine($"topK = {_topK}");
        return true;
    }

    private bool SetMode(string[] parts)
    {
        if (parts.Length < 2)
        {
            _output.WriteLine("Ошибка: используйте :mode bm25|tfidf.");
            return true;
        }

        var mode = parts[1].ToLowerInvariant();
        _rankingMode = mode switch
        {
            "bm25" => RankingMode.Bm25,
            "tfidf" => RankingMode.TfIdf,
            _ => _rankingMode,
        };

        if (mode is not ("bm25" or "tfidf"))
        {
            _output.WriteLine("Ошибка: режим должен быть bm25 или tfidf.");
            return true;
        }

        _output.WriteLine($"mode = {_rankingMode}");
        return true;
    }

    private bool UnknownCommand(string command)
    {
        _output.WriteLine($"Неизвестная команда: {command}. Используйте :help.");
        return true;
    }

    private void ExecuteQuery(string query)
    {
        try
        {
            var active = GetActiveIndex();
            if (active.DocumentCount == 0)
            {
                _output.WriteLine("Индекс пуст. Сначала добавьте документы.");
                return;
            }

            var ranked = _searchService.Search(active, query, _topK, _rankingMode);
            if (ranked.Count == 0)
            {
                _output.WriteLine("Нет результатов.");
                return;
            }

            foreach (var item in ranked)
            {
                _output.WriteLine($"{item.DocumentId}\t{item.Score:F4}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Ошибка запроса: {ex.Message}");
        }
    }

    private IPositionalIndexReader GetActiveIndex()
    {
        if (_diskIndex is not null)
        {
            return _diskIndex;
        }

        if (!_sealed)
        {
            _mutableIndex.Seal();
            _sealed = true;
        }

        return _mutableIndex;
    }
}
