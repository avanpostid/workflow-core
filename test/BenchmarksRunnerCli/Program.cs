using BenchmarkDotNet.Running;
using Benchmarks;

namespace BenchmarksRunnerCli;

public static class Program
{
    private static void Main(string[] args) 
        => BenchmarkSwitcher.FromAssembly(typeof(QueueProvidersBenchmarks).Assembly).Run(args);
}