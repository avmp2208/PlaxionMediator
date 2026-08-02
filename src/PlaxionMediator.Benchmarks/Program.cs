using BenchmarkDotNet.Running;

// BenchmarkSwitcher natively supports "--list flat"/"--list tree"/"--help" for CI/build
// verification without running the (long) benchmarks themselves.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public partial class Program;
