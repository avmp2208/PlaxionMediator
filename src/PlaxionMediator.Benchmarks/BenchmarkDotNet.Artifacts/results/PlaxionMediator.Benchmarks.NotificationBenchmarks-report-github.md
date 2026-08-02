```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method               | Mean     | Error    | StdDev   | Gen0   | Allocated |
|--------------------- |---------:|---------:|---------:|-------:|----------:|
| Publish_OneHandler   | 42.85 ns | 0.446 ns | 0.417 ns | 0.0024 |      32 B |
| Publish_FiveHandlers | 55.65 ns | 0.895 ns | 0.837 ns | 0.0049 |      64 B |
| Publish_TenHandlers  | 77.42 ns | 0.659 ns | 0.550 ns | 0.0079 |     104 B |
