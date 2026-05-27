using BenchmarkDotNet.Running;

namespace Hw4.Benchmarks;

internal static class Program
{
    static int Main(string[] args)
    {
        if (TryRunProfileLoop(args, out var exitCode))
            return exitCode;

        var sanitized = BenchCli.FilterJobCliArgs(args);

        if (sanitized.Length != args.Length && Environment.GetEnvironmentVariable("HW4_QUIET_CLI") != "1")
            Console.WriteLine("[Hw4.Benchmarks] Удалены CLI-аргументы --job / -j: их нельзя сочетать с ManualConfig из кода — будет два job (~вдвое дольше, «тишина» между замерами).");

        var cfg = BenchConfigurator.Config(BenchRuntime.Current);
        BenchmarkSwitcher.FromAssembly(typeof(OperationPutBenchmarks).Assembly).Run(sanitized, cfg);
        return 0;
    }

    /// <summary><c>--hw4-profile-loop put --threads 8 --seconds 30</c> — цикл без BDN для dotnet-trace.</summary>
    static bool TryRunProfileLoop(string[] args, out int exitCode)
    {
        exitCode = 0;
        var idx = Array.FindIndex(args, a => string.Equals(a, "--hw4-profile-loop", StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
            return false;

        var tail = args.Skip(idx + 1).ToArray();
        exitCode = ProfileLoopRunner.Run(tail);
        return true;
    }
}

internal static class BenchCli
{
    public static string[] FilterJobCliArgs(string[] raw)
    {
        if (raw.Length == 0)
            return raw;

        var list = new List<string>(raw.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            var a = raw[i];

            if (string.Equals(a, "--job", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "-j", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < raw.Length && raw[i + 1].Length > 0 && raw[i + 1][0] != '-')
                    i++;
                continue;
            }

            if (a.StartsWith("--job=", StringComparison.OrdinalIgnoreCase)
                || a.StartsWith("-j=", StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(a);
        }

        return list.Count == raw.Length ? raw : list.ToArray();
    }
}
