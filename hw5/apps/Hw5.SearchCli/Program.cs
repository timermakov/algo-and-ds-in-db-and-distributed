using Hw5.SearchCli;

try
{
    var options = SearchCliOptions.Parse(args);
    using var repl = new SearchCliRepl(Console.In, Console.Out);
    if (options.LoadWiki)
    {
        repl.LoadWikiCorpus(options.WikiJsonlPath, options.MaxDocuments);
    }

    repl.Run();
}
catch (Exception ex) when (ex is ArgumentException or FileNotFoundException)
{
    Console.Error.WriteLine($"Ошибка запуска CLI: {ex.Message}");
    Environment.Exit(1);
}
