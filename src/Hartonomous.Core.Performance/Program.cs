using BenchmarkDotNet.Running;

namespace Hartonomous.Core.Performance;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
